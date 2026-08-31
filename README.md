# OttoWikiMcp

POC de um MCP server em .NET 10 que expõe:

1. **Busca e leitura da Wiki** (hoje: uma wiki fake local em `fake-azure-wiki/`, simulando o
   formato de uma Wiki do Azure DevOps — trocar para a wiki real é só mudar `Wiki__RepoUrl`).
2. **Consulta a APIs internas de trabalho** (hoje: `OttoWikiMcp.WorkApiMock`, uma API fake de
   Tickets/Instituições — representa o tipo de API que existiria de verdade na empresa).

Ver a proposta de arquitetura completa (versão "produção, no trabalho") em
[`F:/Projetos/docs`](../../docs/docs/pesquisas/arquitetura-wiki-mcp-k8s.md) — esta POC é a
implementação local de uma fatia dela, para validar a ideia antes de replicar no ambiente
corporativo. Documentos relacionados, também em `F:/Projetos/docs`:

- [O que foi feito nesta rodada (escrita + "ask AI") e por quê](../../docs/docs/pesquisas/poc-otowikimcp-ask-e-escrita.md)
- [Guia prático de replicação no PFS.Automation](../../docs/docs/pesquisas/guia-mcp-docs-pfs-automation.md)
- [Boas práticas: MCP de escrita + RAG sobre wiki interna](../../docs/docs/pesquisas/boas-praticas-mcp-docs-rag.md)

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
| `update_wiki_page(path, content)` | Cria/edita o conteúdo de uma página e commita a mudança no clone local |
| `ask_wiki(question)` | Pergunta em linguagem natural sobre a wiki (ver seção "Ask" abaixo — mock, sem LLM real) |
| `list_tickets(status?, institutionId?)` | Lista tickets, com filtro opcional |
| `get_ticket(id)` | Um ticket específico |
| `list_institutions()` | Lista instituições clientes |
| `get_institution(id)` | Uma instituição específica |

A busca na wiki hoje é **por substring simples** (case-insensitive), não semântica de verdade.
`WikiPlugin` já está modelado como plugin de Semantic Kernel de propósito: para evoluir para
busca semântica real (embeddings + um conector de LLM/Azure OpenAI), a interface das tools MCP
não muda — só a implementação interna de `SearchWiki`.

## Escrita na wiki (`update_wiki_page`)

`update_wiki_page` escreve o arquivo `.md` no clone local (`Services/GitWikiSync.cs`) e faz
`git add -A` + `git commit` **só no clone local** — contra o "remoto" (`fake-azure-wiki/` nesta
POC), a mudança não é publicada. Contra uma Wiki real do Azure DevOps, seria preciso um `git push`
adicional depois do commit (ver o guia de implementação para esse passo num ambiente real, onde
push provavelmente exige revisão/PR antes de ir pra branch principal da wiki). A tool valida que o
`path` não escapa da raiz da wiki (sem `../../`) antes de escrever.

## "Perguntar à IA" (`ask_wiki`) — mock nesta POC

`Services/WikiAskService.cs` reusa `search_wiki` (busca por substring) e devolve os trechos
encontrados com uma frase de abertura, citando as páginas-fonte — **sem chamar nenhum LLM de
verdade**. Isso valida o fluxo pergunta → busca → resposta com citação sem custo nem chave de API.
A escolha do LLM real para o ambiente de trabalho (Azure OpenAI, gateway interno, etc.) fica em
aberto — ver a seção "IA do ask" do guia de implementação para o PFS.Automation.

## Recriando a wiki fake

`fake-azure-wiki/` é seu próprio repositório git (simula o backend git de uma Wiki do Azure
DevOps de verdade) e por isso não é versionado dentro deste repo. Recrie com:

```bash
bash scripts/seed-fake-wiki.sh
```

## Rodando localmente

`Wiki:RepoUrl` não vem com um valor padrão no `appsettings.json` (de propósito — é um caminho de
máquina, não deveria ir versionado). Rode `scripts/seed-fake-wiki.sh` primeiro (ver acima) e
aponte pra pasta gerada:

```bash
# Terminal 1 — API fake de trabalho
cd src/OttoWikiMcp.WorkApiMock
dotnet run --urls http://localhost:5241

# Terminal 2 — MCP server (ajuste o caminho pro seu clone do repo)
cd src/OttoWikiMcp.McpServer
export Wiki__RepoUrl="file:///caminho/absoluto/pra/OttoWikiMcp/fake-azure-wiki"
dotnet run --urls http://localhost:5250
```

Testar o handshake MCP (Streamable HTTP):

```bash
curl -s -X POST http://localhost:5250/mcp \
  -H "Content-Type: application/json" -H "Accept: application/json, text/event-stream" \
  -d '{"jsonrpc":"2.0","id":1,"method":"tools/list"}'
```

## Deploy no Kubernetes (k3s da VPS)

As imagens vão pro Docker Hub (repositório público `guiottoni/ottowikimcp-*`) — o k3s da VPS só
faz `docker pull` sozinho, sem SCP/SSH/`k3s ctr images import` manual. Configuração única:

```bash
docker login -u guiottoni   # uma vez só, salva a credencial local
kubectl --context vps70119-k3s apply -f k8s/namespace.yaml   # só na primeira vez
```

Depois disso, toda atualização é só:

```bash
bash scripts/deploy.sh
```

`scripts/deploy.sh` builda as duas imagens, dá `docker push` e roda
`kubectl rollout restart` nos dois deployments (que têm `imagePullPolicy: Always`, então sempre
puxam o `:latest` mais recente do registry ao reiniciar). Os manifests (`k8s/*.yaml`) já apontam
pra `docker.io/guiottoni/ottowikimcp-*:latest`.

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
