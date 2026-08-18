import { describe, it, expect, vi, beforeEach } from "vitest";
import { fetchConsumption } from "./consumption";

describe("fetchConsumption", () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it("fetches consumption data with correct URL", async () => {
    const mockData = {
      totalLiters: 100,
      buckets: [
        { from: "2026-08-10T19:29:14+00:00", to: "2026-08-11T19:29:14+00:00", liters: 50 },
      ],
    };

    vi.spyOn(globalThis, "fetch").mockResolvedValueOnce(
      new Response(JSON.stringify(mockData), { status: 200 })
    );

    const result = await fetchConsumption({ range: "7d", interval: "1d" });

    expect(fetch).toHaveBeenCalledWith(
      expect.stringContaining("/api/consumption?range=7d&interval=1d")
    );
    expect(result).toEqual(mockData);
  });

  it("includes deviceId when provided", async () => {
    const mockData = { totalLiters: 0, buckets: [] };

    vi.spyOn(globalThis, "fetch").mockResolvedValueOnce(
      new Response(JSON.stringify(mockData), { status: 200 })
    );

    await fetchConsumption({ range: "24h", interval: "1h", deviceId: "sensor-1" });

    expect(fetch).toHaveBeenCalledWith(
      expect.stringContaining("deviceId=sensor-1")
    );
  });

  it("throws error on non-OK response", async () => {
    vi.spyOn(globalThis, "fetch").mockResolvedValueOnce(
      new Response("Bad Request", { status: 400, statusText: "Bad Request" })
    );

    await expect(
      fetchConsumption({ range: "invalid", interval: "1h" })
    ).rejects.toThrow("Failed to fetch consumption: 400");
  });
});
