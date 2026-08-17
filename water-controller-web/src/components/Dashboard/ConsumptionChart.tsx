import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
} from "recharts";
import { format, parseISO } from "date-fns";
import type { ConsumptionBucket } from "../../api/consumption";

interface ConsumptionChartProps {
  buckets: ConsumptionBucket[];
}

const dayNames = ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"];

function formatAxisDate(iso: string): string {
  const d = parseISO(iso);
  const dayName = dayNames[d.getDay()];
  return `${dayName} ${format(d, "MM/dd")}`;
}

function formatTooltipDate(iso: string): string {
  return format(parseISO(iso), "MMM dd, yyyy HH:mm");
}

interface TooltipPayloadItem {
  payload: {
    from: string;
    to: string;
    liters: number;
  };
}

function CustomTooltip({ active, payload }: { active?: boolean; payload?: TooltipPayloadItem[] }) {
  if (!active || !payload?.length) return null;
  const data = payload[0].payload;
  return (
    <div style={styles.tooltip}>
      <div style={styles.tooltipTitle}>
        {formatTooltipDate(data.from)} – {formatTooltipDate(data.to)}
      </div>
      <div style={styles.tooltipValue}>{data.liters.toFixed(2)} L</div>
    </div>
  );
}

export default function ConsumptionChart({ buckets }: ConsumptionChartProps) {
  const chartData = buckets.map((b) => ({
    label: formatAxisDate(b.from),
    liters: b.liters,
    from: b.from,
    to: b.to,
  }));

  return (
    <div style={styles.chartSection}>
      <h2 style={styles.sectionTitle}>CONSUMPTION OVER TIME</h2>
      <ResponsiveContainer width="100%" height={300}>
        <BarChart data={chartData} margin={{ top: 20, right: 30, left: 20, bottom: 5 }}>
          <CartesianGrid strokeDasharray="3 3" stroke="#e5e7eb" />
          <XAxis
            dataKey="label"
            tick={{ fontSize: 12, fill: "#6b7280" }}
            interval={0}
            angle={-45}
            textAnchor="end"
            height={60}
          />
          <YAxis
            tick={{ fontSize: 12, fill: "#6b7280" }}
            label={{ value: "Liters", angle: -90, position: "insideLeft", style: { fontSize: 14, fill: "#6b7280" } }}
          />
          <Tooltip content={<CustomTooltip />} />
          <Bar dataKey="liters" fill="#3b82f6" radius={[4, 4, 0, 0]} />
        </BarChart>
      </ResponsiveContainer>
    </div>
  );
}

const styles: Record<string, React.CSSProperties> = {
  chartSection: {
    marginBottom: 32,
    padding: 24,
    borderRadius: 10,
    border: "1px solid #e5e7eb",
    backgroundColor: "#fff",
  },
  sectionTitle: {
    margin: "0 0 16px 0",
    fontSize: 14,
    fontWeight: 600,
    color: "#6b7280",
    textTransform: "uppercase",
    letterSpacing: "0.05em",
  },
  tooltip: {
    padding: "10px 14px",
    backgroundColor: "#1f2937",
    color: "#fff",
    borderRadius: 8,
    fontSize: 13,
    boxShadow: "0 4px 12px rgba(0,0,0,0.15)",
  },
  tooltipTitle: {
    marginBottom: 4,
    fontWeight: 500,
  },
  tooltipValue: {
    fontSize: 16,
    fontWeight: 700,
  },
};
