using Atrox.Vectra.Authentication.Api.DataAccess.Contracts.Connections;
using CrossCutting.Crypto;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Atrox.Vectra.Authentication.Api.DataAccess.Connections;

public class ConnectionStringBuilder(IConfiguration configuration, ILogger<ConnectionStringBuilder> log, ICrypto crypto) : IConnectionStringBuilder
{
    private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    private readonly ILogger<ConnectionStringBuilder> _log = log ?? throw new ArgumentNullException(nameof(log));
    private readonly ICrypto _crypto = crypto ?? throw new ArgumentNullException(nameof(crypto));

    public string BuildSqlServerConnectionString()
    {
        return BuildConnectionStringFromSection("ConnectionStrings:SqlServer", "SQL Server");
    }

    public string BuildPostgreSqlConnectionString()
    {
        return BuildConnectionStringFromSection("ConnectionStrings:PostgreSql", "PostgreSQL");
    }

    public string BuildConnectionString(string engine)
    {
        if (string.Equals(engine, "SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            return BuildSqlServerConnectionString();
        }

        if (string.Equals(engine, "PostgreSql", StringComparison.OrdinalIgnoreCase))
        {
            return BuildPostgreSqlConnectionString();
        }

        throw new InvalidOperationException($"Unsupported database engine '{engine}'. Allowed values: SqlServer, PostgreSql.");
    }

    private string BuildConnectionStringFromSection(string sectionPath, string engineDisplayName)
    {
        var settings = _configuration.GetSection(sectionPath);
        if (!settings.Exists())
        {
            throw new InvalidOperationException($"{sectionPath} section was not found in appsettings.json.");
        }

        var connectionStringFormat = settings["ConnectionStringFormat"] ?? string.Empty;
        var server = settings["Server"] ?? string.Empty;
        var port = settings["Port"] ?? string.Empty;
        var userId = settings["UserId"] ?? string.Empty;
        var encryptedPassword = settings["Password"] ?? string.Empty;
        var database = settings["Database"] ?? string.Empty;

        if (string.IsNullOrWhiteSpace(connectionStringFormat))
        {
            throw new InvalidOperationException($"{engineDisplayName} ConnectionStringFormat is missing.");
        }

        if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(database))
        {
            throw new InvalidOperationException($"One or more required {engineDisplayName} connection values are missing.");
        }

        var decryptedPassword = string.IsNullOrWhiteSpace(encryptedPassword)
            ? string.Empty
            : _crypto.Decrypt(encryptedPassword);

        var connectionString = connectionStringFormat
            .Replace("{server}", server)
            .Replace("{port}", port)
            .Replace("{userId}", userId)
            .Replace("{decryptedPassword}", decryptedPassword)
            .Replace("{database}", database);

        _log.LogDebug("{engine} connection string built successfully.", engineDisplayName);
        return connectionString;
    }
}
