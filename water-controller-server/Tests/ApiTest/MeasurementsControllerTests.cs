using Api;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Service;
using Xunit;

namespace ApiTest;

public class MeasurementsControllerTests
{
    private readonly IMeasurementService _measurementService;
    private readonly MeasurementsController _sut;

    public MeasurementsControllerTests()
    {
        _measurementService = Substitute.For<IMeasurementService>();
        _sut = new MeasurementsController(_measurementService);
    }

    [Fact]
    public async Task Post_ValidMeasurement_ReturnsOkWithResult()
    {
        var input = new MeasurementInputDto("device-1", 10, 100, 1234567890);
        var expected = new MeasurementOutputDto(1, "device-1", 10, 1234567890, 1.0);
        _measurementService.AddMeasurement(input).Returns(expected);

        var result = await _sut.Post(input);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(expected, okResult.Value);
    }

    [Fact]
    public async Task Post_DelegatesToMeasurementService()
    {
        var input = new MeasurementInputDto("device-1", 10, 100, 1234567890);
        var expected = new MeasurementOutputDto(1, "device-1", 10, 1234567890, 0.222);
        _measurementService.AddMeasurement(input).Returns(expected);

        await _sut.Post(input);

        await _measurementService.Received(1).AddMeasurement(input);
    }

    [Fact]
    public async Task Post_ReturnsDtoFromService()
    {
        var input = new MeasurementInputDto("device-1", 10, 450, 1234567890);
        var serviceResult = new MeasurementOutputDto(42, "device-1", 10, 1234567890, 1.0);
        _measurementService.AddMeasurement(input).Returns(serviceResult);

        var result = await _sut.Post(input);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var viewModel = Assert.IsType<MeasurementOutputDto>(okResult.Value);
        Assert.Equal(42, viewModel.Id);
        Assert.Equal(1.0, viewModel.LitersPerSecond);
    }
}
