using System.Reflection;
using GymFlow.API.Authorization;
using GymFlow.API.Controllers;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace GymFlow.Application.Tests.Controllers;

/// <summary>
/// Regresión del bug del portal del socio: GET /api/unidades exigía el permiso de módulo
/// Unidades-Lectura, pero el rol Socio no tiene permisos de módulo, por lo que el portal
/// recibía 403 y la vista de Horarios nunca cargaba (el filtro de sede no se inicializaba).
/// El GET debe requerir SOLO autenticación ([Authorize] a nivel de controller).
/// </summary>
public class UnidadesControllerTests
{
    [Fact]
    public void Controller_RequiereAutenticacion()
    {
        var attr = typeof(UnidadesController).GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(attr);
    }

    [Fact]
    public void GetAll_NoExigePermisoDeModulo_ParaQueElPortalDelSocioPuedaListarSedes()
    {
        var metodo = typeof(UnidadesController).GetMethod(nameof(UnidadesController.GetAll));
        Assert.NotNull(metodo);

        var attr = metodo!.GetCustomAttribute<RequierePermisoAttribute>();
        Assert.Null(attr);
    }
}
