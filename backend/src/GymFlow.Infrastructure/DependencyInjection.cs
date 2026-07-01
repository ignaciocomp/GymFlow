using GymFlow.Application.Interfaces;
using GymFlow.Infrastructure.Persistence;
using GymFlow.Infrastructure.Repositories;
using GymFlow.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GymFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<GymFlowDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Factory para contextos efímeros aislados (lo usa INotificadorInApp para commitear
        // sus notificaciones sin flushear cambios de negocio pendientes del contexto scoped).
        services.AddDbContextFactory<GymFlowDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")),
            lifetime: ServiceLifetime.Scoped);

        services.AddScoped<IUnidadRepository, UnidadRepository>();
        services.AddScoped<ISocioRepository, SocioRepository>();
        services.AddScoped<IPlanRepository, PlanRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IAuditLogger, AuditLogger>();
        services.AddScoped<IRolRepository, RolRepository>();
        services.AddScoped<IPermisoRepository, PermisoRepository>();
        services.AddScoped<IPermisoCache, PermisoCache>();
        services.AddScoped<IEmpleadoRepository, EmpleadoRepository>();
        services.AddScoped<IUnidadesVisiblesResolver, UnidadesVisiblesResolver>();
        services.AddScoped<ICuotaRepository, CuotaRepository>();
        services.AddScoped<IPagoRepository, PagoRepository>();
        services.AddHttpClient<IMercadoPagoService, MercadoPagoService>();
        services.AddScoped<IPagoUrlBuilder, PagoUrlBuilder>();
        services.AddScoped<ICuotaGeneradorService, CuotaGeneradorService>();
        services.AddScoped<IRecordatorioCuotaRepository, RecordatorioCuotaRepository>();
        services.AddScoped<IClaseRepository, ClaseRepository>();
        services.AddScoped<IHorarioClaseRepository, HorarioClaseRepository>();
        services.AddScoped<IInscripcionClaseRepository, InscripcionClaseRepository>();
        services.AddScoped<IEventoRepository, EventoRepository>();
        services.AddScoped<INotificacionRepository, NotificacionRepository>();
        services.AddScoped<INotificadorInApp, NotificadorInApp>();
        services.AddScoped<IEmailService, SmtpEmailService>();
        services.AddScoped<IGoogleTokenValidator, GoogleIdTokenValidator>();
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<ICodigoRecuperacionMfaRepository, CodigoRecuperacionMfaRepository>();
        services.AddScoped<ITotpService, TotpService>();
        services.AddSingleton<IMfaSecretProtector, AesGcmMfaSecretProtector>();
        services.AddSingleton<IMfaTokenService, MfaTokenService>();
        services.AddSingleton<IQrCodeGenerator, QrCodeGenerator>();
        services.AddMemoryCache();

        return services;
    }
}
