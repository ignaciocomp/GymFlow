using GymFlow.Domain.Constants;
using GymFlow.Domain.Enums;

namespace GymFlow.Domain.Entities;

public class HorarioClase
{
    public Guid Id { get; private set; }
    public Guid ClaseId { get; private set; }
    public Clase Clase { get; private set; } = null!;
    public DiaSemana DiaSemana { get; private set; }
    public TimeOnly HoraInicio { get; private set; }
    public TimeOnly HoraFin { get; private set; }
    public string? Sala { get; private set; }

    private HorarioClase() { } // EF Core

    public HorarioClase(Guid claseId, DiaSemana diaSemana, TimeOnly horaInicio, TimeOnly horaFin, string? sala)
    {
        Id = Guid.NewGuid();
        ClaseId = claseId;
        DiaSemana = diaSemana;

        ValidarRango(horaInicio, horaFin);

        HoraInicio = horaInicio;
        HoraFin = horaFin;
        Sala = string.IsNullOrWhiteSpace(sala) ? null : sala.Trim();
    }

    public void Actualizar(DiaSemana diaSemana, TimeOnly horaInicio, TimeOnly horaFin, string? sala)
    {
        DiaSemana = diaSemana;

        ValidarRango(horaInicio, horaFin);

        HoraInicio = horaInicio;
        HoraFin = horaFin;
        Sala = string.IsNullOrWhiteSpace(sala) ? null : sala.Trim();
    }

    private static void ValidarRango(TimeOnly horaInicio, TimeOnly horaFin)
    {
        if (horaFin <= horaInicio)
            throw new ArgumentException("La hora de fin debe ser posterior a la hora de inicio.");

        // E2E-18 parte 2: los horarios deben caer dentro del horario de apertura del gimnasio.
        if (horaInicio < HorarioApertura.Apertura || horaFin > HorarioApertura.Cierre)
            throw new ArgumentException("El horario debe estar dentro del horario de apertura (07:00 a 22:00).");
    }

    /// <summary>
    /// Verifica si este horario se solapa con otro en la misma sede y sala.
    /// Dos horarios se solapan si son el mismo día y las franjas horarias se cruzan.
    /// </summary>
    public bool SeSolapaCon(HorarioClase otro)
    {
        if (DiaSemana != otro.DiaSemana) return false;
        if (Sala == null || otro.Sala == null) return false;
        if (!string.Equals(Sala, otro.Sala, StringComparison.OrdinalIgnoreCase)) return false;

        return HoraInicio < otro.HoraFin && otro.HoraInicio < HoraFin;
    }
}
