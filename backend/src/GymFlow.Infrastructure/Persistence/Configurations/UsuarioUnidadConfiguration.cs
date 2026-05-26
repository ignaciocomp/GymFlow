using GymFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymFlow.Infrastructure.Persistence.Configurations;

public class UsuarioUnidadConfiguration : IEntityTypeConfiguration<UsuarioUnidad>
{
    public void Configure(EntityTypeBuilder<UsuarioUnidad> builder)
    {
        builder.ToTable("UsuarioUnidades");
        builder.HasKey(uu => new { uu.UsuarioId, uu.UnidadId });

        builder.HasOne(uu => uu.Unidad)
            .WithMany()
            .HasForeignKey(uu => uu.UnidadId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(uu => uu.Plan)
            .WithMany()
            .HasForeignKey(uu => uu.PlanId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
