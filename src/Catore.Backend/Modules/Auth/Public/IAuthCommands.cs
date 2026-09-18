namespace Catore.Backend.Modules.Auth.Public;

public interface IAuthCommands
{
    Task<SignUpResultDto> SignUp(string email, string password);
    Task<LoginResultDto> Login(string email, string password, string ipAddress, string? fcmToken);
    Task<RefreshResultDto> RefreshAccessToken(string refreshToken);
    Task Logout(long userId);
    Task<ChangePasswordResultDto> ChangePassword(long userId, string currentPassword, string newPassword);
    Task<bool> RequestPasswordReset(string email);
    Task<bool> ResetPassword(string resetToken, string newPassword);
    Task<bool> VerifyEmail(string verifyToken);
    Task<ResendVerificationResultDto> ResendVerification(string email);
}
