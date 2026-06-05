using GymFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymFlow.Infrastructure.Persistence.Configurations;

public class InscripcionClaseConfiguration : IEntityTypeConfiguration<InscripcionClase>
{
    public void Configure(EntityTypeBuilder<InscripcionClase> builder)
    {
        builder.ToTable("InscripcionesClase");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.FechaInscripcion).IsRequired();
        builder.Property(i => i.EstaActiva).IsRequired();
        builder.Property(i => i.EsListaEspera).IsRequired().HasDefaultValue(false);

        builder.HasOne(i => i.HorarioClase)
            .WithMany()
            .HasForeignKey(i => i.HorarioClaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Socio)
            .WithMany()
            .HasForeignKey(i => i.SocioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
