import { useEffect, useMemo, useState } from "react";
import {
  api,
  type Ticket,
  type Instituicao,
  type Fundo,
  type TipoDeFundo,
  type TipoMercado,
  type FundoBusca,
  type CnpjInfo,
  type FundoHistoricoPonto,
} from "../api";
import { Badge, Card, cellStyle, inputStyle, selectStyle } from "../ui";

const thStyle: React.CSSProperties = {
  textAlign: "left",
  padding: "6px 8px",
  borderBottom: "1px solid var(--border)",
  color: "var(--text2)",
  fontWeight: "normal",
  fontSize: 10,
  letterSpacing: 1,
};

function money(v: number) {
  return v.toLocaleString("pt-BR", { style: "currency", currency: "BRL", maximumFractionDigits: 0 });
}

const truncCellStyle: React.CSSProperties = {
  ...cellStyle,
  maxWidth: 260,
  overflow: "hidden",
  textOverflow: "ellipsis",
  whiteSpace: "nowrap",
};

const clickableRowStyle: React.CSSProperties = { cursor: "pointer" };

function rowHoverOn(e: React.MouseEvent<HTMLTableRowElement>) {
  e.currentTarget.style.background = "var(--bg2)";
}
function rowHoverOff(e: React.MouseEvent<HTMLTableRowElement>) {
  e.currentTarget.style.background = "transparent";
}

function PapelBadge({ papel }: { papel: string }) {
  const color = papel === "Gestora" ? "var(--gold)" : "var(--accent)";
  return (
    <span
      style={{
        display: "inline-block",
        padding: "1px 6px",
        borderRadius: 3,
        fontSize: 10,
        marginRight: 4,
        border: `1px solid ${color}`,
        color,
      }}
    >
      {papel}
    </span>
  );
}

