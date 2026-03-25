using GymFlow.API;
using GymFlow.Domain.Entities;
using GymFlow.Infrastructure;
using GymFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

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
}

app.UseCors("AllowFrontend");
app.MapControllers();

app.Run();
