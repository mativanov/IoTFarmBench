using IoTFarmBench.RestService.Data;
using IoTFarmBench.RestService.Models;
using Microsoft.AspNetCore.Mvc;

namespace IoTFarmBench.RestService.Controllers;

[ApiController]
[Route("api/devices")]
[Produces("application/json")]
public sealed class DevicesController(DeviceRepository repository) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DeviceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DeviceDto>>> GetAll(CancellationToken cancellationToken)
    {
        var devices = await repository.GetAllAsync(cancellationToken);
        return Ok(devices);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DeviceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DeviceDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var device = await repository.GetByIdAsync(id, cancellationToken);
        return device is null ? NotFound() : Ok(device);
    }
}
