namespace Model;

public sealed class DeviceMetric
{
    public string DeviceId { get; set; } = string.Empty;
    public int MetricId { get; set; }

    public Device Device { get; set; } = null!;
    public Metric Metric { get; set; } = null!;
}
