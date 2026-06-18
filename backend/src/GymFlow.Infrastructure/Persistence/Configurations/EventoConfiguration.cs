using GymFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymFlow.Infrastructure.Persistence.Configurations;

public class EventoConfiguration : IEntityTypeConfiguration<Evento>
{
    public void Configure(EntityTypeBuilder<Evento> builder)
    {
        builder.ToTable("Eventos");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Titulo).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Descripcion).HasMaxLength(1000);
        builder.Property(e => e.Fecha).IsRequired();
        builder.Property(e => e.EstaActivo).IsRequired();
        builder.Property(e => e.FechaCreacion).IsRequired();

        builder.HasOne(e => e.Unidad)
            .WithMany()
            .HasForeignKey(e => e.UnidadId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.UnidadId);
        builder.HasIndex(e => e.Fecha);
    }
}
