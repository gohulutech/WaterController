namespace Model;

public sealed class Measurement
{
    public long MeasurementId { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public int MetricId { get; set; }
    public long Timestamp { get; set; }
    public double Value { get; set; }

    public Device Device { get; set; } = null!;
    public Metric Metric { get; set; } = null!;
}
