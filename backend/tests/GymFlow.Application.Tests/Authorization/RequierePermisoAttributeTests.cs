using System.Security.Claims;
using GymFlow.API.Authorization;
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace GymFlow.Application.Tests.Authorization;

public class RequierePermisoAttributeTests
{
    [Fact]
    public async Task SinAutenticar_DevuelveUnauthorized()
    {
        var attr = new RequierePermisoAttribute(Modulo.Socios, Operacion.Lectura);
        var context = BuildContext(authenticated: false);

        await attr.OnAuthorizationAsync(context);

        Assert.IsType<UnauthorizedResult>(context.Result);
    }

    [Fact]
    public async Task SinClaimRolId_DevuelveForbid()
    {
        var attr = new RequierePermisoAttribute(Modulo.Socios, Operacion.Lectura);
        var context = BuildContext(authenticated: true, rolId: null);

        await attr.OnAuthorizationAsync(context);

        Assert.IsType<ForbidResult>(context.Result);
    }

    [Fact]
    public async Task ConPermiso_NoSeteaResult()
    {
        var rolId = Guid.NewGuid();
        var cache = new Mock<IPermisoCache>();
        cache.Setup(c => c.TienePermisoAsync(rolId, Modulo.Socios, Operacion.Lectura, default))
            .ReturnsAsync(true);

        var attr = new RequierePermisoAttribute(Modulo.Socios, Operacion.Lectura);
        var context = BuildContext(authenticated: true, rolId: rolId, cache: cache.Object);

        await attr.OnAuthorizationAsync(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public async Task SinPermiso_DevuelveForbid()
    {
        var rolId = Guid.NewGuid();
        var cache = new Mock<IPermisoCache>();
        cache.Setup(c => c.TienePermisoAsync(rolId, Modulo.Socios, Operacion.Eliminacion, default))
            .ReturnsAsync(false);

        var attr = new RequierePermisoAttribute(Modulo.Socios, Operacion.Eliminacion);
        var context = BuildContext(authenticated: true, rolId: rolId, cache: cache.Object);

        await attr.OnAuthorizationAsync(context);

        Assert.IsType<ForbidResult>(context.Result);
    }

    private static AuthorizationFilterContext BuildContext(bool authenticated, Guid? rolId = null, IPermisoCache? cache = null)
    {
        var httpContext = new DefaultHttpContext();
        if (authenticated)
        {
            var claims = new List<Claim>();
            if (rolId.HasValue) claims.Add(new Claim("rolId", rolId.Value.ToString()));
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        }
        var services = new ServiceCollection();
        services.AddSingleton(cache ?? Mock.Of<IPermisoCache>());
        httpContext.RequestServices = services.BuildServiceProvider();

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
    }
}
