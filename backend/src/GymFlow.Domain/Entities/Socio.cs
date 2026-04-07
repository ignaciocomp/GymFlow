using GymFlow.Domain.Enums;

namespace GymFlow.Domain.Entities;

public class Socio : Usuario
{
    public DateTime FechaAlta { get; private set; }
    public bool ConsentimientoInformado { get; private set; }
    public DateTime? ConsentimientoTimestamp { get; private set; }
    public string? Telefono { get; private set; }
    public TipoDocumento TipoDocumento { get; private set; }
    public string? DocumentoIdentidad { get; private set; }
    public DateTime? FechaNacimiento { get; private set; }
    public string? MotivoBaja { get; private set; }

    private Socio() { } // EF Core

    public Socio(
        string nombre,
        string apellido,
        string correo,
        string passwordHash,
        DateTime fechaAlta,
        bool consentimientoInformado,
        TipoDocumento tipoDocumento,
        string? telefono = null,
        string? documentoIdentidad = null,
        DateTime? fechaNacimiento = null)
        : base(nombre, apellido, correo, passwordHash, Rol.Socio)
    {
        FechaAlta = fechaAlta;
        Telefono = telefono;
        FechaNacimiento = fechaNacimiento;

        if (!consentimientoInformado)
            throw new ArgumentException("Consentimiento informado is required (Ley 18.331).", nameof(consentimientoInformado));

        ConsentimientoInformado = true;
        ConsentimientoTimestamp = DateTime.UtcNow;

        ValidarDocumento(tipoDocumento, documentoIdentidad);
        TipoDocumento = tipoDocumento;
        DocumentoIdentidad = documentoIdentidad;
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
        TipoDocumento tipoDocumento,
        string? telefono,
        string? documentoIdentidad,
        DateTime? fechaNacimiento)
    {
        ActualizarDatosBase(nombre, apellido, correo);
        Telefono = telefono;
        FechaNacimiento = fechaNacimiento;

        ValidarDocumento(tipoDocumento, documentoIdentidad);
        TipoDocumento = tipoDocumento;
        DocumentoIdentidad = documentoIdentidad;
    }

    private static void ValidarDocumento(TipoDocumento tipoDocumento, string? documentoIdentidad)
    {
        if (tipoDocumento != TipoDocumento.CI)
            return;

        if (string.IsNullOrWhiteSpace(documentoIdentidad))
            throw new ArgumentException(
                "El documento de identidad es obligatorio cuando el tipo es CI.",
                nameof(documentoIdentidad));

        if (!EsCedulaUruguayaValida(documentoIdentidad))
            throw new ArgumentException(
                "El número de cédula de identidad uruguaya no es válido.",
                nameof(documentoIdentidad));
    }

    private static bool EsCedulaUruguayaValida(string doc)
    {
        // Normalizar: eliminar puntos y guiones
        var normalizado = doc.Replace(".", "").Replace("-", "").Trim();

        // Debe tener 7 u 8 dígitos numéricos
        if (normalizado.Length is < 7 or > 8 || !normalizado.All(char.IsDigit))
            return false;

        // Paddear a 8 dígitos con cero a la izquierda si tiene 7
        if (normalizado.Length == 7)
            normalizado = "0" + normalizado;

        // Pesos para los primeros 7 dígitos
        int[] weights = [2, 9, 8, 7, 6, 3, 4];

        int sum = 0;
        for (int i = 0; i < 7; i++)
            sum += (normalizado[i] - '0') * weights[i];

        int checkDigit = normalizado[7] - '0';
        return (sum + checkDigit) % 10 == 0;
    }
}
