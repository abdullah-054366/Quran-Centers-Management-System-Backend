using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Data_Access_Layer.Data
{
    public static class DatabaseContext
    {
        public static HalaqaDbContext Create()
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(
                    "appsettings.json",
                    optional: true,
                    reloadOnChange: true
                )
                .Build();

            // Docker injects the connection string through the standard
            // ConnectionStrings__DefaultConnection environment variable.
            string? connectionString = Environment.GetEnvironmentVariable(
                "ConnectionStrings__DefaultConnection");

            connectionString ??= configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Connection string 'DefaultConnection' not found."
                );
            }

            var options = new DbContextOptionsBuilder<HalaqaDbContext>()
                .UseNpgsql(connectionString)
                .Options;

            return new HalaqaDbContext(options);
        }

        public static bool TestDatabase()
        {
            using var context = Create();

            return context.Database.CanConnect();


        }
    }
}
