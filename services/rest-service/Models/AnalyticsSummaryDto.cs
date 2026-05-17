namespace IoTFarmBench.RestService.Models;

public sealed record AnalyticsSummaryDto(
    long Count,
    double? AvgTemperatureC,
    double? AvgHumidityPercent,
    double? AvgSoilMoisturePercent,
    double? AvgSoilPh,
    double? AvgRainfallMm,
    double? AvgSunlightHours,
    double? AvgNdviIndex,
    double? AvgYieldKgPerHectare,
    DateTimeOffset? MinTimestamp,
    DateTimeOffset? MaxTimestamp);
