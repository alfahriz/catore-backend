using Microsoft.Extensions.Caching.Memory;
using Catore.Backend.Modules.Auth.Public;
using Catore.Backend.Modules.Notification.Public;

namespace Catore.Backend.Modules.Auth.Internal;

internal class AuthService : IAuthQueries, IAuthCommands
{
    private const int MaxFailedAttempts = 3;
    private static readonly TimeSpan LockoutWindow = TimeSpan.FromMinutes(10);

    private const int MaxPasswordResetRequests = 2;
    private static readonly TimeSpan PasswordResetWindow = TimeSpan.FromDays(7);

    private static readonly TimeSpan EmailVerificationWindow = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan ResendVerificationCooldown = TimeSpan.FromSeconds(30);

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
        var existing = await _repository.GetByEmail(email);
        if (existing is not null)
        {
            if (existing.IsEmailVerified)
            {
                return new SignUpResultDto(false, "Email already registered", null);
            }

            if (existing.VerifyTokenExpiry >= DateTime.UtcNow)
            {
                return new SignUpResultDto(false, "Email pending verification, check your inbox", null);
            }

            // Token unverified sudah expired — treat kayak row lama gak pernah ada, timpa.
            await _repository.Delete(existing);
        }

        var verifyToken = TokenService.GenerateSecureToken();

        var account = new UserAccount
        {
            UserAccountPk = Guid.NewGuid(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            IsEmailVerified = false,
            VerifyToken = verifyToken,
            VerifyTokenExpiry = DateTime.UtcNow.Add(EmailVerificationWindow)
        };

        await _repository.Add(account);
        await _emailService.SendVerificationEmail(account.Email, verifyToken);

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

        if (!account.IsEmailVerified)
        {
            return new LoginResultDto(false, "Please verify your email before logging in", null, null);
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
        var resetCountKey = $"reset-request-count:{email}";
        var requestCount = _cache.TryGetValue(resetCountKey, out int count) ? count : 0;
        if (requestCount >= MaxPasswordResetRequests)
        {
            return false;
        }

        var account = await _repository.GetByEmail(email);
        if (account is null) return false;

        _cache.Set(resetCountKey, requestCount + 1, PasswordResetWindow);

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

    public async Task<bool> VerifyEmail(string verifyToken)
    {
        var account = await _repository.GetByVerifyToken(verifyToken);
        if (account is null || account.VerifyTokenExpiry < DateTime.UtcNow)
        {
            return false;
        }

        account.IsEmailVerified = true;
        account.VerifyToken = null;
        account.VerifyTokenExpiry = null;
        await _repository.Update(account);

        return true;
    }

    public async Task<ResendVerificationResultDto> ResendVerification(string email)
    {
        var cooldownKey = $"resend-cooldown:{email}";
        if (_cache.TryGetValue(cooldownKey, out DateTime cooldownUntil) && cooldownUntil > DateTime.UtcNow)
        {
            var secondsRemaining = (int)Math.Ceiling((cooldownUntil - DateTime.UtcNow).TotalSeconds);
            return new ResendVerificationResultDto(false, secondsRemaining);
        }

        var account = await _repository.GetByEmail(email);
        if (account is null || account.IsEmailVerified)
        {
            return new ResendVerificationResultDto(false, 0);
        }

        var newCooldownUntil = DateTime.UtcNow.Add(ResendVerificationCooldown);
        _cache.Set(cooldownKey, newCooldownUntil, ResendVerificationCooldown);

        account.VerifyToken = TokenService.GenerateSecureToken();
        account.VerifyTokenExpiry = DateTime.UtcNow.Add(EmailVerificationWindow);
        await _repository.Update(account);

        await _emailService.SendVerificationEmail(account.Email, account.VerifyToken);

        return new ResendVerificationResultDto(true, (int)ResendVerificationCooldown.TotalSeconds);
    }
}
