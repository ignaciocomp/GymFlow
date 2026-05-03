using GymFlow.Domain.Entities;
using Xunit;

namespace GymFlow.Domain.Tests.Entities;

public class RolTests
{
    [Fact]
    public void Crear_ConNombreValido_AsignaCampos()
    {
        var rol = new Rol("Recepcionista");

        Assert.NotEqual(Guid.Empty, rol.Id);
        Assert.Equal("Recepcionista", rol.Nombre);
        Assert.False(rol.EsSistema);
        Assert.Empty(rol.Permisos);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Crear_ConNombreInvalido_LanzaArgumentException(string? nombre)
    {
        Assert.Throws<ArgumentException>(() => new Rol(nombre!));
    }

    [Fact]
    public void Renombrar_RolDeSistema_LanzaInvalidOperationException()
    {
        var rol = new Rol("Administrador", esSistema: true);

        Assert.Throws<InvalidOperationException>(() => rol.Renombrar("Otro"));
    }

    [Fact]
    public void Renombrar_RolNormal_ActualizaNombre()
    {
        var rol = new Rol("Original");
        rol.Renombrar("Nuevo");

        Assert.Equal("Nuevo", rol.Nombre);
    }

    [Fact]
    public void ReemplazarPermisos_RolDeSistema_LanzaInvalidOperationException()
    {
        var rol = new Rol("Administrador", esSistema: true);

        Assert.Throws<InvalidOperationException>(() => rol.ReemplazarPermisos(new[] { Guid.NewGuid() }));
    }

    [Fact]
    public void ReemplazarPermisos_QuitaAnterioresYAgregaNuevos()
    {
        var rol = new Rol("Custom");
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var p3 = Guid.NewGuid();

        rol.ReemplazarPermisos(new[] { p1, p2 });
        Assert.Equal(2, rol.Permisos.Count);

        rol.ReemplazarPermisos(new[] { p3 });
        Assert.Single(rol.Permisos);
        Assert.Contains(rol.Permisos, rp => rp.PermisoId == p3);
    }

    [Fact]
    public void ReemplazarPermisos_ConDuplicados_DeduplicaSilenciosamente()
    {
        var rol = new Rol("Custom");
        var p1 = Guid.NewGuid();

        rol.ReemplazarPermisos(new[] { p1, p1, p1 });

        Assert.Single(rol.Permisos);
    }
}
