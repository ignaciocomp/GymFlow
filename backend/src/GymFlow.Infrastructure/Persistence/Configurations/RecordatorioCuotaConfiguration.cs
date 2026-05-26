using GymFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymFlow.Infrastructure.Persistence.Configurations;

public class RecordatorioCuotaConfiguration : IEntityTypeConfiguration<RecordatorioCuota>
{
    public void Configure(EntityTypeBuilder<RecordatorioCuota> builder)
    {
        builder.ToTable("RecordatoriosCuota");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.TipoRecordatorio).IsRequired().HasConversion<string>();
        builder.Property(r => r.FechaEnvio).IsRequired();
        builder.Property(r => r.Exitoso).IsRequired();
        builder.Property(r => r.Error).HasMaxLength(500);

        builder.HasOne(r => r.Cuota)
            .WithMany()
            .HasForeignKey(r => r.CuotaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Socio)
            .WithMany()
            .HasForeignKey(r => r.SocioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => new { r.CuotaId, r.TipoRecordatorio, r.FechaEnvio });
    }
}
