using GymFlow.Domain.Constants;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using GymFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace GymFlow.Infrastructure.Tests.Persistence;

/// <summary>
/// Verifica que el seed del rol Dueno (HasData en OnModelCreating) produzca exactamente
/// los permisos esperados: todas las operaciones de Socios, Planes, Clases, Cuotas y
/// Empleados, mas Unidades Lectura y Dashboard Lectura, y NUNCA Auditoria. No abre
/// conexion: solo construye el modelo para leer la seed data.
/// </summary>
public class SeedRolDuenoTests
{
    private static GymFlowDbContext CrearContextSoloModelo()
    {
        // Cadena ficticia: nunca se conecta, solo necesitamos construir el modelo para leer HasData.
        var options = new DbContextOptionsBuilder<GymFlowDbContext>()
            .UseNpgsql("Host=localhost;Database=gymflow_seed_test")
            .Options;
        return new GymFlowDbContext(options);
    }

    // GetSeedData solo esta disponible en el modelo de design-time, no en el read-optimized.
    private static IModel ModeloDesignTime(GymFlowDbContext ctx)
        => ctx.GetService<IDesignTimeModel>().Model;

    private static IReadOnlyList<(Modulo Modulo, Operacion Operacion)> PermisosDelRol(Guid rolId)
    {
        using var ctx = CrearContextSoloModelo();
        var model = ModeloDesignTime(ctx);

        // PermisoId -> (Modulo, Operacion)
        var permisoData = model.FindEntityType(typeof(Permiso))!
            .GetSeedData()
            .ToDictionary(
                d => (Guid)d["Id"]!,
                d => ((Modulo)d["Modulo"]!, (Operacion)d["Operacion"]!));

        return model.FindEntityType(typeof(RolPermiso))!
            .GetSeedData()
            .Where(d => (Guid)d["RolId"]! == rolId)
            .Select(d => permisoData[(Guid)d["PermisoId"]!])
            .ToList();
    }

    [Fact]
    public void Seed_ContieneRolDueno_EsSistema()
    {
        using var ctx = CrearContextSoloModelo();

        var dueno = ModeloDesignTime(ctx).FindEntityType(typeof(Rol))!
            .GetSeedData()
            .SingleOrDefault(d => (Guid)d["Id"]! == RolesSeed.DuenoRolId);

        Assert.NotNull(dueno);
        Assert.Equal("Dueño", dueno!["Nombre"]);
        Assert.True((bool)dueno["EsSistema"]!);
    }

    [Fact]
    public void Seed_Dueno_TieneTodasLasOperacionesDeModulosOperativos()
    {
        var permisos = PermisosDelRol(RolesSeed.DuenoRolId);
        var modulosCompletos = new[] { Modulo.Socios, Modulo.Planes, Modulo.Clases, Modulo.Cuotas, Modulo.Empleados, Modulo.Eventos };

        foreach (var modulo in modulosCompletos)
        {
            foreach (var op in Enum.GetValues<Operacion>())
            {
                Assert.Contains((modulo, op), permisos);
            }
        }
    }

    [Fact]
    public void Seed_Dueno_TieneUnidadesSoloLectura()
    {
        var unidades = PermisosDelRol(RolesSeed.DuenoRolId)
            .Where(p => p.Modulo == Modulo.Unidades)
            .ToList();

        Assert.Equal(new[] { (Modulo.Unidades, Operacion.Lectura) }, unidades);
    }

    [Fact]
    public void Seed_Dueno_TieneDashboardSoloLectura()
    {
        // RF-18 (CU-10, RN-16): el Dueño ve el dashboard de sus unidades — solo Lectura.
        var dashboard = PermisosDelRol(RolesSeed.DuenoRolId)
            .Where(p => p.Modulo == Modulo.Dashboard)
            .ToList();

        Assert.Equal(new[] { (Modulo.Dashboard, Operacion.Lectura) }, dashboard);
    }

    [Fact]
    public void Seed_Dueno_NoTieneAuditoria()
    {
        var permisos = PermisosDelRol(RolesSeed.DuenoRolId);
        Assert.DoesNotContain(permisos, p => p.Modulo == Modulo.Auditoria);
    }

    [Fact]
    public void Seed_Dueno_TieneExactamente26Permisos()
    {
        // 6 modulos completos (Socios, Planes, Clases, Cuotas, Empleados, Eventos) x 4 operaciones = 24,
        // mas Unidades Lectura y Dashboard Lectura = 26.
        Assert.Equal(26, PermisosDelRol(RolesSeed.DuenoRolId).Count);
    }
}
