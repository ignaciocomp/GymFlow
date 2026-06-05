using GymFlow.Domain.Entities;

namespace GymFlow.Domain.Tests.Entities;

public class InscripcionClaseTests
{
    [Fact]
    public void Constructor_PorDefecto_EstaActiva()
    {
        var i = new InscripcionClase(Guid.NewGuid(), Guid.NewGuid());
        Assert.True(i.EstaActiva);
    }

    [Fact]
    public void Constructor_AsignaHorarioClaseId()
    {
        var horarioId = Guid.NewGuid();
        var i = new InscripcionClase(horarioId, Guid.NewGuid());
        Assert.Equal(horarioId, i.HorarioClaseId);
    }

    [Fact]
    public void Cancelar_MarcaComoInactiva()
    {
        var i = new InscripcionClase(Guid.NewGuid(), Guid.NewGuid());
        i.Cancelar();
        Assert.False(i.EstaActiva);
    }
}
