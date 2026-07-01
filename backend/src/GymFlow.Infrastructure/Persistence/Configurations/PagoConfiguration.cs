using GymFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymFlow.Infrastructure.Persistence.Configurations;

public class PagoConfiguration : IEntityTypeConfiguration<Pago>
{
    public void Configure(EntityTypeBuilder<Pago> builder)
    {
        builder.ToTable("Pagos");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.SocioId).IsRequired();
        builder.Property(p => p.Monto).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(p => p.Estado).IsRequired();
        builder.Property(p => p.MpPreferenceId).IsRequired().HasMaxLength(100);
        builder.Property(p => p.MpPaymentId).HasMaxLength(100);
        builder.Property(p => p.MedioPago).HasMaxLength(100);
        builder.Property(p => p.FechaCreacion).IsRequired();
        builder.Property(p => p.FechaAcreditacion);

        builder.HasOne(p => p.Cuota)
            .WithMany()
            .HasForeignKey(p => p.CuotaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
