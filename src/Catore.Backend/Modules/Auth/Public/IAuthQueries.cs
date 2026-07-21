namespace Catore.Backend.Modules.Auth.Public;

public interface IAuthQueries
{
    Task<bool> UserExists(Guid userId);
    Task<AuthAccountDto?> GetAccountInfo(Guid userId);
}
