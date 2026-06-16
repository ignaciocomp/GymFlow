using GymFlow.Domain.Entities;
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
            .HasValue<Socio>("Socio")
            .HasValue<Empleado>("Empleado");

        builder.Property(u => u.Nombre).IsRequired().HasMaxLength(100);
        builder.Property(u => u.Apellido).IsRequired().HasMaxLength(100);
        builder.Property(u => u.Correo).IsRequired().HasMaxLength(200);
        builder.HasIndex(u => u.Correo).IsUnique();
        builder.Property(u => u.PasswordHash).IsRequired(false).HasMaxLength(500);
        builder.Property(u => u.GoogleUserId).IsRequired(false).HasMaxLength(64);
        builder.Property(u => u.EstaActivo).IsRequired();
        builder.Property(u => u.FechaCreacion).IsRequired();

        builder.Property(u => u.RolId).IsRequired(false);
        builder.HasOne(u => u.Rol)
            .WithMany()
            .HasForeignKey(u => u.RolId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(u => u.UnidadesAsignadas)
            .WithOne(uu => uu.Usuario)
            .HasForeignKey(uu => uu.UsuarioId);
    }
}
