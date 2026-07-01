using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using GymFlow.Application.Interfaces;
using GymFlow.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace GymFlow.Infrastructure.Tests.Services;

public class MercadoPagoServiceFirmaTests
{
    private static MercadoPagoService CrearServicio(string? secret)
    {
        var settings = new Dictionary<string, string?>
        {
            ["MercadoPago:WebhookSecret"] = secret,
            ["MercadoPago:AccessToken"] = "test-token",
            ["MercadoPago:BackUrlBase"] = "http://localhost:5173"
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var httpClient = new HttpClient();
        return new MercadoPagoService(httpClient, config, NullLogger<MercadoPagoService>.Instance);
    }

    // Calcula el v1 esperado tal como lo haría Mercado Pago: HMAC-SHA256 en hex
    // del manifest "id:{dataId};request-id:{requestId};ts:{ts};".
    private static string HmacHex(string secret, string manifest)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(manifest));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    [Fact]
    public void ValidarFirma_ConV1Correcto_DevuelveTrue()
    {
        var secret = "clave-de-prueba";
        var svc = CrearServicio(secret);
        var dataId = "12345";
        var requestId = "req-abc";
        var ts = "1700000000";
        var manifest = $"id:{dataId};request-id:{requestId};ts:{ts};";
        var v1 = HmacHex(secret, manifest);
        var xSignature = $"ts={ts},v1={v1}";

        Assert.True(svc.ValidarFirma(xSignature, requestId, dataId));
    }

    [Fact]
    public void ValidarFirma_V1EnMayusculas_DevuelveTrue_CaseInsensitive()
    {
        var secret = "clave-de-prueba";
        var svc = CrearServicio(secret);
        var dataId = "12345";
        var requestId = "req-abc";
        var ts = "1700000000";
        var manifest = $"id:{dataId};request-id:{requestId};ts:{ts};";
        var v1 = HmacHex(secret, manifest).ToUpperInvariant();
        var xSignature = $"ts={ts},v1={v1}";

        Assert.True(svc.ValidarFirma(xSignature, requestId, dataId));
    }

    [Fact]
    public void ValidarFirma_ConV1Alterado_DevuelveFalse()
    {
        var secret = "clave-de-prueba";
        var svc = CrearServicio(secret);
        var dataId = "12345";
        var requestId = "req-abc";
        var ts = "1700000000";
        var manifest = $"id:{dataId};request-id:{requestId};ts:{ts};";
        var v1 = HmacHex(secret, manifest);
        // Alterar el último caracter para simular una firma inválida (tampering).
        var tampered = v1.Substring(0, v1.Length - 1) + (v1[^1] == 'a' ? 'b' : 'a');
        var xSignature = $"ts={ts},v1={tampered}";

        Assert.False(svc.ValidarFirma(xSignature, requestId, dataId));
    }

    [Fact]
    public void ValidarFirma_ConDataIdDistinto_DevuelveFalse()
    {
        var secret = "clave-de-prueba";
        var svc = CrearServicio(secret);
        var requestId = "req-abc";
        var ts = "1700000000";
        var manifest = $"id:12345;request-id:{requestId};ts:{ts};";
        var v1 = HmacHex(secret, manifest);
        var xSignature = $"ts={ts},v1={v1}";

        // El dataId real es otro → el manifest no coincide → firma inválida.
        Assert.False(svc.ValidarFirma(xSignature, requestId, "99999"));
    }

    [Theory]
    [InlineData("no-tiene-formato")]
    [InlineData("v1=abc")]
    [InlineData("ts=1700000000")]
    [InlineData("ts=1700000000,v1=")]
    [InlineData("")]
    public void ValidarFirma_ConHeaderMalformado_DevuelveFalse(string xSignature)
    {
        var svc = CrearServicio("clave-de-prueba");
        Assert.False(svc.ValidarFirma(xSignature, "req-abc", "12345"));
    }

    [Fact]
    public void ValidarFirma_ConHeaderNull_DevuelveFalse()
    {
        var svc = CrearServicio("clave-de-prueba");
        Assert.False(svc.ValidarFirma(null, "req-abc", "12345"));
    }

    [Fact]
    public void ValidarFirma_SinSecretConfigurado_DevuelveFalse()
    {
        var svc = CrearServicio(null);
        var dataId = "12345";
        var requestId = "req-abc";
        var ts = "1700000000";
        var manifest = $"id:{dataId};request-id:{requestId};ts:{ts};";
        var v1 = HmacHex("cualquier-cosa", manifest);
        var xSignature = $"ts={ts},v1={v1}";

        Assert.False(svc.ValidarFirma(xSignature, requestId, dataId));
    }

    [Fact]
    public void ValidarFirma_SinRequestId_DevuelveFalse()
    {
        var secret = "clave-de-prueba";
        var svc = CrearServicio(secret);
        var dataId = "12345";
        var ts = "1700000000";
        // Un manifest sin request-id no coincide con el patrón esperado.
        var manifest = $"id:{dataId};request-id:;ts:{ts};";
        var v1 = HmacHex(secret, manifest);
        var xSignature = $"ts={ts},v1={v1}";

        Assert.False(svc.ValidarFirma(xSignature, null, dataId));
    }
}
