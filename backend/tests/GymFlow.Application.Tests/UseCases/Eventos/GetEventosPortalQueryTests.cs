using GymFlow.Application.Interfaces;
using GymFlow.Application.UseCases.Eventos;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using Moq;

namespace GymFlow.Application.Tests.UseCases.Eventos;

public class GetEventosPortalQueryTests
{
    private readonly Mock<IEventoRepository> _eventoRepo = new();
    private readonly Mock<ISocioRepository> _socioRepo = new();

    private GetEventosPortalQuery CrearQuery() =>
        new(_eventoRepo.Object, _socioRepo.Object);

    private static Socio CrearSocioConUnidades(string correo, params Guid[] unidadIds)
    {
        var socio = new Socio(
            rolSocioId: Guid.NewGuid(),
            nombre: "Maria",
            apellido: "Lopez",
            correo: correo,
            passwordHash: "hash",
            fechaAlta: DateTime.UtcNow,
            consentimientoInformado: true,
            tipoDocumento: TipoDocumento.CI,
            telefono: null,
            documentoIdentidad: "12345672",
            fechaNacimiento: null);

        foreach (var unidadId in unidadIds)
            socio.UnidadesAsignadas.Add(new UsuarioUnidad(socio.Id, unidadId));

        return socio;
    }

    private static Evento CrearEvento(Unidad unidad, DateTime fecha)
    {
        var evento = new Evento("Torneo de verano", "Torneo abierto", fecha, unidad.Id);
        typeof(Evento).GetProperty("Unidad")!.SetValue(evento, unidad);
        return evento;
    }

    [Fact]
    public async Task ExecuteAsync_DevuelveProximosDeMisUnidades()
    {
        var unidadA = new Unidad("Gimnasio Nuevo Malvin", "Malvin, Montevideo");
        var unidadB = new Unidad("Gimnasio Centro", "Centro, Montevideo");
        var socio = CrearSocioConUnidades("socio@test.com", unidadA.Id, unidadB.Id);

        var ahora = DateTime.UtcNow;
        var eventoA = CrearEvento(unidadA, ahora.AddDays(3));
        var eventoB = CrearEvento(unidadB, ahora.AddDays(7));

        _socioRepo.Setup(r => r.GetByCorreoAsync("socio@test.com")).ReturnsAsync(socio);
        _eventoRepo
            .Setup(r => r.GetProximosByUnidadesAsync(
                It.Is<IEnumerable<Guid>>(ids => ids.Contains(unidadA.Id) && ids.Contains(unidadB.Id)),
                It.IsAny<DateTime>()))
            .ReturnsAsync(new[] { eventoA, eventoB });

        var result = (await CrearQuery().ExecuteAsync("socio@test.com")).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(eventoA.Id, result[0].Id);
        Assert.Equal(eventoB.Id, result[1].Id);
        Assert.Equal("Gimnasio Nuevo Malvin", result[0].UnidadNombre);
        Assert.Equal("Gimnasio Centro", result[1].UnidadNombre);

        // El filtrado real (proximos/activos/de mis unidades/orden) lo hace el repo:
        // la query solo le pasa las unidades del socio y DateTime.UtcNow.
        _eventoRepo.Verify(r => r.GetProximosByUnidadesAsync(
            It.IsAny<IEnumerable<Guid>>(), It.IsAny<DateTime>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_SocioNoExiste_LanzaKeyNotFound()
    {
        _socioRepo.Setup(r => r.GetByCorreoAsync(It.IsAny<string>())).ReturnsAsync((Socio?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            CrearQuery().ExecuteAsync("nadie@test.com"));

        _eventoRepo.Verify(r => r.GetProximosByUnidadesAsync(
            It.IsAny<IEnumerable<Guid>>(), It.IsAny<DateTime>()), Times.Never);
    }
}
