using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Socios;

public class CreateSocioCommand
{
    private readonly ISocioRepository _socioRepository;
    private readonly IUnidadRepository _unidadRepository;
    private readonly IPlanRepository _planRepository;
    private readonly IAuditLogger _auditLogger;

    public CreateSocioCommand(
        ISocioRepository socioRepository,
        IUnidadRepository unidadRepository,
        IPlanRepository planRepository,
        IAuditLogger auditLogger)
    {
        _socioRepository = socioRepository;
        _unidadRepository = unidadRepository;
        _planRepository = planRepository;
        _auditLogger = auditLogger;
    }

    public async Task<SocioDto> ExecuteAsync(CreateSocioRequest request, Guid usuarioId, string usuarioNombre)
    {
        if (request.DocumentoIdentidad != null && await _socioRepository.ExisteCedulaAsync(request.DocumentoIdentidad))
            throw new InvalidOperationException("El número de cédula ya está registrado");

        if (await _socioRepository.ExisteCorreoAsync(request.Correo))
            throw new InvalidOperationException("El correo ingresado ya está registrado.");

        var unidades = request.Unidades?.ToList() ?? [];
        if (unidades.Count == 0)
            throw new ArgumentException("Debe asignar al menos una unidad.");

        foreach (var asignacion in unidades)
        {
            var unidad = await _unidadRepository.GetByIdAsync(asignacion.UnidadId);
            if (unidad == null)
                throw new ArgumentException($"La unidad con ID {asignacion.UnidadId} no existe.");

            if (asignacion.PlanId.HasValue)
            {
                var plan = await _planRepository.GetByIdAsync(asignacion.PlanId.Value);
                if (plan == null || !plan.EstaActivo)
                    throw new ArgumentException("El plan seleccionado no existe o no está activo.");
                if (plan.UnidadId != asignacion.UnidadId)
                    throw new ArgumentException($"El plan seleccionado no pertenece a la unidad {unidad.Nombre}.");
            }
        }

        var socio = new Socio(
            nombre: request.Nombre,
            apellido: request.Apellido,
            correo: request.Correo,
            passwordHash: "PENDING_OAUTH",
            fechaAlta: DateTime.UtcNow,
            consentimientoInformado: request.ConsentimientoInformado,
            tipoDocumento: request.TipoDocumento,
            telefono: request.Telefono,
            documentoIdentidad: request.DocumentoIdentidad,
            fechaNacimiento: request.FechaNacimiento.HasValue
                ? DateTime.SpecifyKind(request.FechaNacimiento.Value, DateTimeKind.Utc)
                : null);

        foreach (var asignacion in unidades)
        {
            socio.UnidadesAsignadas.Add(new UsuarioUnidad(socio.Id, asignacion.UnidadId, asignacion.PlanId));
        }

        await _socioRepository.AddAsync(socio);
        await _socioRepository.SaveChangesAsync();

        await _auditLogger.LogAsync(
            usuarioId, usuarioNombre,
            TipoAccionAuditoria.Creacion, "Socio", socio.Id,
            $"Se registró al socio {request.Nombre} {request.Apellido}");

        var saved = await _socioRepository.GetByIdAsync(socio.Id);
        return MapToDto(saved!);
    }

    internal static SocioDto MapToDto(Socio socio)
    {
        return new SocioDto(
            Id: socio.Id,
            Nombre: socio.Nombre,
            Apellido: socio.Apellido,
            Correo: socio.Correo,
            Telefono: socio.Telefono,
            TipoDocumento: socio.TipoDocumento,
            DocumentoIdentidad: socio.DocumentoIdentidad,
            FechaNacimiento: socio.FechaNacimiento,
            FechaAlta: socio.FechaAlta,
            EstaActivo: socio.EstaActivo,
            Unidades: socio.UnidadesAsignadas
                .Select(uu => new UnidadDto(
                    uu.UnidadId,
                    uu.Unidad?.Nombre ?? "",
                    uu.Unidad?.Direccion ?? "",
                    uu.PlanId,
                    uu.Plan?.Nombre))
                .ToList());
    }
}
