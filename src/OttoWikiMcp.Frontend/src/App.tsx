import { useEffect, useState } from "react";
import { api, type Ticket, type Institution } from "./api";

const badgeColor = (value: string) => {
  const v = value.toLowerCase();
  if (v === "aberto" || v === "critica" || v === "alta") return "var(--accent)";
  return "var(--text3)";
};

function Badge({ value }: { value: string }) {
  return (
    <span
      style={{
        padding: "2px 6px",
        borderRadius: 3,
        fontSize: 10,
        border: `1px solid ${badgeColor(value)}`,
        color: badgeColor(value),
      }}
    >
      {value}
    </span>
  );
}

function Card({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <section
      style={{
        background: "var(--card)",
        border: "1px solid var(--border)",
        borderRadius: 4,
        padding: 14,
        marginBottom: 20,
      }}
    >
      <div style={{ fontSize: 10, letterSpacing: 2, color: "var(--accent)", marginBottom: 10 }}>
        {title}
      </div>
      {children}
    </section>
  );
}

export default function App() {
  const [online, setOnline] = useState<boolean | null>(null);
  const [pages, setPages] = useState<string[]>([]);
  const [content, setContent] = useState("Selecione uma página ao lado, ou busque um termo.");
  const [query, setQuery] = useState("");
  const [tickets, setTickets] = useState<Ticket[]>([]);
  const [institutions, setInstitutions] = useState<Institution[]>([]);

  useEffect(() => {
    api.health().then(setOnline).catch(() => setOnline(false));
    api.wikiPages().then(setPages).catch(() => {});
    api.tickets().then(setTickets).catch(() => {});
    api.institutions().then(setInstitutions).catch(() => {});
  }, []);

  async function openPage(path: string) {
    const data = await api.wikiPage(path);
    setContent(data.content);
  }

  async function runSearch(e: React.KeyboardEvent<HTMLInputElement>) {
    if (e.key !== "Enter" || !query.trim()) return;
    const data = await api.wikiSearch(query.trim());
    setContent(data.results);
  }

  return (
    <>
      <header style={{ padding: "20px 28px", borderBottom: "1px solid var(--border)" }}>
        <h1 style={{ margin: 0, fontSize: 20, letterSpacing: 1 }}>
          <span style={{ color: "var(--accent)" }}>OTTO</span>
          <span style={{ color: "var(--gold)" }}>WIKIMCP</span>
        </h1>
        <p style={{ margin: "4px 0 0", color: "var(--text2)", fontSize: 12 }}>
          MCP server local — busca de wiki + API de trabalho (POC) ·{" "}
          <span
            style={{
              display: "inline-block",
              width: 8,
              height: 8,
              borderRadius: "50%",
              marginRight: 6,
              background: online ? "var(--gold)" : "#666",
            }}
          />
          {online === null ? "checando..." : online ? "online" : "offline"}
        </p>
      </header>

      <main
        style={{
          padding: "24px 28px",
          display: "grid",
          gridTemplateColumns: "260px 1fr",
          gap: 20,
          maxWidth: 1200,
        }}
      >
        <Card title="PÁGINAS DA WIKI">
          <input
            type="text"
            placeholder="buscar na wiki..."
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            onKeyDown={runSearch}
            style={{
              background: "var(--bg2)",
              border: "1px solid var(--border-bright)",
              color: "var(--text)",
              fontFamily: "inherit",
              padding: "8px 10px",
              borderRadius: 4,
              width: "100%",
              marginBottom: 10,
            }}
          />
          <ul style={{ listStyle: "none", margin: 0, padding: 0 }}>
            {pages.map((p) => (
              <li
                key={p}
                onClick={() => openPage(p)}
                style={{ padding: "6px 8px", cursor: "pointer", borderRadius: 3, fontSize: 13 }}
                onMouseEnter={(e) => (e.currentTarget.style.background = "var(--bg2)")}
                onMouseLeave={(e) => (e.currentTarget.style.background = "transparent")}
              >
                {p}
              </li>
            ))}
          </ul>
        </Card>

        <div>
          <Card title="CONTEÚDO">
            <div style={{ whiteSpace: "pre-wrap", fontSize: 13, lineHeight: 1.6, minHeight: 200 }}>
              {content}
            </div>
          </Card>

          <Card title="TICKETS">
            <table style={{ width: "100%", borderCollapse: "collapse", fontSize: 12 }}>
              <thead>
                <tr>
                  {["ID", "Assunto", "Status", "Prioridade"].map((h) => (
                    <th
                      key={h}
                      style={{
                        textAlign: "left",
                        padding: "6px 8px",
                        borderBottom: "1px solid var(--border)",
                        color: "var(--text2)",
                        fontWeight: "normal",
                        fontSize: 10,
                        letterSpacing: 1,
                      }}
                    >
                      {h}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {tickets.map((t) => (
                  <tr key={t.id}>
                    <td style={cellStyle}>{t.id}</td>
                    <td style={cellStyle}>{t.subject}</td>
                    <td style={cellStyle}><Badge value={t.status} /></td>
                    <td style={cellStyle}><Badge value={t.priority} /></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </Card>

          <Card title="INSTITUIÇÕES">
            <table style={{ width: "100%", borderCollapse: "collapse", fontSize: 12 }}>
              <thead>
                <tr>
                  {["ID", "Nome", "Plano", "Onboarding"].map((h) => (
                    <th
                      key={h}
                      style={{
                        textAlign: "left",
                        padding: "6px 8px",
                        borderBottom: "1px solid var(--border)",
                        color: "var(--text2)",
                        fontWeight: "normal",
                        fontSize: 10,
                        letterSpacing: 1,
                      }}
                    >
                      {h}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {institutions.map((i) => (
                  <tr key={i.id}>
                    <td style={cellStyle}>{i.id}</td>
                    <td style={cellStyle}>{i.name}</td>
                    <td style={cellStyle}>{i.plan}</td>
                    <td style={cellStyle}>{i.onboardedOn}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </Card>
        </div>
      </main>
    </>
  );
}

const cellStyle: React.CSSProperties = {
  padding: "6px 8px",
  borderBottom: "1px solid var(--border)",
};
