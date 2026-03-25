using GymFlow.Domain.Enums;

namespace GymFlow.Domain.Entities;

public class Socio : Usuario
{
    public Guid? PlanId { get; private set; }
    public Plan? Plan { get; private set; }
    public DateTime FechaAlta { get; private set; }
    public bool ConsentimientoInformado { get; private set; }
    public DateTime? ConsentimientoTimestamp { get; private set; }
    public string? Telefono { get; private set; }
    public string? DocumentoIdentidad { get; private set; }
    public DateTime? FechaNacimiento { get; private set; }
    public string? MotivoBaja { get; private set; }

    private Socio() { } // EF Core

    public Socio(
        string nombre,
        string apellido,
        string correo,
        string passwordHash,
        Guid? planId,
        DateTime fechaAlta,
        bool consentimientoInformado,
        string? telefono = null,
        string? documentoIdentidad = null,
        DateTime? fechaNacimiento = null)
        : base(nombre, apellido, correo, passwordHash, Rol.Socio)
    {
        PlanId = planId;
        FechaAlta = fechaAlta;
        Telefono = telefono;
        DocumentoIdentidad = documentoIdentidad;
        FechaNacimiento = fechaNacimiento;

        if (!consentimientoInformado)
            throw new ArgumentException("Consentimiento informado is required (Ley 18.331).", nameof(consentimientoInformado));

        ConsentimientoInformado = true;
        ConsentimientoTimestamp = DateTime.UtcNow;
    }

    public void DarDeBaja(string? motivo)
    {
        MotivoBaja = motivo;
        Desactivar();
    }

    public void Reactivar()
    {
        MotivoBaja = null;
        Activar();
    }

    public void ActualizarDatosSocio(
        string nombre,
        string apellido,
        string correo,
        Guid? planId,
        string? telefono,
        string? documentoIdentidad,
        DateTime? fechaNacimiento)
    {
        ActualizarDatosBase(nombre, apellido, correo);
        PlanId = planId;
        Telefono = telefono;
        DocumentoIdentidad = documentoIdentidad;
        FechaNacimiento = fechaNacimiento;
    }
}
