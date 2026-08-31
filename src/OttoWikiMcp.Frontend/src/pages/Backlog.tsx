import { useEffect, useState } from "react";
import { Card } from "../ui";

type BacklogItem = {
  priority: number;
  item: string;
  why: string;
  status: "aberto" | "feito";
  outcome?: string;
};

const BACKLOG: BacklogItem[] = [
  {
    priority: 1,
    item: "Rate limiting em ask_wiki",
    why: "ask_wiki agora chama um LLM real (Gemini/Claude) com cota/custo por chamada — sem limite, um uso descuidado (ou um agente em loop) esgota a cota gratuita rápido.",
    status: "feito",
    outcome: "FixedWindowRateLimiter (System.Threading.RateLimiting) compartilhado por WikiAskService — 8 perguntas/minuto pro servidor inteiro, aplicado tanto no endpoint REST quanto na tool MCP (os dois passam pelo mesmo serviço). Sem lease disponível, devolve uma mensagem explicando o limite em vez de erro genérico.",
  },
  {
    priority: 2,
    item: "Auditoria de chamadas de tool (quem/quando chamou update_wiki_page e ask_wiki)",
    why: "Autor do commit git hoje é sempre a identidade do processo, não da pessoa por trás do agente que pediu a escrita.",
    status: "feito",
    outcome: "WikiAuditFilter (IFunctionInvocationFilter do Semantic Kernel) registrado uma vez no Kernel — cobre automaticamente search_wiki/get_wiki_page/update_wiki_page/list_wiki_pages(_json)/list_wiki_tags, sem log manual espalhado por tool. ask_wiki loga direto no WikiAskService (não passa mais pelo Kernel). Limitação que continua em aberto: regista O QUÊ e O QUANDO, não O QUEM — falta autenticação por chamador (ver item 5).",
  },
  {
    priority: 3,
    item: "Detecção de conflito de escrita concorrente (optimistic concurrency via hash do conteúdo lido)",
    why: "Duas edições simultâneas na mesma página se sobrescreviam silenciosamente — sem aviso pra nenhuma das duas.",
    status: "feito",
    outcome: "GET /api/wiki/page agora retorna um hash (SHA-256) junto do conteúdo; PUT aceita expectedHash opcional — se a página mudou desde a leitura, a escrita é recusada com 409 em vez de sobrescrever. Frontend (Docs.tsx) guarda o hash ao carregar a página e mostra um aviso com botão \"Recarregar\" em caso de conflito. Tool MCP update_wiki_page também aceita expectedHash (opcional — um agente pode continuar chamando sem ele).",
  },
  {
    priority: 4,
    item: "Tool dedicada list_wiki_tags()",
    why: "As tags só apareciam embutidas em list_wiki_pages; uma tool própria facilita descoberta de categorias por um agente sem precisar varrer a lista inteira.",
    status: "feito",
    outcome: "KernelFunction + tool MCP nova, retorna as tags ordenadas por frequência (mais usada primeiro), com contagem de páginas por tag.",
  },
  {
    priority: 5,
    item: "Controle de acesso por tool/role, não só por acesso ao servidor MCP",
    why: "Evita o problema de 'confused deputy' — ter acesso de leitura não deveria implicar acesso de escrita. É também o que resolveria o \"quem\" que falta na auditoria (item 2).",
    status: "aberto",
  },
  {
    priority: 6,
    item: "Embeddings neurais em WikiChunkIndex, além do TF-IDF já implementado",
    why: "TF-IDF (implementado) já resolve o pior caso (substring), mas não capta sinônimos/paráfrase de verdade — embeddings são o próximo degrau. Trocar a implementação de WikiChunkIndex.Search não muda a assinatura pública nem o resto do sistema.",
    status: "aberto",
  },
  {
    priority: 7,
    item: "Registro dinâmico de APIs externas (import de OpenAPI via Semantic Kernel + handlers dinâmicos do SDK do MCP)",
    why: "Hoje toda API nova (ex.: WorkApiTools) exige escrever uma classe C# e recompilar. Um registro em config (nome, spec OpenAPI, allowlist) elimina isso a partir de ~3-4 APIs internas diferentes — ver o estudo dedicado, fora desta wiki, no guia de arquitetura (mcp-apis-dinamicas.md).",
    status: "aberto",
  },
  {
    priority: 8,
    item: "Visualizador de diff/histórico na própria UI",
    why: "Hoje só dá pra ver mudanças via git log no terminal; uma tela de histórico por página melhora a experiência de quem não usa git no dia a dia.",
    status: "aberto",
  },
  {
    priority: 9,
    item: "Renderer de markdown mais rico (react-markdown + remark-gfm + rehype-highlight + mermaid)",
    why: "Syntax highlighting, GFM (tabelas/listas de tarefa), diagramas mermaid, e renderização como componentes React reais em vez de HTML injetado.",
    status: "feito",
    outcome: "Implementado — inclusive a resposta de \"Perguntar à IA\" passou a renderizar como markdown de verdade (antes era texto puro), já que o LLM responde formatado.",
  },
  {
    priority: 10,
    item: "ask_wiki com RAG + LLM real (TF-IDF + Gemini/Claude), não mock",
    why: "A versão original de ask_wiki era um mock determinístico — só concatenava trechos de busca por substring, sem entender a pergunta de verdade.",
    status: "feito",
    outcome: "WikiChunkIndex (recuperação, TF-IDF sobre pedaços por seção) + IWikiAnswerGenerator plugável (geração) — Gemini tem prioridade se GEMINI_API_KEY estiver configurada, senão Claude, senão cai pro mock (nunca fica fora do ar por falta de chave). Chave nunca em arquivo versionado — Secret do k8s em produção.",
  },
];

