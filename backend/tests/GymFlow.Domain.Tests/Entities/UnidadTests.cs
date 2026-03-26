using GymFlow.Domain.Entities;

namespace GymFlow.Domain.Tests.Entities;

public class UnidadTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesUnidad()
    {
        var unidad = new Unidad("Gimnasio Nuevo Malvín", "Malvín, Montevideo");

        Assert.NotEqual(Guid.Empty, unidad.Id);
        Assert.Equal("Gimnasio Nuevo Malvín", unidad.Nombre);
        Assert.Equal("Malvín, Montevideo", unidad.Direccion);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidNombre_ThrowsArgumentException(string? nombre)
    {
        Assert.Throws<ArgumentException>(() => new Unidad(nombre!, "Dirección"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidDireccion_ThrowsArgumentException(string? direccion)
    {
        Assert.Throws<ArgumentException>(() => new Unidad("Nombre", direccion!));
    }

    [Fact]
    public void Actualizar_WithValidData_UpdatesProperties()
    {
        var unidad = new Unidad("Nombre Original", "Dirección Original");

        unidad.Actualizar("Nombre Nuevo", "Dirección Nueva");

        Assert.Equal("Nombre Nuevo", unidad.Nombre);
        Assert.Equal("Dirección Nueva", unidad.Direccion);
    }
}
