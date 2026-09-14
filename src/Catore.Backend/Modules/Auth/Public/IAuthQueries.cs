namespace Catore.Backend.Modules.Auth.Public;

public interface IAuthQueries
{
    Task<bool> UserExists(long userId);
    Task<AuthAccountDto?> GetAccountInfo(long userId);
}
