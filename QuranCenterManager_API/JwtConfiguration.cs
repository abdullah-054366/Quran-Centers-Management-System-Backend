using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace QuranCenterManager_API.Configuration
{
    public static class JwtConfiguration
    {
        public static string GetSecret(
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            var secret = configuration["Jwt:Secret"];

            if (string.IsNullOrWhiteSpace(secret))
                throw new InvalidOperationException("Configuration value 'Jwt:Secret' is required. Set it in appsettings.Local.json (see appsettings.Local.example.json) or the Jwt__Secret environment variable.");

            if (Encoding.UTF8.GetByteCount(secret) < 32)
                throw new InvalidOperationException("Configuration value 'Jwt:Secret' must be at least 32 bytes.");

            return secret;
        }
    }
}
