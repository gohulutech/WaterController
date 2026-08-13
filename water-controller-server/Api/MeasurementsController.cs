using Microsoft.AspNetCore.Mvc;
using Model;
using Service;

namespace Api;

[ApiController]
[Route("api/[controller]")]
public class MeasurementsController(IMeasurementService measurementService) : ControllerBase
{
    /// <summary>
    /// Receives a flow measurement from a device.
    /// Payload format (as sent by the ESP32 firmware) is:
    /// {"device_id":"%s","intervalSeconds":1,"pulses_last_second":%d,"timestamp":%ld}
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Measurement>> Post(Measurement measurement)
    {
        Console.WriteLine(
            $"[DEBUG] POST /api/measurements: device={measurement.DeviceId}, interval={measurement.IntervalSeconds}s, " +
            $"pulses={measurement.PulsesLastSecond}, timestamp={measurement.Timestamp}");

        return Ok(await measurementService.AddMeasurement(measurement));
    }
}
