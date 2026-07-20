using GymFlow.Domain.Constants;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;

namespace GymFlow.Domain.Tests.Entities;

public class HorarioClaseTests
{
    private static HorarioClase CrearHorarioValido(
        DiaSemana dia = DiaSemana.Lunes,
        string horaInicio = "09:00",
        string horaFin = "10:00",
        string? sala = "Sala A") =>
        new(Guid.NewGuid(), dia,
            TimeOnly.Parse(horaInicio), TimeOnly.Parse(horaFin), sala);

    [Fact]
    public void Constructor_ConDatosValidos_CreaHorario()
    {
        var claseId = Guid.NewGuid();
        var horario = new HorarioClase(claseId, DiaSemana.Martes,
            new TimeOnly(10, 0), new TimeOnly(11, 30), "Sala B");

        Assert.NotEqual(Guid.Empty, horario.Id);
        Assert.Equal(claseId, horario.ClaseId);
        Assert.Equal(DiaSemana.Martes, horario.DiaSemana);
        Assert.Equal(new TimeOnly(10, 0), horario.HoraInicio);
        Assert.Equal(new TimeOnly(11, 30), horario.HoraFin);
        Assert.Equal("Sala B", horario.Sala);
    }

    [Fact]
    public void Constructor_ConHoraFinIgualAInicio_LanzaArgumentException()
    {
        var hora = new TimeOnly(10, 0);
        Assert.Throws<ArgumentException>(() =>
            new HorarioClase(Guid.NewGuid(), DiaSemana.Lunes, hora, hora, null));
    }

