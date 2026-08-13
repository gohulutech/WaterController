using Infrastructure;
using Microsoft.Extensions.Logging;
using Model;

namespace Service;

public sealed class MeasurementService(
    ILogger<MeasurementService> logger,
    WaterControllerDbContext db) : IMeasurementService
{
    public async Task<Measurement> AddMeasurement(Measurement measurement)
    {
        logger.LogInformation(
            "Received measurement: device={DeviceId}, interval={IntervalSeconds}s, pulses={Pulses}, timestamp={Timestamp}",
            measurement.DeviceId,
            measurement.IntervalSeconds,
            measurement.PulsesLastSecond,
            measurement.Timestamp);

        db.Measurements.Add(measurement);
        await db.SaveChangesAsync();

        return measurement;
    }
}
