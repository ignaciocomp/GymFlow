using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;

namespace GymFlow.Domain.Tests.Entities;

public class RegistroAuditoriaTests
{
    [Fact]
    public void Constructor_ConDatosValidos_CreaEntidadCorrectamente()
    {
        var usuarioId = Guid.NewGuid();
        var entidadId = Guid.NewGuid();

        var registro = new RegistroAuditoria(
            usuarioId: usuarioId,
            usuarioNombre: "Maurice Admin",
            tipoAccion: TipoAccionAuditoria.Creacion,
            entidadAfectada: "Socio",
            entidadId: entidadId,
            descripcion: "Se registró al socio Juan García");

        Assert.NotEqual(Guid.Empty, registro.Id);
        Assert.Equal(usuarioId, registro.UsuarioId);
        Assert.Equal("Maurice Admin", registro.UsuarioNombre);
        Assert.Equal(TipoAccionAuditoria.Creacion, registro.TipoAccion);
        Assert.Equal("Socio", registro.EntidadAfectada);
        Assert.Equal(entidadId, registro.EntidadId);
        Assert.Equal("Se registró al socio Juan García", registro.Descripcion);
        Assert.Null(registro.DetallesCambios);
        Assert.True(registro.FechaHora <= DateTime.UtcNow);
    }

    [Fact]
    public void Constructor_ConDetallesCambios_AlmacenaJson()
    {
        var registro = new RegistroAuditoria(
            usuarioId: Guid.NewGuid(),
            usuarioNombre: "Maurice Admin",
            tipoAccion: TipoAccionAuditoria.Modificacion,
            entidadAfectada: "Socio",
            entidadId: Guid.NewGuid(),
            descripcion: "Se modificaron los datos del socio",
            detallesCambios: "{\"Nombre\":{\"anterior\":\"Juan\",\"nuevo\":\"Carlos\"}}");

        Assert.NotNull(registro.DetallesCambios);
        Assert.Contains("Juan", registro.DetallesCambios);
    }

    [Fact]
    public void Constructor_ConInicioSesion_EntidadIdEsNull()
    {
        var registro = new RegistroAuditoria(
            usuarioId: Guid.NewGuid(),
            usuarioNombre: "Maurice Admin",
            tipoAccion: TipoAccionAuditoria.InicioSesion,
            entidadAfectada: "Sesion",
            entidadId: null,
            descripcion: "Inicio de sesión del administrador Maurice");

        Assert.Null(registro.EntidadId);
        Assert.Equal("Sesion", registro.EntidadAfectada);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ConUsuarioNombreVacio_LanzaArgumentException(string? nombre)
    {
        Assert.Throws<ArgumentException>(() => new RegistroAuditoria(
            usuarioId: Guid.NewGuid(),
            usuarioNombre: nombre!,
            tipoAccion: TipoAccionAuditoria.Creacion,
            entidadAfectada: "Socio",
            entidadId: Guid.NewGuid(),
            descripcion: "test"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ConDescripcionVacia_LanzaArgumentException(string? desc)
    {
        Assert.Throws<ArgumentException>(() => new RegistroAuditoria(
            usuarioId: Guid.NewGuid(),
            usuarioNombre: "Admin",
            tipoAccion: TipoAccionAuditoria.Creacion,
            entidadAfectada: "Socio",
            entidadId: Guid.NewGuid(),
            descripcion: desc!));
    }
}
