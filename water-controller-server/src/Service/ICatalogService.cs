namespace Service;

public interface ICatalogService
{
    Task<IReadOnlyList<DeviceTypeDto>> GetDeviceTypes();
    Task<IReadOnlyList<DeviceDto>> GetDevices();
    Task<IReadOnlyList<MetricDto>> GetMetrics();
    Task<IReadOnlyList<MetricDto>?> GetDeviceMetrics(string deviceId);
}
