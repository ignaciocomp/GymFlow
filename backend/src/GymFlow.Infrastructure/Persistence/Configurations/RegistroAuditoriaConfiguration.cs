using GymFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymFlow.Infrastructure.Persistence.Configurations;

public class RegistroAuditoriaConfiguration : IEntityTypeConfiguration<RegistroAuditoria>
{
    public void Configure(EntityTypeBuilder<RegistroAuditoria> builder)
    {
        builder.ToTable("RegistrosAuditoria");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.UsuarioId).IsRequired();
        builder.Property(r => r.UsuarioNombre).IsRequired().HasMaxLength(200);
        builder.Property(r => r.TipoAccion).IsRequired().HasConversion<string>().HasMaxLength(50);
        builder.Property(r => r.EntidadAfectada).IsRequired().HasMaxLength(100);
        builder.Property(r => r.Descripcion).IsRequired().HasMaxLength(500);
        builder.Property(r => r.DetallesCambios).HasMaxLength(4000);
        builder.Property(r => r.FechaHora).IsRequired();

        builder.HasIndex(r => r.FechaHora).IsDescending();
        builder.HasIndex(r => r.UsuarioId);
        builder.HasIndex(r => r.EntidadId);
    }
}
