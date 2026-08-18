import { useEffect, useState } from "react";
import {
  fetchConsumption,
  type ConsumptionBucket,
} from "../../api/consumption";
import Header from "./Header";
import SummaryCards from "./SummaryCards";
import ConsumptionChart from "./ConsumptionChart";
import BucketTable from "./BucketTable";

function parseFilter(filter: string): { range: string; interval: string } {
  const [range, interval] = filter.split("-");
  return { range, interval };
}

export default function Dashboard() {
  const [filter, setFilter] = useState("7d-1d");
  const [buckets, setBuckets] = useState<ConsumptionBucket[]>([]);
  const [error, setError] = useState<string | null>(null);

  const { range, interval } = parseFilter(filter);

  useEffect(() => {
    let cancelled = false;

    fetchConsumption({ range, interval })
      .then((data) => {
        if (cancelled) return;
        setBuckets(data.buckets);
        setError(null);
      })
      .catch((err) => {
        if (cancelled) return;
        setError(err.message);
      });

    return () => {
      cancelled = true;
    };
  }, [range, interval]);

  const totalLiters = buckets.reduce((sum, b) => sum + b.liters, 0);

  return (
    <div style={styles.container}>
      <Header filter={filter} onFilterChange={setFilter} />
      {error && <div style={styles.error}>{error}</div>}
      <SummaryCards buckets={buckets} range={range} interval={interval} />
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
