import { describe, it, expect } from "vitest";
import { render, screen } from "@testing-library/react";
import SummaryCards from "./SummaryCards";
import type { ConsumptionBucket } from "../../api/consumption";

// Mock i18next
vi.mock("react-i18next", () => ({
  useTranslation: () => ({
    t: (key: string) => {
      const translations: Record<string, string> = {
        "summary.totalVolume": "Total Volume",
        "summary.dailyAverage": "Daily Average",
        "summary.hourlyAverage": "Hourly Average",
        "summary.peakSingleDay": "Peak Single Day",
        "summary.peakSingleHour": "Peak Single Hour",
        "summary.activeDays": "Active Days",
        "summary.activeHours": "Active Hours",
        "summary.withUsage": "with usage",
        "summary.perDay": "L/day",
        "summary.perHour": "L/hr",
        "header.range.7d": "7 Days",
        "header.range.1d": "1 Day",
        "header.range.30d": "30 Days",
      };
      return translations[key] || key;
    },
  }),
}));

const mockBuckets: ConsumptionBucket[] = [
  { from: "2026-08-10T19:29:14+00:00", to: "2026-08-11T19:29:14+00:00", liters: 0 },
  { from: "2026-08-11T19:29:14+00:00", to: "2026-08-12T19:29:14+00:00", liters: 0 },
  { from: "2026-08-12T19:29:14+00:00", to: "2026-08-13T19:29:14+00:00", liters: 213.28 },
  { from: "2026-08-13T19:29:14+00:00", to: "2026-08-14T19:29:14+00:00", liters: 13.88 },
];

describe("SummaryCards", () => {
  it("renders total volume", () => {
    render(<SummaryCards buckets={mockBuckets} range="7d" interval="1d" />);
    expect(screen.getByText("227.16 L")).toBeInTheDocument();
  });

  it("renders daily average", () => {
    render(<SummaryCards buckets={mockBuckets} range="7d" interval="1d" />);
    expect(screen.getByText("56.79 L/day")).toBeInTheDocument();
  });

  it("renders peak bucket", () => {
    render(<SummaryCards buckets={mockBuckets} range="7d" interval="1d" />);
    expect(screen.getByText(/213\.28 L/)).toBeInTheDocument();
  });

  it("renders active days count", () => {
    render(<SummaryCards buckets={mockBuckets} range="7d" interval="1d" />);
    expect(screen.getByText(/2 \/ 4/)).toBeInTheDocument();
  });

  it("shows hourly labels when interval is 1h", () => {
    render(<SummaryCards buckets={mockBuckets} range="1d" interval="1h" />);
    expect(screen.getByText("Hourly Average")).toBeInTheDocument();
    expect(screen.getByText("Peak Single Hour")).toBeInTheDocument();
    expect(screen.getByText("Active Hours")).toBeInTheDocument();
  });
});
