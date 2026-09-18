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

    public async Task<bool> UserExists(long userId)
    {
        var account = await _repository.GetById(userId);
        return account is not null;
    }

    public async Task<AuthAccountDto?> GetAccountInfo(long userId)
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
            if (existing.IsEmailVerif)
            {
                return new SignUpResultDto(false, "Email already registered", null);
            }

            if (existing.EmailVerifiedTokenExpiredAt >= DateTime.UtcNow)
            {
                return new SignUpResultDto(false, "Email pending verification, check your inbox", null);
            }

            // Token unverified sudah expired — treat kayak row lama gak pernah ada, timpa.
            await _repository.Delete(existing);
        }

        var verifyToken = TokenService.GenerateSecureToken();

        var account = new MUser
        {
            Email = email,
            Password = BCrypt.Net.BCrypt.HashPassword(password),
            IsEmailVerif = false,
            EmailVerifiedToken = verifyToken,
            EmailVerifiedTokenExpiredAt = DateTime.UtcNow.Add(EmailVerificationWindow)
        };

        await _repository.Add(account);
        await _emailService.SendVerificationEmail(account.Email, verifyToken);

        return new SignUpResultDto(true, null, account.UserPk);
    }

    public async Task<LoginResultDto> Login(string email, string password, string ipAddress, string? fcmToken)
    {
        var lockoutKey = $"login-lockout:{email}:{ipAddress}";
        if (_cache.TryGetValue(lockoutKey, out int attempts) && attempts >= MaxFailedAttempts)
        {
            return new LoginResultDto(false, "Too many failed attempts. Try again later.", null, null);
        }

        var account = await _repository.GetByEmail(email);

        if (account is null || !BCrypt.Net.BCrypt.Verify(password, account.Password))
        {
            attempts = _cache.TryGetValue(lockoutKey, out int current) ? current + 1 : 1;
            _cache.Set(lockoutKey, attempts, LockoutWindow);
            return new LoginResultDto(false, "Invalid email or password", null, null);
        }

        if (!account.IsEmailVerif)
        {
            return new LoginResultDto(false, "Please verify your email before logging in", null, null);
        }

        _cache.Remove(lockoutKey);

        var previousSessionId = account.SessionId;
        var newSessionId = Guid.NewGuid();

        account.SessionId = newSessionId;
        account.JwtRefreshToken = TokenService.GenerateSecureToken();
        account.JwtRefreshTokenExpiredAt = _tokenService.GetRefreshTokenExpiry();
        await _repository.Update(account);

        if (fcmToken is not null)
        {
            await _notificationCommands.UpdateFcmToken(account.UserPk, fcmToken);
        }

        if (previousSessionId is not null)
        {
            await _notificationSender.SendForceLogoutNotif(account.UserPk);
        }

        var accessToken = _tokenService.GenerateAccessToken(account.UserPk, newSessionId);

        return new LoginResultDto(true, null, accessToken, account.JwtRefreshToken);
    }

    // Rotasi refresh token (token lama LANGSUNG diganti token baru, bukan reusable) — praktik
    // standar: kalau refresh token lama bocor & dipakai penyerang duluan, token itu udah gak
    // valid lagi begitu pemilik asli refresh sekali. SessionId TETAP SAMA (bukan digenerate ulang
    // kayak Login) — ini perpanjangan sesi yg sama, bukan sesi baru, jadi gak trigger force-logout
    // notif ke device lain (Login yg trigger itu, refresh bukan).
    public async Task<RefreshResultDto> RefreshAccessToken(string refreshToken)
    {
        var account = await _repository.GetByRefreshToken(refreshToken);
        if (account is null || account.JwtRefreshTokenExpiredAt is null || account.JwtRefreshTokenExpiredAt < DateTime.UtcNow)
        {
            return new RefreshResultDto(false, "Invalid or expired refresh token", null, null);
        }

        var sessionId = account.SessionId ?? Guid.NewGuid();
        account.SessionId = sessionId;
        account.JwtRefreshToken = TokenService.GenerateSecureToken();
        account.JwtRefreshTokenExpiredAt = _tokenService.GetRefreshTokenExpiry();
        await _repository.Update(account);

        var accessToken = _tokenService.GenerateAccessToken(account.UserPk, sessionId);

        return new RefreshResultDto(true, null, accessToken, account.JwtRefreshToken);
    }

    public async Task Logout(long userId)
    {
        var account = await _repository.GetById(userId);
        if (account is null) return;

        account.JwtRefreshToken = null;
        account.JwtRefreshTokenExpiredAt = null;
        await _repository.Update(account);
    }

    // PRD: wajib verifikasi current password dulu (re-auth) sebelum bisa ganti — mencegah orang
    // lain ganti password kalau device ketinggalan saat sesi masih login. Sukses ganti password
    // SELALU invalidate sesi (refresh token dimatikan, sama pola ResetPassword) — termasuk device
    // yg lagi dipakai ganti password ini sendiri, konsisten "sesi lain" di PRD (app ini single-
    // session, jadi "device sendiri" ITU LAH satu-satunya sesi yg ada). Access token yg lagi
    // dipegang tetap jalan sampai natural expire (15 menit) baru ketauan pas refresh gagal —
    // level proteksi yg sama kayak Login/ResetPassword di modul ini, bukan pengurangan.
    public async Task<ChangePasswordResultDto> ChangePassword(long userId, string currentPassword, string newPassword)
    {
        var account = await _repository.GetById(userId);
        if (account is null || !BCrypt.Net.BCrypt.Verify(currentPassword, account.Password))
        {
            return new ChangePasswordResultDto(false, "Current password is incorrect");
        }

        account.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
        account.SessionId = null;
        account.JwtRefreshToken = null;
        account.JwtRefreshTokenExpiredAt = null;
        await _repository.Update(account);

        return new ChangePasswordResultDto(true, null);
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
        account.ResetTokenExpiredAt = DateTime.UtcNow.AddHours(1);
        await _repository.Update(account);

        await _notificationSender.SendPasswordResetRequestedNotif(account.UserPk);
        await _emailService.SendPasswordResetEmail(account.Email, account.ResetToken);

        return true;
    }

    public async Task<bool> ResetPassword(string resetToken, string newPassword)
    {
        var account = await _repository.GetByResetToken(resetToken);
        if (account is null || account.ResetTokenExpiredAt < DateTime.UtcNow)
        {
            return false;
        }

        account.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
        account.ResetToken = null;
        account.ResetTokenExpiredAt = null;
        account.SessionId = null;
        account.JwtRefreshToken = null;
        account.JwtRefreshTokenExpiredAt = null;
        await _repository.Update(account);

        return true;
    }

    public async Task<bool> VerifyEmail(string verifyToken)
    {
        var account = await _repository.GetByVerifyToken(verifyToken);
        if (account is null || account.EmailVerifiedTokenExpiredAt < DateTime.UtcNow)
        {
            return false;
        }

        account.IsEmailVerif = true;
        account.EmailVerifiedToken = null;
        account.EmailVerifiedTokenExpiredAt = null;
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
        if (account is null || account.IsEmailVerif)
        {
            return new ResendVerificationResultDto(false, 0);
        }

        var newCooldownUntil = DateTime.UtcNow.Add(ResendVerificationCooldown);
        _cache.Set(cooldownKey, newCooldownUntil, ResendVerificationCooldown);

        account.EmailVerifiedToken = TokenService.GenerateSecureToken();
        account.EmailVerifiedTokenExpiredAt = DateTime.UtcNow.Add(EmailVerificationWindow);
        await _repository.Update(account);

        await _emailService.SendVerificationEmail(account.Email, account.EmailVerifiedToken);

        return new ResendVerificationResultDto(true, (int)ResendVerificationCooldown.TotalSeconds);
    }
}
