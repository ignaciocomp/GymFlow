using GymFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymFlow.Infrastructure.Persistence.Configurations;

public class CodigoRecuperacionMfaConfiguration : IEntityTypeConfiguration<CodigoRecuperacionMfa>
{
    public void Configure(EntityTypeBuilder<CodigoRecuperacionMfa> builder)
    {
        builder.ToTable("CodigosRecuperacionMfa");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.EmpleadoId).IsRequired();
        builder.Property(c => c.CodigoHash).IsRequired().HasMaxLength(500);
        builder.Property(c => c.Usado).IsRequired();
        builder.Property(c => c.FechaUso);

        // FK al empleado (TPH sobre la tabla Usuarios). El empleado no expone navegación inversa.
        builder.HasOne<Empleado>()
            .WithMany()
            .HasForeignKey(c => c.EmpleadoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.EmpleadoId);
    }
}
