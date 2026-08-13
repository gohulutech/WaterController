using Model;

namespace Service;

public interface IMeasurementService
{
    Task<Measurement> AddMeasurement(Measurement measurement);
}
