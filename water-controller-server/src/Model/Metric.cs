namespace Model;

public sealed class Metric
{
    public const string FlowPulsesCode = "flow-pulses";

    public int MetricId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;

    public ICollection<Measurement> Measurements { get; set; } = new List<Measurement>();
    public ICollection<DeviceMetric> DeviceMetrics { get; set; } = new List<DeviceMetric>();
}
