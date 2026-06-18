using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;
using GymFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Infrastructure.Repositories;

public class SocioRepository : ISocioRepository
{
    private readonly GymFlowDbContext _context;

    public SocioRepository(GymFlowDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Socio>> GetAllAsync(bool includeInactive = false)
    {
        var query = _context.Socios
            .Include(s => s.UnidadesAsignadas)
                .ThenInclude(uu => uu.Unidad)
            .Include(s => s.UnidadesAsignadas)
                .ThenInclude(uu => uu.Plan)
            .AsQueryable();

        if (!includeInactive)
            query = query.Where(s => s.EstaActivo);

        return await query.OrderBy(s => s.Apellido).ThenBy(s => s.Nombre).ToListAsync();
    }

    public async Task<Socio?> GetByIdAsync(Guid id)
    {
        return await _context.Socios
            .Include(s => s.UnidadesAsignadas)
                .ThenInclude(uu => uu.Unidad)
            .Include(s => s.UnidadesAsignadas)
                .ThenInclude(uu => uu.Plan)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<Socio?> GetByDocumentoIdentidadAsync(string documentoIdentidad)
    {
        return await _context.Socios
            .Include(s => s.UnidadesAsignadas)
                .ThenInclude(uu => uu.Unidad)
            .Include(s => s.UnidadesAsignadas)
                .ThenInclude(uu => uu.Plan)
            .FirstOrDefaultAsync(s => s.DocumentoIdentidad == documentoIdentidad);
    }

    public async Task<Socio?> GetByCorreoAsync(string correo)
    {
        return await _context.Socios
            .Include(s => s.UnidadesAsignadas)
                .ThenInclude(uu => uu.Unidad)
            .Include(s => s.UnidadesAsignadas)
                .ThenInclude(uu => uu.Plan)
            .FirstOrDefaultAsync(s => s.Correo.ToLower() == correo.ToLower());
    }

    public async Task<bool> ExisteCorreoAsync(string correo)
    {
        return await _context.Usuarios.AnyAsync(u => u.Correo.ToLower() == correo.ToLower());
    }

    public async Task<bool> ExisteCedulaAsync(string cedula)
    {
        return await _context.Socios.AnyAsync(s => s.DocumentoIdentidad == cedula);
    }

    public async Task<IEnumerable<Socio>> SearchAsync(
        string? nombre, Guid? unidadId, Guid? planId, bool? estaActivo, IReadOnlyCollection<Guid>? unidadesPermitidas = null)
    {
        var query = _context.Socios
            .Include(s => s.UnidadesAsignadas)
                .ThenInclude(uu => uu.Unidad)
            .Include(s => s.UnidadesAsignadas)
                .ThenInclude(uu => uu.Plan)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(nombre))
        {
            var term = nombre.ToLower();
            query = query.Where(s =>
                s.Nombre.ToLower().Contains(term) ||
                s.Apellido.ToLower().Contains(term) ||
                s.Correo.ToLower().Contains(term));
        }

        if (unidadesPermitidas is not null)
            query = query.Where(s => s.UnidadesAsignadas.Any(uu => unidadesPermitidas.Contains(uu.UnidadId)));

        if (unidadId.HasValue)
            query = query.Where(s => s.UnidadesAsignadas.Any(uu => uu.UnidadId == unidadId.Value));

        if (planId.HasValue)
            query = query.Where(s => s.UnidadesAsignadas.Any(uu => uu.PlanId == planId.Value));

        if (estaActivo.HasValue)
            query = query.Where(s => s.EstaActivo == estaActivo.Value);

        return await query.OrderBy(s => s.Apellido).ThenBy(s => s.Nombre).ToListAsync();
    }

    public async Task<IEnumerable<Socio>> GetActivosByUnidadAsync(Guid unidadId)
    {
        return await _context.Socios
            .Include(s => s.UnidadesAsignadas)
                .ThenInclude(uu => uu.Unidad)
            .Include(s => s.UnidadesAsignadas)
                .ThenInclude(uu => uu.Plan)
            .Where(s => s.EstaActivo && s.UnidadesAsignadas.Any(uu => uu.UnidadId == unidadId))
            .OrderBy(s => s.Apellido).ThenBy(s => s.Nombre)
            .ToListAsync();
    }

    public async Task AddAsync(Socio socio)
    {
        await _context.Socios.AddAsync(socio);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
