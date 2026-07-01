using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;
using GymFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Infrastructure.Repositories;

public class PagoRepository : IPagoRepository
{
    private readonly GymFlowDbContext _context;

    public PagoRepository(GymFlowDbContext context) => _context = context;

    public async Task<Pago?> GetByIdAsync(Guid id)
    {
        return await _context.Pagos
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    // El external_reference de la preferencia MP es el Pago.Id, así que es un GetById.
    public Task<Pago?> GetByExternalReferenceAsync(Guid pagoId) => GetByIdAsync(pagoId);

    public async Task<IEnumerable<Pago>> GetByCuotaIdAsync(Guid cuotaId)
    {
        return await _context.Pagos
            .Where(p => p.CuotaId == cuotaId)
            .OrderByDescending(p => p.FechaCreacion)
            .ToListAsync();
    }

    public async Task<IEnumerable<Pago>> GetBySocioIdAsync(Guid socioId)
    {
        return await _context.Pagos
            .Include(p => p.Cuota)
            .Where(p => p.SocioId == socioId)
            .OrderByDescending(p => p.FechaCreacion)
            .ToListAsync();
    }

    public async Task AddAsync(Pago pago) => await _context.Pagos.AddAsync(pago);

    public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
}
