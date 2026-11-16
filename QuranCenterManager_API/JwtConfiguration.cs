using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace QuranCenterManager_API.Configuration
{
    public static class JwtConfiguration
    {
        private const string DevelopmentSecret = "THIS_IS_A_VERY_SECRET_KEY_123456";

        public static string GetSecret(
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            var secret = configuration["Jwt:Secret"];

            if (string.IsNullOrWhiteSpace(secret) && environment.IsDevelopment())
                secret = DevelopmentSecret;

            if (string.IsNullOrWhiteSpace(secret))
                throw new InvalidOperationException("Configuration value 'Jwt:Secret' is required.");

            if (Encoding.UTF8.GetByteCount(secret) < 32)
                throw new InvalidOperationException("Configuration value 'Jwt:Secret' must be at least 32 bytes.");

            return secret;
        }
    }
}
