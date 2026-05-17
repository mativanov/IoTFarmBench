using IoTFarmBench.RestService.Data;
using IoTFarmBench.RestService.Models;
using Microsoft.AspNetCore.Mvc;

namespace IoTFarmBench.RestService.Controllers;

[ApiController]
[Route("api/readings")]
[Produces("application/json")]
public sealed class ReadingsController(
    SensorReadingRepository readingRepository,
    DeviceRepository deviceRepository) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SensorReadingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<SensorReadingDto>>> GetLatest(
        [FromQuery] Guid? deviceId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        if (!ValidateLimit(limit, out var error))
        {
            return BadRequest(error);
        }

        if (from is not null && to is not null && from > to)
        {
            return BadRequest("The 'from' timestamp must be earlier than or equal to 'to'.");
        }

        var readings = await readingRepository.GetLatestAsync(deviceId, from, to, limit, cancellationToken);
        return Ok(readings);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SensorReadingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SensorReadingDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var reading = await readingRepository.GetByIdAsync(id, cancellationToken);
        return reading is null ? NotFound() : Ok(reading);
    }

    [HttpPost]
    [ProducesResponseType(typeof(SensorReadingDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SensorReadingDto>> Create(
        [FromBody] CreateSensorReadingRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.SensorId))
        {
            return BadRequest("sensorId is required.");
        }

        if (request.Timestamp is null)
        {
            return BadRequest("timestamp is required.");
        }

        var created = await readingRepository.CreateAsync(request, deviceRepository, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpGet("selective")]
    [ProducesResponseType(typeof(IReadOnlyList<Dictionary<string, object?>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<Dictionary<string, object?>>>> GetSelective(
        [FromQuery] string? fields,
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        if (!ValidateLimit(limit, out var error))
        {
            return BadRequest(error);
        }

        var requestedFields = ParseFields(fields);
        if (requestedFields.Count == 0)
        {
            return BadRequest("The 'fields' query parameter is required.");
        }

        var invalidFields = requestedFields
            .Where(field => !SensorReadingRepository.AllowedSelectiveFields.Contains(field))
            .ToArray();

        if (invalidFields.Length > 0)
        {
            return BadRequest(new
            {
                message = "One or more requested fields are invalid.",
                invalidFields,
                allowedFields = SensorReadingRepository.AllowedSelectiveFields.OrderBy(field => field)
            });
        }

        var rows = await readingRepository.GetSelectiveAsync(requestedFields, limit, cancellationToken);
        return Ok(rows);
    }

    private static bool ValidateLimit(int limit, out string? error)
    {
        if (limit is < 1 or > 1000)
        {
            error = "limit must be between 1 and 1000.";
            return false;
        }

        error = null;
        return true;
    }

    private static IReadOnlyList<string> ParseFields(string? fields)
    {
        if (string.IsNullOrWhiteSpace(fields))
        {
            return Array.Empty<string>();
        }

        return fields
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
