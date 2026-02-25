namespace Atrox.Vectra.Authentication.Api.DataAccess.Contracts.Connections;

public interface IConnectionStringBuilder
{
    string BuildConnectionString(string engine);
    string BuildSqlServerConnectionString();
    string BuildPostgreSqlConnectionString();
}
