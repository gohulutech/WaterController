import { useTranslation } from "react-i18next";

interface HeaderProps {
  filter: string;
  onFilterChange: (value: string) => void;
}

export default function Header({ filter, onFilterChange }: HeaderProps) {
  const { t } = useTranslation();

  return (
    <div style={styles.header}>
      <h1 style={styles.title}>{t("dashboard.title")}</h1>
      <div style={styles.controls}>
        <select value={filter} onChange={(e) => onFilterChange(e.target.value)} style={styles.select}>
          <option value="1d-1h">{t("header.filter.1d-1h")}</option>
          <option value="7d-1d">{t("header.filter.7d-1d")}</option>
          <option value="30d-1d">{t("header.filter.30d-1d")}</option>
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
