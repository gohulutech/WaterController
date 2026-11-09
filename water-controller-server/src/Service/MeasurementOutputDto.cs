using System.Text.Json.Serialization;
using Model;

namespace Service;

/// <summary>
/// Measurement as returned by the API. Raw pulse counts are intentionally
/// not exposed — callers see the flow rate in liters per second instead.
/// </summary>
public sealed record MeasurementOutputDto(
    long Id,
    [property: JsonPropertyName("device_id")] string DeviceId,
    [property: JsonPropertyName("intervalSeconds")] int IntervalSeconds,
    [property: JsonPropertyName("timestamp")] long Timestamp,
    [property: JsonPropertyName("litersPerSecond")] double LitersPerSecond)
{
    public static MeasurementOutputDto FromMeasurement(
        Measurement measurement,
        int intervalSeconds,
        int pulses) =>
        new(
            measurement.MeasurementId,
            measurement.DeviceId,
            intervalSeconds,
            measurement.Timestamp,
            pulses / (double)Device.PulsesPerLiter / intervalSeconds);
}
