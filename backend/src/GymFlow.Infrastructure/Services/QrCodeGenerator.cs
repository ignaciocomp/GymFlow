using GymFlow.Application.Interfaces;
using QRCoder;

namespace GymFlow.Infrastructure.Services;

/// <summary>
/// Genera el QR del URI <c>otpauth://</c> con QRCoder y lo devuelve como data URI
/// PNG (<c>data:image/png;base64,...</c>), para que el frontend lo muestre sin
/// pedir el binario aparte. El secreto viaja dentro del QR pero NUNCA se loguea.
/// </summary>
public class QrCodeGenerator : IQrCodeGenerator
{
    public string GenerarPngDataUri(string contenido)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(contenido, QRCodeGenerator.ECCLevel.Q);
        var pngQr = new PngByteQRCode(data);
        var png = pngQr.GetGraphic(10);
        return $"data:image/png;base64,{Convert.ToBase64String(png)}";
    }
}
