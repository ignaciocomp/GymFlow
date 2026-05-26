using GymFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymFlow.Infrastructure.Persistence.Configurations;

public class RolConfiguration : IEntityTypeConfiguration<Rol>
{
    public void Configure(EntityTypeBuilder<Rol> builder)
    {
        builder.ToTable("Roles");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Nombre).IsRequired().HasMaxLength(100);
        builder.HasIndex(r => r.Nombre).IsUnique();
        builder.Property(r => r.EsSistema).IsRequired();
        builder.Property(r => r.FechaCreacion).IsRequired();

        builder.HasMany(r => r.Permisos)
            .WithOne(rp => rp.Rol)
            .HasForeignKey(rp => rp.RolId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
