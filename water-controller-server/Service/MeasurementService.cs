using Infrastructure;
using Microsoft.Extensions.Logging;
using Model;
using Model.Services;

namespace Service;

public sealed class MeasurementService(
    ILogger<MeasurementService> logger,
    WaterControllerDbContext db) : IMeasurementService
{
    public async Task<MeasurementOutputViewModel> AddMeasurement(MeasurementInputViewModel input)
    {
        logger.LogInformation(
            "Received measurement: device={DeviceId}, interval={IntervalSeconds}s, pulses={Pulses}, timestamp={Timestamp}",
            input.DeviceId,
            input.IntervalSeconds,
            input.Pulses,
            input.Timestamp);

        var measurement = new Measurement(
            input.DeviceId,
            input.IntervalSeconds,
            input.Pulses,
            input.Timestamp);

        db.Measurements.Add(measurement);
        await db.SaveChangesAsync();

        return MeasurementOutputViewModel.FromMeasurement(measurement);
    }
}
