using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Model;

namespace Service;

public sealed class MeasurementService(
    ILogger<MeasurementService> logger,
    WaterControllerDbContext db) : IMeasurementService
{
    public async Task<MeasurementOutputDto> AddMeasurement(MeasurementInputDto input)
    {
        logger.LogInformation(
            "Received measurement: device={DeviceId}, interval={IntervalSeconds}s, pulses={Pulses}, timestamp={Timestamp}",
            input.DeviceId,
            input.IntervalSeconds,
            input.Pulses,
            input.Timestamp);

        var metric = await db.Metrics
            .SingleOrDefaultAsync(m => m.Code == Metric.FlowPulsesCode);
        if (metric is null)
        {
            metric = new Metric
            {
                Code = Metric.FlowPulsesCode,
                Name = "Flow pulses",
                Unit = "pulses",
            };
            db.Metrics.Add(metric);
        }

        var deviceType = await db.DeviceTypes
            .SingleOrDefaultAsync(t => t.Name == "Flow Sensor");
        if (deviceType is null)
        {
            deviceType = new DeviceType { Name = "Flow Sensor" };
            db.DeviceTypes.Add(deviceType);
        }

        var device = await db.Devices
            .SingleOrDefaultAsync(d => d.DeviceId == input.DeviceId);
        if (device is null)
        {
            device = new Device
            {
                DeviceId = input.DeviceId,
                Name = input.DeviceId,
                DeviceType = deviceType,
            };
            db.Devices.Add(device);
        }

        await db.SaveChangesAsync();

        var link = await db.DeviceMetrics.SingleOrDefaultAsync(dm =>
            dm.DeviceId == device.DeviceId && dm.MetricId == metric.MetricId);
        if (link is null)
        {
            db.DeviceMetrics.Add(
                new DeviceMetric
                {
                    DeviceId = device.DeviceId,
                    MetricId = metric.MetricId,
                });
        }

        var measurement = new Measurement
        {
            DeviceId = device.DeviceId,
            MetricId = metric.MetricId,
            Timestamp = input.Timestamp,
            Value = input.Pulses,
        };

        db.Measurements.Add(measurement);
        await db.SaveChangesAsync();

        return MeasurementOutputDto.FromMeasurement(measurement, input.IntervalSeconds, input.Pulses);
    }
}
