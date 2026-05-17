namespace IoTFarmBench.RestService.Models;

public sealed record DeviceDto(
    Guid Id,
    string SensorId,
    string? FarmId,
    string? Region,
    double? Latitude,
    double? Longitude);
