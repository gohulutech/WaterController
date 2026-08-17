import { format, parseISO } from "date-fns";
import type { ConsumptionBucket } from "../../api/consumption";

interface SummaryCardsProps {
  buckets: ConsumptionBucket[];
  range: string;
}

export default function SummaryCards({ buckets, range }: SummaryCardsProps) {
  const totalLiters = buckets.reduce((sum, b) => sum + b.liters, 0);
  const dailyAverage = totalLiters / buckets.length;

  const peakBucket = buckets.reduce(
    (max, b) => (b.liters > max.liters ? b : max),
    buckets[0],
  );

  if (peakBucket == null) return null;

  const activeDays = buckets.filter((b) => b.liters > 0).length;

  const rangeLabel =
    range === "7d" ? "7 Days" : range === "30d" ? "30 Days" : "1 Day";

  return (
    <div style={styles.cardGrid}>
      <div style={styles.card}>
        <div style={styles.cardLabel}>Total Volume ({rangeLabel})</div>
        <div style={styles.cardValue}>{totalLiters.toFixed(2)} L</div>
      </div>
      <div style={styles.card}>
        <div style={styles.cardLabel}>Daily Average</div>
        <div style={styles.cardValue}>{dailyAverage.toFixed(2)} L/day</div>
      </div>
      <div style={styles.card}>
        <div style={styles.cardLabel}>Peak Single Day</div>
        <div style={styles.cardValue}>
          {peakBucket.liters.toFixed(2)} L
          <span style={styles.cardSubtext}>
            ({format(parseISO(peakBucket.from), "MMM dd")} –{" "}
            {format(parseISO(peakBucket.to), "MMM dd")})
          </span>
        </div>
      </div>
      <div style={styles.card}>
        <div style={styles.cardLabel}>Active Days</div>
        <div style={styles.cardValue}>
          {activeDays} / {buckets.length} days with usage
        </div>
      </div>
    </div>
  );
}

const styles: Record<string, React.CSSProperties> = {
  cardGrid: {
    display: "grid",
    gridTemplateColumns: "1fr 1fr",
    gap: 16,
    marginBottom: 32,
  },
  card: {
    padding: 20,
    borderRadius: 10,
    border: "1px solid #e5e7eb",
    backgroundColor: "#f9fafb",
  },
  cardLabel: {
    fontSize: 13,
    fontWeight: 500,
    color: "#6b7280",
    marginBottom: 8,
    textTransform: "uppercase",
    letterSpacing: "0.05em",
  },
  cardValue: {
    fontSize: 28,
    fontWeight: 700,
    color: "#111827",
  },
  cardSubtext: {
    fontSize: 14,
    fontWeight: 400,
    color: "#6b7280",
    marginLeft: 8,
  },
};
