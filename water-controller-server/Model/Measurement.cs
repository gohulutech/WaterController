using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Model;

/// <summary>
/// A flow measurement reported by a device.
/// Wire format (as sent by the ESP32 firmware):
/// {"device_id":"%s","intervalSeconds":1,"pulses_last_second":%d,"timestamp":%ld}
/// </summary>
public sealed record Measurement(
    [property: JsonPropertyName("device_id")] [Required] string DeviceId,
    [property: JsonPropertyName("intervalSeconds")] int IntervalSeconds,
    [property: JsonPropertyName("pulses")] int Pulses,
    [property: JsonPropertyName("timestamp")] long Timestamp,
    long Id = 0);
