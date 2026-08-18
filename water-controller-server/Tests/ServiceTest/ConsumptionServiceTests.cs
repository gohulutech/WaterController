using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Model;
using Service;
using Xunit;

namespace ServiceTest;

public class ConsumptionServiceTests : IDisposable
{
    private readonly WaterControllerDbContext _db;
    private readonly ConsumptionService _sut;

    public ConsumptionServiceTests()
    {
        var options = new DbContextOptionsBuilder<WaterControllerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db = new WaterControllerDbContext(options);
        _sut = new ConsumptionService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    [Fact]
    public async Task GetConsumption_EmptyDatabase_ReturnsZeroLiters()
    {
        var result = await _sut.GetConsumption(3600, 900);

        Assert.Equal(0, result.TotalLiters);
        Assert.Equal(4, result.Buckets.Count);
        Assert.All(result.Buckets, b => Assert.Equal(0, b.Liters));
    }

    [Fact]
    public async Task GetConsumption_SingleMeasurement_AppearsInCorrectBucket()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        // range=600, interval=300 → 2 buckets: [now-600, now-300) and [now-300, now)
        var measurement = new Measurement("device-1", 10, 450, now - 100);
        _db.Measurements.Add(measurement);
        await _db.SaveChangesAsync();

        var result = await _sut.GetConsumption(600, 300);

        Assert.Equal(2, result.Buckets.Count);
        Assert.Equal(0, result.Buckets[0].Liters, 2);
        Assert.Equal(1.0, result.Buckets[1].Liters, 2);
        Assert.Equal(1.0, result.TotalLiters, 2);
    }

    [Fact]
    public async Task GetConsumption_MultipleMeasurements_SumsWithinBucket()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        _db.Measurements.AddRange(
            new Measurement("device-1", 10, 225, now - 50),
            new Measurement("device-1", 10, 225, now - 80)
        );
        await _db.SaveChangesAsync();

        var result = await _sut.GetConsumption(600, 600);

        Assert.Single(result.Buckets);
        Assert.Equal(1.0, result.TotalLiters, 2);
    }

    [Fact]
    public async Task GetConsumption_MeasurementsAcrossBuckets_DistributesCorrectly()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        // Two buckets: [0, 300) and [300, 600)
        _db.Measurements.AddRange(
            new Measurement("device-1", 10, 450, now - 100), // bucket 0
            new Measurement("device-1", 10, 900, now - 400) // bucket 1
        );
        await _db.SaveChangesAsync();

        var result = await _sut.GetConsumption(600, 300);

        Assert.Equal(2, result.Buckets.Count);
        Assert.Equal(3.0, result.TotalLiters, 2);
    }

    [Fact]
    public async Task GetConsumption_DeviceFilter_OnlyReturnsMatchingDevice()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        _db.Measurements.AddRange(
            new Measurement("device-1", 10, 450, now - 100),
            new Measurement("device-2", 10, 450, now - 100)
        );
        await _db.SaveChangesAsync();

        var result = await _sut.GetConsumption(600, 600, "device-1");

        Assert.Single(result.Buckets);
        Assert.Equal(1.0, result.TotalLiters, 2);
    }

    [Fact]
    public async Task GetConsumption_DeviceFilterNull_ReturnsAllDevices()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        _db.Measurements.AddRange(
            new Measurement("device-1", 10, 450, now - 100),
            new Measurement("device-2", 10, 450, now - 100)
        );
        await _db.SaveChangesAsync();

        var result = await _sut.GetConsumption(600, 600, null);

        Assert.Equal(2.0, result.TotalLiters, 2);
    }

    [Fact]
    public async Task GetConsumption_MeasurementOutsideRange_IsExcluded()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        _db.Measurements.AddRange(
            new Measurement("device-1", 10, 450, now - 100), // in range
            new Measurement("device-1", 10, 450, now - 1000) // out of range (1000s ago, range is 600s)
        );
        await _db.SaveChangesAsync();

        var result = await _sut.GetConsumption(600, 600);

        Assert.Equal(1.0, result.TotalLiters, 2);
    }

    [Fact]
    public async Task GetConsumption_BucketCount_MatchesCeilingOfRangeDivInterval()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        // range=1000, interval=300 → ceil(1000/300) = 4 buckets
        var result = await _sut.GetConsumption(1000, 300);

        Assert.Equal(4, result.Buckets.Count);
    }

    [Fact]
    public async Task GetConsumption_BucketTimestamps_CorrectlySpaced()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var rangeSeconds = 600L;
        var intervalSeconds = 300L;

        var result = await _sut.GetConsumption(rangeSeconds, intervalSeconds);

        Assert.Equal(2, result.Buckets.Count);
        // First bucket
        Assert.True(
            result.Buckets[0].To - result.Buckets[0].From == TimeSpan.FromSeconds(intervalSeconds)
        );
        // Buckets are contiguous
        Assert.Equal(result.Buckets[0].To, result.Buckets[1].From);
    }

    [Fact]
    public async Task GetConsumption_WithOffsetMinute_RangeStartSnappedToNextMidnight()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var offsetMinutes = -300; // UTC-5 (Bogotá)
        var offsetSeconds = offsetMinutes * 60;
        var rangeSeconds = 86400L;
        var intervalSeconds = 3600L;

        var rangeStart = now - rangeSeconds;
        var secondsSinceLocalMidnight = (rangeStart + offsetSeconds) % 86400;
        if (secondsSinceLocalMidnight < 0) secondsSinceLocalMidnight += 86400;
        var expectedRangeStart = rangeStart + (86400 - secondsSinceLocalMidnight);

        var result = await _sut.GetConsumption(rangeSeconds, intervalSeconds, null, offsetMinutes);

        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(expectedRangeStart), result.Buckets[0].From);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(now), result.Buckets[^1].To);
    }

    [Fact]
    public async Task GetConsumption_WithOffsetMinute_FewerBucketsThanRangeDivInterval()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var offsetMinutes = -300;
        var intervalSeconds = 3600L;

        var result = await _sut.GetConsumption(86400, intervalSeconds, null, offsetMinutes);

        Assert.True(result.Buckets.Count <= 24);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(now), result.Buckets[^1].To);
    }

    [Fact]
    public async Task GetConsumption_ZeroOffset_BucketsNotSnapped()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var result = await _sut.GetConsumption(86400, 86400, null, 0);

        Assert.Single(result.Buckets);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(now), result.Buckets[0].To);
    }
}
