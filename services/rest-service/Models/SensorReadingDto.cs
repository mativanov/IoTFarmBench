namespace IoTFarmBench.RestService.Models;

public sealed record SensorReadingDto(
    Guid Id,
    Guid DeviceId,
    DateTimeOffset Timestamp,
    string? CropType,
    double? SoilMoisturePercent,
    double? SoilPh,
    double? TemperatureC,
    double? RainfallMm,
    double? HumidityPercent,
    double? SunlightHours,
    string? IrrigationType,
    string? FertilizerType,
    double? PesticideUsageMl,
    DateOnly? SowingDate,
    DateOnly? HarvestDate,
    int? TotalDays,
    double? YieldKgPerHectare,
    double? NdviIndex,
    string? CropDiseaseStatus);
