using System.ComponentModel.DataAnnotations;

namespace IoTFarmBench.RestService.Models;

public sealed class CreateSensorReadingRequest
{
    [Required]
    public string? SensorId { get; init; }

    public string? FarmId { get; init; }
    public string? Region { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }

    [Required]
    public DateTimeOffset? Timestamp { get; init; }

    public string? CropType { get; init; }
    public double? SoilMoisturePercent { get; init; }
    public double? SoilPh { get; init; }
    public double? TemperatureC { get; init; }
    public double? RainfallMm { get; init; }
    public double? HumidityPercent { get; init; }
    public double? SunlightHours { get; init; }
    public string? IrrigationType { get; init; }
    public string? FertilizerType { get; init; }
    public double? PesticideUsageMl { get; init; }
    public DateOnly? SowingDate { get; init; }
    public DateOnly? HarvestDate { get; init; }
    public int? TotalDays { get; init; }
    public double? YieldKgPerHectare { get; init; }
    public double? NdviIndex { get; init; }
    public string? CropDiseaseStatus { get; init; }
}
