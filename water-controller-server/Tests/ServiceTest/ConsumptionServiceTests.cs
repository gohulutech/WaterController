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

    private async Task<Metric> SeedMetric(string code, string name, string unit)
    {
        var metric = new Metric { Code = code, Name = name, Unit = unit };
        _db.Metrics.Add(metric);
        await _db.SaveChangesAsync();
        return metric;
    }

    private async Task AddMeasurement(Metric metric, string deviceId, double value, long timestamp)
    {
        _db.Measurements.Add(
            new Measurement
            {
                DeviceId = deviceId,
                MetricId = metric.MetricId,
                Timestamp = timestamp,
                Value = value,
                Metric = metric,
            });
        await _db.SaveChangesAsync();
    }

    private async Task<Metric> SeedFlowPulsesAsync()
    {
        var deviceType = new DeviceType { Name = "Flow Sensor" };
        var metric = new Metric { Code = Metric.FlowPulsesCode, Name = "Flow pulses", Unit = "pulses" };
        var device = new Device { DeviceId = "device-1", Name = "device-1", DeviceType = deviceType };
        _db.DeviceTypes.Add(deviceType);
        _db.Metrics.Add(metric);
        _db.Devices.Add(device);
        _db.DeviceMetrics.Add(new DeviceMetric { Device = device, Metric = metric });
        await _db.SaveChangesAsync();
        return metric;
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
        var metric = await SeedFlowPulsesAsync();
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        // range=600, interval=300 → 2 buckets: [now-600, now-300) and [now-300, now)
        await AddMeasurement(metric, "device-1", 450, now - 100);

        var result = await _sut.GetConsumption(600, 300);

        Assert.Equal(2, result.Buckets.Count);
        Assert.Equal(0, result.Buckets[0].Liters, 2);
        Assert.Equal(1.0, result.Buckets[1].Liters, 2);
        Assert.Equal(1.0, result.TotalLiters, 2);
    }

    [Fact]
    public async Task GetConsumption_MultipleMeasurements_SumsWithinBucket()
    {
        var metric = await SeedFlowPulsesAsync();
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        await AddMeasurement(metric, "device-1", 225, now - 50);
        await AddMeasurement(metric, "device-1", 225, now - 80);

        var result = await _sut.GetConsumption(600, 600);

        Assert.Single(result.Buckets);
        Assert.Equal(1.0, result.TotalLiters, 2);
    }

    [Fact]
    public async Task GetConsumption_MeasurementsAcrossBuckets_DistributesCorrectly()
    {
        var metric = await SeedFlowPulsesAsync();
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        // Two buckets: [0, 300) and [300, 600)
        await AddMeasurement(metric, "device-1", 450, now - 100); // bucket 0
        await AddMeasurement(metric, "device-1", 900, now - 400); // bucket 1

        var result = await _sut.GetConsumption(600, 300);

        Assert.Equal(2, result.Buckets.Count);
        Assert.Equal(3.0, result.TotalLiters, 2);
    }

    [Fact]
    public async Task GetConsumption_DeviceFilter_OnlyReturnsMatchingDevice()
    {
        var metric = await SeedFlowPulsesAsync();
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        await AddMeasurement(metric, "device-1", 450, now - 100);
        await AddMeasurement(metric, "device-2", 450, now - 100);

        var result = await _sut.GetConsumption(600, 600, "device-1");

        Assert.Single(result.Buckets);
        Assert.Equal(1.0, result.TotalLiters, 2);
    }

    [Fact]
    public async Task GetConsumption_DeviceFilterNull_ReturnsAllDevices()
    {
        var metric = await SeedFlowPulsesAsync();
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        await AddMeasurement(metric, "device-1", 450, now - 100);
        await AddMeasurement(metric, "device-2", 450, now - 100);

        var result = await _sut.GetConsumption(600, 600, null);

        Assert.Equal(2.0, result.TotalLiters, 2);
    }

    [Fact]
    public async Task GetConsumption_MeasurementOutsideRange_IsExcluded()
    {
        var metric = await SeedFlowPulsesAsync();
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        await AddMeasurement(metric, "device-1", 450, now - 100); // in range
        await AddMeasurement(metric, "device-1", 450, now - 1000); // out of range

        var result = await _sut.GetConsumption(600, 600);

        Assert.Equal(1.0, result.TotalLiters, 2);
    }

    [Fact]
    public async Task GetConsumption_IgnoresNonFlowPulsesMetrics()
    {
        var metric = await SeedFlowPulsesAsync();
        var otherMetric = await SeedMetric("temperature", "Temperature", "celsius");
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        await AddMeasurement(metric, "device-1", 450, now - 100);
        await AddMeasurement(otherMetric, "device-1", 450, now - 100);

        var result = await _sut.GetConsumption(600, 600);

        Assert.Equal(1.0, result.TotalLiters, 2);
    }

    [Fact]
    public async Task GetConsumption_BucketCount_MatchesCeilingOfRangeDivInterval()
    {
        // range=1000, interval=300 → ceil(1000/300) = 4 buckets
        var result = await _sut.GetConsumption(1000, 300);

        Assert.Equal(4, result.Buckets.Count);
    }

    [Fact]
    public async Task GetConsumption_BucketTimestamps_CorrectlySpaced()
    {
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
