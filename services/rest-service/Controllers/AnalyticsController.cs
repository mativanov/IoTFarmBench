using IoTFarmBench.RestService.Data;
using IoTFarmBench.RestService.Models;
using Microsoft.AspNetCore.Mvc;

namespace IoTFarmBench.RestService.Controllers;

[ApiController]
[Route("api/analytics")]
[Produces("application/json")]
public sealed class AnalyticsController(AnalyticsRepository repository) : ControllerBase
{
    [HttpGet("summary")]
    [ProducesResponseType(typeof(AnalyticsSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AnalyticsSummaryDto>> GetSummary(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] string? region,
        [FromQuery] string? cropType,
        CancellationToken cancellationToken)
    {
        if (from is not null && to is not null && from > to)
        {
            return BadRequest("The 'from' timestamp must be earlier than or equal to 'to'.");
        }

        var summary = await repository.GetSummaryAsync(from, to, region, cropType, cancellationToken);
        return Ok(summary);
    }

    [HttpGet("by-region")]
    [ProducesResponseType(typeof(IReadOnlyList<AggregatedReadingDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AggregatedReadingDto>>> GetByRegion(CancellationToken cancellationToken)
    {
        var rows = await repository.GetByRegionAsync(cancellationToken);
        return Ok(rows);
    }
}
