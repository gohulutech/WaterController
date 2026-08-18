namespace Service;

public interface IMeasurementService
{
    Task<MeasurementOutputViewModel> AddMeasurement(MeasurementInputViewModel input);
}
