using Microsoft.AspNetCore.Mvc;
using Service;

namespace Api;

[ApiController]
[Route("api/catalog")]
public class CatalogController(ICatalogService catalogService) : ControllerBase
{
    [HttpGet("device-types")]
    public async Task<ActionResult<IReadOnlyList<DeviceTypeDto>>> GetDeviceTypes() =>
        Ok(await catalogService.GetDeviceTypes());

    [HttpGet("devices")]
    public async Task<ActionResult<IReadOnlyList<DeviceDto>>> GetDevices() =>
        Ok(await catalogService.GetDevices());

    [HttpGet("metrics")]
    public async Task<ActionResult<IReadOnlyList<MetricDto>>> GetMetrics() =>
        Ok(await catalogService.GetMetrics());

    [HttpGet("devices/{deviceId}/metrics")]
    public async Task<ActionResult<IReadOnlyList<MetricDto>>> GetDeviceMetrics(string deviceId)
    {
        var metrics = await catalogService.GetDeviceMetrics(deviceId);
        if (metrics is null)
        {
            return NotFound();
        }

        return Ok(metrics);
    }
}
