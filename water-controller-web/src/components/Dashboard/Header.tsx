interface HeaderProps {
  range: string;
  interval: string;
  onRangeChange: (value: string) => void;
  onIntervalChange: (value: string) => void;
}

export default function Header({ range, interval, onRangeChange, onIntervalChange }: HeaderProps) {
  return (
    <div style={styles.header}>
      <h1 style={styles.title}>Water Consumption Dashboard</h1>
      <div style={styles.controls}>
        <select value={range} onChange={(e) => onRangeChange(e.target.value)} style={styles.select}>
          <option value="1d">1 Day</option>
          <option value="7d">7 Days</option>
          <option value="30d">30 Days</option>
        </select>
        <select value={interval} onChange={(e) => onIntervalChange(e.target.value)} style={styles.select}>
          <option value="1h">1 Hour</option>
          <option value="1d">1 Day</option>
        </select>
      </div>
    </div>
  );
}

const styles: Record<string, React.CSSProperties> = {
  header: {
    display: "flex",
    justifyContent: "space-between",
    alignItems: "center",
    marginBottom: 24,
    paddingBottom: 16,
    borderBottom: "1px solid #e5e7eb",
  },
  title: {
    margin: 0,
    fontSize: 22,
    fontWeight: 700,
  },
  controls: {
    display: "flex",
    gap: 8,
  },
  select: {
    padding: "6px 12px",
    borderRadius: 6,
    border: "1px solid #d1d5db",
    fontSize: 14,
    backgroundColor: "#fff",
    cursor: "pointer",
  },
};
