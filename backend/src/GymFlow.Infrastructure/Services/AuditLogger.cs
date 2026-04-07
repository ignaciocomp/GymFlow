using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;

namespace GymFlow.Infrastructure.Services;

public class AuditLogger : IAuditLogger
{
    private readonly IAuditLogRepository _repository;

    public AuditLogger(IAuditLogRepository repository)
    {
        _repository = repository;
    }

    public async Task LogAsync(
        Guid usuarioId,
        string usuarioNombre,
        TipoAccionAuditoria tipoAccion,
        string entidadAfectada,
        Guid? entidadId,
        string descripcion,
        string? detallesCambios = null)
    {
        var registro = new RegistroAuditoria(
            usuarioId,
            usuarioNombre,
            tipoAccion,
            entidadAfectada,
            entidadId,
            descripcion,
            detallesCambios);

        await _repository.AddAsync(registro);
        await _repository.SaveChangesAsync();
    }
}
