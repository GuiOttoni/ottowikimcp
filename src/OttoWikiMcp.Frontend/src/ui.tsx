export const badgeColor = (value: string) => {
  const v = value.toLowerCase();
  if (v === "aberto" || v === "critica" || v === "alta") return "var(--accent)";
  return "var(--text3)";
};

export function Badge({ value }: { value: string }) {
  return (
    <span
      className="rounded-full px-2 py-0.5 text-[10px]"
      style={{ border: `1px solid ${badgeColor(value)}`, color: badgeColor(value) }}
    >
      {value}
    </span>
  );
}

export function Card({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <section className="mb-5 rounded-xl border border-border bg-card p-4 shadow-md shadow-black/20">
      <div className="mb-2.5 text-[10px] tracking-[2px] text-accent">{title}</div>
      {children}
    </section>
  );
}

export const cellStyle: React.CSSProperties = {
  padding: "6px 8px",
  borderBottom: "1px solid var(--border)",
};

export const buttonStyle: React.CSSProperties = {
  background: "var(--bg2)",
  border: "1px solid var(--border-bright)",
  color: "var(--text)",
  fontFamily: "inherit",
  fontSize: 12,
  padding: "6px 14px",
  borderRadius: 8,
  cursor: "pointer",
  marginBottom: 10,
  transition: "background 0.15s, border-color 0.15s",
};

export const inputStyle: React.CSSProperties = {
  background: "var(--bg2)",
  border: "1px solid var(--border-bright)",
  color: "var(--text)",
  fontFamily: "inherit",
  padding: "8px 12px",
  borderRadius: 8,
  width: "100%",
  marginBottom: 10,
  boxSizing: "border-box",
};

export const selectStyle: React.CSSProperties = {
  ...inputStyle,
  marginBottom: 0,
  width: "auto",
  cursor: "pointer",
};
