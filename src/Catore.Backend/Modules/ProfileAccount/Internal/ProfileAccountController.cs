using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Catore.Backend.Modules.ProfileAccount.Public;

namespace Catore.Backend.Modules.ProfileAccount.Internal;

[ApiController]
[Route("api/v1/profile")]
[Authorize]
public class ProfileAccountController : ControllerBase
{
    private readonly IProfileAccountQueries _queries;
    private readonly IProfileAccountCommands _commands;

    public ProfileAccountController(IProfileAccountQueries queries, IProfileAccountCommands commands)
    {
        _queries = queries;
        _commands = commands;
    }

    private long CurrentUserId => long.Parse(User.FindFirstValue("userid")!);

    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        var profile = await _queries.GetFullProfile(CurrentUserId);
        if (profile is null)
        {
            return NotFound();
        }
        return Ok(profile);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequestDto request)
    {
        var result = await _commands.UpdateProfile(CurrentUserId, request);
        if (!result.Success)
        {
            return BadRequest(new { error = result.ErrorMessage });
        }
        return Ok();
    }

    [HttpPost("activity-assessment")]
    public async Task<IActionResult> SubmitActivityAssessment([FromBody] ActivityAssessmentRequest request)
    {
        var result = await _commands.SubmitActivityAssessment(CurrentUserId, request.WorkEnvironment, request.ExerciseFrequency);
        return Ok(new { activityLevel = result.ActivityLevel });
    }

    [HttpPost("timezone/refresh")]
    public async Task<IActionResult> RefreshTimezone([FromBody] TimezoneRefreshRequest request)
    {
        var result = await _commands.RefreshTimezone(CurrentUserId, request.Timezone);
        if (!result.Success)
        {
            return BadRequest(new { error = result.ErrorMessage });
        }
        return Ok();
    }
}

public record ActivityAssessmentRequest(string WorkEnvironment, string ExerciseFrequency);
public record TimezoneRefreshRequest(string Timezone);
