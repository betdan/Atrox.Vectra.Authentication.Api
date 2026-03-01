using Atrox.Vectra.Authentication.Api.DataAccess.Contracts.Connections;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace Atrox.Vectra.Authentication.Api.HealthChecks;

public class DatabaseHealthCheck(IConfiguration configuration, IConnectionStringBuilder connectionStringBuilder) : IHealthCheck
{
    private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    private readonly IConnectionStringBuilder _connectionStringBuilder = connectionStringBuilder ?? throw new ArgumentNullException(nameof(connectionStringBuilder));

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var engine = _configuration["Database:Engine"];
            if (string.IsNullOrWhiteSpace(engine))
            {
                return HealthCheckResult.Unhealthy("Database engine is not configured.");
            }

            if (string.Equals(engine, "SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                await CheckSqlServerAsync(cancellationToken).ConfigureAwait(false);
                return HealthCheckResult.Healthy("SQL Server is reachable.");
            }

            if (string.Equals(engine, "PostgreSql", StringComparison.OrdinalIgnoreCase))
            {
                await CheckPostgreSqlAsync(cancellationToken).ConfigureAwait(false);
                return HealthCheckResult.Healthy("PostgreSQL is reachable.");
            }

            return HealthCheckResult.Unhealthy($"Unsupported database engine '{engine}'.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database connectivity check failed.", ex);
        }
    }

    private async Task CheckSqlServerAsync(CancellationToken cancellationToken)
    {
        var connectionString = _connectionStringBuilder.BuildSqlServerConnectionString();
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = new SqlCommand("SELECT 1", connection);
        await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task CheckPostgreSqlAsync(CancellationToken cancellationToken)
    {
        var connectionString = _connectionStringBuilder.BuildPostgreSqlConnectionString();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = new NpgsqlCommand("SELECT 1", connection);
        await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
    }
}
