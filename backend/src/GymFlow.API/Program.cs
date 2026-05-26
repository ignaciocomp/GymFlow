using System.Text;
using GymFlow.API;
using GymFlow.Domain.Constants;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using GymFlow.Infrastructure;
using GymFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddHostedService<GymFlow.API.BackgroundServices.CuotaGeneracionBackgroundService>();
builder.Services.AddHostedService<GymFlow.API.BackgroundServices.RecordatorioBackgroundService>();

// JWT authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "GymFlowDevSecretKey2026!SuperSecure";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        policy.WithOrigins(origins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Auto-apply pending migrations on startup + seed data
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GymFlowDbContext>();
    db.Database.Migrate();

    if (!db.Unidades.Any())
    {
        var espacioMora = new Unidad("Espacio Mora", "Av. 8 de Octubre 2845");
        var gimnasioNM = new Unidad("Gimnasio Nuevo Malvín", "Av. Italia 5765");
        db.Unidades.AddRange(espacioMora, gimnasioNM);
        db.SaveChanges();
    }

    if (!db.Planes.Any())
    {
        var espacioMora = db.Unidades.First(u => u.Nombre == "Espacio Mora");
        var gimnasioNM = db.Unidades.First(u => u.Nombre == "Gimnasio Nuevo Malvín");

        db.Planes.AddRange(
            new Plan("Plan Musculación", 2500, "Acceso a sala de musculación", espacioMora.Id),
            new Plan("Plan Completo", 3500, "Acceso a todas las actividades", espacioMora.Id),
            new Plan("Plan Musculación", 2500, "Acceso a sala de musculación", gimnasioNM.Id),
            new Plan("Plan Completo", 3500, "Acceso a todas las actividades", gimnasioNM.Id),
            new Plan("Plan Libre", 4500, "Acceso a ambas sedes y todas las actividades", gimnasioNM.Id)
        );
        db.SaveChanges();
    }

    // Seed socio de prueba vinculado al usuario hardcodeado socio@gymflow.com
    if (!db.Socios.Any(s => s.Correo == "socio@gymflow.com"))
    {
        var hasher = scope.ServiceProvider.GetRequiredService<GymFlow.Application.Interfaces.IPasswordHasher>();
        var unidad = db.Unidades.First(u => u.Nombre == "Espacio Mora");
        var plan = db.Planes.First(p => p.UnidadId == unidad.Id && p.Nombre == "Plan Musculación");

        var socio = new Socio(
            rolSocioId: RolesSeed.SocioRolId,
            nombre: "María",
            apellido: "López",
            correo: "socio@gymflow.com",
            passwordHash: hasher.Hash("socio123"),
            fechaAlta: new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            consentimientoInformado: true,
            tipoDocumento: TipoDocumento.CI,
            telefono: "099 123 456",
            documentoIdentidad: "12345672",
            fechaNacimiento: new DateTime(1992, 6, 20, 0, 0, 0, DateTimeKind.Utc));

        socio.UnidadesAsignadas.Add(new UsuarioUnidad(socio.Id, unidad.Id, plan.Id));
        db.Socios.Add(socio);
        db.SaveChanges();

        if (!db.Set<GymFlow.Domain.Entities.Cuota>().Any(c => c.SocioId == socio.Id))
        {
            var cuotaSeed = new GymFlow.Domain.Entities.Cuota(
                socioId: socio.Id,
                unidadId: unidad.Id,
                planId: plan.Id,
                nombrePlan: plan.Nombre,
                monto: plan.Precio,
                fechaEmision: socio.FechaAlta);
            db.Set<GymFlow.Domain.Entities.Cuota>().Add(cuotaSeed);
            db.SaveChanges();
        }
    }

    var socioSeed = db.Socios.FirstOrDefault(s => s.Correo == "socio@gymflow.com");
    if (socioSeed is not null && !db.Set<GymFlow.Domain.Entities.Cuota>().Any(c => c.SocioId == socioSeed.Id))
    {
        var asignacionSeed = db.UsuarioUnidades.FirstOrDefault(uu => uu.UsuarioId == socioSeed.Id && uu.PlanId.HasValue);
        if (asignacionSeed is not null)
        {
            var planSeed = db.Planes.First(p => p.Id == asignacionSeed.PlanId!.Value);
            var cuotaSeed = new GymFlow.Domain.Entities.Cuota(
                socioId: socioSeed.Id,
                unidadId: asignacionSeed.UnidadId,
                planId: planSeed.Id,
                nombrePlan: planSeed.Nombre,
                monto: planSeed.Precio,
                fechaEmision: socioSeed.FechaAlta);
            db.Set<GymFlow.Domain.Entities.Cuota>().Add(cuotaSeed);
            db.SaveChanges();
        }
    }
}

// Middleware global de manejo de excepciones: captura cualquier excepción no manejada
// en los controllers y devuelve un 500 con un mensaje genérico, evitando exponer
// detalles internos (stack trace, tipos de excepción, etc.) al cliente.
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            error = "Ocurrió un error interno. Intente nuevamente."
        });
    });
});

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

app.MapFallbackToFile("index.html");

app.Run();

