import { useEffect, useMemo, useState } from "react";
import { api, type McpTool } from "../api";
import { Card } from "../ui";

const categoryLabel: Record<string, string> = {
  WikiTools: "Wiki (leitura, busca e escrita)",
  WikiSyncTool: "Wiki (sincronização)",
  WorkApiTools: "API de trabalho (tickets, instituições, fundos)",
};

function ToolModal({ tool, onClose }: { tool: McpTool; onClose: () => void }) {
  useEffect(() => {
    function onEsc(e: KeyboardEvent) {
      if (e.key === "Escape") onClose();
    }
    document.addEventListener("keydown", onEsc);
    return () => document.removeEventListener("keydown", onEsc);
  }, [onClose]);

  return (
    <div
      onClick={onClose}
      className="fixed inset-0 z-[100] flex items-center justify-center bg-black/70 p-6"
    >
      <div
        onClick={(e) => e.stopPropagation()}
        className="max-h-[80vh] w-full max-w-xl overflow-y-auto rounded-xl border border-border-bright bg-card p-6 shadow-2xl shadow-black/60"
      >
        <div className="flex items-start justify-between gap-4">
          <div>
            <div style={{ fontSize: 10, letterSpacing: 1, color: "var(--text2)" }}>
              {categoryLabel[tool.category] ?? tool.category}
            </div>
            <h2 style={{ fontSize: 18, color: "var(--gold)", margin: "4px 0 0" }}>{tool.name}</h2>
          </div>
          <button onClick={onClose} className="text-xl leading-none text-text2 hover:text-text">
            ×
          </button>
        </div>

        <p style={{ fontSize: 13, lineHeight: 1.6, marginTop: 12 }}>{tool.description}</p>

        <div style={{ marginTop: 16 }}>
          <div style={{ fontSize: 10, letterSpacing: 1, color: "var(--text2)", marginBottom: 6 }}>
            PARÂMETROS
          </div>
          {tool.parameters.length === 0 ? (
            <div style={{ fontSize: 12, color: "var(--text2)" }}>Sem parâmetros — chame sem argumentos.</div>
          ) : (
            <table style={{ width: "100%", borderCollapse: "collapse", fontSize: 12 }}>
              <thead>
                <tr>
                  {["Nome", "Tipo", "Obrigatório", "Descrição"].map((h) => (
                    <th
                      key={h}
                      style={{
                        textAlign: "left",
                        padding: "5px 6px",
                        borderBottom: "1px solid var(--border)",
                        color: "var(--text2)",
                        fontWeight: "normal",
                        fontSize: 10,
                      }}
                    >
                      {h}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {tool.parameters.map((p) => (
                  <tr key={p.name}>
                    <td style={{ padding: "5px 6px", borderBottom: "1px solid var(--border)", color: "var(--gold)" }}>
                      {p.name}
                    </td>
                    <td style={{ padding: "5px 6px", borderBottom: "1px solid var(--border)" }}>{p.type}</td>
                    <td style={{ padding: "5px 6px", borderBottom: "1px solid var(--border)" }}>
                      {p.required ? "sim" : "não"}
                    </td>
                    <td style={{ padding: "5px 6px", borderBottom: "1px solid var(--border)" }}>{p.description}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>

        <div style={{ marginTop: 16 }}>
          <div style={{ fontSize: 10, letterSpacing: 1, color: "var(--text2)", marginBottom: 6 }}>
            EXEMPLO DE CHAMADA (JSON-RPC via /mcp)
          </div>
          <pre
            style={{
              background: "var(--bg2)",
              border: "1px solid var(--border)",
              borderRadius: 6,
              padding: 10,
              fontSize: 11,
              overflowX: "auto",
            }}
          >
            {JSON.stringify(
              {
                jsonrpc: "2.0",
                id: 1,
                method: "tools/call",
                params: {
                  name: tool.name,
                  arguments: Object.fromEntries(tool.parameters.map((p) => [p.name, `<${p.type}>`])),
                },
              },
              null,
              2
            )}
          </pre>
        </div>
      </div>
    </div>
  );
}

export default function McpTools() {
  const [tools, setTools] = useState<McpTool[]>([]);
  const [selected, setSelected] = useState<McpTool | null>(null);

  useEffect(() => {
    api.mcpTools().then(setTools).catch(() => {});
  }, []);

  const grouped = useMemo(() => {
    const m = new Map<string, McpTool[]>();
    for (const t of tools) {
      if (!m.has(t.category)) m.set(t.category, []);
      m.get(t.category)!.push(t);
    }
    return m;
  }, [tools]);

  return (
    <div style={{ maxWidth: 900 }}>
      <Card title={`FERRAMENTAS MCP (${tools.length})`}>
        <p style={{ fontSize: 12, color: "var(--text2)", marginBottom: 14 }}>
          Lista extraída ao vivo do código (via reflexão sobre as classes <code>[McpServerToolType]</code>) — sempre
          em sincronia com as tools de verdade registradas no servidor. Clique numa tool pra ver parâmetros e um
          exemplo de chamada.
        </p>
        {[...grouped.entries()].map(([category, categoryTools]) => (
          <div key={category} style={{ marginBottom: 18 }}>
            <div style={{ fontSize: 11, letterSpacing: 1, color: "var(--accent)", marginBottom: 8 }}>
              {(categoryLabel[category] ?? category).toUpperCase()}
            </div>
            <div className="grid grid-cols-1 gap-2 sm:grid-cols-2">
              {categoryTools.map((t) => (
                <div
                  key={t.name}
                  onClick={() => setSelected(t)}
                  className="cursor-pointer rounded-lg border border-border p-3 transition-colors hover:border-border-bright hover:bg-bg2"
                >
                  <div style={{ fontSize: 13, color: "var(--gold)" }}>{t.name}</div>
                  <div
                    style={{
                      fontSize: 12,
                      color: "var(--text2)",
                      marginTop: 4,
                      overflow: "hidden",
                      textOverflow: "ellipsis",
                      display: "-webkit-box",
                      WebkitLineClamp: 2,
                      WebkitBoxOrient: "vertical",
                    }}
                  >
                    {t.description}
                  </div>
                </div>
              ))}
            </div>
          </div>
        ))}
      </Card>

      {selected && <ToolModal tool={selected} onClose={() => setSelected(null)} />}
    </div>
  );
}
