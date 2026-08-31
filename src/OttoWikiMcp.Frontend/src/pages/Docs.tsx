import { useEffect, useMemo, useRef, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";
import rehypeHighlight from "rehype-highlight";
import rehypeSlug from "rehype-slug";
import GithubSlugger from "github-slugger";
import MDEditor from "@uiw/react-md-editor";
import "highlight.js/styles/atom-one-dark.css";
import "@uiw/react-md-editor/markdown-editor.css";
import "@uiw/react-markdown-preview/markdown.css";
import { api, type WikiPage } from "../api";
import { Card, buttonStyle, inputStyle } from "../ui";
import { MermaidDiagram } from "../MermaidDiagram";

/**
 * Nome de exibição pra pastas puras (sem página própria, ex.: "FundosDeInvestimento" — a pasta
 * em si não tem `FundosDeInvestimento.md`) — insere espaço antes de maiúscula-após-minúscula
 * (PascalCase → "Palavras Separadas") e troca hífen por espaço. Só um fallback: páginas de verdade
 * usam o `title` vindo do backend (frontmatter `title:` ou primeiro `# heading`, com acentuação
 * correta), isto aqui nunca "inventa" acento que o nome do arquivo não tinha.
 */
function humanizeSegment(name: string): string {
  return name.replace(/([a-z0-9])([A-Z])/g, "$1 $2").replace(/-/g, " ");
}

type Heading = { depth: 2 | 3; text: string; slug: string };

/**
 * Extrai os headings ##/### do markdown pra montar o sumário "Nesta página". Usa `github-slugger`
 * — a MESMA lib que `rehype-slug` usa por baixo dos panos pra gerar o `id` nos headings
 * renderizados — pra garantir que o slug calculado aqui bate exatamente com o `id` real do DOM
 * (inclusive o comportamento de deduplicação e o tratamento de pontuação/travessões).
 */
function extractHeadings(markdown: string): Heading[] {
  const slugger = new GithubSlugger();
  const headings: Heading[] = [];
  for (const line of markdown.split("\n")) {
    const m = /^(#{2,3})\s+(.+)$/.exec(line.trim());
    if (!m) continue;
    const depth = m[1].length as 2 | 3;
    const text = m[2].replace(/[#*`]/g, "").trim();
    headings.push({ depth, text, slug: slugger.slug(text) });
  }
  return headings;
}

/** Remove um frontmatter YAML simples (```---\ntags: [...]\n---```) antes de renderizar/exibir. */
function stripFrontmatter(content: string): string {
  const lines = content.split("\n");
  if (lines[0]?.trim() !== "---") return content;
  for (let i = 1; i < lines.length; i++) {
    if (lines[i].trim() === "---") return lines.slice(i + 1).join("\n").replace(/^\n+/, "");
  }
  return content;
}

/** Bloco de código com botão de copiar; detecta ```mermaid``` via o nó hast original e desvia pro MermaidDiagram. */
function CodeBlock(props: { node?: { children?: Array<{ properties?: { className?: string[] }; children?: Array<{ value?: string }> }> }; children?: React.ReactNode }) {
  const preRef = useRef<HTMLPreElement>(null);
  const [copied, setCopied] = useState(false);

  const codeNode = props.node?.children?.[0];
  const codeClassName = codeNode?.properties?.className?.join(" ") ?? "";
  if (/language-mermaid/.test(codeClassName)) {
    const raw = codeNode?.children?.map((c) => c.value ?? "").join("") ?? "";
    return <MermaidDiagram chart={raw} />;
  }

  function copy() {
    const text = preRef.current?.innerText ?? "";
    navigator.clipboard?.writeText(text).then(() => {
      setCopied(true);
      setTimeout(() => setCopied(false), 1500);
    });
  }

  return (
    <div style={{ position: "relative" }}>
      <button
        onClick={copy}
        style={{
          position: "absolute",
          top: 6,
          right: 6,
          fontSize: 10,
          background: "var(--bg2)",
          border: "1px solid var(--border-bright)",
          color: "var(--text2)",
          borderRadius: 3,
          padding: "2px 6px",
          cursor: "pointer",
        }}
      >
        {copied ? "copiado" : "copiar"}
      </button>
      <pre ref={preRef}>{props.children}</pre>
    </div>
  );
}

/** Tabelas do GFM (remark-gfm) não vêm com wrapper de scroll — sem isso, uma tabela larga (URLs longas em Endpoints-Consumidos, por ex.) estoura a largura da página inteira em vez de rolar só dentro de si mesma. */
function MarkdownTable(props: React.TableHTMLAttributes<HTMLTableElement>) {
  return (
    <div className="table-scroll">
      <table {...props} />
    </div>
  );
}

type TreeNode = { name: string; fullPath?: string; tags?: string[]; title?: string; children: Map<string, TreeNode> };

function buildTree(pages: WikiPage[]): TreeNode {
  const root: TreeNode = { name: "", children: new Map() };
  for (const p of pages) {
    const parts = p.path.split("/");
    let node = root;
    parts.forEach((part, i) => {
      if (!node.children.has(part)) node.children.set(part, { name: part, children: new Map() });
      node = node.children.get(part)!;
      if (i === parts.length - 1) {
        node.fullPath = p.path;
        node.tags = p.tags;
        node.title = p.title ?? undefined;
      }
    });
  }
  return root;
}

/** Lista os `fullPath` em ordem de leitura (mesma ordem alfabética que a árvore renderiza), pra nav anterior/próxima. */
function flattenTree(node: TreeNode): string[] {
  const entries = [...node.children.values()].sort((a, b) => a.name.localeCompare(b.name));
  const out: string[] = [];
  for (const child of entries) {
    if (child.fullPath) out.push(child.fullPath);
    out.push(...flattenTree(child));
  }
  return out;
}

function TagChip({
  tag,
  onClick,
  count,
  size = "normal",
}: {
  tag: string;
  onClick?: () => void;
  count?: number;
  size?: "normal" | "cloud";
}) {
  const fontSize = size === "cloud" && count ? Math.min(9 + count * 0.4, 12) : 9;
  return (
    <span
      onClick={onClick}
      style={{
        display: "inline-block",
        padding: size === "cloud" ? "2px 6px" : "1px 5px",
        borderRadius: 3,
        fontSize,
        marginLeft: size === "cloud" ? 0 : 6,
        border: "1px solid var(--gold)",
        color: "var(--gold)",
        cursor: onClick ? "pointer" : "default",
        lineHeight: 1.5,
      }}
    >
      {tag}
      {count !== undefined && <span style={{ opacity: 0.6, fontSize: 9, marginLeft: 3 }}>{count}</span>}
    </span>
  );
}

/** Caminho (lista de fullPaths de pasta) da raiz até o nó que contém `activePath` — usado pra auto-expandir só os grupos no caminho da página atual, como o template de referência (grupo ativo já vem aberto, os demais fechados). */
function ancestorGroupPaths(node: TreeNode, activePath: string | null, prefix = ""): string[] {
  if (!activePath) return [];
  const out: string[] = [];
  for (const [key, child] of node.children) {
    const childPath = prefix ? `${prefix}/${key}` : key;
    if (activePath === childPath || activePath.startsWith(childPath + "/")) {
      out.push(childPath);
      out.push(...ancestorGroupPaths(child, activePath, childPath));
    }
  }
  return out;
}

/**
 * Árvore de páginas com grupos colapsáveis (estilo Docusaurus/Headlesshost knowledgebase): uma
 * pasta com filhos é um "grupo" com chevron — clicar no texto do grupo expande/colapsa (e navega
 * também, se a pasta tiver uma página própria, ex.: `Arquitetura.md` + `Arquitetura/`). Páginas
 * folha ganham um marcador (barrinha) que acende quando ativas, igual ao template de referência.
 */
function PageTree({
  node,
  depth,
  activePath,
  expanded,
  onToggle,
  onOpen,
  groupPath = "",
}: {
  node: TreeNode;
  depth: number;
  activePath: string | null;
  expanded: Set<string>;
  onToggle: (path: string) => void;
  onOpen: (path: string) => void;
  groupPath?: string;
}) {
  const entries = [...node.children.values()].sort((a, b) => a.name.localeCompare(b.name));
  return (
    <ul style={{ listStyle: "none", margin: 0, padding: depth ? "0 0 0 14px" : 0 }}>
      {entries.map((child) => {
        const childPath = groupPath ? `${groupPath}/${child.name}` : child.name;
        const isGroup = child.children.size > 0;
        const isOpen = expanded.has(childPath);
        const isActive = child.fullPath === activePath;
        return (
          <li key={child.name} style={{ marginBottom: 2 }}>
            <div
              onClick={() => {
                if (child.fullPath) onOpen(child.fullPath);
                else if (isGroup) onToggle(childPath);
              }}
              style={{
                display: "flex",
                alignItems: "center",
                gap: 6,
                padding: "5px 8px",
                cursor: "pointer",
                borderRadius: 3,
                fontSize: 13,
                fontWeight: isGroup && !child.fullPath ? 600 : 400,
                background: isActive ? "var(--bg2)" : "transparent",
              }}
              onMouseEnter={(e) => {
                if (!isActive) e.currentTarget.style.background = "var(--bg2)";
              }}
              onMouseLeave={(e) => {
                if (!isActive) e.currentTarget.style.background = "transparent";
              }}
            >
              {isGroup && (
                <span
                  onClick={(e) => {
                    e.stopPropagation();
                    onToggle(childPath);
                  }}
                  style={{
                    display: "inline-block",
                    width: 10,
                    fontSize: 10,
                    color: "var(--text2)",
                    transform: isOpen ? "rotate(90deg)" : "rotate(0deg)",
                    transition: "transform 0.12s",
                  }}
                >
                  ▶
                </span>
              )}
              {!isGroup && (
                <span
                  style={{
                    display: "inline-block",
                    width: 4,
                    height: 4,
                    borderRadius: "50%",
                    background: isActive ? "var(--gold)" : "var(--border-bright)",
                    flex: "none",
                  }}
                />
              )}
              <span
                style={{
                  color: isActive ? "var(--gold)" : isGroup && !child.fullPath ? "var(--text)" : "var(--text2)",
                  overflow: "hidden",
                  textOverflow: "ellipsis",
                  whiteSpace: "nowrap",
                }}
              >
                {child.title ?? humanizeSegment(child.name)}
              </span>
            </div>
            {isGroup && isOpen && (
              <PageTree
                node={child}
                depth={depth + 1}
                activePath={activePath}
                expanded={expanded}
                onToggle={onToggle}
                onOpen={onOpen}
                groupPath={childPath}
              />
            )}
          </li>
        );
      })}
    </ul>
  );
}

function Breadcrumb({
  path,
  pages,
  onNavigate,
}: {
  path: string;
  pages: WikiPage[];
  onNavigate: (path: string) => void;
}) {
  const parts = path.split("/");
  return (
    <div style={{ fontSize: 12, color: "var(--text2)", marginBottom: 10 }}>
      <span style={{ cursor: "pointer" }} onClick={() => onNavigate("")}>
        Documentação
      </span>
      {parts.map((part, i) => {
        const isLast = i === parts.length - 1;
        const partialPath = parts.slice(0, i + 1).join("/");
        const label = pages.find((p) => p.path === partialPath)?.title ?? humanizeSegment(part);
        return (
          <span key={partialPath}>
            {" "}
            <span style={{ color: "var(--border-bright)" }}>›</span>{" "}
            <span
              style={{ cursor: isLast ? "default" : "pointer", color: isLast ? "var(--gold)" : "var(--text2)" }}
              onClick={() => !isLast && onNavigate(partialPath)}
            >
              {label}
            </span>
          </span>
        );
      })}
    </div>
  );
}

function TableOfContents({ headings }: { headings: Heading[] }) {
  if (headings.length === 0) return null;
  function scrollTo(slug: string) {
    document.getElementById(slug)?.scrollIntoView({ behavior: "smooth", block: "start" });
  }
  return (
    <aside style={{ position: "sticky", top: 90, alignSelf: "start" }}>
      <div style={{ fontSize: 10, color: "var(--text2)", letterSpacing: 1, marginBottom: 8 }}>
        NESTA PÁGINA
      </div>
      <ul style={{ listStyle: "none", margin: 0, padding: 0 }}>
        {headings.map((h) => (
          <li key={h.slug} style={{ marginBottom: 4 }}>
            <span
              onClick={() => scrollTo(h.slug)}
              style={{
                cursor: "pointer",
                fontSize: 12,
                color: "var(--text2)",
                paddingLeft: h.depth === 3 ? 12 : 0,
                display: "block",
              }}
              onMouseEnter={(e) => (e.currentTarget.style.color = "var(--gold)")}
              onMouseLeave={(e) => (e.currentTarget.style.color = "var(--text2)")}
            >
              {h.text}
            </span>
          </li>
        ))}
      </ul>
    </aside>
  );
}

function pageLabel(path: string, pages: WikiPage[]): string {
  return pages.find((p) => p.path === path)?.title ?? humanizeSegment(path.split("/").pop() ?? path);
}

function PrevNextNav({
  prevPath,
  nextPath,
  pages,
  onOpen,
}: {
  prevPath: string | null;
  nextPath: string | null;
  pages: WikiPage[];
  onOpen: (path: string) => void;
}) {
  if (!prevPath && !nextPath) return null;
  return (
    <div className="flex justify-between mt-6 pt-4 border-t border-[var(--border)]">
      <div>
        {prevPath && (
          <div onClick={() => onOpen(prevPath)} className="cursor-pointer">
            <div style={{ fontSize: 10, letterSpacing: 1, color: "var(--text2)", marginBottom: 2 }}>ANTERIOR</div>
            <div style={{ fontSize: 13, color: "var(--gold)" }}>← {pageLabel(prevPath, pages)}</div>
          </div>
        )}
      </div>
      <div style={{ textAlign: "right" }}>
        {nextPath && (
          <div onClick={() => onOpen(nextPath)} className="cursor-pointer">
            <div style={{ fontSize: 10, letterSpacing: 1, color: "var(--text2)", marginBottom: 2 }}>PRÓXIMA</div>
            <div style={{ fontSize: 13, color: "var(--gold)" }}>{pageLabel(nextPath, pages)} →</div>
          </div>
        )}
      </div>
    </div>
  );
}

export default function Docs() {
  const params = useParams();
  const navigate = useNavigate();
  const currentPath = params["*"] || null;

  const [pages, setPages] = useState<WikiPage[]>([]);
  const [activeTag, setActiveTag] = useState<string | null>(null);
  const [expandedGroups, setExpandedGroups] = useState<Set<string>>(new Set());
  const autoExpandedFor = useRef<string | null>(null);
  const [content, setContent] = useState("Selecione uma página ao lado, ou busque um termo.");
  const [contentHash, setContentHash] = useState<string | null>(null);
  const [isPageContent, setIsPageContent] = useState(false);
  const [query, setQuery] = useState("");

  const [editing, setEditing] = useState(false);
  const [draft, setDraft] = useState("");
  const [saveStatus, setSaveStatus] = useState<string | null>(null);

  const [question, setQuestion] = useState("");
  const [answer, setAnswer] = useState<{ answer: string; sources: string[] } | null>(null);
  const [asking, setAsking] = useState(false);

  useEffect(() => {
    api.wikiPages().then(setPages).catch(() => {});
  }, []);

  useEffect(() => {
    if (!currentPath) {
      setIsPageContent(false);
      setContent("Selecione uma página ao lado, ou busque um termo.");
      return;
    }
    api.wikiPage(currentPath).then((data) => {
      setContent(data.content);
      setContentHash(data.hash);
      setIsPageContent(true);
      setEditing(false);
      setSaveStatus(null);
    });
  }, [currentPath]);

  function openPage(path: string) {
    navigate(`/docs/${path}`);
  }

  async function runSearch(e: React.KeyboardEvent<HTMLInputElement>) {
    if (e.key !== "Enter" || !query.trim()) return;
    const data = await api.wikiSearch(query.trim());
    navigate("/docs");
    setContent(data.results);
    setIsPageContent(false);
    setEditing(false);
  }

  function startEditing() {
    setDraft(content);
    setEditing(true);
    setSaveStatus(null);
  }

  async function saveEdit() {
    if (!currentPath) return;
    try {
      const result = await api.wikiUpdate(currentPath, draft, contentHash ?? undefined);
      setContent(draft);
      setContentHash(result.hash);
      setEditing(false);
      setSaveStatus("Salvo.");
      api.wikiPages().then(setPages).catch(() => {});
    } catch (err) {
      const message = (err as Error).message;
      // Conflito de escrita concorrente (409) vem com "CONFLITO: ..." no corpo — mensagem
      // própria (com botão de recarregar) em vez do erro genérico de rede.
      if (message.includes("CONFLITO")) {
        setSaveStatus("CONFLITO: esta página foi alterada por outra edição enquanto você editava. Recarregue antes de salvar de novo.");
      } else {
        setSaveStatus(`Erro ao salvar: ${message}`);
      }
    }
  }

  async function reloadAfterConflict() {
    if (!currentPath) return;
    const data = await api.wikiPage(currentPath);
    setContent(data.content);
    setContentHash(data.hash);
    setDraft(data.content);
    setSaveStatus(null);
  }

  async function runAsk(e: React.KeyboardEvent<HTMLInputElement>) {
    if (e.key !== "Enter" || !question.trim()) return;
    setAsking(true);
    try {
      const data = await api.wikiAsk(question.trim());
      setAnswer(data);
    } finally {
      setAsking(false);
    }
  }

  const tagCounts = useMemo(() => {
    const counts = new Map<string, number>();
    for (const p of pages) for (const t of p.tags) counts.set(t, (counts.get(t) ?? 0) + 1);
    return counts;
  }, [pages]);
  const allTags = useMemo(() => [...tagCounts.keys()].sort(), [tagCounts]);
  const filteredPages = useMemo(
    () => (activeTag ? pages.filter((p) => p.tags.includes(activeTag)) : pages),
    [pages, activeTag]
  );
  const tree = useMemo(() => buildTree(filteredPages), [filteredPages]);
  const fullTree = useMemo(() => buildTree(pages), [pages]);
  const flatOrder = useMemo(() => flattenTree(fullTree), [fullTree]);

  // Auto-expande só os grupos no caminho da página ativa (uma vez por navegação) — grupos que o
  // usuário abriu/fechou manualmente em outros ramos continuam do jeito que ele deixou.
  useEffect(() => {
    if (!currentPath || pages.length === 0 || autoExpandedFor.current === currentPath) return;
    autoExpandedFor.current = currentPath;
    const ancestors = ancestorGroupPaths(fullTree, currentPath);
    if (ancestors.length === 0) return;
    setExpandedGroups((prev) => new Set([...prev, ...ancestors]));
  }, [currentPath, pages, fullTree]);

  function toggleGroup(path: string) {
    setExpandedGroups((prev) => {
      const next = new Set(prev);
      if (next.has(path)) next.delete(path);
      else next.add(path);
      return next;
    });
  }
  const currentIndex = currentPath ? flatOrder.indexOf(currentPath) : -1;
  const prevPath = currentIndex > 0 ? flatOrder[currentIndex - 1] : null;
  const nextPath = currentIndex >= 0 && currentIndex < flatOrder.length - 1 ? flatOrder[currentIndex + 1] : null;
  const currentPageTags = pages.find((p) => p.path === currentPath)?.tags ?? [];
  const headings = useMemo(() => (isPageContent ? extractHeadings(stripFrontmatter(content)) : []), [content, isPageContent]);

  const showToc = isPageContent && !editing && headings.length > 0;

  return (
    <div
      style={{
        display: "grid",
        gridTemplateColumns: showToc ? "260px 1fr 200px" : "260px 1fr",
        gap: 28,
        alignItems: "start",
      }}
    >
      <div style={{ position: "sticky", top: 90, alignSelf: "start", maxHeight: "calc(100vh - 110px)", overflowY: "auto", overflowX: "hidden" }}>
      <Card title="PÁGINAS DA WIKI">
        <input
          type="text"
          placeholder="buscar na wiki..."
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          onKeyDown={runSearch}
          style={inputStyle}
        />
        {allTags.length > 0 && (
          <div style={{ marginBottom: 14 }}>
            <div style={{ fontSize: 10, color: "var(--text2)", letterSpacing: 1, marginBottom: 6 }}>
              TAGS
            </div>
            <div style={{ display: "flex", flexWrap: "wrap", gap: 4, alignItems: "center" }}>
              {allTags.map((t) => (
                <span
                  key={t}
                  style={{
                    opacity: activeTag && activeTag !== t ? 0.4 : 1,
                    transition: "opacity 0.15s",
                  }}
                >
                  <TagChip
                    tag={t}
                    count={tagCounts.get(t)}
                    size="cloud"
                    onClick={() => setActiveTag(activeTag === t ? null : t)}
                  />
                </span>
              ))}
            </div>
            {activeTag && (
              <div style={{ fontSize: 11, color: "var(--text2)", marginTop: 4 }}>
                Filtrando por <strong style={{ color: "var(--gold)" }}>{activeTag}</strong> ·{" "}
                <span style={{ cursor: "pointer", textDecoration: "underline" }} onClick={() => setActiveTag(null)}>
                  limpar
                </span>
              </div>
            )}
          </div>
        )}
        <PageTree
          node={tree}
          depth={0}
          activePath={currentPath}
          expanded={expandedGroups}
          onToggle={toggleGroup}
          onOpen={openPage}
        />
      </Card>
      </div>

      <div style={{ minWidth: 0 }}>
        <Card title="CONTEÚDO">
          {isPageContent && currentPath && (
            <Breadcrumb path={currentPath} pages={pages} onNavigate={(p) => navigate(p ? `/docs/${p}` : "/docs")} />
          )}
          {isPageContent && currentPageTags.length > 0 && (
            <div style={{ display: "flex", alignItems: "center", gap: 8, marginBottom: 16 }}>
              <span style={{ fontSize: 10, letterSpacing: 1, color: "var(--text2)" }}>TAGS</span>
              {currentPageTags.map((t) => (
                <TagChip key={t} tag={t} onClick={() => setActiveTag(t)} />
              ))}
            </div>
          )}
          {!editing && currentPath && (
            <button onClick={startEditing} style={buttonStyle}>
              Editar
            </button>
          )}
          {editing ? (
            <>
              <div data-color-mode="dark" className="rounded-md overflow-hidden border border-[var(--border-bright)]">
                <MDEditor
                  value={draft}
                  onChange={(v) => setDraft(v ?? "")}
                  height={420}
                  visibleDragbar={false}
                  previewOptions={{ remarkPlugins: [remarkGfm], rehypePlugins: [rehypeHighlight] }}
                />
              </div>
              <div className="mt-2 flex gap-2">
                <button onClick={saveEdit} style={buttonStyle}>
                  Salvar
                </button>
                <button onClick={() => setEditing(false)} style={buttonStyle}>
                  Cancelar
                </button>
              </div>
            </>
          ) : isPageContent ? (
            <div className="markdown-body" style={{ fontSize: 13, lineHeight: 1.6, minHeight: 200 }}>
              <ReactMarkdown
                remarkPlugins={[remarkGfm]}
                rehypePlugins={[rehypeHighlight, rehypeSlug]}
                components={{ pre: CodeBlock as never, table: MarkdownTable }}
              >
                {stripFrontmatter(content)}
              </ReactMarkdown>
            </div>
          ) : (
            <div style={{ whiteSpace: "pre-wrap", fontSize: 13, lineHeight: 1.6, minHeight: 200 }}>
              {content}
            </div>
          )}
          {!editing && isPageContent && (
            <PrevNextNav prevPath={prevPath} nextPath={nextPath} pages={pages} onOpen={openPage} />
          )}
          {saveStatus && (
            <div style={{ marginTop: 8, fontSize: 12, color: saveStatus.startsWith("CONFLITO") ? "var(--accent)" : "var(--text2)" }}>
              {saveStatus}
              {saveStatus.startsWith("CONFLITO") && (
                <button onClick={reloadAfterConflict} style={{ ...buttonStyle, marginLeft: 8, marginBottom: 0, padding: "2px 10px" }}>
                  Recarregar
                </button>
              )}
            </div>
          )}
        </Card>

        <Card title="PERGUNTAR À IA">
          <input
            type="text"
            placeholder="pergunte algo sobre a wiki..."
            value={question}
            onChange={(e) => setQuestion(e.target.value)}
            onKeyDown={runAsk}
            style={inputStyle}
          />
          {asking && <div style={{ fontSize: 12, color: "var(--text2)" }}>buscando...</div>}
          {answer && !asking && (
            <div style={{ fontSize: 13, lineHeight: 1.6 }}>
              <div className="markdown-body" style={{ marginBottom: 10 }}>
                <ReactMarkdown remarkPlugins={[remarkGfm]}>{answer.answer}</ReactMarkdown>
              </div>
              {answer.sources.length > 0 && (
                <div>
                  <span style={{ color: "var(--text2)", fontSize: 11 }}>Fontes: </span>
                  {answer.sources.map((s) => (
                    <span
                      key={s}
                      onClick={() => openPage(s)}
                      style={{ cursor: "pointer", color: "var(--accent)", marginRight: 8, fontSize: 12 }}
                    >
                      {s}
                    </span>
                  ))}
                </div>
              )}
            </div>
          )}
        </Card>
      </div>

      {showToc && <TableOfContents headings={headings} />}
    </div>
  );
}
