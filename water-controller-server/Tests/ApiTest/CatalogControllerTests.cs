using Api;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Service;
using Xunit;

namespace ApiTest;

public class CatalogControllerTests
{
    private readonly ICatalogService _catalogService;
    private readonly CatalogController _sut;

    public CatalogControllerTests()
    {
        _catalogService = Substitute.For<ICatalogService>();
        _sut = new CatalogController(_catalogService);
    }

    [Fact]
    public async Task GetDeviceTypes_ReturnsOkWithResult()
    {
        var expected = new List<DeviceTypeDto> { new(1, "Flow Sensor") };
        _catalogService.GetDeviceTypes().Returns(expected);

        var result = await _sut.GetDeviceTypes();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(expected, okResult.Value);
    }

    [Fact]
    public async Task GetDevices_ReturnsOkWithResult()
    {
        var expected = new List<DeviceDto> { new("device-1", 1, "device-1") };
        _catalogService.GetDevices().Returns(expected);

        var result = await _sut.GetDevices();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(expected, okResult.Value);
    }

    [Fact]
    public async Task GetMetrics_ReturnsOkWithResult()
    {
        var expected = new List<MetricDto> { new(1, "flow-pulses", "Flow pulses", "pulses") };
        _catalogService.GetMetrics().Returns(expected);

        var result = await _sut.GetMetrics();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(expected, okResult.Value);
    }

    [Fact]
    public async Task GetDeviceMetrics_ReturnsOkWithResult()
    {
        var expected = new List<MetricDto> { new(1, "flow-pulses", "Flow pulses", "pulses") };
        _catalogService.GetDeviceMetrics("device-1").Returns(expected);

        var result = await _sut.GetDeviceMetrics("device-1");

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(expected, okResult.Value);
    }

    [Fact]
    public async Task GetDeviceMetrics_UnknownDevice_ReturnsNotFound()
    {
        _catalogService.GetDeviceMetrics("unknown").Returns((IReadOnlyList<MetricDto>?)null);

        var result = await _sut.GetDeviceMetrics("unknown");

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetDeviceTypes_DelegatesToCatalogService()
    {
        _catalogService.GetDeviceTypes().Returns(new List<DeviceTypeDto>());

        await _sut.GetDeviceTypes();

        await _catalogService.Received(1).GetDeviceTypes();
    }
}
