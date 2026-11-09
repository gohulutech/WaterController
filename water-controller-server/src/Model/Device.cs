namespace Model;

public sealed class Device
{
    public const int PulsesPerLiter = 450;

    public string DeviceId { get; set; } = string.Empty;
    public int DeviceTypeId { get; set; }
    public string Name { get; set; } = string.Empty;

    public DeviceType DeviceType { get; set; } = null!;
    public ICollection<Measurement> Measurements { get; set; } = new List<Measurement>();
    public ICollection<DeviceMetric> DeviceMetrics { get; set; } = new List<DeviceMetric>();
}
