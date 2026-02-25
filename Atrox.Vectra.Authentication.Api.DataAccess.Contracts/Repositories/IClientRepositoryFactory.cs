namespace Atrox.Vectra.Authentication.Api.DataAccess.Contracts.Repositories;

public interface IClientRepositoryFactory
{
    IClientRepository CreateRepository();
}
