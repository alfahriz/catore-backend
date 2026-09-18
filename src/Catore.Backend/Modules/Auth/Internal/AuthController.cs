using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Catore.Backend.Modules.Auth.Public;

namespace Catore.Backend.Modules.Auth.Internal;

[ApiController]
[Route("api/v1/auth")]
[Authorize]
public class AuthController : ControllerBase
{
    private readonly IAuthCommands _authCommands;

    public AuthController(IAuthCommands authCommands)
    {
        _authCommands = authCommands;
    }

    [AllowAnonymous]
    [HttpPost("signup")]
    public async Task<IActionResult> SignUp([FromBody] SignUpRequest request)
    {
        if (request.Password.Length < 6 || request.Password.Length > 12)
        {
            return BadRequest(new { error = "Password must be 6-12 characters" });
        }
        if (request.Password != request.ConfirmPassword)
        {
            return BadRequest(new { error = "Passwords do not match" });
        }

        var result = await _authCommands.SignUp(request.Email, request.Password);
        if (!result.Success)
        {
            return BadRequest(new { error = result.ErrorMessage });
        }

        return Ok(new { userPk = result.UserPk });
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var result = await _authCommands.Login(request.Email, request.Password, ipAddress, request.FcmToken);

        if (!result.Success)
        {
            return Unauthorized(new { error = result.ErrorMessage });
        }

        return Ok(new { accessToken = result.AccessToken, refreshToken = result.RefreshToken });
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        var result = await _authCommands.RefreshAccessToken(request.RefreshToken);
        if (!result.Success)
        {
            return Unauthorized(new { error = result.ErrorMessage });
        }

        return Ok(new { accessToken = result.AccessToken, refreshToken = result.RefreshToken });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var userId = long.Parse(User.FindFirstValue("userid")!);
        await _authCommands.Logout(userId);
        return Ok();
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (request.NewPassword.Length < 6 || request.NewPassword.Length > 12)
        {
            return BadRequest(new { error = "Password must be 6-12 characters" });
        }

        var userId = long.Parse(User.FindFirstValue("userid")!);
        var result = await _authCommands.ChangePassword(userId, request.CurrentPassword, request.NewPassword);
        if (!result.Success)
        {
            return BadRequest(new { error = result.ErrorMessage });
        }

        return Ok();
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        await _authCommands.RequestPasswordReset(request.Email);
        // Selalu return Ok, terlepas dari email ditemukan atau tidak (hindari account enumeration)
        return Ok();
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        if (request.NewPassword.Length < 6 || request.NewPassword.Length > 12)
        {
            return BadRequest(new { error = "Password must be 6-12 characters" });
        }

        var success = await _authCommands.ResetPassword(request.ResetToken, request.NewPassword);
        if (!success)
        {
            return BadRequest(new { error = "Invalid or expired reset token" });
        }

        return Ok();
    }

    [AllowAnonymous]
    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        var success = await _authCommands.VerifyEmail(request.VerifyToken);
        if (!success)
        {
            return BadRequest(new { error = "Invalid or expired verification token" });
        }

        return Ok();
    }

    [AllowAnonymous]
    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationRequest request)
    {
        var result = await _authCommands.ResendVerification(request.Email);
        // Selalu return Ok, terlepas dari email ditemukan/sudah verified (hindari account enumeration).
        // cooldownSecondsRemaining > 0 dipakai FE buat render countdown tombol resend.
        return Ok(new { cooldownSecondsRemaining = result.CooldownSecondsRemaining });
    }
}

public record SignUpRequest(string Email, string Password, string ConfirmPassword);
public record LoginRequest(string Email, string Password, string? FcmToken);
public record RefreshRequest(string RefreshToken);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string ResetToken, string NewPassword);
public record VerifyEmailRequest(string VerifyToken);
public record ResendVerificationRequest(string Email);
