import { useEffect, useRef, useState } from "react";
import { NavLink, Outlet, useNavigate } from "react-router-dom";
import { api } from "./api";

const navLinkClass = ({ isActive }: { isActive: boolean }) =>
  `text-xs tracking-wide pb-1 border-b-2 transition-colors ${
    isActive ? "text-gold border-gold" : "text-text2 border-transparent hover:text-text"
  }`;

/** Extrai os caminhos de página ("### {path}" no formato de texto do search_wiki) pra listar como resultados clicáveis. */
function extractResultPaths(resultsText: string): string[] {
  return resultsText
    .split("\n")
    .filter((l) => l.startsWith("### "))
    .map((l) => l.slice(4).trim());
}

/** Busca global no cabeçalho — igual ao padrão de knowledge bases conhecidas (Docusaurus/Headlesshost): input com dropdown de resultados, debounced, clicar navega direto pra página. */
function HeaderSearch() {
  const [value, setValue] = useState("");
  const [results, setResults] = useState<string[] | null>(null);
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();
  const boxRef = useRef<HTMLDivElement>(null);
  const debounceRef = useRef<ReturnType<typeof setTimeout> | undefined>(undefined);

  useEffect(() => {
    function onClickOutside(e: MouseEvent) {
      if (boxRef.current && !boxRef.current.contains(e.target as Node)) {
        setResults(null);
      }
    }
    document.addEventListener("mousedown", onClickOutside);
    return () => document.removeEventListener("mousedown", onClickOutside);
  }, []);

  function onChange(v: string) {
    setValue(v);
    clearTimeout(debounceRef.current);
    if (!v.trim()) {
      setResults(null);
      return;
    }
    debounceRef.current = setTimeout(async () => {
      setLoading(true);
      try {
        const data = await api.wikiSearch(v.trim());
        setResults(extractResultPaths(data.results));
      } finally {
        setLoading(false);
      }
    }, 400);
  }

  function openResult(path: string) {
    setResults(null);
    setValue("");
    navigate(`/docs/${path}`);
  }

  return (
    <div ref={boxRef} className="relative w-64">
      <input
        type="search"
        placeholder="Buscar na wiki..."
        value={value}
        onChange={(e) => onChange(e.target.value)}
        className="w-full box-border rounded-lg border border-border-bright bg-bg2 px-3 py-2 font-mono text-xs text-text
                   outline-none transition-shadow focus:ring-2 focus:ring-accent/40"
      />
      {results && (
        <div className="absolute right-0 top-[calc(100%+6px)] z-[60] max-h-80 w-80 overflow-y-auto rounded-lg
                        border border-border-bright bg-card shadow-2xl shadow-black/50">
          {loading && <div className="p-2.5 text-xs text-text2">buscando...</div>}
          {!loading && results.length === 0 && <div className="p-2.5 text-xs text-text2">Nada encontrado.</div>}
          {!loading &&
            results.map((path) => (
              <div
                key={path}
                onClick={() => openResult(path)}
                className="cursor-pointer border-b border-border px-2.5 py-2 text-sm transition-colors hover:bg-bg2"
              >
                {path}
              </div>
            ))}
        </div>
      )}
    </div>
  );
}

export default function Layout() {
  const [online, setOnline] = useState<boolean | null>(null);

  useEffect(() => {
    api.health().then(setOnline).catch(() => setOnline(false));
  }, []);

  return (
    <>
      <header className="sticky top-0 z-50 border-b border-border bg-bg/95 shadow-lg shadow-black/40 backdrop-blur-sm">
        <div className="mx-auto flex max-w-6xl flex-wrap items-center gap-7 px-7 py-3.5">
          <h1 className="m-0 whitespace-nowrap text-lg tracking-wide">
            <span className="text-accent">OTTO</span>
            <span className="text-gold">WIKIMCP</span>
          </h1>
          <nav className="flex gap-5">
            <NavLink to="/" end className={navLinkClass}>
              DASHBOARD
            </NavLink>
            <NavLink to="/docs" className={navLinkClass}>
              DOCUMENTAÇÃO
            </NavLink>
            <NavLink to="/mcp-tools" className={navLinkClass}>
              FERRAMENTAS MCP
            </NavLink>
            <NavLink to="/arquitetura" className={navLinkClass}>
              ARQUITETURA
            </NavLink>
            <NavLink to="/backlog" className={navLinkClass}>
              BACKLOG
            </NavLink>
          </nav>
          <span className="inline-flex items-center gap-1.5 text-[11px] text-text2">
            <span
              className={`inline-block h-2 w-2 rounded-full ${online ? "bg-gold" : "bg-neutral-600"}`}
            />
            {online === null ? "checando..." : online ? "online" : "offline"}
          </span>
          <div className="ml-auto">
            <HeaderSearch />
          </div>
        </div>
      </header>
      <main className="mx-auto max-w-6xl px-7 py-6">
        <Outlet />
      </main>
    </>
  );
}
