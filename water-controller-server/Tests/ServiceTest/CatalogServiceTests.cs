using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Model;
using Service;
using Xunit;

namespace ServiceTest;

public class CatalogServiceTests : IDisposable
{
    private readonly WaterControllerDbContext _db;
    private readonly CatalogService _sut;

    public CatalogServiceTests()
    {
        var options = new DbContextOptionsBuilder<WaterControllerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db = new WaterControllerDbContext(options);
        _sut = new CatalogService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    private async Task SeedCatalogAsync()
    {
        var deviceType = new DeviceType { Name = "Flow Sensor" };
        var metric = new Metric { Code = Metric.FlowPulsesCode, Name = "Flow pulses", Unit = "pulses" };
        var device = new Device { DeviceId = "device-1", Name = "device-1", DeviceType = deviceType };

        _db.DeviceTypes.Add(deviceType);
        _db.Metrics.Add(metric);
        _db.Devices.Add(device);
        _db.DeviceMetrics.Add(new DeviceMetric { Device = device, Metric = metric });
        await _db.SaveChangesAsync();
    }

    [Fact]
    public async Task GetDeviceTypes_ReturnsSeededTypes()
    {
        await SeedCatalogAsync();

        var result = await _sut.GetDeviceTypes();

        var type = Assert.Single(result);
        Assert.Equal("Flow Sensor", type.Name);
        Assert.True(type.DeviceTypeId > 0);
    }

    [Fact]
    public async Task GetDevices_ReturnsSeededDevices()
    {
        await SeedCatalogAsync();

        var result = await _sut.GetDevices();

        var device = Assert.Single(result);
        Assert.Equal("device-1", device.DeviceId);
        Assert.Equal("device-1", device.Name);
        Assert.True(device.DeviceTypeId > 0);
    }

    [Fact]
    public async Task GetMetrics_ReturnsSeededMetrics()
    {
        await SeedCatalogAsync();

        var result = await _sut.GetMetrics();

        var metric = Assert.Single(result);
        Assert.Equal(Metric.FlowPulsesCode, metric.Code);
        Assert.Equal("Flow pulses", metric.Name);
        Assert.Equal("pulses", metric.Unit);
    }

    [Fact]
    public async Task GetDeviceMetrics_ReturnsOnlyLinkedMetrics()
    {
        await SeedCatalogAsync();
        _db.Metrics.Add(new Metric { Code = "temperature", Name = "Temperature", Unit = "celsius" });
        await _db.SaveChangesAsync();

        var result = await _sut.GetDeviceMetrics("device-1");

        var metric = Assert.Single(result);
        Assert.Equal(Metric.FlowPulsesCode, metric.Code);
    }

    [Fact]
    public async Task GetDeviceMetrics_UnknownDevice_ReturnsNull()
    {
        await SeedCatalogAsync();

        var result = await _sut.GetDeviceMetrics("unknown");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetDeviceMetrics_DeviceWithNoLinks_ReturnsEmptyList()
    {
        await SeedCatalogAsync();
        _db.Devices.Add(
            new Device
            {
                DeviceId = "device-2",
                Name = "device-2",
                DeviceType = new DeviceType { Name = "Flow Sensor" },
            });
        await _db.SaveChangesAsync();

        var result = await _sut.GetDeviceMetrics("device-2");

        Assert.NotNull(result);
        Assert.Empty(result);
    }
}
