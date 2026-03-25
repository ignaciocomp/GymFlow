using GymFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymFlow.Infrastructure.Persistence.Configurations;

public class SocioConfiguration : IEntityTypeConfiguration<Socio>
{
    public void Configure(EntityTypeBuilder<Socio> builder)
    {
        builder.Property(s => s.FechaAlta).IsRequired();
        builder.Property(s => s.ConsentimientoInformado).IsRequired();
        builder.Property(s => s.ConsentimientoTimestamp);
        builder.Property(s => s.Telefono).HasMaxLength(50);
        builder.Property(s => s.DocumentoIdentidad).HasMaxLength(50);
        builder.Property(s => s.FechaNacimiento);
        builder.Property(s => s.MotivoBaja).HasMaxLength(500);

        builder.HasOne(s => s.Plan)
            .WithMany()
            .HasForeignKey(s => s.PlanId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
