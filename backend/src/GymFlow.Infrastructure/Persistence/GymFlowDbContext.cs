using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
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
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Permiso> Permisos => Set<Permiso>();
    public DbSet<RolPermiso> RolPermisos => Set<RolPermiso>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GymFlowDbContext).Assembly);
        base.OnModelCreating(modelBuilder);

        // Seed Roles del sistema
        modelBuilder.Entity<Rol>().HasData(
            new { Id = RolSeed.AdminRolId, Nombre = "Administrador", EsSistema = true, FechaCreacion = RolSeed.SeedTimestamp },
            new { Id = RolSeed.SocioRolId, Nombre = "Socio", EsSistema = true, FechaCreacion = RolSeed.SeedTimestamp }
        );

        // Seed Permisos: producto cartesiano de Modulo × Operacion
        var permisoSeeds = new List<object>();
        var permisoIds = new Dictionary<(Modulo, Operacion), Guid>();

        foreach (var m in Enum.GetValues<Modulo>())
        {
            foreach (var o in Enum.GetValues<Operacion>())
            {
                var id = DeterministicGuid($"{m}-{o}");
                permisoIds[(m, o)] = id;
                permisoSeeds.Add(new { Id = id, Modulo = m, Operacion = o });
            }
        }
        modelBuilder.Entity<Permiso>().HasData(permisoSeeds);

        // Seed RolPermisos: Admin tiene todos
        var rolPermisoSeeds = permisoIds.Values
            .Select(pid => (object)new { RolId = RolSeed.AdminRolId, PermisoId = pid })
            .ToList();
        modelBuilder.Entity<RolPermiso>().HasData(rolPermisoSeeds);
    }

    private static Guid DeterministicGuid(string input)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
        return new Guid(hash);
    }
}

public static class RolSeed
{
    public static readonly Guid AdminRolId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid SocioRolId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly DateTime SeedTimestamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
}
