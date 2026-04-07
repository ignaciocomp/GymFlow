using GymFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Infrastructure.Persistence;

public class GymFlowDbContext : DbContext
{
    public GymFlowDbContext(DbContextOptions<GymFlowDbContext> options) : base(options) { }

    public DbSet<Unidad> Unidades => Set<Unidad>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Socio> Socios => Set<Socio>();
    public DbSet<Plan> Planes => Set<Plan>();
    public DbSet<UsuarioUnidad> UsuarioUnidades => Set<UsuarioUnidad>();
    public DbSet<RegistroAuditoria> RegistrosAuditoria => Set<RegistroAuditoria>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GymFlowDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
