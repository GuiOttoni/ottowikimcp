import { Link } from "react-router-dom";
import { Card } from "../ui";
import { MermaidDiagram } from "../MermaidDiagram";

const DIAGRAM = `flowchart TB
    subgraph Cliente
        Browser["Navegador (Dashboard / Documentação / Ferramentas MCP)"]
        AgenteIA["Agente de IA (Claude Code) via MCP"]
    end

    subgraph Cluster["Kubernetes (k3s)"]
        subgraph McpServer["OttoWikiMcp.McpServer (ASP.NET Core 10)"]
            MCP["/mcp — transporte MCP (Streamable HTTP)"]
            REST["/api/* — REST pro frontend"]
            SPA["wwwroot/ — build estático do React"]
            SK["Semantic Kernel (WikiPlugin)"]
        end
        WorkApi["OttoWikiMcp.WorkApiMock (ASP.NET Core 10)"]
        Wiki[("Wiki (pasta local versionada em git,\\ncuidada pelo próprio OttoWikiMcp)")]
    end

    subgraph Externo["APIs públicas"]
        BrasilAPI["BrasilAPI\\n(CNPJ ao vivo)"]
        CVM["CVM Dados Abertos\\n(baked, offline)"]
    end

    Browser -->|HTTPS| REST
    Browser -->|carrega| SPA
    AgenteIA -->|JSON-RPC| MCP
    MCP --> SK
    REST --> SK
    SK --> Wiki
    REST -->|HttpClient nomeado| WorkApi
    MCP -->|WorkApiTools| WorkApi
    WorkApi -->|só na busca de CNPJ, em tempo real| BrasilAPI
    WorkApi -.->|dados carregados 1x no startup, gerados offline a partir de| CVM`;

const STACK: Array<{ camada: string; tecnologia: string; papel: string }> = [
  { camada: "Frontend", tecnologia: "React 19 + Vite + TypeScript", papel: "SPA servida como arquivos estáticos pelo próprio backend (wwwroot/)" },
  { camada: "Roteamento", tecnologia: "react-router-dom", papel: "Rotas /, /docs/*, /mcp-tools, /arquitetura" },
  { camada: "Estilo", tecnologia: "Tailwind CSS v4 (@tailwindcss/vite) + tokens CSS próprios", papel: "Paleta escura, vermelho pastel + dourado; adoção incremental (:root + @theme)" },
  { camada: "Markdown", tecnologia: "react-markdown + remark-gfm + rehype-highlight", papel: "Renderiza pra componentes React reais, sem dangerouslySetInnerHTML na maior parte do fluxo" },
  { camada: "Diagramas", tecnologia: "mermaid (import dinâmico)", papel: "Blocos ```mermaid``` (e esta própria página) viram SVG de verdade" },
  { camada: "Editor", tecnologia: "@uiw/react-md-editor", papel: "Split-pane com preview ao vivo pro modo de edição da wiki" },
  { camada: "Backend MCP", tecnologia: "ASP.NET Core 10 + SDK oficial ModelContextProtocol", papel: "Expõe tools via [McpServerTool], transporte Streamable HTTP em /mcp" },
  { camada: "Orquestração de tools", tecnologia: "Microsoft Semantic Kernel", papel: "WikiPlugin registrado como plugin de Kernel — tools chamam via kernel.InvokeAsync" },
  { camada: "API de trabalho", tecnologia: "ASP.NET Core 10, Minimal API + Controllers", papel: "OttoWikiMcp.WorkApiMock — tickets, fundos, instituições" },
  { camada: "Persistência da wiki", tecnologia: "Git (via git do sistema, sem lib .NET)", papel: "GitWikiSync clona/atualiza/commita — funciona tanto com a wiki como pasta dentro do próprio projeto quanto com um repositório git externo (ex.: Azure DevOps Wiki); sem PAT" },
  { camada: "Dados de fundos", tecnologia: 'JSON "baked" (Data/*.json)', papel: "Gerado offline a partir da CVM, carregado em memória no startup" },
  { camada: "Deploy", tecnologia: "Docker + Kubernetes (k3s)", papel: "Duas imagens (mcpserver, workapi), publicadas no Docker Hub" },
  { camada: "CI/CD", tecnologia: "scripts/deploy.sh", papel: "build → push → kubectl rollout restart, um comando só" },
];

