using GymFlow.Application.UseCases.Auditoria;
using GymFlow.Application.UseCases.Planes;
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
        services.AddScoped<ReactivatePlanCommand>();
        services.AddScoped<GetSocioByIdQuery>();
        services.AddScoped<UpdateSocioCommand>();
        services.AddScoped<ReactivateSocioCommand>();
        services.AddScoped<GetAuditoriaQuery>();
        return services;
    }
}
