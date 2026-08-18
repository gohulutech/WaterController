namespace Service;

/// <summary>
/// Consumed liters over a time range, split into interval buckets.
/// </summary>
public sealed record ConsumptionOutputDto(
    double TotalLiters,
    IReadOnlyList<ConsumptionBucketDto> Buckets);
