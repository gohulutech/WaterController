namespace Service;

public interface IConsumptionService
{
    Task<ConsumptionOutputDto> GetConsumption(
        long rangeSeconds,
        long intervalSeconds,
        string? deviceId = null,
        int offsetMinutes = 0
    );
}
