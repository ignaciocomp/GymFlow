using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Inscripciones;

public class InscribirSocioCommand
{
    private readonly IInscripcionClaseRepository _inscripcionRepo;
    private readonly IClaseRepository _claseRepo;
    private readonly ICuotaRepository _cuotaRepo;
    private readonly ISocioRepository _socioRepo;
    private readonly IEmailService _emailService;
    private readonly IAuditLogger _auditLogger;

    public InscribirSocioCommand(
        IInscripcionClaseRepository inscripcionRepo,
        IClaseRepository claseRepo,
        ICuotaRepository cuotaRepo,
        ISocioRepository socioRepo,
        IEmailService emailService,
        IAuditLogger auditLogger)
    {
        _inscripcionRepo = inscripcionRepo;
        _claseRepo = claseRepo;
        _cuotaRepo = cuotaRepo;
        _socioRepo = socioRepo;
        _emailService = emailService;
        _auditLogger = auditLogger;
    }

    public async Task<InscripcionClaseDto> ExecuteAsync(Guid socioId, Guid claseId, Guid usuarioId, string usuarioNombre)
    {
        var clase = await _claseRepo.GetByIdAsync(claseId)
            ?? throw new KeyNotFoundException("La clase no existe.");

        if (!clase.EstaActivo)
            throw new InvalidOperationException("No se puede inscribir a una clase cancelada.");

        if (await _cuotaRepo.TieneCuotasVencidasEnUnidadAsync(socioId, clase.UnidadId))
            throw new InvalidOperationException("Tu cuota está vencida en esta sede. Regularizá tu pago para inscribirte.");

        var existente = await _inscripcionRepo.GetActivaBySocioYClaseAsync(socioId, claseId);
        if (existente != null)
            throw new InvalidOperationException("Ya estás inscripto en esta clase.");

        var ocupados = await _inscripcionRepo.GetInscripcionesActivasCountAsync(claseId);
        var esListaEspera = ocupados >= clase.CapacidadMaxima;

        var inscripcion = new InscripcionClase(claseId, socioId, esListaEspera);
        await _inscripcionRepo.AddAsync(inscripcion);
        await _inscripcionRepo.SaveChangesAsync();

        var socio = await _socioRepo.GetByIdAsync(socioId);

        if (!esListaEspera && socio != null)
        {
            var (asunto, cuerpo) = InscripcionEmailTemplates.Confirmacion(socio, clase);
            // Best-effort: no se chequea el resultado del envío.
            await _emailService.EnviarAsync(socio.Correo, asunto, cuerpo);
        }

        var descripcion = esListaEspera
            ? $"Socio agregado a la lista de espera de la clase '{clase.Nombre}'."
            : $"Socio inscripto en la clase '{clase.Nombre}'.";

        await _auditLogger.LogAsync(usuarioId, usuarioNombre, TipoAccionAuditoria.Creacion,
            "Inscripcion", inscripcion.Id, descripcion);

        var posicion = esListaEspera
            ? await _inscripcionRepo.GetPosicionEnListaEsperaAsync(inscripcion.Id)
            : (int?)null;

        return InscripcionMapper.ToDto(inscripcion, clase, ocupados + (esListaEspera ? 0 : 1), posicion);
    }
}
