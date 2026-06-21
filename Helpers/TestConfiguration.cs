using Microsoft.Extensions.Configuration;

namespace Shopping.Tests.Helpers
{
    public static class TestConfiguration
    {
        public static IConfiguration Build()
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "JwtService:Key", "ThisIsASecretKeyForTestingPurposes123!" },
                    { "JwtService:Duration", "60" },
                    { "JwtService:RefreshTokenDurationDays", "7" }
                })
                .Build();
        }
    }
}