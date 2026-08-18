using Microsoft.AspNetCore.Mvc;
using Service;

namespace Api;

[ApiController]
[Route("api/[controller]")]
public class MeasurementsController(IMeasurementService measurementService) : ControllerBase
{
    /// <summary>
    /// Receives a flow measurement from a device.
    /// Payload format (as sent by the ESP32 firmware) is:
    /// {"device_id":"%s","intervalSeconds":10,"pulses":%d,"timestamp":%ld}
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<MeasurementOutputDto>> Post(MeasurementInputDto measurement)
    {
        Console.WriteLine(
            $"[DEBUG] POST /api/measurements: device={measurement.DeviceId}, interval={measurement.IntervalSeconds}s, " +
            $"pulses={measurement.Pulses}, timestamp={measurement.Timestamp}");

        return Ok(await measurementService.AddMeasurement(measurement));
    }
}
