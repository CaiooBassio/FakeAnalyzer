using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Pomelo.EntityFrameworkCore.MySql;

namespace InstaFakeAnalyzer.Data
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var provider = configuration["DatabaseProvider"] ?? "Postgres";
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

            switch (provider.ToLowerInvariant())
            {
                case "sqlite":
                    optionsBuilder.UseSqlite(connectionString);
                    break;

                case "mysql":
                    optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
                    break;

                case "postgres":



                case "postgresql":



                default:
                    optionsBuilder.UseNpgsql(connectionString);
                    break;
            }

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}