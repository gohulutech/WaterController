using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Model;
using NSubstitute;
using Service;
using Xunit;

namespace ServiceTest;

public class MeasurementServiceTests : IDisposable
{
    private readonly WaterControllerDbContext _db;
    private readonly ILogger<MeasurementService> _logger;
    private readonly MeasurementService _sut;

    public MeasurementServiceTests()
    {
        var options = new DbContextOptionsBuilder<WaterControllerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db = new WaterControllerDbContext(options);
        _logger = Substitute.For<ILogger<MeasurementService>>();
        _sut = new MeasurementService(_logger, _db);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    [Fact]
    public async Task AddMeasurement_SavesToDatabase()
    {
        var input = new MeasurementInputDto("device-1", 10, 100, 1234567890);

        await _sut.AddMeasurement(input);

        var saved = await _db.Measurements.SingleAsync();
        Assert.Equal("device-1", saved.DeviceId);
        Assert.Equal(100.0, saved.Value);
        Assert.Equal(1234567890, saved.Timestamp);

        var metric = await _db.Metrics.SingleAsync();
        Assert.Equal(Metric.FlowPulsesCode, metric.Code);
        Assert.Equal(metric.MetricId, saved.MetricId);
    }

    [Fact]
    public async Task AddMeasurement_ReturnsOutputDto()
    {
        var input = new MeasurementInputDto("device-1", 10, 450, 1234567890);

        var result = await _sut.AddMeasurement(input);

        Assert.Equal("device-1", result.DeviceId);
        Assert.Equal(10, result.IntervalSeconds);
        Assert.Equal(1234567890, result.Timestamp);
        Assert.Equal(450.0 / 450 / 10, result.LitersPerSecond, 6);
    }

    [Fact]
    public async Task AddMeasurement_ReturnsMeasurementIdAsId()
    {
        var input = new MeasurementInputDto("device-1", 10, 450, 1234567890);

        var result = await _sut.AddMeasurement(input);

        var saved = await _db.Measurements.SingleAsync();
        Assert.Equal(saved.MeasurementId, result.Id);
    }

    [Fact]
    public async Task AddMeasurement_LogsInformation()
    {
        var input = new MeasurementInputDto("device-1", 10, 100, 1234567890);

        await _sut.AddMeasurement(input);

        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("device-1")),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task AddMeasurement_MultipleSavesAll()
    {
        await _sut.AddMeasurement(new MeasurementInputDto("device-1", 10, 100, 1000));
        await _sut.AddMeasurement(new MeasurementInputDto("device-2", 5, 200, 2000));

        Assert.Equal(2, await _db.Measurements.CountAsync());
    }

    [Fact]
    public async Task AddMeasurement_AutoProvisionsCatalog()
    {
        await _sut.AddMeasurement(new MeasurementInputDto("device-1", 10, 100, 1234567890));

        var deviceType = await _db.DeviceTypes.SingleAsync();
        Assert.Equal("Flow Sensor", deviceType.Name);

        var metric = await _db.Metrics.SingleAsync();
        Assert.Equal(Metric.FlowPulsesCode, metric.Code);
        Assert.Equal("Flow pulses", metric.Name);
        Assert.Equal("pulses", metric.Unit);

        var device = await _db.Devices.SingleAsync();
        Assert.Equal("device-1", device.DeviceId);
        Assert.Equal("device-1", device.Name);
        Assert.Equal(deviceType.DeviceTypeId, device.DeviceTypeId);

        var link = await _db.DeviceMetrics.SingleAsync();
        Assert.Equal("device-1", link.DeviceId);
        Assert.Equal(metric.MetricId, link.MetricId);
    }

    [Fact]
    public async Task AddMeasurement_RepeatedCalls_ReuseCatalog()
    {
        await _sut.AddMeasurement(new MeasurementInputDto("device-1", 10, 100, 1000));
        await _sut.AddMeasurement(new MeasurementInputDto("device-1", 10, 200, 2000));

        Assert.Equal(1, await _db.DeviceTypes.CountAsync());
        Assert.Equal(1, await _db.Metrics.CountAsync());
        Assert.Equal(1, await _db.Devices.CountAsync());
        Assert.Equal(1, await _db.DeviceMetrics.CountAsync());
        Assert.Equal(2, await _db.Measurements.CountAsync());
    }

    [Fact]
    public async Task AddMeasurement_SecondDevice_CreatesSecondDeviceAndLink_ReusesTypeAndMetric()
    {
        await _sut.AddMeasurement(new MeasurementInputDto("device-1", 10, 100, 1000));
        await _sut.AddMeasurement(new MeasurementInputDto("device-2", 10, 100, 2000));

        Assert.Equal(1, await _db.DeviceTypes.CountAsync());
        Assert.Equal(1, await _db.Metrics.CountAsync());
        Assert.Equal(2, await _db.Devices.CountAsync());
        Assert.Equal(2, await _db.DeviceMetrics.CountAsync());
        Assert.Equal(2, await _db.Measurements.CountAsync());
    }
}
