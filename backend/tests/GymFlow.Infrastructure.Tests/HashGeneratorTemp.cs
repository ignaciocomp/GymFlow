using GymFlow.Infrastructure.Services;
using Xunit;
using Xunit.Abstractions;

namespace GymFlow.Infrastructure.Tests;

public class HashGeneratorTemp
{
    private readonly ITestOutputHelper _output;
    public HashGeneratorTemp(ITestOutputHelper output) => _output = output;

    [Fact(Skip = "Run manualmente cuando se necesite regenerar el hash de bootstrap")]
    public void GenerarHashAdmin()
    {
        var hasher = new BCryptPasswordHasher();
        var hash = hasher.Hash("admin123");
        _output.WriteLine($"HASH_ADMIN={hash}");
        Assert.True(hasher.Verify("admin123", hash));
    }
}
