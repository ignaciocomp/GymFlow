using GymFlow.Application.UseCases.Auditoria;
using GymFlow.Application.UseCases.Permisos;
using GymFlow.Application.UseCases.Planes;
using GymFlow.Application.UseCases.Roles;
using GymFlow.Application.UseCases.Socios;
using GymFlow.Application.UseCases.Unidades;

namespace GymFlow.API;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<GetUnidadesQuery>();
        services.AddScoped<CreateSocioCommand>();
        services.AddScoped<GetSociosQuery>();
        services.AddScoped<DeleteSocioCommand>();
        services.AddScoped<GetPlanesQuery>();
        services.AddScoped<GetPlanByIdQuery>();
        services.AddScoped<CreatePlanCommand>();
        services.AddScoped<UpdatePlanCommand>();
        services.AddScoped<DeletePlanCommand>();
        services.AddScoped<GetSocioByIdQuery>();
        services.AddScoped<UpdateSocioCommand>();
        services.AddScoped<ReactivateSocioCommand>();
        services.AddScoped<GetAuditoriaQuery>();
        services.AddScoped<GetRolesQuery>();
        services.AddScoped<GetRolByIdQuery>();
        services.AddScoped<GetPermisosQuery>();
        services.AddScoped<CrearRolCommand>();
        services.AddScoped<ActualizarRolCommand>();
        services.AddScoped<EliminarRolCommand>();
        return services;
    }
}
