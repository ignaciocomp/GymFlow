using GymFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymFlow.Infrastructure.Persistence.Configurations;

public class ClaseConfiguration : IEntityTypeConfiguration<Clase>
{
    public void Configure(EntityTypeBuilder<Clase> builder)
    {
        builder.ToTable("Clases");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Nombre).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Descripcion).HasMaxLength(500);
        builder.Property(c => c.CapacidadMaxima).IsRequired();
        builder.Property(c => c.DuracionMinutos).IsRequired();
        builder.Property(c => c.Instructor).IsRequired().HasMaxLength(200);
        builder.Property(c => c.EstaActivo).IsRequired();

        builder.HasOne(c => c.Unidad)
            .WithMany()
            .HasForeignKey(c => c.UnidadId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.Inscripciones)
            .WithOne(i => i.Clase)
            .HasForeignKey(i => i.ClaseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