    [Fact]
    public void Constructor_ConHoraFinAnteriorAInicio_LanzaArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new HorarioClase(Guid.NewGuid(), DiaSemana.Lunes,
                new TimeOnly(10, 0), new TimeOnly(9, 0), null));
    }

    [Fact]
    public void Constructor_ConSalaEnBlanco_GuardaNull()
    {
        var horario = new HorarioClase(Guid.NewGuid(), DiaSemana.Lunes,
            new TimeOnly(9, 0), new TimeOnly(10, 0), "   ");

        Assert.Null(horario.Sala);
    }

    [Fact]
    public void Constructor_ConSalaConEspacios_LaTrimmea()
    {
        var horario = new HorarioClase(Guid.NewGuid(), DiaSemana.Lunes,
            new TimeOnly(9, 0), new TimeOnly(10, 0), "  Sala A  ");

        Assert.Equal("Sala A", horario.Sala);
    }

    [Fact]
    public void Actualizar_ConDatosValidos_ActualizaCampos()
    {
        var horario = CrearHorarioValido();

        horario.Actualizar(DiaSemana.Viernes,
            new TimeOnly(14, 0), new TimeOnly(15, 30), "Sala C");

        Assert.Equal(DiaSemana.Viernes, horario.DiaSemana);
        Assert.Equal(new TimeOnly(14, 0), horario.HoraInicio);
        Assert.Equal(new TimeOnly(15, 30), horario.HoraFin);
        Assert.Equal("Sala C", horario.Sala);
    }

    [Fact]
    public void Actualizar_ConHoraFinInvalida_LanzaArgumentException()
    {
        var horario = CrearHorarioValido();

        Assert.Throws<ArgumentException>(() =>
            horario.Actualizar(DiaSemana.Lunes,
                new TimeOnly(10, 0), new TimeOnly(9, 0), null));
    }

    // --- Horario de apertura (E2E-18 parte 2): el gym abre de 07:00 a 22:00 ---

    [Fact]
    public void Constructor_ConInicioAntesDeApertura_LanzaArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new HorarioClase(Guid.NewGuid(), DiaSemana.Lunes,
                new TimeOnly(6, 30), new TimeOnly(8, 0), null));

        Assert.Equal("El horario debe estar dentro del horario de apertura (07:00 a 22:00).", ex.Message);
    }

    [Fact]
    public void Constructor_ConFinDespuesDelCierre_LanzaArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new HorarioClase(Guid.NewGuid(), DiaSemana.Lunes,
                new TimeOnly(21, 0), new TimeOnly(22, 30), null));
    }

    [Fact]
    public void Constructor_EnLosBordesDeApertura_EsValido()
    {
        var horario = new HorarioClase(Guid.NewGuid(), DiaSemana.Lunes,
            HorarioApertura.Apertura, HorarioApertura.Cierre, null);

        Assert.Equal(new TimeOnly(7, 0), horario.HoraInicio);
        Assert.Equal(new TimeOnly(22, 0), horario.HoraFin);
    }

    [Fact]
    public void Actualizar_ConInicioAntesDeApertura_LanzaArgumentException()
    {
        var horario = CrearHorarioValido();

        Assert.Throws<ArgumentException>(() =>
            horario.Actualizar(DiaSemana.Lunes,
                new TimeOnly(5, 0), new TimeOnly(8, 0), null));
    }

    [Fact]
    public void Actualizar_ConFinDespuesDelCierre_LanzaArgumentException()
    {
        var horario = CrearHorarioValido();

        Assert.Throws<ArgumentException>(() =>
            horario.Actualizar(DiaSemana.Lunes,
                new TimeOnly(21, 0), new TimeOnly(23, 0), null));
    }

    // --- SeSolapaCon tests ---

    [Fact]
    public void SeSolapaCon_MismoDiaMismaSalaYSeSolapan_RetornaTrue()
    {
        var h1 = CrearHorarioValido(DiaSemana.Lunes, "09:00", "10:00", "Sala A");
        var h2 = CrearHorarioValido(DiaSemana.Lunes, "09:30", "10:30", "Sala A");

        Assert.True(h1.SeSolapaCon(h2));
    }

    [Fact]
    public void SeSolapaCon_MismoDiaMismaSalaSinSolape_RetornaFalse()
    {
        var h1 = CrearHorarioValido(DiaSemana.Lunes, "09:00", "10:00", "Sala A");
        var h2 = CrearHorarioValido(DiaSemana.Lunes, "10:00", "11:00", "Sala A");

        Assert.False(h1.SeSolapaCon(h2));
    }

    [Fact]
    public void SeSolapaCon_DiferenteDia_RetornaFalse()
    {
        var h1 = CrearHorarioValido(DiaSemana.Lunes, "09:00", "10:00", "Sala A");
        var h2 = CrearHorarioValido(DiaSemana.Martes, "09:00", "10:00", "Sala A");

        Assert.False(h1.SeSolapaCon(h2));
    }

    [Fact]
    public void SeSolapaCon_DiferenteSala_RetornaFalse()
    {
        var h1 = CrearHorarioValido(DiaSemana.Lunes, "09:00", "10:00", "Sala A");
        var h2 = CrearHorarioValido(DiaSemana.Lunes, "09:00", "10:00", "Sala B");

        Assert.False(h1.SeSolapaCon(h2));
    }

    [Fact]
    public void SeSolapaCon_SalaNula_RetornaFalse()
    {
        var h1 = CrearHorarioValido(DiaSemana.Lunes, "09:00", "10:00", "Sala A");
        var h2 = CrearHorarioValido(DiaSemana.Lunes, "09:00", "10:00", null);

        Assert.False(h1.SeSolapaCon(h2));
    }

    [Fact]
    public void SeSolapaCon_SalaCaseInsensitive_RetornaTrue()
    {
        var h1 = CrearHorarioValido(DiaSemana.Lunes, "09:00", "10:00", "sala a");
        var h2 = CrearHorarioValido(DiaSemana.Lunes, "09:30", "10:30", "SALA A");

        Assert.True(h1.SeSolapaCon(h2));
    }

    [Fact]
    public void SeSolapaCon_ContenidoDentro_RetornaTrue()
    {
        var h1 = CrearHorarioValido(DiaSemana.Lunes, "08:00", "12:00", "Sala A");
        var h2 = CrearHorarioValido(DiaSemana.Lunes, "09:00", "10:00", "Sala A");

        Assert.True(h1.SeSolapaCon(h2));
    }
}