export default function Arquitetura() {
  return (
    <div style={{ maxWidth: 1000 }}>
      <Card title="ARQUITETURA DO OTTOWIKIMCP">
        <p style={{ fontSize: 13, lineHeight: 1.6, color: "var(--text2)", marginTop: 0 }}>
          Como as peças se encaixam, qual API está por trás de cada uma, e a stack técnica de cada
          camada.
        </p>
        <ul style={{ fontSize: 13, lineHeight: 1.7, paddingLeft: 18, margin: 0, color: "var(--text2)" }}>
          <li>
            Lista interativa de tools MCP →{" "}
            <Link to="/mcp-tools" style={{ color: "var(--gold)" }}>
              Ferramentas MCP
            </Link>
          </li>
          <li>
            Endpoints consumidos (internos e externos) →{" "}
            <Link to="/docs/Arquitetura/Endpoints-Consumidos" style={{ color: "var(--gold)" }}>
              Endpoints Consumidos
            </Link>
          </li>
          <li>
            Schema de dados de fundos →{" "}
            <Link to="/docs/Arquitetura/Banco-de-Dados-Fundos" style={{ color: "var(--gold)" }}>
              Banco de Dados — Fundos de Investimento
            </Link>
          </li>
        </ul>
      </Card>

      <Card title="DIAGRAMA DE COMPONENTES">
        <MermaidDiagram chart={DIAGRAM} />
      </Card>

      <Card title="CAMADAS E STACK TÉCNICA">
        <div className="table-scroll">
          <table style={{ width: "100%", borderCollapse: "collapse", fontSize: 12 }}>
            <thead>
              <tr>
                {["Camada", "Tecnologia", "Papel"].map((h) => (
                  <th
                    key={h}
                    style={{
                      textAlign: "left",
                      padding: "6px 8px",
                      borderBottom: "1px solid var(--border-bright)",
                      color: "var(--text2)",
                      fontWeight: "normal",
                      fontSize: 10,
                      letterSpacing: 1,
                    }}
                  >
                    {h.toUpperCase()}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {STACK.map((row) => (
                <tr key={row.camada}>
                  <td style={{ padding: "6px 8px", borderBottom: "1px solid var(--border)", color: "var(--gold)", whiteSpace: "nowrap" }}>
                    {row.camada}
                  </td>
                  <td style={{ padding: "6px 8px", borderBottom: "1px solid var(--border)", whiteSpace: "nowrap" }}>
                    {row.tecnologia}
                  </td>
                  <td style={{ padding: "6px 8px", borderBottom: "1px solid var(--border)", color: "var(--text2)" }}>
                    {row.papel}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </Card>

      <Card title="POR QUE DOIS SERVIÇOS, NÃO UM SÓ">
        <ul style={{ fontSize: 13, lineHeight: 1.7, paddingLeft: 18, margin: 0 }}>
          <li>
            <code style={{ color: "var(--gold)" }}>WorkApiMock</code> representa a API interna que
            já existiria de verdade numa empresa — tickets, dados de fundos.
          </li>
          <li>
            <code style={{ color: "var(--gold)" }}>McpServer</code> não reimplementa nada disso, só{" "}
            <strong>consome</strong> via <code style={{ color: "var(--gold)" }}>HttpClient</code>{" "}
            nomeado — igual consumiria uma API real no ambiente de trabalho.
          </li>
          <li>
            Isso não é acidente de design: é o cenário que a POC foi feita pra replicar. No guia de
            implementação pro ambiente de trabalho, o &ldquo;WorkApi&rdquo; já existe — não precisa
            ser recriado, só apontado.
          </li>
        </ul>
      </Card>

      <Card title="UM PROCESSO, DOIS TRANSPORTES (DENTRO DO MCPSERVER)">
        <ul style={{ fontSize: 13, lineHeight: 1.7, paddingLeft: 18, margin: 0 }}>
          <li>
            A lógica de negócio mora num só lugar: <code style={{ color: "var(--gold)" }}>WikiPlugin</code>,
            registrado como plugin de Semantic Kernel.
          </li>
          <li>
            Ela é exposta duas vezes, por dois transportes diferentes — <code style={{ color: "var(--gold)" }}>/mcp</code>{" "}
            (JSON-RPC, pra agentes de IA) e <code style={{ color: "var(--gold)" }}>/api/*</code>{" "}
            (REST, pro próprio frontend React).
          </li>
          <li>
            Nenhum dos dois duplica a integração com a wiki — ambos chamam o mesmo Kernel por baixo.
          </li>
        </ul>
      </Card>

      <Card title="COMO EVOLUIR ESTA ARQUITETURA">
        <div style={{ fontSize: 13, lineHeight: 1.7 }}>
          <div style={{ fontSize: 11, letterSpacing: 1, color: "var(--accent)", marginBottom: 6 }}>
            TRAZENDO INFORMAÇÃO DE OUTROS MODELOS/AGENTES
          </div>
          <ul style={{ paddingLeft: 18, margin: "0 0 16px" }}>
            <li>
              O MCP já é o ponto de encontro: qualquer cliente que fale o protocolo (não só
              Claude Code) enxerga as mesmas tools em <code style={{ color: "var(--gold)" }}>/mcp</code> —
              trazer outro agente/modelo é registrar mais um cliente, não mudar o servidor.
            </li>
            <li>
              Pro sentido contrário (o próprio servidor consultando outro modelo/agente como
              fonte), o ponto de entrada é sempre virar um <strong>plugin de Semantic Kernel</strong> —
              hoje só existe <code style={{ color: "var(--gold)" }}>WikiPlugin</code>; um segundo
              agente vira só mais um plugin registrado no mesmo <code style={{ color: "var(--gold)" }}>Kernel</code>.
            </li>
            <li>
              Isso vale também pra APIs externas quaisquer, sem escrever uma classe de tool nova
              a cada uma — ver a pesquisa dedicada a isso (import dinâmico de OpenAPI via Semantic
              Kernel + handlers dinâmicos do SDK do MCP, fora desta wiki, no guia de arquitetura).
            </li>
          </ul>

          <div style={{ fontSize: 11, letterSpacing: 1, color: "var(--accent)", marginBottom: 6 }}>
            COMO EVOLUIR A ARQUITETURA COMO UM TODO
          </div>
          <ul style={{ paddingLeft: 18, margin: "0 0 16px" }}>
            <li>
              O padrão que já se repete três vezes (Wiki, fundos/instituições, tickets) é o
              molde pra qualquer domínio novo: um plugin de Kernel + um wrapper{" "}
              <code style={{ color: "var(--gold)" }}>[McpServerTool]</code> + um endpoint REST —
              sem inventar uma estrutura diferente por domínio.
            </li>
            <li>
              O próximo salto real não é adicionar mais domínios um por um à mão, é tornar o
              registro de fontes de dados <strong>configuração, não código</strong> — um domínio
              novo vira uma entrada num registro (nome, origem, allowlist), não uma classe C#
              nova a cada vez.
            </li>
            <li>
              <code style={{ color: "var(--gold)" }}>WorkApiMock</code> continua existindo mesmo
              depois disso — ele representa a API real da empresa, não é substituído pela
              evolução do lado do MCP.
            </li>
          </ul>

          <div style={{ fontSize: 11, letterSpacing: 1, color: "var(--accent)", marginBottom: 6 }}>
            COMO EVOLUIR O RAG (MAIS INFORMAÇÃO, MELHOR RECUPERAÇÃO)
          </div>
          <ul style={{ paddingLeft: 18, margin: 0 }}>
            <li>
              Hoje <code style={{ color: "var(--gold)" }}>search_wiki</code> é busca por
              substring — funciona pra termo exato, erra pergunta parafraseada mesmo quando a
              resposta está na wiki (confirmado nesta POC).
            </li>
            <li>
              Primeiro passo real: busca híbrida (lexical + embeddings) em vez de só substring —
              o gargalo de um RAG quase sempre é a recuperação, não o modelo que responde depois.
            </li>
            <li>
              Semantic Kernel já tem conectores de memória/vetor prontos pra isso — trocar a
              implementação de busca não exige tocar nas tools MCP que a chamam, só o que está
              por trás de <code style={{ color: "var(--gold)" }}>search_wiki</code>.
            </li>
            <li>
              Mais fontes de conteúdo pro RAG (além da wiki) entram pelo mesmo mecanismo da
              seção anterior — cada fonte nova é mais um plugin/registro, indexado do jeito que
              fizer sentido pra ela, consultado pelo mesmo <code style={{ color: "var(--gold)" }}>ask_wiki</code>.
            </li>
          </ul>
        </div>
      </Card>
    </div>
  );
}
