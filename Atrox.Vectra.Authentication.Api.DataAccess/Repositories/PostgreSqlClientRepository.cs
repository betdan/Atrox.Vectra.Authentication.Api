using Atrox.Vectra.Authentication.Api.Business.Models.Auth;
using Atrox.Vectra.Authentication.Api.DataAccess.Contracts.Connections;
using Atrox.Vectra.Authentication.Api.DataAccess.Contracts.Repositories;
using Npgsql;

namespace Atrox.Vectra.Authentication.Api.DataAccess.Repositories;

public class PostgreSqlClientRepository(IConnectionStringBuilder connectionStringBuilder) : IClientRepository
{
    private readonly string _connectionString = connectionStringBuilder.BuildPostgreSqlConnectionString();

    public async Task<AuthClient> GetByApiKeyHashAsync(string apiKeyHash, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT client_id, company_id, client_name, is_active, created_at, expires_at, last_used_at
            FROM ATROX.atrox_security_client
            WHERE api_key_hash = @api_key_hash
            """;

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@api_key_hash", apiKeyHash);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        return new AuthClient
        {
            ClientId = reader.GetGuid(0),
            CompanyId = reader.GetGuid(1),
            ClientName = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
            IsActive = reader.GetBoolean(3),
            CreatedAt = reader.GetDateTime(4),
            ExpiresAt = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
            LastUsedAt = reader.IsDBNull(6) ? null : reader.GetDateTime(6)
        };
    }

    public async Task UpdateLastUsedAtAsync(Guid clientId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE ATROX.atrox_security_client
            SET last_used_at = CURRENT_TIMESTAMP
            WHERE client_id = @client_id
            """;

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@client_id", clientId);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
