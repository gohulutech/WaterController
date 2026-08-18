import { useTranslation } from "react-i18next";
import { format, parseISO } from "date-fns";
import type { ConsumptionBucket } from "../../api/consumption";

interface SummaryCardsProps {
  buckets: ConsumptionBucket[];
  range: string;
  interval: string;
}

export default function SummaryCards({ buckets, range, interval }: SummaryCardsProps) {
  const { t } = useTranslation();

  const totalLiters = buckets.reduce((sum, b) => sum + b.liters, 0);
  const dailyAverage = totalLiters / buckets.length;

  const peakBucket = buckets.reduce(
    (max, b) => (b.liters > max.liters ? b : max),
    buckets[0],
  );

  if (peakBucket == null) return null;

  const activeDays = buckets.filter((b) => b.liters > 0).length;
  const isHourly = interval === "1h";
  const rangeLabel = t(`header.range.${range}`);

  return (
    <div style={styles.cardGrid}>
      <div style={styles.card}>
        <div style={styles.cardLabel}>{t("summary.totalVolume")} ({rangeLabel})</div>
        <div style={styles.cardValue}>{totalLiters.toFixed(2)} L</div>
      </div>
      <div style={styles.card}>
        <div style={styles.cardLabel}>{isHourly ? t("summary.hourlyAverage") : t("summary.dailyAverage")}</div>
        <div style={styles.cardValue}>{dailyAverage.toFixed(2)} {isHourly ? t("summary.perHour") : t("summary.perDay")}</div>
      </div>
      <div style={styles.card}>
        <div style={styles.cardLabel}>{isHourly ? t("summary.peakSingleHour") : t("summary.peakSingleDay")}</div>
        <div style={styles.cardValue}>
          {peakBucket.liters.toFixed(2)} L
          <span style={styles.cardSubtext}>
            ({format(parseISO(peakBucket.from), "MMM dd")} –{" "}
            {format(parseISO(peakBucket.to), "MMM dd")})
          </span>
        </div>
      </div>
      <div style={styles.card}>
        <div style={styles.cardLabel}>{isHourly ? t("summary.activeHours") : t("summary.activeDays")}</div>
        <div style={styles.cardValue}>
          {activeDays} / {buckets.length} {isHourly ? t("summary.activeHours").toLowerCase() : t("summary.activeDays").toLowerCase()} {t("summary.withUsage")}
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
