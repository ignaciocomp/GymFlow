using GymFlow.Application.Common;

namespace GymFlow.Application.Tests.Common;

public class GeneradorPasswordTests
{
    [Fact]
    public void Generar_CumpleLongitudYComposicion()
    {
        var pw = GeneradorPassword.Generar();
        Assert.True(pw.Length >= 12);
        Assert.Contains(pw, char.IsUpper);
        Assert.Contains(pw, char.IsLower);
        Assert.Contains(pw, char.IsDigit);
    }

    [Fact]
    public void Generar_ProducePasswordsDistintos()
    {
        Assert.NotEqual(GeneradorPassword.Generar(), GeneradorPassword.Generar());
    }
}
