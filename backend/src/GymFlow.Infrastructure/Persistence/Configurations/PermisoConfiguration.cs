using GymFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymFlow.Infrastructure.Persistence.Configurations;

public class PermisoConfiguration : IEntityTypeConfiguration<Permiso>
{
    public void Configure(EntityTypeBuilder<Permiso> builder)
    {
        builder.ToTable("Permisos");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Modulo).IsRequired().HasConversion<string>().HasMaxLength(50);
        builder.Property(p => p.Operacion).IsRequired().HasConversion<string>().HasMaxLength(50);
        builder.HasIndex(p => new { p.Modulo, p.Operacion }).IsUnique();
    }
}
