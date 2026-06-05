using GymFlow.Domain.Entities;

namespace GymFlow.Domain.Tests.Entities;

public class InscripcionClaseTests
{
    [Fact]
    public void Constructor_PorDefecto_NoEsListaEspera()
    {
        var i = new InscripcionClase(Guid.NewGuid(), Guid.NewGuid());
        Assert.False(i.EsListaEspera);
        Assert.True(i.EstaActiva);
    }

    [Fact]
    public void Constructor_ConListaEspera_MarcaFlag()
    {
        var i = new InscripcionClase(Guid.NewGuid(), Guid.NewGuid(), esListaEspera: true);
        Assert.True(i.EsListaEspera);
    }

    [Fact]
    public void PromoverDeListaEspera_EnListaEspera_QuitaFlag()
    {
        var i = new InscripcionClase(Guid.NewGuid(), Guid.NewGuid(), esListaEspera: true);
        i.PromoverDeListaEspera();
        Assert.False(i.EsListaEspera);
    }

    [Fact]
    public void PromoverDeListaEspera_NoEnListaEspera_LanzaExcepcion()
    {
        var i = new InscripcionClase(Guid.NewGuid(), Guid.NewGuid());
        Assert.Throws<InvalidOperationException>(() => i.PromoverDeListaEspera());
    }
}
