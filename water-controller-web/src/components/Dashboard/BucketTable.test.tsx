import { describe, it, expect } from "vitest";
import { render, screen } from "@testing-library/react";
import BucketTable from "./BucketTable";
import type { ConsumptionBucket } from "../../api/consumption";

// Mock i18next
vi.mock("react-i18next", () => ({
  useTranslation: () => ({
    t: (key: string) => {
      const translations: Record<string, string> = {
        "table.title": "BUCKET BREAKDOWN",
        "table.dateInterval": "Date Interval",
        "table.litersConsumed": "Liters Consumed",
        "table.percentOfTotal": "% of Total",
        "table.status": "Status",
        "table.high": "High",
        "table.normal": "Normal",
        "table.idle": "Idle",
      };
      return translations[key] || key;
    },
  }),
}));

const mockBuckets: ConsumptionBucket[] = [
  { from: "2026-08-12T19:29:14+00:00", to: "2026-08-13T19:29:14+00:00", liters: 213.28 },
  { from: "2026-08-13T19:29:14+00:00", to: "2026-08-14T19:29:14+00:00", liters: 13.88 },
  { from: "2026-08-14T19:29:14+00:00", to: "2026-08-15T19:29:14+00:00", liters: 0 },
];

describe("BucketTable", () => {
  it("renders table headers", () => {
    render(<BucketTable buckets={mockBuckets} totalLiters={227.16} />);
    expect(screen.getByText("Date Interval")).toBeInTheDocument();
    expect(screen.getByText("Liters Consumed")).toBeInTheDocument();
    expect(screen.getByText("% of Total")).toBeInTheDocument();
    expect(screen.getByText("Status")).toBeInTheDocument();
  });

  it("renders bucket rows", () => {
    render(<BucketTable buckets={mockBuckets} totalLiters={227.16} />);
    expect(screen.getByText("213.28 L")).toBeInTheDocument();
    expect(screen.getByText("13.88 L")).toBeInTheDocument();
    expect(screen.getByText("0.00 L")).toBeInTheDocument();
  });

  it("shows High status for over 100L", () => {
    render(<BucketTable buckets={mockBuckets} totalLiters={227.16} />);
    expect(screen.getByText("High")).toBeInTheDocument();
  });

  it("shows Normal status for 1-100L", () => {
    render(<BucketTable buckets={mockBuckets} totalLiters={227.16} />);
    expect(screen.getByText("Normal")).toBeInTheDocument();
  });

  it("shows Idle status for 0L", () => {
    render(<BucketTable buckets={mockBuckets} totalLiters={227.16} />);
    expect(screen.getByText("Idle")).toBeInTheDocument();
  });
});
