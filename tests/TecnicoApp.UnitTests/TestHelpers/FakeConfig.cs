using Microsoft.Extensions.Configuration;

namespace TecnicoApp.UnitTests.TestHelpers;

public static class FakeConfig
{
    // A minimal in-memory IConfiguration carrying just what InvoicePayLinkService needs
    // (Jwt:Secret, reused as the HMAC key for deriving pay tokens — see that class for why).
    public static IConfiguration Create() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "unit-test-secret-at-least-32-characters-long",
            })
            .Build();
}
