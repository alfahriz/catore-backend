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
    private readonly IConsumptionQueries _queries;
    private readonly IConsumptionCommands _commands;

    public ConsumptionController(IConsumptionQueries queries, IConsumptionCommands commands)
    {
        _queries = queries;
        _commands = commands;
    }

    private long CurrentUserId => long.Parse(User.FindFirstValue("userid")!);

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

    [HttpGet("daily-record/{date}")]
    public async Task<IActionResult> GetDailyRecord(DateOnly date)
    {
        var dailyRecord = await _queries.GetOrCreateDailyRecord(CurrentUserId, date);
        return Ok(dailyRecord);
    }

    [HttpPatch("daily-record/{date}")]
    public async Task<IActionResult> UpdateDailyRecord(DateOnly date, [FromBody] UpdateDailyRecordRequestDto request)
    {
        var dailyRecord = await _commands.UpdateDailyRecord(CurrentUserId, date, request);
        if (dailyRecord is null)
        {
            return NotFound();
        }
        return Ok(dailyRecord);
    }

    [HttpGet("autocomplete")]
    public async Task<IActionResult> Autocomplete([FromQuery] string query, [FromQuery] int page = 0, [FromQuery] int pageSize = 20)
    {
        var results = await _queries.SearchAutocomplete(query, page, pageSize);
        return Ok(results);
    }

    [HttpGet("quick-add")]
    public async Task<IActionResult> QuickAdd()
    {
        var results = await _queries.GetQuickAdd(CurrentUserId);
        return Ok(results);
    }
}
