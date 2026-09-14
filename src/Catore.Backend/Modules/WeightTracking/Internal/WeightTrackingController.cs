using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Catore.Backend.Modules.WeightTracking.Public;

namespace Catore.Backend.Modules.WeightTracking.Internal;

[ApiController]
[Route("api/v1/weightlog")]
[Authorize]
public class WeightTrackingController : ControllerBase
{
    private readonly IWeightTrackingQueries _queries;
    private readonly IWeightTrackingCommands _commands;

    public WeightTrackingController(IWeightTrackingQueries queries, IWeightTrackingCommands commands)
    {
        _queries = queries;
        _commands = commands;
    }

    private long CurrentUserId => long.Parse(User.FindFirstValue("userid")!);

    [HttpPost]
    public async Task<IActionResult> AddWeightLog([FromBody] AddWeightLogRequestDto request)
    {
        var result = await _commands.AddOrUpdateWeightLog(CurrentUserId, DateTime.UtcNow, request.WeightValue);
        if (!result.Success)
        {
            return BadRequest(new { error = result.ErrorMessage });
        }
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetWeightHistory([FromQuery] DateOnly startDate, [FromQuery] DateOnly endDate)
    {
        var history = await _queries.GetWeightHistory(CurrentUserId, startDate, endDate);
        return Ok(history);
    }
}
