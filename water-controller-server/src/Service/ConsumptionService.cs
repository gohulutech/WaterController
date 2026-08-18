using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Model;

namespace Service;

public sealed class ConsumptionService(WaterControllerDbContext db) : IConsumptionService
{
    public async Task<ConsumptionOutputDto> GetConsumption(
        long rangeSeconds,
        long intervalSeconds,
        string? deviceId = null,
        int offsetMinutes = 0
    )
    {
        var rangeEnd = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var rangeStart = rangeEnd - rangeSeconds;

        if (offsetMinutes != 0)
        {
            var offsetSeconds = offsetMinutes * 60;
            var secondsSinceLocalMidnight = (rangeStart + offsetSeconds) % 86400;
            if (secondsSinceLocalMidnight < 0) secondsSinceLocalMidnight += 86400;
            rangeStart += 86400 - secondsSinceLocalMidnight;
        }

        var bucketCount = (int)Math.Ceiling((rangeEnd - rangeStart) / (double)intervalSeconds);

        var measurements = await db
            .Measurements.Where(m =>
                m.Timestamp >= rangeStart
                && m.Timestamp < rangeEnd
                && (deviceId == null || m.DeviceId == deviceId)
            )
            .Select(m => new { m.Timestamp, m.Pulses })
            .ToListAsync();

        var buckets = new List<ConsumptionBucketDto>(bucketCount);
        for (var i = 0; i < bucketCount; i++)
        {
            buckets.Add(
                new ConsumptionBucketDto(
                    DateTimeOffset.FromUnixTimeSeconds(rangeStart + i * intervalSeconds),
                    DateTimeOffset.FromUnixTimeSeconds(
                        Math.Min(rangeStart + (i + 1) * intervalSeconds, rangeEnd)
                    ),
                    0
                )
            );
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

        return new ConsumptionOutputDto(buckets.Sum(b => b.Liters), buckets);
    }
}
