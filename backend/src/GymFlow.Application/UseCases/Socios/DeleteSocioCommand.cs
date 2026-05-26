using GymFlow.Application.Interfaces;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.Socios;

public class DeleteSocioCommand
{
    private readonly ISocioRepository _repository;
    private readonly IAuditLogger _auditLogger;

    public DeleteSocioCommand(ISocioRepository repository, IAuditLogger auditLogger)
    {
        _repository = repository;
        _auditLogger = auditLogger;
    }

    public async Task ExecuteAsync(Guid socioId, string? motivo, Guid usuarioId, string usuarioNombre)
    {
        var socio = await _repository.GetByIdAsync(socioId);

        if (socio == null)
            throw new KeyNotFoundException($"Socio with ID {socioId} not found.");

        if (!socio.EstaActivo)
            throw new InvalidOperationException("El socio ya está dado de baja.");

        // Soft delete (RN-02: baja lógica, no se eliminan registros)
        socio.DarDeBaja(motivo);

        await _repository.SaveChangesAsync();

        await _auditLogger.LogAsync(
            usuarioId,
            usuarioNombre,
            TipoAccionAuditoria.Baja,
            "Socio",
            socioId,
            $"Se dio de baja al socio {socio.Nombre} {socio.Apellido}. Motivo: {motivo ?? "No especificado"}");
    }
}
