using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.Interfaces;

public interface IAuditLogRepository
{
    Task<IEnumerable<RegistroAuditoria>> SearchAsync(
        DateTime? desde,
        DateTime? hasta,
        TipoAccionAuditoria? tipoAccion,
        Guid? entidadId);
    Task AddAsync(RegistroAuditoria registro);
    Task SaveChangesAsync();
}
