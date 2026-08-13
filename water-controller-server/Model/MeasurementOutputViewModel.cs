using System.Text.Json.Serialization;

namespace Model;

/// <summary>
/// Measurement as returned by the API. Raw pulse counts are intentionally
/// not exposed — callers see the flow rate in liters per second instead.
/// </summary>
public sealed record MeasurementOutputViewModel(
    long Id,
    [property: JsonPropertyName("device_id")] string DeviceId,
    [property: JsonPropertyName("intervalSeconds")] int IntervalSeconds,
    [property: JsonPropertyName("timestamp")] long Timestamp,
    [property: JsonPropertyName("litersPerSecond")] double LitersPerSecond)
{
    public const int PulsesPerLiter = 450;

    public static MeasurementOutputViewModel FromMeasurement(Measurement measurement) =>
        new(
            measurement.Id,
            measurement.DeviceId,
            measurement.IntervalSeconds,
            measurement.Timestamp,
            measurement.Pulses / (double)PulsesPerLiter / measurement.IntervalSeconds);
}
