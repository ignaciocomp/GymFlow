using GymFlow.Domain.Entities;
using Xunit;

namespace GymFlow.Domain.Tests.Entities;

public class EmpleadoMfaTests
{
    private static Empleado CrearEmpleado() =>
        new("Juan", "Pérez", "juan@gymflow.com", "hashed_pwd", Guid.NewGuid());

    [Fact]
    public void ActivarMfa_SeteaSecretoYHabilita()
    {
        var empleado = CrearEmpleado();
        const string secretoProtegido = "blob-cifrado-base64";

        empleado.ActivarMfa(secretoProtegido);

        Assert.True(empleado.MfaHabilitado);
        Assert.Equal(secretoProtegido, empleado.MfaSecret);
    }

    [Fact]
    public void RegistrarIntentoFallido_AlQuinto_Bloquea()
    {
        var empleado = CrearEmpleado();
        var ahora = new DateTime(2026, 6, 16, 10, 0, 0, DateTimeKind.Utc);

        // 4 intentos: todavía no bloqueado
        for (var i = 0; i < 4; i++)
            empleado.RegistrarIntentoFallidoMfa(ahora);

        Assert.False(empleado.EstaBloqueadoMfa(ahora));
        Assert.Null(empleado.MfaBloqueadoHasta);

        // 5º intento: bloquea por 15 minutos
        empleado.RegistrarIntentoFallidoMfa(ahora);

        Assert.True(empleado.EstaBloqueadoMfa(ahora));
        Assert.Equal(ahora.AddMinutes(15), empleado.MfaBloqueadoHasta);
    }

    [Fact]
    public void RegistrarVerificacionExitosa_ReseteaContadorYBloqueo()
    {
        var empleado = CrearEmpleado();
        var ahora = new DateTime(2026, 6, 16, 10, 0, 0, DateTimeKind.Utc);

        for (var i = 0; i < 5; i++)
            empleado.RegistrarIntentoFallidoMfa(ahora);

        Assert.True(empleado.EstaBloqueadoMfa(ahora));

        empleado.RegistrarVerificacionExitosaMfa();

        Assert.Equal(0, empleado.MfaIntentosFallidos);
        Assert.Null(empleado.MfaBloqueadoHasta);
    }

    [Fact]
    public void EstaBloqueadoMfa_TrasExpirar_DevuelveFalse()
    {
        var empleado = CrearEmpleado();
        var ahora = new DateTime(2026, 6, 16, 10, 0, 0, DateTimeKind.Utc);

        for (var i = 0; i < 5; i++)
            empleado.RegistrarIntentoFallidoMfa(ahora);

        var despues = ahora.AddMinutes(16);

        Assert.False(empleado.EstaBloqueadoMfa(despues));
    }

    [Fact]
    public void ResetearMfa_LimpiaTodo()
    {
        var empleado = CrearEmpleado();
        var ahora = new DateTime(2026, 6, 16, 10, 0, 0, DateTimeKind.Utc);

        empleado.ActivarMfa("blob-cifrado-base64");
        for (var i = 0; i < 3; i++)
            empleado.RegistrarIntentoFallidoMfa(ahora);

        empleado.ResetearMfa();

        Assert.False(empleado.MfaHabilitado);
        Assert.Null(empleado.MfaSecret);
        Assert.Equal(0, empleado.MfaIntentosFallidos);
        Assert.Null(empleado.MfaBloqueadoHasta);
    }
}
