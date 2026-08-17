import { format, parseISO } from "date-fns";
import type { ConsumptionBucket } from "../../api/consumption";

interface BucketTableProps {
  buckets: ConsumptionBucket[];
  totalLiters: number;
}

function getStatus(liters: number): { label: string; color: string } {
  if (liters === 0) return { label: "Idle", color: "#9ca3af" };
  if (liters <= 100) return { label: "Normal", color: "#22c55e" };
  return { label: "High", color: "#ef4444" };
}

export default function BucketTable({ buckets, totalLiters }: BucketTableProps) {
  return (
    <div style={styles.tableSection}>
      <h2 style={styles.sectionTitle}>BUCKET BREAKDOWN</h2>
      <table style={styles.table}>
        <thead>
          <tr>
            <th style={styles.th}>Date Interval</th>
            <th style={styles.th}>Liters Consumed</th>
            <th style={styles.th}>% of Total</th>
            <th style={styles.th}>Status</th>
          </tr>
        </thead>
        <tbody>
          {buckets.map((bucket, idx) => {
            const status = getStatus(bucket.liters);
            const pct = totalLiters > 0 ? (bucket.liters / totalLiters) * 100 : 0;
            const isZero = bucket.liters === 0;
            return (
              <tr key={idx} style={isZero ? styles.trZero : styles.tr}>
                <td style={isZero ? styles.tdZero : styles.td}>
                  {format(parseISO(bucket.from), "MMM dd HH:mm")} – {format(parseISO(bucket.to), "MMM dd HH:mm")}
                </td>
                <td style={isZero ? styles.tdZero : styles.td}>
                  {bucket.liters.toFixed(2)} L
                </td>
                <td style={isZero ? styles.tdZero : styles.td}>
                  {pct.toFixed(1)}%
                </td>
                <td style={styles.td}>
                  <span style={{ ...styles.statusBadge, backgroundColor: status.color + "20", color: status.color }}>
                    {status.label}
                  </span>
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}

const styles: Record<string, React.CSSProperties> = {
  tableSection: {
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
  table: {
    width: "100%",
    borderCollapse: "collapse",
    fontSize: 14,
  },
  th: {
    textAlign: "left",
    padding: "10px 12px",
    borderBottom: "2px solid #e5e7eb",
    fontWeight: 600,
    color: "#374151",
    fontSize: 12,
    textTransform: "uppercase",
    letterSpacing: "0.05em",
  },
  tr: {
    borderBottom: "1px solid #f3f4f6",
  },
  trZero: {
    borderBottom: "1px solid #f3f4f6",
    backgroundColor: "#fafafa",
  },
  td: {
    padding: "10px 12px",
    color: "#1f2937",
  },
  tdZero: {
    padding: "10px 12px",
    color: "#9ca3af",
  },
  statusBadge: {
    display: "inline-block",
    padding: "2px 10px",
    borderRadius: 12,
    fontSize: 12,
    fontWeight: 600,
  },
};
