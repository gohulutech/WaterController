namespace Service;

public interface IConsumptionService
{
    Task<ConsumptionOutputViewModel> GetConsumption(long rangeSeconds, long intervalSeconds, string? deviceId = null);
}
