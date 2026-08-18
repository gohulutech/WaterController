using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Service;

/// <summary>
/// Payload received from a device when reporting a measurement.
/// Wire format (as sent by the ESP32 firmware):
/// {"device_id":"%s","intervalSeconds":10,"pulses":%d,"timestamp":%ld}
/// pulses = pulse count accumulated during intervalSeconds.
/// </summary>
public sealed record MeasurementInputViewModel(
    [property: JsonPropertyName("device_id")] [Required] string DeviceId,
    [property: JsonPropertyName("intervalSeconds")] [Range(1, int.MaxValue)] int IntervalSeconds,
    [property: JsonPropertyName("pulses")] [Range(0, int.MaxValue)] int Pulses,
    [property: JsonPropertyName("timestamp")] long Timestamp);
