using Model;
using Service;
using Xunit;

namespace ServiceTest;

public class MeasurementOutputDtoTests
{
    [Fact]
    public void FromMeasurement_CalculatesLitersPerSecondCorrectly()
    {
        var measurement = new Measurement
        {
            MeasurementId = 1,
            DeviceId = "device-1",
            MetricId = 1,
            Timestamp = 1234567890,
            Value = 450,
        };

        var result = MeasurementOutputDto.FromMeasurement(measurement, 10, 450);

        // 450 pulses / 450 pulsesPerLiter / 10 seconds = 0.1 L/s
        Assert.Equal(0.1, result.LitersPerSecond, 6);
    }

    [Fact]
    public void FromMeasurement_CopiesAllFields()
    {
        var measurement = new Measurement
        {
            MeasurementId = 42,
            DeviceId = "device-1",
            MetricId = 1,
            Timestamp = 1234567890,
            Value = 450,
        };

        var result = MeasurementOutputDto.FromMeasurement(measurement, 10, 450);

        Assert.Equal(42, result.Id);
        Assert.Equal("device-1", result.DeviceId);
        Assert.Equal(10, result.IntervalSeconds);
        Assert.Equal(1234567890, result.Timestamp);
    }

    [Fact]
    public void FromMeasurement_ZeroPulses_ReturnsZeroLitersPerSecond()
    {
        var measurement = new Measurement
        {
            MeasurementId = 1,
            DeviceId = "device-1",
            MetricId = 1,
            Timestamp = 1234567890,
            Value = 0,
        };

        var result = MeasurementOutputDto.FromMeasurement(measurement, 10, 0);

        Assert.Equal(0.0, result.LitersPerSecond);
    }

    [Fact]
    public void FromMeasurement_HigherPulseCount_ScalesCorrectly()
    {
        var measurement = new Measurement
        {
            MeasurementId = 1,
            DeviceId = "device-1",
            MetricId = 1,
            Timestamp = 1234567890,
            Value = 900,
        };

        var result = MeasurementOutputDto.FromMeasurement(measurement, 1, 900);

        // 900 / 450 / 1 = 2.0 L/s
        Assert.Equal(2.0, result.LitersPerSecond, 6);
    }

    [Fact]
    public void PulsesPerLiter_Is450()
    {
        Assert.Equal(450, Device.PulsesPerLiter);
    }
}
