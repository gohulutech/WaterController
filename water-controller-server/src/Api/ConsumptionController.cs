using Microsoft.AspNetCore.Mvc;
using Service;

namespace Api;

[ApiController]
[Route("api/[controller]")]
public class ConsumptionController(IConsumptionService consumptionService) : ControllerBase
{
    /// <summary>
    /// Returns consumed liters over a time range, bucketed by interval.
    /// Examples: /api/consumption?range=24h&amp;interval=1h
    ///           /api/consumption?range=7d&amp;interval=1d
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ConsumptionOutputViewModel>> Get(
        [FromQuery] string range,
        [FromQuery] string interval,
        [FromQuery] string? deviceId = null)
    {
        if (!DurationParser.TryParse(range, out var rangeSeconds))
        {
            return BadRequest($"Invalid range: '{range}'. Use formats like '24h' or '7d'.");
        }

        if (!DurationParser.TryParse(interval, out var intervalSeconds))
        {
            return BadRequest($"Invalid interval: '{interval}'. Use formats like '1h' or '1d'.");
        }

        if (intervalSeconds > rangeSeconds)
        {
            return BadRequest("Interval cannot be larger than range.");
        }

        var normalizedDeviceId = string.IsNullOrWhiteSpace(deviceId) ? null : deviceId.Trim();
        return Ok(await consumptionService.GetConsumption(rangeSeconds, intervalSeconds, normalizedDeviceId));
    }
}
