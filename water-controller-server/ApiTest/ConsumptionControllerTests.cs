using Api;
using Microsoft.AspNetCore.Mvc;
using Model;
using Model.Services;
using NSubstitute;
using Xunit;

namespace ApiTest;

public class ConsumptionControllerTests
{
    private readonly IConsumptionService _consumptionService;
    private readonly ConsumptionController _sut;

    public ConsumptionControllerTests()
    {
        _consumptionService = Substitute.For<IConsumptionService>();
        _sut = new ConsumptionController(_consumptionService);
    }

    [Fact]
    public async Task Get_ValidRequest_ReturnsOkWithResult()
    {
        var expected = new ConsumptionOutputViewModel(10.5, []);
        _consumptionService.GetConsumption(86400, 3600, null).Returns(expected);

        var result = await _sut.Get("24h", "1h", null);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(expected, okResult.Value);
    }

    [Fact]
    public async Task Get_InvalidRange_ReturnsBadRequest()
    {
        var result = await _sut.Get("invalid", "1h");

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("Invalid range", badRequest.Value!.ToString());
    }

    [Fact]
    public async Task Get_InvalidInterval_ReturnsBadRequest()
    {
        var result = await _sut.Get("24h", "invalid");

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("Invalid interval", badRequest.Value!.ToString());
    }

    [Fact]
    public async Task Get_IntervalLargerThanRange_ReturnsBadRequest()
    {
        var result = await _sut.Get("1h", "24h");

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("Interval cannot be larger", badRequest.Value!.ToString());
    }

    [Fact]
    public async Task Get_WithDeviceId_PassesToDeviceService()
    {
        var expected = new ConsumptionOutputViewModel(5.0, []);
        _consumptionService.GetConsumption(86400, 3600, "device-1").Returns(expected);

        var result = await _sut.Get("24h", "1h", "device-1");

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(expected, okResult.Value);
    }

    [Fact]
    public async Task Get_WhitespaceDeviceId_NormalizesToNull()
    {
        var expected = new ConsumptionOutputViewModel(5.0, []);
        _consumptionService.GetConsumption(86400, 3600, null).Returns(expected);

        var result = await _sut.Get("24h", "1h", "   ");

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(expected, okResult.Value);
    }

    [Fact]
    public async Task Get_TrimsDeviceIdWhitespace()
    {
        var expected = new ConsumptionOutputViewModel(5.0, []);
        _consumptionService.GetConsumption(86400, 3600, "device-1").Returns(expected);

        var result = await _sut.Get("24h", "1h", "  device-1  ");

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(expected, okResult.Value);
    }

    [Theory]
    [InlineData("30s", "30s")]
    [InlineData("5m", "5m")]
    [InlineData("7d", "1d")]
    public async Task Get_ValidDurations_CallsServiceWithCorrectSeconds(string range, string interval)
    {
        var expected = new ConsumptionOutputViewModel(0, []);
        _consumptionService
            .GetConsumption(Arg.Any<long>(), Arg.Any<long>(), Arg.Any<string?>())
            .Returns(expected);

        await _sut.Get(range, interval);

        await _consumptionService.Received(1).GetConsumption(
            Arg.Any<long>(),
            Arg.Any<long>(),
            Arg.Any<string?>());
    }
}
