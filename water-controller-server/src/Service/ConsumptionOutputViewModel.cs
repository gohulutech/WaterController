namespace Service;

/// <summary>
/// Consumed liters over a time range, split into interval buckets.
/// </summary>
public sealed record ConsumptionOutputViewModel(
    double TotalLiters,
    IReadOnlyList<ConsumptionBucketViewModel> Buckets);
