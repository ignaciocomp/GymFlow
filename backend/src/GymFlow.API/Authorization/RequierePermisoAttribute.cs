using GymFlow.Application.Interfaces;
using GymFlow.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GymFlow.API.Authorization;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class RequierePermisoAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly Modulo _modulo;
    private readonly Operacion _operacion;

    public RequierePermisoAttribute(Modulo modulo, Operacion operacion)
    {
        _modulo = modulo;
        _operacion = operacion;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (user.Identity is null || !user.Identity.IsAuthenticated)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var rolIdClaim = user.FindFirst("rolId")?.Value;
        if (!Guid.TryParse(rolIdClaim, out var rolId))
        {
            context.Result = new ForbidResult();
            return;
        }

        var cache = context.HttpContext.RequestServices.GetRequiredService<IPermisoCache>();
        var tiene = await cache.TienePermisoAsync(rolId, _modulo, _operacion);
        if (!tiene)
        {
            context.Result = new ForbidResult();
        }
    }
}
