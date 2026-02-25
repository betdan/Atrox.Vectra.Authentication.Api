using Atrox.Vectra.Authentication.Api.DataAccess.Contracts.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Atrox.Vectra.Authentication.Api.DataAccess.Repositories;

public class ClientRepositoryFactory(IServiceProvider serviceProvider, IConfiguration configuration) : IClientRepositoryFactory
{
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

    public IClientRepository CreateRepository()
    {
        var engine = _configuration["Database:Engine"];
        if (string.Equals(engine, "SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            return _serviceProvider.GetRequiredService<SqlServerClientRepository>();
        }

        if (string.Equals(engine, "PostgreSql", StringComparison.OrdinalIgnoreCase))
        {
            return _serviceProvider.GetRequiredService<PostgreSqlClientRepository>();
        }

        throw new InvalidOperationException("Invalid Database:Engine value. Allowed values: SqlServer, PostgreSql.");
    }
}
