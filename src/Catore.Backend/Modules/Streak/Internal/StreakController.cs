using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Catore.Backend.Modules.Streak.Public;

namespace Catore.Backend.Modules.Streak.Internal;

[ApiController]
[Route("api/v1/streak")]
[Authorize]
public class StreakController : ControllerBase
{
    private readonly IStreakQueries _queries;

    public StreakController(IStreakQueries queries)
    {
        _queries = queries;
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue("userid")!);

    [HttpGet]
    public async Task<IActionResult> GetStreakSummary()
    {
        var summary = await _queries.GetStreakSummary(CurrentUserId);
        return Ok(summary);
    }
}
