import { describe, it, expect } from "vitest";
import { render, screen, fireEvent } from "@testing-library/react";
import Header from "./Header";

// Mock i18next
vi.mock("react-i18next", () => ({
  useTranslation: () => ({
    t: (key: string) => {
      const translations: Record<string, string> = {
        "dashboard.title": "Water Consumption Dashboard",
        "header.filter.1d-1h": "1 Day - 1 Hour",
        "header.filter.7d-1d": "7 Days - 1 Day",
        "header.filter.30d-1d": "30 Days - 1 Day",
      };
      return translations[key] || key;
    },
  }),
}));

describe("Header", () => {
  it("renders the dashboard title", () => {
    render(<Header filter="7d-1d" onFilterChange={() => {}} />);
    expect(screen.getByText("Water Consumption Dashboard")).toBeInTheDocument();
  });

  it("renders the filter select", () => {
    render(<Header filter="7d-1d" onFilterChange={() => {}} />);
    expect(screen.getByRole("combobox")).toHaveValue("7d-1d");
  });

  it("calls onFilterChange when selection changes", async () => {
    const handleChange = vi.fn();
    render(<Header filter="7d-1d" onFilterChange={handleChange} />);

    fireEvent.change(screen.getByRole("combobox"), {
      target: { value: "1d-1h" },
    });

    expect(handleChange).toHaveBeenCalledWith("1d-1h");
  });
});
