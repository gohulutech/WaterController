using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Model.Services;
using Model;

namespace Service;

public sealed class ConsumptionService(WaterControllerDbContext db) : IConsumptionService
{
    public async Task<ConsumptionOutputViewModel> GetConsumption(long rangeSeconds, long intervalSeconds, string? deviceId = null)
    {
        var rangeEnd = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var rangeStart = rangeEnd - rangeSeconds;
        var bucketCount = (int)Math.Ceiling(rangeSeconds / (double)intervalSeconds);

        var measurements = await db.Measurements
            .Where(m => m.Timestamp >= rangeStart && m.Timestamp < rangeEnd
                        && (deviceId == null || m.DeviceId == deviceId))
            .Select(m => new { m.Timestamp, m.Pulses })
            .ToListAsync();

        var buckets = new List<ConsumptionBucketViewModel>(bucketCount);
        for (var i = 0; i < bucketCount; i++)
        {
            buckets.Add(new ConsumptionBucketViewModel(
                DateTimeOffset.FromUnixTimeSeconds(rangeStart + i * intervalSeconds),
                DateTimeOffset.FromUnixTimeSeconds(Math.Min(rangeStart + (i + 1) * intervalSeconds, rangeEnd)),
                0));
        }

        foreach (var m in measurements)
        {
            var index = (int)((m.Timestamp - rangeStart) / intervalSeconds);
            if (index < 0 || index >= bucketCount)
            {
                continue;
            }

            var liters = m.Pulses / (double)Device.PulsesPerLiter;
            buckets[index] = buckets[index] with { Liters = buckets[index].Liters + liters };
        }

        return new ConsumptionOutputViewModel(buckets.Sum(b => b.Liters), buckets);
    }
}
