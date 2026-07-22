using Microsoft.Extensions.Caching.Memory;
using Catore.Backend.Modules.Auth.Public;
using Catore.Backend.Modules.Notification.Public;

namespace Catore.Backend.Modules.Auth.Internal;

internal class AuthService : IAuthQueries, IAuthCommands
{
    private const int MaxFailedAttempts = 3;
    private static readonly TimeSpan LockoutWindow = TimeSpan.FromMinutes(10);

    private readonly AuthRepository _repository;
    private readonly TokenService _tokenService;
    private readonly EmailService _emailService;
    private readonly INotificationCommands _notificationCommands;
    private readonly INotificationSender _notificationSender;
    private readonly IMemoryCache _cache;

    public AuthService(
        AuthRepository repository,
        TokenService tokenService,
        EmailService emailService,
        INotificationCommands notificationCommands,
        INotificationSender notificationSender,
        IMemoryCache cache)
    {
        _repository = repository;
        _tokenService = tokenService;
        _emailService = emailService;
        _notificationCommands = notificationCommands;
        _notificationSender = notificationSender;
        _cache = cache;
    }

    public async Task<bool> UserExists(Guid userId)
    {
        var account = await _repository.GetById(userId);
        return account is not null;
    }

    public async Task<AuthAccountDto?> GetAccountInfo(Guid userId)
    {
        var account = await _repository.GetById(userId);
        if (account is null) return null;
        return new AuthAccountDto(account.CreatedOn);
    }

    public async Task<SignUpResultDto> SignUp(string email, string password)
    {
        if (await _repository.EmailExists(email))
        {
            return new SignUpResultDto(false, "Email already registered", null);
        }

        var account = new UserAccount
        {
            UserAccountPk = Guid.NewGuid(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
        };

        await _repository.Add(account);

        return new SignUpResultDto(true, null, account.UserAccountPk);
    }

    public async Task<LoginResultDto> Login(string email, string password, string ipAddress, string? fcmToken)
    {
        var lockoutKey = $"login-lockout:{email}:{ipAddress}";
        if (_cache.TryGetValue(lockoutKey, out int attempts) && attempts >= MaxFailedAttempts)
        {
            return new LoginResultDto(false, "Too many failed attempts. Try again later.", null, null);
        }

        var account = await _repository.GetByEmail(email);

        if (account is null || !BCrypt.Net.BCrypt.Verify(password, account.PasswordHash))
        {
            attempts = _cache.TryGetValue(lockoutKey, out int current) ? current + 1 : 1;
            _cache.Set(lockoutKey, attempts, LockoutWindow);
            return new LoginResultDto(false, "Invalid email or password", null, null);
        }

        _cache.Remove(lockoutKey);

        var previousSessionId = account.ActiveSessionId;
        var newSessionId = Guid.NewGuid();

        account.ActiveSessionId = newSessionId;
        account.RefreshToken = TokenService.GenerateSecureToken();
        account.RefreshTokenExpiry = _tokenService.GetRefreshTokenExpiry();
        await _repository.Update(account);

        if (fcmToken is not null)
        {
            await _notificationCommands.UpdateFcmToken(account.UserAccountPk, fcmToken);
        }

        if (previousSessionId is not null)
        {
            await _notificationSender.SendForceLogoutNotif(account.UserAccountPk);
        }

        var accessToken = _tokenService.GenerateAccessToken(account.UserAccountPk, newSessionId);

        return new LoginResultDto(true, null, accessToken, account.RefreshToken);
    }

    public async Task Logout(Guid userId)
    {
        var account = await _repository.GetById(userId);
        if (account is null) return;

        account.RefreshToken = null;
        account.RefreshTokenExpiry = null;
        await _repository.Update(account);
    }

    public async Task<bool> RequestPasswordReset(string email)
    {
        var account = await _repository.GetByEmail(email);
        if (account is null) return false;

        account.ResetToken = TokenService.GenerateSecureToken();
        account.ResetTokenExpiry = DateTime.UtcNow.AddHours(1);
        await _repository.Update(account);

        await _notificationSender.SendPasswordResetRequestedNotif(account.UserAccountPk);
        await _emailService.SendPasswordResetEmail(account.Email, account.ResetToken);

        return true;
    }

    public async Task<bool> ResetPassword(string resetToken, string newPassword)
    {
        var account = await _repository.GetByResetToken(resetToken);
        if (account is null || account.ResetTokenExpiry < DateTime.UtcNow)
        {
            return false;
        }

        account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        account.ResetToken = null;
        account.ResetTokenExpiry = null;
        account.ActiveSessionId = null;
        account.RefreshToken = null;
        account.RefreshTokenExpiry = null;
        await _repository.Update(account);

        return true;
    }
}
