using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;

namespace GymFlow.Domain.Tests.Entities;

public class UsuarioTests
{
    // Helper: Usuario es abstracta, usamos Socio como instancia concreta
    private static Socio CrearSocio() =>
        new Socio(
            rolSocioId: Guid.NewGuid(),
            nombre: "Juan",
            apellido: "García",
            correo: "juan@test.com",
            passwordHash: null,
            fechaAlta: DateTime.UtcNow,
            consentimientoInformado: true,
            tipoDocumento: TipoDocumento.Otro,
            documentoIdentidad: null);

    // --- VincularGoogle ---

    [Fact]
    public void GoogleUserId_PorDefecto_EsNull()
    {
        var socio = CrearSocio();

        Assert.Null(socio.GoogleUserId);
    }

    [Fact]
    public void VincularGoogle_ConCampoVacio_SeteaElValor()
    {
        var socio = CrearSocio();

        socio.VincularGoogle("google-sub-123");

        Assert.Equal("google-sub-123", socio.GoogleUserId);
    }

    [Fact]
    public void VincularGoogle_ConCampoYaSeteado_NoLoPisa()
    {
        var socio = CrearSocio();
        socio.VincularGoogle("google-sub-original");

        socio.VincularGoogle("google-sub-otro");

        Assert.Equal("google-sub-original", socio.GoogleUserId);
    }

    [Fact]
    public void VincularGoogle_ConMismoValor_EsIdempotente()
    {
        var socio = CrearSocio();
        socio.VincularGoogle("google-sub-123");

        var ex = Record.Exception(() => socio.VincularGoogle("google-sub-123"));

        Assert.Null(ex);
        Assert.Equal("google-sub-123", socio.GoogleUserId);
    }

    [Fact]
    public void VincularGoogle_ConValorNull_LanzaArgumentException()
    {
        var socio = CrearSocio();

        Assert.Throws<ArgumentException>(() => socio.VincularGoogle(null!));
    }

    [Fact]
    public void VincularGoogle_ConValorVacio_LanzaArgumentException()
    {
        var socio = CrearSocio();

        Assert.Throws<ArgumentException>(() => socio.VincularGoogle("  "));
    }
}
