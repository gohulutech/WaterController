namespace Service;

public interface IMeasurementService
{
    Task<MeasurementOutputDto> AddMeasurement(MeasurementInputDto input);
}
