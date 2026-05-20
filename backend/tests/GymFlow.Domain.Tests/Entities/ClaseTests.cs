using GymFlow.Domain.Entities;

namespace GymFlow.Domain.Tests.Entities;

public class ClaseTests
{
    private static Clase CrearClaseValida() =>
        new("Yoga", "Clase de yoga", 20, 60, "Laura García", Guid.NewGuid());

    [Fact]
    public void Constructor_ConDatosValidos_CreaClaseActiva()
    {
        var unidadId = Guid.NewGuid();
        var clase = new Clase("Yoga", "Clase de yoga", 20, 60, "Laura García", unidadId);

        Assert.NotEqual(Guid.Empty, clase.Id);
        Assert.Equal("Yoga", clase.Nombre);
        Assert.Equal("Clase de yoga", clase.Descripcion);
        Assert.Equal(20, clase.CapacidadMaxima);
        Assert.Equal(60, clase.DuracionMinutos);
        Assert.Equal("Laura García", clase.Instructor);
        Assert.Equal(unidadId, clase.UnidadId);
        Assert.True(clase.EstaActivo);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ConNombreInvalido_LanzaArgumentException(string? nombre)
    {
        Assert.Throws<ArgumentException>(() =>
            new Clase(nombre!, "Desc", 20, 60, "Instructor", Guid.NewGuid()));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_ConCapacidadInvalida_LanzaArgumentException(int capacidad)
    {
        Assert.Throws<ArgumentException>(() =>
            new Clase("Yoga", "Desc", capacidad, 60, "Instructor", Guid.NewGuid()));
    }

    [Fact]
    public void Constructor_ConCapacidadSuperiorAlMaximo_LanzaArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new Clase("Yoga", "Desc", Clase.CapacidadMaximaPermitida + 1, 60, "Instructor", Guid.NewGuid()));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Constructor_ConDuracionInvalida_LanzaArgumentException(int duracion)
    {
        Assert.Throws<ArgumentException>(() =>
            new Clase("Yoga", "Desc", 20, duracion, "Instructor", Guid.NewGuid()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ConInstructorInvalido_LanzaArgumentException(string? instructor)
    {
        Assert.Throws<ArgumentException>(() =>
            new Clase("Yoga", "Desc", 20, 60, instructor!, Guid.NewGuid()));
    }

    [Fact]
    public void Actualizar_ConDatosValidos_ActualizaPropiedades()
    {
        var clase = CrearClaseValida();

        clase.Actualizar("Pilates", "Clase de pilates", 15, 45, "María López", inscripcionesActivas: 0);

        Assert.Equal("Pilates", clase.Nombre);
        Assert.Equal("Clase de pilates", clase.Descripcion);
        Assert.Equal(15, clase.CapacidadMaxima);
        Assert.Equal(45, clase.DuracionMinutos);
        Assert.Equal("María López", clase.Instructor);
    }

    [Fact]
    public void Actualizar_ReducirCapacidadPorDebajoDeInscripciones_LanzaInvalidOperationException()
    {
        var clase = CrearClaseValida(); // capacidad 20

        Assert.Throws<InvalidOperationException>(() =>
            clase.Actualizar("Yoga", "Desc", 5, 60, "Instructor", inscripcionesActivas: 10));
    }

    [Fact]
    public void Actualizar_CapacidadIgualAInscripciones_NoLanzaExcepcion()
    {
        var clase = CrearClaseValida();

        clase.Actualizar("Yoga", "Desc", 10, 60, "Instructor", inscripcionesActivas: 10);

        Assert.Equal(10, clase.CapacidadMaxima);
    }

    [Fact]
    public void Cancelar_ClaseActiva_DesactivaClase()
    {
        var clase = CrearClaseValida();

        clase.Cancelar();

        Assert.False(clase.EstaActivo);
    }

    [Fact]
    public void Cancelar_ClaseYaCancelada_LanzaInvalidOperationException()
    {
        var clase = CrearClaseValida();
        clase.Cancelar();

        Assert.Throws<InvalidOperationException>(() => clase.Cancelar());
    }

    [Fact]
    public void Reactivar_ClaseCancelada_ActivaClase()
    {
        var clase = CrearClaseValida();
        clase.Cancelar();

        clase.Reactivar();

        Assert.True(clase.EstaActivo);
    }

    [Fact]
    public void Reactivar_ClaseYaActiva_LanzaInvalidOperationException()
    {
        var clase = CrearClaseValida();

        Assert.Throws<InvalidOperationException>(() => clase.Reactivar());
    }
}
