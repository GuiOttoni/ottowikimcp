# OttoWikiMcp

POC de um MCP server em .NET 10 que expõe:

1. **Busca e leitura da Wiki** (hoje: uma wiki fake local em `fake-azure-wiki/`, simulando o
   formato de uma Wiki do Azure DevOps — trocar para a wiki real é só mudar `Wiki__RepoUrl`).
2. **Consulta a APIs internas de trabalho** (hoje: `OttoWikiMcp.WorkApiMock`, uma API fake de
   Tickets/Instituições — representa o tipo de API que existiria de verdade na empresa).

Ver a proposta de arquitetura completa (versão "produção, no trabalho") em
[`F:/Projetos/docs`](../../docs/docs/pesquisas/arquitetura-wiki-mcp-k8s.md) — esta POC é a
implementação local de uma fatia dela, para validar a ideia antes de replicar no ambiente
corporativo.

## Por que sem PAT

O plano original previa autenticar contra a Wiki do Azure DevOps com um PAT (Personal Access
Token). Na prática, é comum que a política da empresa **bloqueie a criação de PAT**. A solução
usada aqui: `GitWikiSync` (`src/OttoWikiMcp.McpServer/Services/GitWikiSync.cs`) clona/atualiza a
wiki chamando o **`git` do sistema operacional diretamente**, em vez de qualquer biblioteca .NET
de Git com autenticação própria. Contra uma wiki real do Azure DevOps
(`https://dev.azure.com/{org}/{project}/_git/{project}.wiki`), isso significa que quem autentica
é o **Git Credential Manager** já instalado na máquina — que dispara um login interativo via
navegador (OAuth contra o Entra ID da organização) na primeira vez. Times que bloqueiam PAT quase
sempre ainda permitem login normal via AAD, e é esse caminho que o GCM usa — nenhum PAT é gerado
nem armazenado em lugar nenhum.

**Trocar da wiki fake para a real**: mude só a variável de ambiente `Wiki__RepoUrl` (ou
`Wiki:RepoUrl` no `appsettings.json`) para a URL real. Nenhum código muda.

## Estrutura

```
OttoWikiMcp/
  fake-azure-wiki/              # repo git fake simulando uma Wiki do Azure DevOps
  src/
    OttoWikiMcp.McpServer/      # o MCP server (ASP.NET Core + ModelContextProtocol.AspNetCore)
      Services/GitWikiSync.cs   # clone/pull via `git` do sistema (sem PAT)
      Plugins/WikiPlugin.cs     # plugin nativo do Semantic Kernel para ler a wiki local
      Tools/                    # tools MCP (search_wiki, get_wiki_page, list_tickets, etc.)
    OttoWikiMcp.WorkApiMock/    # API fake de Tickets/Instituições (dados em memória)
  k8s/                          # manifests de deploy (namespace, workapi, mcpserver)
```

## Tools MCP expostas

| Tool | O que faz |
|---|---|
| `search_wiki(query)` | Busca por texto nas páginas da wiki |
| `get_wiki_page(path)` | Conteúdo completo de uma página |
| `list_wiki_pages()` | Lista todas as páginas |
| `sync_wiki()` | Força um `git pull` da wiki agora |
| `list_tickets(status?, institutionId?)` | Lista tickets, com filtro opcional |
| `get_ticket(id)` | Um ticket específico |
| `list_institutions()` | Lista instituições clientes |
| `get_institution(id)` | Uma instituição específica |

A busca na wiki hoje é **por substring simples** (case-insensitive), não semântica de verdade.
`WikiPlugin` já está modelado como plugin de Semantic Kernel de propósito: para evoluir para
busca semântica real (embeddings + um conector de LLM/Azure OpenAI), a interface das tools MCP
não muda — só a implementação interna de `SearchWiki`.

## Recriando a wiki fake

`fake-azure-wiki/` é seu próprio repositório git (simula o backend git de uma Wiki do Azure
DevOps de verdade) e por isso não é versionado dentro deste repo. Recrie com:

```bash
bash scripts/seed-fake-wiki.sh
```

## Rodando localmente

```bash
# Terminal 1 — API fake de trabalho
cd src/OttoWikiMcp.WorkApiMock
dotnet run --urls http://localhost:5241

# Terminal 2 — MCP server
cd src/OttoWikiMcp.McpServer
dotnet run --urls http://localhost:5250
```

Testar o handshake MCP (Streamable HTTP):

```bash
curl -s -X POST http://localhost:5250/mcp \
  -H "Content-Type: application/json" -H "Accept: application/json, text/event-stream" \
  -d '{"jsonrpc":"2.0","id":1,"method":"tools/list"}'
```

## Deploy no Kubernetes (k3s da VPS)

Sem registry: as imagens são exportadas com `docker save`, copiadas pra VPS via SCP e importadas
direto no containerd do k3s com `k3s ctr images import` (single-node, dispensa um registry).

```bash
docker build -f src/OttoWikiMcp.WorkApiMock/Dockerfile -t ottowikimcp-workapi:latest src/OttoWikiMcp.WorkApiMock
docker build -f src/OttoWikiMcp.McpServer/Dockerfile -t ottowikimcp-server:latest .

docker save ottowikimcp-workapi:latest -o dist/workapi.tar
docker save ottowikimcp-server:latest -o dist/mcpserver.tar
# scp dist/*.tar pra VPS, depois na VPS: k3s ctr images import workapi.tar (e mcpserver.tar)

kubectl --context vps70119-k3s apply -f k8s/namespace.yaml
kubectl --context vps70119-k3s apply -f k8s/workapi.yaml
kubectl --context vps70119-k3s apply -f k8s/mcpserver.yaml
```

O MCP server fica exposto via `NodePort` em `:30880` — ou seja, de fora do cluster:
`http://<ip-da-vps>:30880/mcp`.

> ⚠️ **Sem autenticação nenhuma no endpoint MCP nesta POC.** Isso é aceitável só porque é uma VPS
> pessoal de teste. Antes de replicar esse padrão no trabalho — ou de deixar isso exposto por
> muito tempo — ver a seção de autenticação da [pesquisa de
> arquitetura](../../docs/docs/pesquisas/arquitetura-wiki-mcp-k8s.md#7-autenticação--o-ponto-mais-delicado):
> no mínimo um token de time atrás de rede interna/VPN antes de considerar produção.

## Registrando no Claude Code

```json
{
  "mcpServers": {
    "otto-wiki": {
      "url": "http://<ip-da-vps>:30880/mcp"
    }
  }
}
```
