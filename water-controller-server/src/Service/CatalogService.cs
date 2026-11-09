using Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Service;

public sealed class CatalogService(WaterControllerDbContext db) : ICatalogService
{
    public async Task<IReadOnlyList<DeviceTypeDto>> GetDeviceTypes() =>
        await db.DeviceTypes
            .Select(t => new DeviceTypeDto(t.DeviceTypeId, t.Name))
            .ToListAsync();

    public async Task<IReadOnlyList<DeviceDto>> GetDevices() =>
        await db.Devices
            .Select(d => new DeviceDto(d.DeviceId, d.DeviceTypeId, d.Name))
            .ToListAsync();

    public async Task<IReadOnlyList<MetricDto>> GetMetrics() =>
        await db.Metrics
            .Select(m => new MetricDto(m.MetricId, m.Code, m.Name, m.Unit))
            .ToListAsync();

    public async Task<IReadOnlyList<MetricDto>?> GetDeviceMetrics(string deviceId)
    {
        var deviceExists = await db.Devices.AnyAsync(d => d.DeviceId == deviceId);
        if (!deviceExists)
        {
            return null;
        }

        return await db.DeviceMetrics
            .Where(dm => dm.DeviceId == deviceId)
            .Select(dm => new MetricDto(
                dm.Metric.MetricId,
                dm.Metric.Code,
                dm.Metric.Name,
                dm.Metric.Unit))
            .ToListAsync();
    }
}
