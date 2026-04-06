using GymFlow.Domain.Enums;

namespace GymFlow.Application.Interfaces;

public interface IAuditLogger
{
    Task LogAsync(
        Guid usuarioId,
        string usuarioNombre,
        TipoAccionAuditoria tipoAccion,
        string entidadAfectada,
        Guid? entidadId,
        string descripcion,
        string? detallesCambios = null);
}
