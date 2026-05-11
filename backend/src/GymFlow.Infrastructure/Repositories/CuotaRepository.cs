using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using GymFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Infrastructure.Repositories;

public class CuotaRepository : ICuotaRepository
{
    private readonly GymFlowDbContext _context;

    public CuotaRepository(GymFlowDbContext context) => _context = context;

    public async Task<Cuota?> GetByIdAsync(Guid id)
    {
        return await _context.Cuotas
            .Include(c => c.Socio)
            .Include(c => c.Unidad)
            .Include(c => c.Plan)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<IEnumerable<Cuota>> GetBySocioIdAsync(Guid socioId)
    {
        return await _context.Cuotas
            .Include(c => c.Unidad)
            .Where(c => c.SocioId == socioId && c.Estado != EstadoCuota.Anulada)
            .OrderByDescending(c => c.FechaVencimiento)
            .ToListAsync();
    }

    public async Task<IEnumerable<Cuota>> SearchAsync(Guid socioId, EstadoCuota? estado, int? mes, int? anio, Guid? unidadId, bool incluirAnuladas = false)
    {
        var query = _context.Cuotas
            .Include(c => c.Socio)
            .Include(c => c.Unidad)
            .Where(c => c.SocioId == socioId)
            .AsQueryable();

        if (!incluirAnuladas)
            query = query.Where(c => c.Estado != EstadoCuota.Anulada);

        if (estado.HasValue)
            query = query.Where(c => c.Estado == estado.Value);

        if (mes.HasValue)
            query = query.Where(c => c.FechaVencimiento.Month == mes.Value);

        if (anio.HasValue)
            query = query.Where(c => c.FechaVencimiento.Year == anio.Value);

        if (unidadId.HasValue)
            query = query.Where(c => c.UnidadId == unidadId.Value);

        return await query
            .OrderByDescending(c => c.FechaVencimiento)
            .ToListAsync();
    }

    public async Task<Cuota?> GetUltimaCuotaAsync(Guid socioId, Guid unidadId)
    {
        return await _context.Cuotas
            .Where(c => c.SocioId == socioId && c.UnidadId == unidadId && c.Estado != EstadoCuota.Anulada)
            .OrderByDescending(c => c.FechaVencimiento)
            .FirstOrDefaultAsync();
    }

    public async Task AddAsync(Cuota cuota) => await _context.Cuotas.AddAsync(cuota);

    public async Task DeletePendientesBySocioAsync(Guid socioId)
    {
        var pendientes = await _context.Cuotas
            .Where(c => c.SocioId == socioId && c.Estado == EstadoCuota.Pendiente)
            .ToListAsync();
        _context.Cuotas.RemoveRange(pendientes);
    }

    public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
}
