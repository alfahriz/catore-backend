using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Catore.Backend.Modules.Auth.Public;

namespace Catore.Backend.Modules.Auth.Internal;

[ApiController]
[Route("auth")]
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
        if (request.Password.Length < 6)
        {
            return BadRequest(new { error = "Password must be at least 6 characters" });
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

        return Ok(new { userAccountPk = result.UserAccountPk });
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

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var userId = Guid.Parse(User.FindFirstValue("userid")!);
        await _authCommands.Logout(userId);
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
        if (request.NewPassword.Length < 6)
        {
            return BadRequest(new { error = "Password must be at least 6 characters" });
        }

        var success = await _authCommands.ResetPassword(request.ResetToken, request.NewPassword);
        if (!success)
        {
            return BadRequest(new { error = "Invalid or expired reset token" });
        }

        return Ok();
    }
}

public record SignUpRequest(string Email, string Password, string ConfirmPassword);
public record LoginRequest(string Email, string Password, string? FcmToken);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string ResetToken, string NewPassword);
