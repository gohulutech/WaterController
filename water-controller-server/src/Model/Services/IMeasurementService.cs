using Model;

namespace Model.Services;

public interface IMeasurementService
{
    Task<MeasurementOutputViewModel> AddMeasurement(MeasurementInputViewModel input);
}
