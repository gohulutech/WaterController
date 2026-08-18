namespace Service;

/// <summary>
/// One aggregation bucket of the consumption time series.
/// </summary>
public sealed record ConsumptionBucketDto(
    DateTimeOffset From,
    DateTimeOffset To,
    double Liters);
