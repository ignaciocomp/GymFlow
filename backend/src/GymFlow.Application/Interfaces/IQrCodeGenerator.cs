namespace GymFlow.Application.Interfaces;

/// <summary>
/// Genera el QR del URI <c>otpauth://</c> para el alta de MFA. La implementación
/// (Infrastructure, con QRCoder) devuelve un PNG embebido como data URI listo para
/// usar directamente en el <c>src</c> de un <c>&lt;img&gt;</c> del frontend.
/// </summary>
public interface IQrCodeGenerator
{
    /// <summary>
    /// Genera un QR PNG para el contenido dado y lo devuelve como data URI
    /// (<c>data:image/png;base64,...</c>).
    /// </summary>
    string GenerarPngDataUri(string contenido);
}
