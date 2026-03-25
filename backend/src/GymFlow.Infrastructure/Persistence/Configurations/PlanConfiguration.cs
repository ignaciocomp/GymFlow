using GymFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymFlow.Infrastructure.Persistence.Configurations;

public class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> builder)
    {
        builder.ToTable("Planes");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Nombre).IsRequired().HasMaxLength(100);
        builder.Property(p => p.Precio).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(p => p.Descripcion).HasMaxLength(500);
        builder.Property(p => p.EstaActivo).IsRequired();

        builder.HasOne(p => p.Unidad)
            .WithMany()
            .HasForeignKey(p => p.UnidadId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
