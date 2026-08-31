export type Ticket = {
  id: number;
  instituicaoId: number;
  subject: string;
  status: string;
  priority: string;
  createdAt: string;
};

export type Instituicao = {
  id: number;
  nome: string;
  cnpj: string;
  situacaoCadastral: string | null;
  dataAbertura: string | null;
  papeis: string[];
};

export type TipoDeFundo = { id: number; nome: string; descricao: string };
export type TipoMercado = { id: number; nome: string; descricao: string };

export type Fundo = {
  id: number;
  nome: string;
  cnpj: string;
  codigoCvm: string | null;
  tipoDeFundoId: number;
  tipoMercadoId: number;
  gestoraId: number | null;
  administradoraId: number | null;
  patrimonioLiquido: number;
  dataInicio: string | null;
  benchmark: string | null;
  taxaAdministracaoPercentual: number | null;
  taxaPerformancePercentual: number | null;
  moeda: string;
};

export type FundoBusca = { cnpj: string; nome: string; tipo: string; administrador: string; gestor: string };

export type FundoHistoricoPonto = { fundoId: number; data: string; valorCota: number; patrimonioLiquido: number };

export type CnpjInfo = {
  razao_social?: string;
  descricao_situacao_cadastral?: string;
  data_inicio_atividade?: string;
  descricao_natureza_juridica?: string;
  municipio?: string;
  uf?: string;
};

export type WikiPage = {
  path: string;
  tags: string[];
  title: string | null;
};

export type McpToolParam = { name: string; type: string; description: string; required: boolean };
export type McpTool = { name: string; description: string; category: string; parameters: McpToolParam[] };

async function json<T>(res: Response): Promise<T> {
  if (!res.ok) throw new Error(`${res.status} ${await res.text()}`);
  return res.json();
}

export const api = {
  health: () => fetch("/healthz").then((r) => r.ok),

  wikiPages: () => fetch("/api/wiki/pages").then((r) => json<WikiPage[]>(r)),

  wikiPage: (path: string) =>
    fetch(`/api/wiki/page?path=${encodeURIComponent(path)}`).then((r) =>
      json<{ path: string; content: string; hash: string }>(r)
    ),

  wikiSearch: (query: string) =>
    fetch(`/api/wiki/search?q=${encodeURIComponent(query)}`).then((r) =>
      json<{ query: string; results: string }>(r)
    ),

  /** `expectedHash` habilita a detecção de conflito de escrita concorrente — se a página mudou
   * desde que foi lida, o backend responde 409 e `json()` rejeita com essa mensagem. */
  wikiUpdate: (path: string, content: string, expectedHash?: string) =>
    fetch("/api/wiki/page", {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ path, content, expectedHash }),
    }).then((r) => json<{ message: string; hash: string }>(r)),

  wikiAsk: (question: string) =>
    fetch("/api/wiki/ask", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ question }),
    }).then((r) => json<{ question: string; answer: string; sources: string[] }>(r)),

  tickets: () => fetch("/api/tickets").then((r) => json<Ticket[]>(r)),

  fundos: () => fetch("/api/fundos").then((r) => json<Fundo[]>(r)),

  fundoHistorico: (id: number) =>
    fetch(`/api/fundos/${id}/historico`).then((r) => json<FundoHistoricoPonto[]>(r)),

  tiposDeFundo: () => fetch("/api/fundos/tipos").then((r) => json<TipoDeFundo[]>(r)),

  tiposMercado: () => fetch("/api/fundos/mercados").then((r) => json<TipoMercado[]>(r)),

  instituicoes: (papel?: string) =>
    fetch(`/api/fundos/instituicoes${papel ? `?papel=${encodeURIComponent(papel)}` : ""}`).then((r) =>
      json<Instituicao[]>(r)
    ),

  buscarFundos: (q: string) =>
    fetch(`/api/fundos/buscar?q=${encodeURIComponent(q)}`).then((r) => json<FundoBusca[]>(r)),

  buscarCnpj: (cnpj: string) =>
    fetch(`/api/fundos/buscar-cnpj/${cnpj.replace(/\D/g, "")}`).then((r) => json<CnpjInfo>(r)),

  mcpTools: () => fetch("/api/mcp/tools").then((r) => json<McpTool[]>(r)),
};
