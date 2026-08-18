const API_BASE = `${import.meta.env.VITE_API_URL}/api`;

export interface ConsumptionBucket {
  from: string;
  to: string;
  liters: number;
}

export interface ConsumptionResponse {
  totalLiters: number;
  buckets: ConsumptionBucket[];
}

export interface ConsumptionParams {
  range: string; // e.g. "24h", "7d", "30d"
  interval: string; // e.g. "1h", "1d"
  deviceId?: string;
  offsetMinutes?: number;
}

export async function fetchConsumption(
  params: ConsumptionParams,
): Promise<ConsumptionResponse> {
  const query = new URLSearchParams({
    range: params.range,
    interval: params.interval,
  });

  if (params.deviceId) {
    query.set("deviceId", params.deviceId);
  }

  if (params.offsetMinutes != null)
    query.set("offsetMinutes", String(params.offsetMinutes));

  const res = await fetch(`${API_BASE}/consumption?${query}`);

  if (!res.ok) {
    const text = await res.text();
    throw new Error(`Failed to fetch consumption: ${res.status} ${text}`);
  }

  return res.json();
}
