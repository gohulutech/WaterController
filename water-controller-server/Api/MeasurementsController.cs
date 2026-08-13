using Microsoft.AspNetCore.Mvc;
using Model;

namespace Api;

[ApiController]
[Route("api/[controller]")]
public class MeasurementsController : ControllerBase
{
    /// <summary>
    /// Receives a flow measurement from a device.
    /// Payload format (as sent by the ESP32 firmware) is:
    /// {"device_id":"%s","intervalSeconds":1,"pulses_last_second":%d,"timestamp":%ld}
    /// </summary>
    [HttpPost]
    public ActionResult<Measurement> Post(Measurement measurement)
    {
        return Ok(measurement);
    }
}
