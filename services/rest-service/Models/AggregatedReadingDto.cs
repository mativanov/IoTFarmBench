namespace IoTFarmBench.RestService.Models;

public sealed record AggregatedReadingDto(
    string? Region,
    long Count,
    double? AvgTemperatureC,
    double? AvgHumidityPercent,
    double? AvgSoilMoisturePercent,
    double? AvgNdviIndex,
    double? AvgYieldKgPerHectare);
