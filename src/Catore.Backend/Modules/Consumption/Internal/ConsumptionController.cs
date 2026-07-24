using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Catore.Backend.Modules.Consumption.Public;

namespace Catore.Backend.Modules.Consumption.Internal;

[ApiController]
[Route("api/v1/consumption")]
[Authorize]
public class ConsumptionController : ControllerBase
{
    private readonly IConsumptionCommands _commands;

    public ConsumptionController(IConsumptionCommands commands)
    {
        _commands = commands;
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue("userid")!);

    [HttpPost("entries")]
    public async Task<IActionResult> AddEntries([FromBody] AddEntriesRequestDto request)
    {
        var result = await _commands.AddEntries(CurrentUserId, request);
        if (!result.Success)
        {
            return BadRequest(new { error = result.ErrorMessage });
        }
        return Ok(new { savedEntries = result.SavedEntries, dailyRecord = result.DailyRecord });
    }
}