export default function Dashboard() {
  const [tickets, setTickets] = useState<Ticket[]>([]);
  const [instituicoes, setInstituicoes] = useState<Instituicao[]>([]);
  const [fundos, setFundos] = useState<Fundo[]>([]);
  const [tiposDeFundo, setTiposDeFundo] = useState<TipoDeFundo[]>([]);
  const [tiposMercado, setTiposMercado] = useState<TipoMercado[]>([]);

  const [query, setQuery] = useState("");
  const [searching, setSearching] = useState(false);
  const [searchResults, setSearchResults] = useState<FundoBusca[] | null>(null);
  const [cnpjInfo, setCnpjInfo] = useState<CnpjInfo | { error: string } | null>(null);

  const [fundoTipoFiltro, setFundoTipoFiltro] = useState<number | "">("");
  const [fundoMercadoFiltro, setFundoMercadoFiltro] = useState<number | "">("");
  const [fundoNomeFiltro, setFundoNomeFiltro] = useState("");
  const [instPapelFiltro, setInstPapelFiltro] = useState<"" | "Gestora" | "Administradora">("");
  const [instNomeFiltro, setInstNomeFiltro] = useState("");

  const [selectedFundo, setSelectedFundo] = useState<Fundo | null>(null);
  const [selectedHistorico, setSelectedHistorico] = useState<FundoHistoricoPonto[] | null>(null);
  const [loadingHistorico, setLoadingHistorico] = useState(false);

  useEffect(() => {
    api.tickets().then(setTickets).catch(() => {});
    api.instituicoes().then(setInstituicoes).catch(() => {});
    api.fundos().then(setFundos).catch(() => {});
    api.tiposDeFundo().then(setTiposDeFundo).catch(() => {});
    api.tiposMercado().then(setTiposMercado).catch(() => {});
  }, []);

  const instituicaoNome = useMemo(() => {
    const m = new Map<number, string>();
    for (const i of instituicoes) m.set(i.id, i.nome);
    return m;
  }, [instituicoes]);
  const tipoDeFundoNome = useMemo(() => {
    const m = new Map<number, string>();
    for (const t of tiposDeFundo) m.set(t.id, t.nome);
    return m;
  }, [tiposDeFundo]);
  const tipoMercadoNome = useMemo(() => {
    const m = new Map<number, string>();
    for (const t of tiposMercado) m.set(t.id, t.nome);
    return m;
  }, [tiposMercado]);

  const fundosFiltrados = useMemo(() => {
    return fundos.filter(
      (f) =>
        (fundoTipoFiltro === "" || f.tipoDeFundoId === fundoTipoFiltro) &&
        (fundoMercadoFiltro === "" || f.tipoMercadoId === fundoMercadoFiltro) &&
        (fundoNomeFiltro.trim() === "" || f.nome.toLowerCase().includes(fundoNomeFiltro.trim().toLowerCase()))
    );
  }, [fundos, fundoTipoFiltro, fundoMercadoFiltro, fundoNomeFiltro]);

  const instituicoesFiltradas = useMemo(() => {
    return instituicoes.filter(
      (i) =>
        (instPapelFiltro === "" || i.papeis.includes(instPapelFiltro)) &&
        (instNomeFiltro.trim() === "" || i.nome.toLowerCase().includes(instNomeFiltro.trim().toLowerCase()))
    );
  }, [instituicoes, instPapelFiltro, instNomeFiltro]);

  async function openFundo(f: Fundo) {
    setSelectedFundo(f);
    setSelectedHistorico(null);
    setLoadingHistorico(true);
    try {
      const historico = await api.fundoHistorico(f.id);
      setSelectedHistorico(historico);
    } finally {
      setLoadingHistorico(false);
    }
  }

  async function runSearch(e: React.KeyboardEvent<HTMLInputElement>) {
    if (e.key !== "Enter" || query.trim().length < 2) return;
    setSearching(true);
    setCnpjInfo(null);
    try {
      const isCnpjLike = query.replace(/\D/g, "").length >= 11;
      const [funds, cnpj] = await Promise.all([
        api.buscarFundos(query.trim()),
        isCnpjLike && query.replace(/\D/g, "").length === 14
          ? api.buscarCnpj(query).catch(() => null)
          : Promise.resolve(null),
      ]);
      setSearchResults(funds);
      if (cnpj) setCnpjInfo(cnpj);
    } catch (err) {
      setSearchResults([]);
      setCnpjInfo({ error: (err as Error).message });
    } finally {
      setSearching(false);
    }
  }

  return (
    <div style={{ display: "grid", gridTemplateColumns: "1fr", gap: 0, width: "100%" }}>
      <Card title="BUSCAR FUNDO (NOME OU CNPJ — CVM + BRASILAPI AO VIVO)">
        <input
          type="text"
          placeholder="ex.: Itaú, Kinea, ou um CNPJ..."
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          onKeyDown={runSearch}
          style={inputStyle}
        />
        {searching && <div style={{ fontSize: 12, color: "var(--text2)" }}>buscando...</div>}
        {cnpjInfo && "error" in cnpjInfo && (
          <div style={{ fontSize: 12, color: "var(--text2)", marginBottom: 8 }}>CNPJ: não encontrado na BrasilAPI.</div>
        )}
        {cnpjInfo && "razao_social" in cnpjInfo && (
          <div
            style={{
              fontSize: 12,
              marginBottom: 10,
              padding: 8,
              border: "1px solid var(--border-bright)",
              borderRadius: 4,
            }}
          >
            <strong style={{ color: "var(--gold)" }}>{cnpjInfo.razao_social}</strong> — {cnpjInfo.descricao_situacao_cadastral}
            {cnpjInfo.municipio && ` · ${cnpjInfo.municipio}/${cnpjInfo.uf}`}
            {cnpjInfo.data_inicio_atividade && ` · aberta em ${cnpjInfo.data_inicio_atividade}`}
            <span style={{ color: "var(--text2)" }}> (consulta ao vivo via BrasilAPI)</span>
          </div>
        )}
        {searchResults && (
          <div className="table-scroll">
            <table style={{ width: "100%", borderCollapse: "collapse", fontSize: 12 }}>
              <thead>
                <tr>
                  {["Fundo", "Tipo", "CNPJ", "Administrador"].map((h) => (
                    <th key={h} style={thStyle}>
                      {h}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {searchResults.slice(0, 20).map((f) => (
                  <tr key={f.cnpj}>
                    <td style={truncCellStyle} title={f.nome}>
                      {f.nome}
                    </td>
                    <td style={cellStyle}>{f.tipo}</td>
                    <td style={cellStyle}>{f.cnpj}</td>
                    <td style={truncCellStyle} title={f.administrador}>
                      {f.administrador}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
        {searchResults && searchResults.length === 0 && (
          <div style={{ fontSize: 12, color: "var(--text2)" }}>Nenhum fundo encontrado.</div>
        )}
        {searchResults && searchResults.length > 20 && (
          <div style={{ fontSize: 11, color: "var(--text2)", marginTop: 6 }}>
            mostrando 20 de {searchResults.length} resultados
          </div>
        )}
      </Card>

      <Card title={`FUNDOS DE INVESTIMENTO (${fundosFiltrados.length} de ${fundos.length}, dados reais CVM)`}>
        <div className="mb-3 flex flex-wrap gap-2">
          <input
            type="text"
            placeholder="filtrar por nome..."
            value={fundoNomeFiltro}
            onChange={(e) => setFundoNomeFiltro(e.target.value)}
            style={{ ...inputStyle, width: 200, marginBottom: 0 }}
          />
          <select
            value={fundoTipoFiltro}
            onChange={(e) => setFundoTipoFiltro(e.target.value ? Number(e.target.value) : "")}
            style={selectStyle}
          >
            <option value="">Todos os tipos</option>
            {tiposDeFundo.map((t) => (
              <option key={t.id} value={t.id}>
                {t.nome}
              </option>
            ))}
          </select>
          <select
            value={fundoMercadoFiltro}
            onChange={(e) => setFundoMercadoFiltro(e.target.value ? Number(e.target.value) : "")}
            style={selectStyle}
          >
            <option value="">Todos os mercados</option>
            {tiposMercado.map((t) => (
              <option key={t.id} value={t.id}>
                {t.nome}
              </option>
            ))}
          </select>
          {(fundoTipoFiltro !== "" || fundoMercadoFiltro !== "" || fundoNomeFiltro !== "") && (
            <button
              onClick={() => {
                setFundoTipoFiltro("");
                setFundoMercadoFiltro("");
                setFundoNomeFiltro("");
              }}
              style={{ ...selectStyle, color: "var(--text2)" }}
            >
              limpar filtros
            </button>
          )}
        </div>
        <div className="table-scroll" style={{ maxHeight: 340, overflowY: "auto" }}>
          <table style={{ width: "100%", borderCollapse: "collapse", fontSize: 12 }}>
            <thead>
              <tr>
                {["Fundo", "Tipo", "Mercado", "Gestora", "Administradora", "PL"].map((h) => (
                  <th key={h} style={thStyle}>
                    {h}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {fundosFiltrados.map((f) => (
                <tr
                  key={f.id}
                  onClick={() => openFundo(f)}
                  style={{
                    ...clickableRowStyle,
                    background: selectedFundo?.id === f.id ? "var(--bg2)" : "transparent",
                  }}
                  onMouseEnter={rowHoverOn}
                  onMouseLeave={rowHoverOff}
                >
                  <td style={{ ...truncCellStyle, color: "var(--gold)" }} title={f.nome}>
                    {f.nome}
                  </td>
                  <td style={cellStyle}>{tipoDeFundoNome.get(f.tipoDeFundoId) ?? f.tipoDeFundoId}</td>
                  <td style={cellStyle}>{tipoMercadoNome.get(f.tipoMercadoId) ?? f.tipoMercadoId}</td>
                  <td style={truncCellStyle} title={f.gestoraId ? instituicaoNome.get(f.gestoraId) : ""}>
                    {f.gestoraId ? instituicaoNome.get(f.gestoraId) : "—"}
                  </td>
                  <td style={truncCellStyle} title={f.administradoraId ? instituicaoNome.get(f.administradoraId) : ""}>
                    {f.administradoraId ? instituicaoNome.get(f.administradoraId) : "—"}
                  </td>
                  <td style={cellStyle}>{money(f.patrimonioLiquido)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </Card>

      {selectedFundo && (
        <Card title={`DETALHE DO FUNDO`}>
          <div className="flex items-start justify-between gap-3">
            <div>
              <div style={{ fontSize: 15, color: "var(--gold)", marginBottom: 6 }}>{selectedFundo.nome}</div>
              <div style={{ fontSize: 12, color: "var(--text2)", lineHeight: 1.8 }}>
                CNPJ: <span style={{ color: "var(--text)" }}>{selectedFundo.cnpj}</span> · Código CVM:{" "}
                <span style={{ color: "var(--text)" }}>{selectedFundo.codigoCvm ?? "—"}</span> · Moeda:{" "}
                <span style={{ color: "var(--text)" }}>{selectedFundo.moeda}</span>
                <br />
                Tipo: <span style={{ color: "var(--text)" }}>{tipoDeFundoNome.get(selectedFundo.tipoDeFundoId)}</span> ·
                Mercado: <span style={{ color: "var(--text)" }}>{tipoMercadoNome.get(selectedFundo.tipoMercadoId)}</span>
                <br />
                Gestora:{" "}
                <span style={{ color: "var(--text)" }}>
                  {selectedFundo.gestoraId ? instituicaoNome.get(selectedFundo.gestoraId) : "—"}
                </span>
                <br />
                Administradora:{" "}
                <span style={{ color: "var(--text)" }}>
                  {selectedFundo.administradoraId ? instituicaoNome.get(selectedFundo.administradoraId) : "—"}
                </span>
                <br />
                Início: <span style={{ color: "var(--text)" }}>{selectedFundo.dataInicio ?? "—"}</span> · PL atual:{" "}
                <span style={{ color: "var(--text)" }}>{money(selectedFundo.patrimonioLiquido)}</span>
              </div>
            </div>
            <button onClick={() => setSelectedFundo(null)} style={{ ...selectStyle, color: "var(--text2)" }}>
              fechar
            </button>
          </div>

          <div style={{ marginTop: 14 }}>
            <div style={{ fontSize: 10, letterSpacing: 1, color: "var(--text2)", marginBottom: 6 }}>
              HISTÓRICO DE COTA
            </div>
            {loadingHistorico && <div style={{ fontSize: 12, color: "var(--text2)" }}>carregando...</div>}
            {!loadingHistorico && selectedHistorico?.length === 0 && (
              <div style={{ fontSize: 12, color: "var(--text2)" }}>
                Sem histórico público (fundos FIDC/FII não reportam nesse formato).
              </div>
            )}
            {!loadingHistorico && selectedHistorico && selectedHistorico.length > 0 && (
              <div className="table-scroll">
                <table style={{ width: "100%", borderCollapse: "collapse", fontSize: 12 }}>
                  <thead>
                    <tr>
                      {["Data", "Valor da cota", "Patrimônio líquido"].map((h) => (
                        <th key={h} style={thStyle}>
                          {h}
                        </th>
                      ))}
                    </tr>
                  </thead>
                  <tbody>
                    {selectedHistorico.map((p) => (
                      <tr key={p.data}>
                        <td style={cellStyle}>{p.data}</td>
                        <td style={cellStyle}>{p.valorCota.toLocaleString("pt-BR", { minimumFractionDigits: 4 })}</td>
                        <td style={cellStyle}>{money(p.patrimonioLiquido)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        </Card>
      )}

      <Card title={`INSTITUIÇÕES (${instituicoesFiltradas.length} de ${instituicoes.length}, gestoras/administradoras reais — CNPJ verificado)`}>
        <div className="mb-3 flex flex-wrap gap-2">
          <input
            type="text"
            placeholder="filtrar por nome..."
            value={instNomeFiltro}
            onChange={(e) => setInstNomeFiltro(e.target.value)}
            style={{ ...inputStyle, width: 200, marginBottom: 0 }}
          />
          <select
            value={instPapelFiltro}
            onChange={(e) => setInstPapelFiltro(e.target.value as "" | "Gestora" | "Administradora")}
            style={selectStyle}
          >
            <option value="">Todos os papéis</option>
            <option value="Gestora">Gestora</option>
            <option value="Administradora">Administradora</option>
          </select>
          {(instPapelFiltro !== "" || instNomeFiltro !== "") && (
            <button
              onClick={() => {
                setInstPapelFiltro("");
                setInstNomeFiltro("");
              }}
              style={{ ...selectStyle, color: "var(--text2)" }}
            >
              limpar filtros
            </button>
          )}
        </div>
        <div className="table-scroll" style={{ maxHeight: 300, overflowY: "auto" }}>
          <table style={{ width: "100%", borderCollapse: "collapse", fontSize: 12 }}>
            <thead>
              <tr>
                {["Nome", "CNPJ", "Papéis", "Situação"].map((h) => (
                  <th key={h} style={thStyle}>
                    {h}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {instituicoesFiltradas.map((i) => (
                <tr key={i.id}>
                  <td style={truncCellStyle} title={i.nome}>
                    {i.nome}
                  </td>
                  <td style={cellStyle}>{i.cnpj}</td>
                  <td style={cellStyle}>
                    {i.papeis.map((p) => (
                      <PapelBadge key={p} papel={p} />
                    ))}
                  </td>
                  <td style={cellStyle}>{i.situacaoCadastral ?? "—"}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </Card>

      <Card title="TICKETS">
        <div className="table-scroll">
        <table style={{ width: "100%", borderCollapse: "collapse", fontSize: 12 }}>
          <thead>
            <tr>
              {["ID", "Assunto", "Instituição", "Status", "Prioridade"].map((h) => (
                <th key={h} style={thStyle}>
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
                <td style={cellStyle}>{instituicaoNome.get(t.instituicaoId) ?? t.instituicaoId}</td>
                <td style={cellStyle}>
                  <Badge value={t.status} />
                </td>
                <td style={cellStyle}>
                  <Badge value={t.priority} />
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        </div>
      </Card>
    </div>
  );
}
