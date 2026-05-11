using GymFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymFlow.Infrastructure.Persistence.Configurations;

public class CuotaConfiguration : IEntityTypeConfiguration<Cuota>
{
    public void Configure(EntityTypeBuilder<Cuota> builder)
    {
        builder.ToTable("Cuotas");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.NombrePlan).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Monto).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(c => c.FechaEmision).IsRequired();
        builder.Property(c => c.FechaVencimiento).IsRequired();
        builder.Property(c => c.Estado).IsRequired();
        builder.Property(c => c.FechaPago);
        builder.Property(c => c.FechaBaja);

        builder.HasOne(c => c.Socio)
            .WithMany()
            .HasForeignKey(c => c.SocioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Unidad)
            .WithMany()
            .HasForeignKey(c => c.UnidadId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Plan)
            .WithMany()
            .HasForeignKey(c => c.PlanId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
