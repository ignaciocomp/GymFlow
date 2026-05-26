using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using GymFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Infrastructure.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly GymFlowDbContext _context;

    public AuditLogRepository(GymFlowDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<RegistroAuditoria>> SearchAsync(
        DateTime? desde,
        DateTime? hasta,
        TipoAccionAuditoria? tipoAccion,
        Guid? entidadId)
    {
        var query = _context.RegistrosAuditoria.AsQueryable();

        if (desde.HasValue)
            query = query.Where(r => r.FechaHora >= desde.Value);

        if (hasta.HasValue)
            query = query.Where(r => r.FechaHora <= hasta.Value);

        if (tipoAccion.HasValue)
            query = query.Where(r => r.TipoAccion == tipoAccion.Value);

        if (entidadId.HasValue)
            query = query.Where(r => r.EntidadId == entidadId.Value);

        return await query.OrderByDescending(r => r.FechaHora).ToListAsync();
    }

    public async Task AddAsync(RegistroAuditoria registro)
    {
        await _context.RegistrosAuditoria.AddAsync(registro);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
