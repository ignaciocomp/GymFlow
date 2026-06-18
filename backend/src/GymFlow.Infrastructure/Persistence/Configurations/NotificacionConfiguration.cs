using GymFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymFlow.Infrastructure.Persistence.Configurations;

public class NotificacionConfiguration : IEntityTypeConfiguration<Notificacion>
{
    public void Configure(EntityTypeBuilder<Notificacion> builder)
    {
        builder.ToTable("Notificaciones");
        builder.HasKey(n => n.Id);

        builder.Property(n => n.Tipo).IsRequired();
        builder.Property(n => n.Titulo).IsRequired().HasMaxLength(200);
        builder.Property(n => n.Mensaje).IsRequired().HasMaxLength(1000);
        builder.Property(n => n.Leida).IsRequired();
        builder.Property(n => n.FechaCreacion).IsRequired();
        builder.Property(n => n.FechaLectura);

        builder.HasOne(n => n.Socio)
            .WithMany()
            .HasForeignKey(n => n.SocioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(n => new { n.SocioId, n.Leida });
        builder.HasIndex(n => n.FechaCreacion);
    }
}
