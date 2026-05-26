using GymFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymFlow.Infrastructure.Persistence.Configurations;

public class HorarioClaseConfiguration : IEntityTypeConfiguration<HorarioClase>
{
    public void Configure(EntityTypeBuilder<HorarioClase> builder)
    {
        builder.ToTable("HorariosClase");
        builder.HasKey(h => h.Id);

        builder.Property(h => h.DiaSemana)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(h => h.HoraInicio).IsRequired();
        builder.Property(h => h.HoraFin).IsRequired();
        builder.Property(h => h.Sala).HasMaxLength(100);

        builder.HasOne(h => h.Clase)
            .WithMany()
            .HasForeignKey(h => h.ClaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(h => new { h.ClaseId, h.DiaSemana });
    }
}
