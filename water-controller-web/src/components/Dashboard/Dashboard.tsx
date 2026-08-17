import { useEffect, useState } from "react";
import {
  fetchConsumption,
  type ConsumptionBucket,
} from "../../api/consumption";
import Header from "./Header";
import SummaryCards from "./SummaryCards";
import ConsumptionChart from "./ConsumptionChart";
import BucketTable from "./BucketTable";

export default function Dashboard() {
  const [range, setRange] = useState("7d");
  const [interval, setInterval] = useState("1d");
  const [buckets, setBuckets] = useState<ConsumptionBucket[]>([]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    fetchConsumption({ range, interval })
      .then((data) => {
        if (!cancelled) {
          setBuckets(data.buckets);
          setError(null);
        }
      })
      .catch((err) => {
        if (!cancelled) {
          setError(err.message);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [range, interval]);

  const totalLiters = buckets.reduce((sum, b) => sum + b.liters, 0);

  return (
    <div style={styles.container}>
      <Header
        range={range}
        interval={interval}
        onRangeChange={setRange}
        onIntervalChange={setInterval}
      />
      {error && <div style={styles.error}>{error}</div>}
      <SummaryCards buckets={buckets} range={range} />
      <ConsumptionChart buckets={buckets} />
      <BucketTable buckets={buckets} totalLiters={totalLiters} />
    </div>
  );
}

const styles: Record<string, React.CSSProperties> = {
  container: {
    maxWidth: 960,
    margin: "0 auto",
    padding: 24,
    fontFamily:
      '-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif',
    color: "#1f2937",
  },
  error: {
    padding: 12,
    marginBottom: 16,
    borderRadius: 8,
    backgroundColor: "#fef2f2",
    color: "#dc2626",
    border: "1px solid #fecaca",
  },
};
