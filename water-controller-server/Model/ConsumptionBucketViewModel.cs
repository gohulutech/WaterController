namespace Model;

/// <summary>
/// One aggregation bucket of the consumption time series.
/// </summary>
public sealed record ConsumptionBucketViewModel(
    DateTimeOffset From,
    DateTimeOffset To,
    double Liters);
