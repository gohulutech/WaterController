using Model;
using Xunit;

namespace ModelTest;

public class MeasurementOutputViewModelTests
{
    [Fact]
    public void FromMeasurement_CalculatesLitersPerSecondCorrectly()
    {
        var measurement = new Measurement("device-1", 10, 450, 1234567890, 1);

        var result = MeasurementOutputViewModel.FromMeasurement(measurement);

        // 450 pulses / 450 pulsesPerLiter / 10 seconds = 0.1 L/s
        Assert.Equal(0.1, result.LitersPerSecond, 6);
    }

    [Fact]
    public void FromMeasurement_CopiesAllFields()
    {
        var measurement = new Measurement("device-1", 10, 450, 1234567890, 42);

        var result = MeasurementOutputViewModel.FromMeasurement(measurement);

        Assert.Equal(42, result.Id);
        Assert.Equal("device-1", result.DeviceId);
        Assert.Equal(10, result.IntervalSeconds);
        Assert.Equal(1234567890, result.Timestamp);
    }

    [Fact]
    public void FromMeasurement_ZeroPulses_ReturnsZeroLitersPerSecond()
    {
        var measurement = new Measurement("device-1", 10, 0, 1234567890, 1);

        var result = MeasurementOutputViewModel.FromMeasurement(measurement);

        Assert.Equal(0.0, result.LitersPerSecond);
    }

    [Fact]
    public void FromMeasurement_HigherPulseCount_ScalesCorrectly()
    {
        var measurement = new Measurement("device-1", 1, 900, 1234567890, 1);

        var result = MeasurementOutputViewModel.FromMeasurement(measurement);

        // 900 / 450 / 1 = 2.0 L/s
        Assert.Equal(2.0, result.LitersPerSecond, 6);
    }

    [Fact]
    public void PulsesPerLiter_Is450()
    {
        Assert.Equal(450, Device.PulsesPerLiter);
    }
}
