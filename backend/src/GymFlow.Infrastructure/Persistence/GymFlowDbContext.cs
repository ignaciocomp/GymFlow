using GymFlow.Domain.Constants;
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
    public DbSet<Empleado> Empleados => Set<Empleado>();
    public DbSet<Plan> Planes => Set<Plan>();
    public DbSet<UsuarioUnidad> UsuarioUnidades => Set<UsuarioUnidad>();
    public DbSet<RegistroAuditoria> RegistrosAuditoria => Set<RegistroAuditoria>();
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Permiso> Permisos => Set<Permiso>();
    public DbSet<RolPermiso> RolPermisos => Set<RolPermiso>();
    public DbSet<Cuota> Cuotas => Set<Cuota>();
    public DbSet<RecordatorioCuota> RecordatoriosCuota => Set<RecordatorioCuota>();
    public DbSet<Clase> Clases => Set<Clase>();
    public DbSet<InscripcionClase> InscripcionesClase => Set<InscripcionClase>();
    public DbSet<HorarioClase> HorariosClase => Set<HorarioClase>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GymFlowDbContext).Assembly);
        base.OnModelCreating(modelBuilder);

        // Seed Roles del sistema
        modelBuilder.Entity<Rol>().HasData(
            new { Id = RolSeed.AdminRolId, Nombre = "Administrador", EsSistema = true, FechaCreacion = RolSeed.SeedTimestamp },
            new { Id = RolSeed.SocioRolId, Nombre = "Socio", EsSistema = true, FechaCreacion = RolSeed.SeedTimestamp },
            new { Id = RolSeed.DuenoRolId, Nombre = "Dueño", EsSistema = true, FechaCreacion = RolSeed.SeedTimestamp }
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

        // Seed RolPermisos: Dueno opera sus unidades — todas las operaciones de Socios,
        // Planes, Clases, Cuotas y Empleados, mas Unidades Lectura. SIN Auditoria (exclusiva del Admin).
        var modulosOperativosDueno = new[] { Modulo.Socios, Modulo.Planes, Modulo.Clases, Modulo.Cuotas, Modulo.Empleados };
        var permisosDueno = permisoIds
            .Where(kvp => modulosOperativosDueno.Contains(kvp.Key.Item1)
                || (kvp.Key.Item1 == Modulo.Unidades && kvp.Key.Item2 == Operacion.Lectura))
            .OrderBy(kvp => kvp.Value)
            .Select(kvp => (object)new { RolId = RolSeed.DuenoRolId, PermisoId = kvp.Value });
        rolPermisoSeeds.AddRange(permisosDueno);

        modelBuilder.Entity<RolPermiso>().HasData(rolPermisoSeeds);

        // Seed Empleado admin de bootstrap
        // Hash precalculado con BCrypt para "admin123" — determinístico para que la migración no genere salts aleatorios cada build.
        const string adminPasswordHashBootstrap = "$2a$11$8TnD1uScCjtswRRfjtIMDufn8npEr3r1lKxd/aJ6LCv9wFtEPjvXS";
        modelBuilder.Entity<Empleado>().HasData(new
        {
            Id = EmpleadoSeed.AdminBootstrapId,
            Nombre = "Admin",
            Apellido = "Inicial",
            Correo = "admin@gymflow.com",
            PasswordHash = adminPasswordHashBootstrap,
            RolId = RolesSeed.AdminRolId,
            EstaActivo = true,
            FechaCreacion = RolSeed.SeedTimestamp
        });
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
    public static readonly Guid AdminRolId = RolesSeed.AdminRolId;
    public static readonly Guid SocioRolId = RolesSeed.SocioRolId;
    public static readonly Guid DuenoRolId = RolesSeed.DuenoRolId;
    public static readonly DateTime SeedTimestamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
}

public static class EmpleadoSeed
{
    public static readonly Guid AdminBootstrapId = Guid.Parse("33333333-3333-3333-3333-333333333333");
}
