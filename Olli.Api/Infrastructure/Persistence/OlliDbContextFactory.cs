using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Olli.Api.Domain.Entities;

namespace Olli.Api.Infrastructure.Persistence;

public class OlliDbContextFactory : IDesignTimeDbContextFactory<OlliDb>
{
    public OlliDb CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("FiapOracle")
                               ?? throw new InvalidOperationException("Connection string FiapOracle nao configurada.");

        var options = new DbContextOptionsBuilder<OlliDb>()
            .UseOracle(connectionString)
            .Options;

        return new OlliDb(options);
    }
}
