namespace Catore.Backend.Modules.Auth.Internal;

internal class MUser
{
    public long UserPk { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? JwtRefreshToken { get; set; }
    public DateTime? JwtRefreshTokenExpiredAt { get; set; }
    public Guid? SessionId { get; set; }
    public string? ResetToken { get; set; }
    public DateTime? ResetTokenExpiredAt { get; set; }
    public bool IsEmailVerif { get; set; }
    public string? EmailVerifiedToken { get; set; }
    public DateTime? EmailVerifiedTokenExpiredAt { get; set; }
    public DateTime CreatedOn { get; set; }
    public long? CreatedBy { get; set; }
    public DateTime ModifiedOn { get; set; }
    public string? ModifiedBy { get; set; }
}
