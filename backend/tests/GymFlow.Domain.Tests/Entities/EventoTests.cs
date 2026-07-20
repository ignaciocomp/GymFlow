using GymFlow.Domain.Entities;

namespace GymFlow.Domain.Tests.Entities;

public class EventoTests
{
    private static Evento CrearEventoValido() =>
        new("Torneo de verano", "Torneo interno de socios", new DateTime(2026, 12, 1, 18, 0, 0, DateTimeKind.Utc), Guid.NewGuid());

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Ctor_ConTituloVacio_Lanza(string? titulo)
    {
        Assert.Throws<ArgumentException>(() =>
            new Evento(titulo!, "Desc", DateTime.UtcNow, Guid.NewGuid()));
    }

    [Fact]
    public void Ctor_ConTituloVacio_ElMensajeNoFiltraElParametro()
    {
        // E2E-24 (barrido): las validaciones de negocio que ve el usuario no llevan
        // paramName para que ArgumentException no agregue "(Parameter '...')".
        var ex = Assert.Throws<ArgumentException>(() =>
            new Evento("", "Desc", DateTime.UtcNow, Guid.NewGuid()));

        Assert.Equal("El título es obligatorio.", ex.Message);
    }

    [Fact]
    public void Actualizar_ConTituloVacio_ElMensajeNoFiltraElParametro()
    {
        var evento = CrearEventoValido();

        var ex = Assert.Throws<ArgumentException>(() =>
            evento.Actualizar("  ", "Desc", DateTime.UtcNow));

        Assert.Equal("El título es obligatorio.", ex.Message);
    }

    [Fact]
    public void Ctor_SeteaCamposYActivo()
    {
        var unidadId = Guid.NewGuid();
        var fecha = new DateTime(2026, 12, 1, 18, 0, 0, DateTimeKind.Utc);
        var antes = DateTime.UtcNow;

        var evento = new Evento("Torneo de verano", "Torneo interno", fecha, unidadId);

        Assert.NotEqual(Guid.Empty, evento.Id);
        Assert.Equal("Torneo de verano", evento.Titulo);
        Assert.Equal("Torneo interno", evento.Descripcion);
        Assert.Equal(fecha, evento.Fecha);
        Assert.Equal(unidadId, evento.UnidadId);
        Assert.True(evento.EstaActivo);
        Assert.True(evento.FechaCreacion >= antes && evento.FechaCreacion <= DateTime.UtcNow);
    }

    [Fact]
    public void Actualizar_CambiaCampos()
    {
        var evento = CrearEventoValido();
        // fecha pasada permitida: el dominio NO valida fecha pasada
        var nuevaFecha = new DateTime(2020, 1, 1, 10, 0, 0, DateTimeKind.Utc);

        evento.Actualizar("Charla de nutrición", "Charla abierta", nuevaFecha);

        Assert.Equal("Charla de nutrición", evento.Titulo);
        Assert.Equal("Charla abierta", evento.Descripcion);
        Assert.Equal(nuevaFecha, evento.Fecha);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Actualizar_ConTituloVacio_Lanza(string? titulo)
    {
        var evento = CrearEventoValido();

        Assert.Throws<ArgumentException>(() =>
            evento.Actualizar(titulo!, "Desc", DateTime.UtcNow));
    }

    [Fact]
    public void Cancelar_DesactivaYReactivar_Activa()
    {
        var evento = CrearEventoValido();

        evento.Cancelar();
        Assert.False(evento.EstaActivo);

        evento.Reactivar();
        Assert.True(evento.EstaActivo);
    }

    [Fact]
    public void Cancelar_EsIdempotente_NoLanza()
    {
        var evento = CrearEventoValido();
        evento.Cancelar();

        // idempotente: cancelar de nuevo no lanza
        evento.Cancelar();

        Assert.False(evento.EstaActivo);
    }

    [Fact]
    public void Reactivar_EsIdempotente_NoLanza()
    {
        var evento = CrearEventoValido();

        // idempotente: reactivar uno ya activo no lanza
        evento.Reactivar();

        Assert.True(evento.EstaActivo);
    }
}
