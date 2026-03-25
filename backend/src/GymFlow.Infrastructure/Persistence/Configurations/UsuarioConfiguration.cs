using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymFlow.Infrastructure.Persistence.Configurations;

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("Usuarios");
        builder.HasKey(u => u.Id);

        // TPH discriminator
        builder.HasDiscriminator<string>("TipoUsuario")
            .HasValue<Socio>("Socio");

        builder.Property(u => u.Nombre).IsRequired().HasMaxLength(100);
        builder.Property(u => u.Apellido).IsRequired().HasMaxLength(100);
        builder.Property(u => u.Correo).IsRequired().HasMaxLength(200);
        builder.HasIndex(u => u.Correo).IsUnique();
        builder.Property(u => u.PasswordHash).IsRequired().HasMaxLength(500);
        builder.Property(u => u.Rol).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(u => u.EstaActivo).IsRequired();
        builder.Property(u => u.FechaCreacion).IsRequired();

        // N:M with Unidad via UsuarioUnidad
        builder.HasMany(u => u.UnidadesAsignadas)
            .WithOne(uu => uu.Usuario)
            .HasForeignKey(uu => uu.UsuarioId);
    }
}
