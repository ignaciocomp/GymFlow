using GymFlow.Domain.Entities;
using Xunit;

namespace GymFlow.Domain.Tests.Entities;

public class EmpleadoTests
{
    [Fact]
    public void Constructor_ConDatosValidos_CreaEmpleadoActivo()
    {
        var rolId = Guid.NewGuid();

        var empleado = new Empleado("Juan", "Pérez", "juan@gymflow.com", "hashed_pwd", rolId);

        Assert.Equal("Juan", empleado.Nombre);
        Assert.Equal("Pérez", empleado.Apellido);
        Assert.Equal("juan@gymflow.com", empleado.Correo);
        Assert.Equal("hashed_pwd", empleado.PasswordHash);
        Assert.Equal(rolId, empleado.RolId);
        Assert.True(empleado.EstaActivo);
    }

    [Fact]
    public void Constructor_ConPasswordHashVacio_LanzaArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new Empleado("Juan", "Pérez", "juan@gymflow.com", "", Guid.NewGuid()));
    }

    [Fact]
    public void Constructor_ConPasswordHashNull_LanzaArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new Empleado("Juan", "Pérez", "juan@gymflow.com", null!, Guid.NewGuid()));
    }

    [Fact]
    public void CambiarRol_ConRolIdValido_ActualizaRolId()
    {
        var empleado = new Empleado("Juan", "Pérez", "juan@gymflow.com", "hash", Guid.NewGuid());
        var nuevoRolId = Guid.NewGuid();

        empleado.CambiarRol(nuevoRolId);

        Assert.Equal(nuevoRolId, empleado.RolId);
    }

    [Fact]
    public void CambiarRol_ConGuidEmpty_LanzaArgumentException()
    {
        var empleado = new Empleado("Juan", "Pérez", "juan@gymflow.com", "hash", Guid.NewGuid());

        Assert.Throws<ArgumentException>(() => empleado.CambiarRol(Guid.Empty));
    }
}
