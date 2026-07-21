namespace Catore.Backend.Modules.Profile.Public;

public interface IProfileCommands
{
    Task SetUpgraded(Guid userId);
}