function BacklogModal({ item, onClose }: { item: BacklogItem; onClose: () => void }) {
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
              PRIORIDADE {item.priority} ·{" "}
              <span style={{ color: item.status === "feito" ? "var(--gold)" : "var(--accent)" }}>
                {item.status === "feito" ? "FEITO" : "ABERTO"}
              </span>
            </div>
            <h2 style={{ fontSize: 16, color: "var(--gold)", margin: "4px 0 0", lineHeight: 1.4 }}>
              {item.item}
            </h2>
          </div>
          <button onClick={onClose} className="text-xl leading-none text-text2 hover:text-text">
            ×
          </button>
        </div>

        <div style={{ marginTop: 16 }}>
          <div style={{ fontSize: 10, letterSpacing: 1, color: "var(--text2)", marginBottom: 6 }}>
            POR QUE
          </div>
          <p style={{ fontSize: 13, lineHeight: 1.7 }}>{item.why}</p>
        </div>

        {item.outcome && (
          <div style={{ marginTop: 16 }}>
            <div style={{ fontSize: 10, letterSpacing: 1, color: "var(--text2)", marginBottom: 6 }}>
              O QUE FOI FEITO
            </div>
            <p style={{ fontSize: 13, lineHeight: 1.7 }}>{item.outcome}</p>
          </div>
        )}
      </div>
    </div>
  );
}

export default function Backlog() {
  const [selected, setSelected] = useState<BacklogItem | null>(null);
  const abertos = BACKLOG.filter((b) => b.status === "aberto").length;

  return (
    <div style={{ maxWidth: 900 }}>
      <Card title={`BACKLOG (${abertos} ABERTOS / ${BACKLOG.length} TOTAL)`}>
        <p style={{ fontSize: 12, color: "var(--text2)", marginBottom: 14 }}>
          Melhorias identificadas ao longo da POC, ainda não implementadas (ou já implementadas e
          marcadas como tal). Clique num item pra ver o porquê — itens feitos também mostram o que
          foi implementado.
        </p>
        <div style={{ display: "flex", flexDirection: "column", gap: 8 }}>
          {BACKLOG.map((b) => (
            <div
              key={b.priority}
              onClick={() => setSelected(b)}
              className="cursor-pointer rounded-lg border border-border p-3 transition-colors hover:border-border-bright hover:bg-bg2"
              style={{ display: "flex", alignItems: "flex-start", gap: 10 }}
            >
              <span
                style={{
                  fontSize: 10,
                  color: "var(--text2)",
                  border: "1px solid var(--border-bright)",
                  borderRadius: 4,
                  padding: "1px 6px",
                  flexShrink: 0,
                  marginTop: 2,
                }}
              >
                #{b.priority}
              </span>
              <div style={{ flex: 1 }}>
                <div style={{ fontSize: 13 }}>{b.item}</div>
              </div>
              <span
                style={{
                  fontSize: 9,
                  letterSpacing: 1,
                  color: b.status === "feito" ? "var(--gold)" : "var(--accent)",
                  flexShrink: 0,
                  marginTop: 3,
                }}
              >
                {b.status === "feito" ? "FEITO" : "ABERTO"}
              </span>
            </div>
          ))}
        </div>
      </Card>

      {selected && <BacklogModal item={selected} onClose={() => setSelected(null)} />}
    </div>
  );
}
