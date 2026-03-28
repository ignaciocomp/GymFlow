using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;

namespace GymFlow.Domain.Tests.Entities;

public class SocioTests
{
    // Helper: crea un Socio válido con el tipo y documento dados
    private static Socio CrearSocio(
        TipoDocumento tipoDocumento,
        string? documentoIdentidad) =>
        new Socio(
            nombre: "Juan",
            apellido: "García",
            correo: "juan@test.com",
            passwordHash: "hash",
            planId: null,
            fechaAlta: DateTime.UtcNow,
            consentimientoInformado: true,
            tipoDocumento: tipoDocumento,
            documentoIdentidad: documentoIdentidad);

    // --- Validación de cédula en constructor ---

    [Fact]
    public void Constructor_ConCI_YCedulaValida8Digitos_NoLanzaExcepcion()
    {
        var ex = Record.Exception(() => CrearSocio(TipoDocumento.CI, "54321163"));
        Assert.Null(ex);
    }

    [Fact]
    public void Constructor_ConCI_YCedulaValida7Digitos_NoLanzaExcepcion()
    {
        // 7 dígitos → se paddea: 01234561, suma=109, verificador=1, (109+1)%10=0
        var ex = Record.Exception(() => CrearSocio(TipoDocumento.CI, "1234561"));
        Assert.Null(ex);
    }

    [Fact]
    public void Constructor_ConCI_YCedulaConPuntosYGuion_NoLanzaExcepcion()
    {
        var ex = Record.Exception(() => CrearSocio(TipoDocumento.CI, "5.432.116-3"));
        Assert.Null(ex);
    }

    [Fact]
    public void Constructor_ConCI_YDigitoVerificadorIncorrecto_LanzaArgumentException()
    {
        // 12345678: suma=148, verificador esperado=2, recibido=8 → inválida
        Assert.Throws<ArgumentException>(() => CrearSocio(TipoDocumento.CI, "12345678"));
    }

    [Fact]
    public void Constructor_ConCI_YTextoNoNumerico_LanzaArgumentException()
    {
        Assert.Throws<ArgumentException>(() => CrearSocio(TipoDocumento.CI, "ABCDEFG1"));
    }

    [Fact]
    public void Constructor_ConCI_YDocumentoNull_LanzaArgumentException()
    {
        Assert.Throws<ArgumentException>(() => CrearSocio(TipoDocumento.CI, null));
    }

    [Fact]
    public void Constructor_ConCI_YDocumentoVacio_LanzaArgumentException()
    {
        Assert.Throws<ArgumentException>(() => CrearSocio(TipoDocumento.CI, ""));
    }

    // --- Tipos sin validación de cédula ---

    [Fact]
    public void Constructor_ConPasaporte_YDocumentoArbitrario_NoLanzaExcepcion()
    {
        var ex = Record.Exception(() => CrearSocio(TipoDocumento.Pasaporte, "XY123456"));
        Assert.Null(ex);
    }

    [Fact]
    public void Constructor_ConOtro_SinDocumento_NoLanzaExcepcion()
    {
        var ex = Record.Exception(() => CrearSocio(TipoDocumento.Otro, null));
        Assert.Null(ex);
    }

    // --- Validación en ActualizarDatosSocio ---

    [Fact]
    public void ActualizarDatosSocio_ConCI_YCedulaInvalida_LanzaArgumentException()
    {
        var socio = CrearSocio(TipoDocumento.Otro, null);

        Assert.Throws<ArgumentException>(() =>
            socio.ActualizarDatosSocio(
                nombre: "Juan",
                apellido: "García",
                correo: "juan@test.com",
                planId: null,
                tipoDocumento: TipoDocumento.CI,
                documentoIdentidad: "12345678",
                telefono: null,
                fechaNacimiento: null));
    }

    [Fact]
    public void ActualizarDatosSocio_ConCI_YCedulaValida_NoLanzaExcepcion()
    {
        var socio = CrearSocio(TipoDocumento.Otro, null);

        var ex = Record.Exception(() =>
            socio.ActualizarDatosSocio(
                nombre: "Juan",
                apellido: "García",
                correo: "juan@test.com",
                planId: null,
                tipoDocumento: TipoDocumento.CI,
                documentoIdentidad: "54321163",
                telefono: null,
                fechaNacimiento: null));

        Assert.Null(ex);
    }
}
