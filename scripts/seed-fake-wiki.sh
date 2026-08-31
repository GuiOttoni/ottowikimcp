#!/bin/bash
# Recria fake-azure-wiki/ do zero. A secao "Fundos de Investimento" e conteudo REAL
# (fontes: ANBIMA, CVM, BrasilAPI) - nao ficticio. O restante (Arquitetura/Onboarding/Runbooks,
# sobre um sistema de tickets de suporte generico) e material de demonstracao da POC, usado
# pra validar o GitWikiSync (git clone/pull) sem precisar de um Azure DevOps real.
# ATENCAO: nunca rode isto com o cwd do shell dentro de fake-azure-wiki/ (trava o rm -rf no Windows).
# Rodar a partir da raiz do repo OttoWikiMcp: bash scripts/seed-fake-wiki.sh
set -e

ROOT="$(dirname "$0")/../fake-azure-wiki"
rm -rf "$ROOT"
mkdir -p "$ROOT/Arquitetura" "$ROOT/FundosDeInvestimento" "$ROOT/Runbooks"

cat > "$ROOT/.order" <<'EOF'
Home
Arquitetura
Onboarding
Runbooks
EOF

cat > "$ROOT/Home.md" <<'EOF'
---
tags: [indice]
---
# Wiki do Time — Plataforma

Bem-vindo à wiki interna.

## 📊 Fundos de Investimento

Base de conhecimento **real e substantiva** sobre o mercado de fundos de investimento brasileiro —
conceitos, classificação ANBIMA/CVM, regulação (Resolução CVM 175), tributação, tamanho e
distribuição do mercado, e a diferença entre fundos onshore e offshore. Não é conteúdo de
demonstração: é pesquisa de verdade, com fontes citadas.

- [Visão Geral](/FundosDeInvestimento/Visao-Geral) — o que é um fundo, cota, taxas, come-cotas
- [Tipos de Fundos](/FundosDeInvestimento/Tipos-de-Fundos) — classificação ANBIMA/CVM
- [Onshore vs. Offshore](/FundosDeInvestimento/Onshore-vs-Offshore)
- [Regulação — Resolução CVM 175](/FundosDeInvestimento/Regulacao-CVM-175)
- [Tributação](/FundosDeInvestimento/Tributacao)
- [Distribuição e Tamanho do Mercado](/FundosDeInvestimento/Distribuicao-e-Mercado)
- [Fundos Cadastrados (dados reais CVM)](/FundosDeInvestimento/Fundos-Cadastrados-CVM)
- [Administradoras Reais](/FundosDeInvestimento/Administradoras-Reais)
- [Glossário](/FundosDeInvestimento/Glossario)

Os fundos, administradoras e gestoras usados nos exemplos técnicos (ver
[Banco de Dados — Fundos de Investimento](/Arquitetura/Banco-de-Dados-Fundos)) são **entidades
reais registradas na CVM**, com CNPJ verificado via BrasilAPI — não são mais dados fictícios.

## Demonstração técnica da POC (conteúdo de exemplo)

O restante da wiki — arquitetura de um sistema de tickets de suporte genérico, onboarding,
runbooks — é material de demonstração da POC do OttoWikiMcp (MCP server de documentação),
com dados sintéticos:

- [Arquitetura](/Arquitetura)
- [Onboarding](/Onboarding)
- [Runbooks](/Runbooks)
EOF

cat > "$ROOT/Arquitetura.md" <<'EOF'
---
tags: [arquitetura]
---
# Arquitetura

Visão geral dos principais sistemas da plataforma.

## Sistema de Tickets

API interna que gerencia tickets de suporte abertos por instituições clientes. Cada ticket
pertence a uma instituição e tem um status (`aberto`, `em_andamento`, `resolvido`, `fechado`).

## Sistema de Instituições

Cadastro central de instituições clientes (nome, plano contratado, data de onboarding).

## Integrações

- Wiki (Azure DevOps) — documentação viva, versionada em git.
- API de Tickets — `GET /api/tickets`, `GET /api/tickets/{id}`.
- API de Instituições — `GET /api/institutions`, `GET /api/institutions/{id}`.
EOF

cat > "$ROOT/Arquitetura/.order" <<'EOF'
Fluxo-de-Tickets
Banco-de-Dados
Banco-de-Dados-Fundos
EOF

cat > "$ROOT/Arquitetura/Fluxo-de-Tickets.md" <<'EOF'
---
tags: [arquitetura, tickets]
---
# Fluxo de Tickets

1. Instituição abre um ticket via portal.
2. Ticket entra como `aberto`, atribuído automaticamente à fila do time responsável.
3. Analista assume o ticket → status vira `em_andamento`.
4. Ao resolver, status vira `resolvido`. Fecha automaticamente após 7 dias sem resposta do
   cliente (`fechado`).

## SLA por prioridade

| Prioridade | SLA de primeira resposta |
|---|---|
| Crítica | 1 hora |
| Alta | 4 horas |
| Normal | 1 dia útil |
| Baixa | 3 dias úteis |
EOF

cat > "$ROOT/Arquitetura/Banco-de-Dados.md" <<'EOF'
---
tags: [arquitetura, dados]
---
# Banco de Dados

PostgreSQL. Tabelas principais: `institutions`, `tickets`, `users`.

`tickets.institution_id` referencia `institutions.id`. Índice composto em
`(institution_id, status)` para consultas do painel de suporte.
EOF

cat > "$ROOT/Arquitetura/Banco-de-Dados-Fundos.md" <<'EOF'
---
tags: [arquitetura, fundos, dados]
---
# Banco de Dados — Fundos de Investimento

Modelo relacional do domínio de fundos de investimento, adicionado ao `OttoWikiMcp.WorkApiMock`
para dar mais contexto de teste ao MCP (perguntas mais complexas do que tickets/instituições de
suporte). **Os dados são reais**: os 26 fundos, as administradoras e gestoras vêm do registro
público da CVM (Dados Abertos, `registro_fundo_classe.zip`) e do histórico de cota/patrimônio
(`inf_diario_fi`), com CNPJ das administradoras verificado via BrasilAPI
(`brasilapi.com.br/api/cnpj`) — consultado em 2026-08-31, congelado em
`Data/fundos-cvm.json`/`Data/historico-cotas-cvm.json` (o serviço não chama essas APIs em runtime).
Limitação conhecida e deliberada: o conjunto só tem fundos **onshore** — a CVM não registra fundos
offshore, não existe fonte pública gratuita equivalente pra isso (offshore só existe no conteúdo
educacional da wiki, nunca neste dataset). Campos de benchmark e taxa de administração/performance
também não constam no dataset público usado e ficam `null`, nunca inventados.

Conceitos de negócio (o que é cada tipo de fundo, diferença onshore/offshore) estão documentados
separadamente em [Fundos de Investimento — Visão Geral](/FundosDeInvestimento/Visao-Geral); esta
página é sobre o **schema técnico**.

## Diagrama entidade-relacionamento

```mermaid
erDiagram
    TipoMercado ||--o{ Fundo : classifica
    TipoDeFundo ||--o{ Fundo : classifica
    Gestora ||--o{ Fundo : gerencia
    Administradora ||--o{ Fundo : administra
    Fundo ||--o{ HistoricoCota : possui

    TipoMercado {
        int Id PK
        string Nome
        string Descricao
    }
    TipoDeFundo {
        int Id PK
        string Nome
        string Descricao
    }
    Gestora {
        int Id PK
        string Nome
        string Cnpj
        date DataFundacao
        decimal PatrimonioTotalSobGestaoBi
    }
    Administradora {
        int Id PK
        string Nome
    }
    Fundo {
        int Id PK
        string Nome
        string Cnpj
        int TipoDeFundoId FK
        int TipoMercadoId FK
        int GestoraId FK
        int AdministradoraId FK
        decimal PatrimonioLiquido
        date DataInicio
        string Benchmark
        decimal TaxaAdministracaoPercentual
        decimal TaxaPerformancePercentual
        string Moeda
    }
    HistoricoCota {
        int FundoId FK
        date Data
        decimal ValorCota
        decimal PatrimonioLiquido
    }
```

## Tabelas

| Tabela | Papel | Campos-chave |
|---|---|---|
| `TipoMercado` | Lookup — Onshore ou Offshore | `Id`, `Nome`, `Descricao` |
| `TipoDeFundo` | Lookup — classificação ANBIMA/CVM (Renda Fixa, Ações, Multimercado, Cambial, FIDC, FII) | `Id`, `Nome`, `Descricao` |
| `Gestora` | Empresa que gere a estratégia do fundo (asset manager) | `Id`, `Nome`, `Cnpj`, `DataFundacao`, `PatrimonioTotalSobGestaoBi` |
| `Administradora` | Instituição responsável pela administração fiduciária do fundo (controladoria, custódia, compliance) — papel distinto do gestor por exigência regulatória | `Id`, `Nome` |
| `Fundo` | O fundo em si | `Id`, `Nome`, `Cnpj`, `TipoDeFundoId`, `TipoMercadoId`, `GestoraId`, `AdministradoraId`, `PatrimonioLiquido`, `DataInicio`, `Benchmark`, `TaxaAdministracaoPercentual`, `TaxaPerformancePercentual`, `Moeda` |
| `HistoricoCota` | Série temporal de valor de cota e PL — permite perguntas de rentabilidade num período | `FundoId`, `Data`, `ValorCota`, `PatrimonioLiquido` |

**Por que Gestora e Administradora são entidades separadas**: na regulação brasileira de fundos, o
**administrador** é o responsável legal perante a CVM (controladoria, custódia, compliance,
cálculo de cota), enquanto o **gestor** é quem toma as decisões de investimento — podem ser (e
frequentemente são) empresas diferentes, mesmo quando uma gestora grande também administra alguns
de seus próprios fundos. Modelar como duas tabelas evita forçar um fundo a ter "uma organização só"
quando o mundo real tem dois papéis.

## Onshore vs. Offshore no schema

`TipoMercado` é só uma tabela de duas linhas (`Onshore`/`Offshore`), mas a diferença de negócio por
trás dela é grande — moeda (`Fundo.Moeda`), regulador aplicável e regras de tributação/acesso do
investidor mudam conforme esse campo. Ver
[Onshore vs. Offshore](/FundosDeInvestimento/Onshore-vs-Offshore) para a explicação completa.
Como só existem fundos onshore no dataset real usado aqui (ver limitação acima), todos os 26
fundos desta amostra são `Onshore` — distribuídos entre Multimercado (10), Renda Fixa (5), FIDC
(4), FII (4) e Cambial (3). `FIDC` e `FII`, apesar de serem veículos legais exclusivamente
brasileiros (não existe "FII americano"), aparecem normalmente no cadastro unificado da CVM
(`registro_fundo_classe.zip`) e por isso têm instâncias reais neste conjunto — não precisou de uma
fonte separada.

## Tools MCP que expõem este schema

| Tool | Equivalente REST |
|---|---|
| `list_funds(tipoDeFundo?, tipoMercado?)` | `GET /api/fundos?tipoDeFundoId=&tipoMercadoId=` |
| `get_fund(id)` | `GET /api/fundos/{id}` |
| `get_fund_performance(id)` | `GET /api/fundos/{id}/historico` |
| `list_fund_types()` | `GET /api/fundos/tipos` |
| `list_market_types()` | `GET /api/fundos/mercados` |
| `list_fund_managers()` | `GET /api/fundos/gestoras` |

Exemplos de pergunta que este schema permite responder via `ask_wiki`/tools combinadas: "quais
fundos a BTG Pactual Serviços Financeiros administra?", "qual fundo de ações tem o maior
patrimônio líquido?", "qual fundo teve a cota mais volátil nos últimos 6 meses?" (via
`get_fund_performance` de cada fundo, com dados reais mês a mês).
EOF

cat > "$ROOT/Onboarding.md" <<'EOF'
---
tags: [onboarding]
---
# Onboarding

Guia de primeiros passos para novos membros do time.

1. Peça acesso ao Azure DevOps (org + projeto).
2. Clone os repositórios principais (ver Arquitetura).
3. Configure o ambiente local (`.env.example` em cada repo).
4. Leia os [Runbooks](/Runbooks) antes do seu primeiro plantão.
EOF

cat > "$ROOT/Runbooks.md" <<'EOF'
---
tags: [runbook]
---
# Runbooks

Runbooks operacionais do time. Ver também [Incidentes](/Runbooks/Incidentes) para casos mais
graves que exigem escalonamento formal.

## Ticket crítico sem resposta

1. Verifique o status na API de Tickets (`GET /api/tickets/{id}`).
2. Confirme a instituição afetada (`GET /api/institutions/{id}`).
3. Escale para o plantonista se o SLA estourou (ver [Fluxo de Tickets](/Arquitetura/Fluxo-de-Tickets)).

## Instituição não consegue logar

1. Confira o plano contratado da instituição (pode estar suspenso).
2. Verifique últimos tickets abertos por ela — pode já ter um ticket relacionado.
EOF

cat > "$ROOT/Runbooks/Incidentes.md" <<'EOF'
---
tags: [runbook, incidente]
---
# Incidentes

Passos para declarar e conduzir um incidente formal (subpágina de [Runbooks](/Runbooks), para
validar que a wiki suporta mais de um nível de subpasta).

1. Declare o incidente no canal `#incidentes` com severidade (SEV1–SEV3).
2. Nomeie um incident commander.
3. Registre a linha do tempo em tempo real.
4. Ao encerrar, escreva um post-mortem em até 48h.
EOF

cat > "$ROOT/FundosDeInvestimento/.order" <<'EOF'
Visao-Geral
Tipos-de-Fundos
Onshore-vs-Offshore
Regulacao-CVM-175
Tributacao
Distribuicao-e-Mercado
Fundos-Cadastrados-CVM
Administradoras-Reais
Glossario
EOF

cat > "$ROOT/FundosDeInvestimento/Visao-Geral.md" <<'EOF'
---
tags: [fundos, educacional, conceitos]
---
# Fundos de Investimento — Visão Geral

Um fundo de investimento é uma **comunhão de recursos** (juridicamente, um condomínio) formada
para aplicar dinheiro de vários investidores num conjunto de ativos, seguindo uma política de
investimento definida em regulamento. Em vez de cada investidor comprar ativos diretamente, todos
compram **cotas** do fundo, e um gestor profissional decide onde alocar o patrimônio comum.

## Estrutura básica

Um fundo de investimento envolve, no mínimo, estes participantes:

| Participante | Papel |
|---|---|
| **Gestora** | Decide onde investir o patrimônio do fundo (compra e venda de ativos), dentro da política definida no regulamento |
| **Administradora** | Responsável legal pelo funcionamento do fundo perante o regulador — controla cotistas, calcula a cota diária, publica informações obrigatórias |
| **Custodiante** | Guarda os ativos do fundo e liquida as operações (compra/venda de títulos) |
| **Distribuidor** | Vende cotas do fundo aos investidores (bancos, corretoras, plataformas de investimento) |
| **Cotista** | O investidor — dono de uma fração do patrimônio do fundo, proporcional às cotas que possui |

No Brasil, fundos de investimento são regulados pela **CVM** (Comissão de Valores Mobiliários) e
seguem regras consolidadas hoje principalmente na **Resolução CVM 175**, que substituiu a antiga
Instrução CVM 555 a partir de 2023.

## Cota e patrimônio líquido

- O **patrimônio líquido (PL)** de um fundo é o valor total de seus ativos menos suas obrigações
  (taxas a pagar, por exemplo).
- A **cota** é a menor fração desse patrimônio. O valor de uma cota é `PL ÷ número total de cotas`.
- Quando você aplica R$ 1.000 num fundo cuja cota vale R$ 10, você recebe 100 cotas. Se, um ano
  depois, a cota valer R$ 11, seu patrimônio nesse fundo passa a valer R$ 1.100 — o número de
  cotas não muda, o **valor de cada cota** é o que sobe ou desce com o desempenho do fundo.
- A maioria dos fundos abertos permite fracionar cotas (não é preciso comprar uma cota inteira).

## Taxas

- **Taxa de administração**: um percentual ao ano sobre o patrimônio do fundo, cobrado
  proporcionalmente todo dia útil (embutido no valor da cota, o investidor não vê uma cobrança
  separada). Remunera gestora, administradora e demais prestadores de serviço do fundo.
- **Taxa de performance**: cobrada só quando o fundo supera um **benchmark** (indicador de
  referência, como o CDI ou o Ibovespa) — tipicamente 20% sobre o que exceder o benchmark, e só
  depois de recuperar eventuais perdas anteriores (mecanismo de "linha d'água").
- **Taxa de saída/entrada**: menos comum hoje, cobrada em alguns fundos ao aplicar ou resgatar.

## Tributação: come-cotas

Fundos de renda fixa e multimercado (não fundos de ações) sofrem uma cobrança antecipada de
Imposto de Renda chamada **come-cotas**: duas vezes por ano (último dia útil de maio e de
novembro), o fundo "recolhe" IR sobre o rendimento do período, reduzindo a quantidade de cotas do
investidor pelo valor equivalente ao imposto devido — daí o nome. Fundos de ações não têm
come-cotas; o IR é cobrado só no resgate.

## Aplicação e resgate

- **Aplicação**: o investidor entrega dinheiro e recebe cotas, calculadas pelo valor da cota do
  dia (fundos costumam operar em **D+0** para conversão da cota, ou seja, no mesmo dia da
  aplicação, mas isso varia por fundo).
- **Resgate**: o investidor solicita a venda de cotas; o fundo então vende ativos (se necessário)
  para ter caixa disponível. O prazo entre o pedido e o dinheiro cair na conta é a **liquidação**
  (ex.: "D+2" significa dois dias úteis após a solicitação).
- **Carência**: alguns fundos (especialmente multimercados mais sofisticados) impõem um prazo
  mínimo antes de permitir resgate, ou uma janela de resgate específica (ex.: só é possível pedir
  resgate no início de cada mês, com liquidação 30 ou 60 dias depois) — isso dá ao gestor mais
  previsibilidade de caixa para investir em ativos menos líquidos.

## Público-alvo

Fundos podem ser destinados a diferentes perfis de investidor, com regras de acesso diferentes:

- **Varejo (investidor geral)**: qualquer pessoa pode investir, sem exigência de patrimônio mínimo
  — mas o fundo também tem mais restrições de risco/estratégia que pode adotar.
- **Investidor qualificado**: pessoa física ou jurídica com pelo menos R$ 1 milhão em investimentos
  financeiros, ou certificação profissional específica (conforme regulamentação da CVM) — acessa
  fundos com estratégias mais amplas e menos restrições.
- **Investidor profissional**: patamar acima do qualificado (a partir de R$ 10 milhões em
  investimentos financeiros, ou categorias específicas como instituições financeiras e fundos de
  investimento) — acessa o leque mais amplo de estratégias, incluindo os fundos mais alavancados
  ou concentrados.

## Tamanho da indústria no Brasil

A indústria brasileira de fundos é uma das maiores do mundo em relação ao PIB. Segundo dados da
ANBIMA, o patrimônio líquido total da indústria alcançou cerca de **R$ 11,1 trilhões** no primeiro
semestre de 2026, com captação líquida de aproximadamente R$ 184,7 bilhões no período — mais que o
dobro da captação do mesmo semestre do ano anterior. Esse volume torna os fundos de investimento
um dos principais instrumentos de poupança e alocação de capital do país, ao lado da caderneta de
poupança e da renda fixa direta (Tesouro Direto, CDBs).

## Quem investe em fundos: varejo vs. institucional

Além do investidor pessoa física, uma fatia relevante — historicamente **acima de 39%** do
patrimônio total da indústria — vem de **investidores institucionais**:

| Tipo de investidor institucional | O que é |
|---|---|
| **Fundos de pensão (EFPC)** | Entidades Fechadas de Previdência Complementar — administram a aposentadoria de funcionários de uma empresa ou grupo de empresas (ex.: fundos de pensão de estatais e grandes corporações) |
| **RPPS** | Regime Próprio de Previdência Social — fundo de previdência de servidores públicos municipais/estaduais, com regras próprias de credenciamento e limites de alocação definidos por resolução do Conselho Monetário Nacional |
| **Seguradoras** | Investem as reservas técnicas que garantem o pagamento futuro de sinistros e planos de previdência (PGBL/VGBL) |

Institucionais costumam ter times de investimento próprios, processos de due diligence mais
formais sobre a gestora, e às vezes acessam classes de cota exclusivas com taxas menores — volume
alto de capital compensa o menor custo por real investido.

## Como o investidor final chega até um fundo

Poucos investidores compram cotas diretamente da gestora. O caminho mais comum passa por
**distribuidores**:

- **Bancos**: distribuição via gerente de conta ou, cada vez mais, um modelo de consultor de
  investimentos dentro da própria rede — os grandes bancos têm balcão de distribuição que cobre
  mais de 90% dos municípios brasileiros.
- **Corretoras e plataformas de investimento**: distribuição via **assessores de investimento**
  (profissionais certificados, registrados na CVM através de uma **EAI** — Empresa de Assessoria
  de Investimento — vinculada a uma corretora).
- **Consultores especializados em institucional**: para fundos de pensão e RPPS, existe um
  ecossistema de assessoria dedicado (devido às regras específicas de credenciamento e
  enquadramento desses investidores).

Ver também [Tipos de Fundos](/FundosDeInvestimento/Tipos-de-Fundos),
[Onshore vs. Offshore](/FundosDeInvestimento/Onshore-vs-Offshore),
[Distribuição e Mercado](/FundosDeInvestimento/Distribuicao-e-Mercado),
[Tributação](/FundosDeInvestimento/Tributacao) e o
[Glossário](/FundosDeInvestimento/Glossario).

> Conteúdo educacional produzido com base em material público da ANBIMA e de fontes do mercado
> financeiro brasileiro, reescrito para fins de documentação interna — não é aconselhamento de
> investimento.
EOF

cat > "$ROOT/FundosDeInvestimento/Tipos-de-Fundos.md" <<'EOF'
---
tags: [fundos, educacional, classificacao]
---
# Tipos de Fundos — Classificação ANBIMA/CVM

A ANBIMA (Associação Brasileira das Entidades dos Mercados Financeiro e de Capitais) mantém, em
conjunto com a CVM, uma classificação padronizada de fundos usada pelo mercado inteiro para
comparar produtos parecidos entre si. A classificação tem **3 níveis**:

- **Nível 1** — classe de ativo predominante (Renda Fixa, Ações, Multimercado, Cambial).
- **Nível 2** — tipo de gestão e principais riscos (indexado, ativo, investimento no exterior).
- **Nível 3** — estratégia específica dentro do nível 2.

## Renda Fixa

Investem a maior parte do patrimônio em títulos públicos e/ou privados de renda fixa (prefixados,
pós-fixados atrelados ao CDI/Selic, ou indexados à inflação). É a classe mais numerosa do mercado
brasileiro, indo de fundos muito conservadores (títulos públicos de curtíssimo prazo) a fundos de
crédito privado com risco relevante.

| Subtipo comum | Característica |
|---|---|
| Simples | Só títulos públicos federais ou operações compromissadas neles lastreadas — risco mais baixo da categoria |
| Referenciado (DI) | Busca acompanhar de perto um indexador, tipicamente o CDI |
| Duração livre / crédito privado | Maior liberdade de prazo e mistura de emissores privados — mais risco e potencial de retorno |

## Ações

Investem no mínimo 67% do patrimônio em ações negociadas em bolsa (ou ativos equivalentes),
buscando acompanhar ou superar um índice de referência (o mais comum é o Ibovespa).

| Subtipo comum | Característica |
|---|---|
| Indexado | Réplica passiva de um índice (ex.: ETFs de Ibovespa) |
| Ativo | Gestor escolhe ações individualmente buscando superar o índice |
| Setorial | Concentrado num setor específico (bancos, small caps, dividendos, etc.) |

## Multimercado

A categoria mais heterogênea: pode combinar juros, câmbio, ações, crédito e ativos no exterior
numa única carteira, seguindo estratégias que variam bastante de gestora para gestora. É onde
ficam a maioria dos fundos "macro" e "long & short" do mercado brasileiro.

| Subtipo comum | Característica |
|---|---|
| Macro | Aposta em cenários macroeconômicos (juros, câmbio, inflação) |
| Long & Short | Compra uma ação e vende outra a descoberto, buscando lucrar com a diferença entre elas, independente da direção do mercado |
| Livre | Grande liberdade de alocação, poucas amarras de política de investimento |
| Investimento no exterior | Aloca parte relevante (ou a totalidade) do patrimônio em ativos fora do Brasil |

## Cambial

O principal fator de risco da carteira é a variação de moedas estrangeiras — normalmente o dólar
americano frente ao real. É uma categoria pequena em número de fundos, usada sobretudo como
proteção (hedge) cambial.

## FIDC — Fundo de Investimento em Direitos Creditórios

Compra **direitos creditórios** (recebíveis) de empresas — duplicatas, contratos de cartão de
crédito, financiamentos, aluguéis — antecipando caixa para quem originou o recebível e oferecendo
ao cotista uma rentabilidade atrelada ao risco de crédito dessa carteira. Estruturalmente é
diferente dos fundos "tradicionais": costuma ter **cotas seniores** (menor risco, prioridade de
pagamento) e **cotas subordinadas** (absorvem perdas primeiro, remuneração potencialmente maior).
É um veículo exclusivamente **onshore** — não existe equivalente direto fora do arcabouço
regulatório brasileiro.

## FII — Fundo de Investimento Imobiliário

Investe em ativos imobiliários — imóveis prontos para renda (galpões logísticos, lajes
corporativas, shoppings), recebíveis imobiliários (CRI) ou cotas de outros FIIs. Cotas de FII são
**negociadas em bolsa** (ticker terminado em "11", ex.: XPML11), com liquidez diária diferente de
um fundo aberto tradicional (que negocia direto com a administradora). Distribui rendimentos
periodicamente aos cotistas (geralmente mensal), historicamente um dos maiores atrativos da
categoria para pessoa física. Assim como o FIDC, é um veículo exclusivamente **onshore**.

## Fundos de Previdência (PGBL/VGBL)

PGBL e VGBL não são "tipos de fundo" na classificação ANBIMA — são **envelopes de previdência
privada** que, por trás, investem o dinheiro do participante em fundos de investimento
especialmente constituídos para esse fim (sufixo "Previdência" no nome do fundo). A diferença
entre eles é tributária/fiscal (dedução do IR na declaração completa para o PGBL, tributação só
sobre o rendimento no VGBL), não de carteira. Um fundo de previdência pode ter qualquer classe
ANBIMA por trás (Renda Fixa Previdência, Multimercado Previdência, etc.) — a única exigência
regulatória é reduzir a exposição a renda variável perto da aposentadoria em alguns produtos com
ciclo de vida (fundos "data-alvo").

## Comparativo rápido

| Classe | Risco típico | Uso comum |
|---|---|---|
| Renda Fixa | Baixo a moderado | Reserva de emergência, parte conservadora da carteira |
| Ações | Alto | Crescimento de patrimônio no longo prazo |
| Multimercado | Moderado a alto (muito variável) | Diversificação, estratégias táticas |
| Cambial | Moderado a alto | Proteção contra desvalorização do real |
| FIDC | Moderado a alto (risco de crédito) | Exposição a recebíveis, cotas seniores/subordinadas |
| FII | Moderado | Renda mensal (aluguel) + exposição imobiliária, liquidez via bolsa |

Ver também [Visão Geral](/FundosDeInvestimento/Visao-Geral) e
[Onshore vs. Offshore](/FundosDeInvestimento/Onshore-vs-Offshore) (fundos multimercado com
investimento no exterior são a ponte natural entre os dois mundos).

> Classificação baseada na estrutura de 3 níveis publicada pela ANBIMA, reescrita para fins
> didáticos — consulte sempre o regulamento oficial de um fundo específico antes de investir.
EOF

cat > "$ROOT/FundosDeInvestimento/Onshore-vs-Offshore.md" <<'EOF'
---
tags: [fundos, educacional, offshore, onshore]
---
# Fundos Onshore vs. Offshore

Uma das decisões mais relevantes ao estruturar ou escolher um fundo é onde ele é **constituído** e
**regulado**. Isso define moeda, jurisdição, quem pode investir e como a tributação funciona.

## Onshore

Um fundo **onshore** é constituído e registrado no Brasil, sob regras da **CVM** (hoje,
principalmente a Resolução CVM 175). Suas cotas são denominadas em **reais**, mesmo que o fundo
aloque parte do patrimônio em ativos no exterior.

- **Regulador**: CVM.
- **Moeda de denominação da cota**: Real (BRL) — ainda que o fundo compre ativos em dólar, a cota
  em si é sempre em reais.
- **Tributação**: segue as regras brasileiras (come-cotas semestral para renda fixa/multimercado,
  IR no resgate para fundos de ações).
- **Acesso**: pode ser aberto a investidores de varejo (dependendo do tipo/estratégia) ou
  restrito a qualificados/profissionais.
- **Exemplo prático**: um fundo multimercado brasileiro que aloca 30% do patrimônio em ações
  americanas — ele é onshore (constituído no Brasil, cota em reais), mesmo tendo exposição
  internacional.

## Offshore

Um fundo **offshore** é constituído **fora do Brasil** — tipicamente em jurisdições como Ilhas
Cayman, Luxemburgo, Irlanda ou Estados Unidos — sob a regulação local daquele país, não da CVM
brasileira.

- **Regulador**: o órgão regulador do país onde o fundo é constituído (ex.: SEC nos EUA, CSSF em
  Luxemburgo), não a CVM.
- **Moeda de denominação da cota**: normalmente dólar americano ou euro, não reais.
- **Tributação**: para o investidor brasileiro, o rendimento é tributado como ganho de capital no
  exterior via Imposto de Renda declarado. A partir de 2024, a Lei 14.754 mudou o tratamento de
  fundos exclusivos e offshore controlados por brasileiros: passou a valer **tributação automática
  periódica** ("come-cotas" anual, em 31/12) para certas estruturas antes usadas para diferir
  imposto indefinidamente, aproximando o tratamento desses veículos do de um fundo onshore comum.
  Regras específicas variam bastante por estrutura — sempre confirmar a versão vigente com um
  especialista tributário antes de decidir.
- **Acesso**: até recentemente, fundos offshore eram quase exclusivos de investidores qualificados
  ou profissionais. A regulamentação mais recente (CVM 175 e evoluções) passou a permitir que
  algumas estruturas offshore sejam distribuídas a investidores de varejo através de plataformas
  locais — o investidor não precisa abrir conta no exterior, aplica em reais através do
  distribuidor local, que converte para a moeda do fundo por trás dos panos.

## Por que investir em cada um

| Motivo | Onshore | Offshore |
|---|---|---|
| Simplicidade operacional/tributária | ✅ Regras conhecidas, tributação já embutida na cota | Mais complexo, exige atenção à declaração de IR |
| Diversificação geográfica/moeda | Limitada (fundo é sempre em reais) | ✅ Exposição direta a outra moeda e outro mercado |
| Proteção patrimonial (jurisdição diferente do Brasil) | ❌ | ✅ Um dos motivos mais citados por investidores de alto patrimônio |
| Acesso a gestores/estratégias globais | Indireto (via fundos que investem no exterior) | ✅ Acesso direto a gestoras internacionais |
| Barreira de entrada | Baixa (muitos fundos abertos a varejo) | Historicamente alta (qualificado/profissional), vem caindo |

## Requisitos de investidor (resumo)

| Categoria | Requisito aproximado |
|---|---|
| Investidor geral (varejo) | Nenhum patrimônio mínimo exigido |
| Investidor qualificado | A partir de R$ 1 milhão em investimentos financeiros, ou certificação profissional reconhecida |
| Investidor profissional | A partir de R$ 10 milhões em investimentos financeiros, ou categorias específicas (instituições financeiras, fundos de investimento, etc.) |

Fundos offshore com estratégias mais complexas ou alavancadas ainda tendem a exigir qualificado ou
profissional, mesmo com a flexibilização recente de acesso via plataformas locais.

## Resumo em uma frase

**Onshore** = constituído no Brasil, regulado pela CVM, cota em reais. **Offshore** = constituído
fora do Brasil, regulado pelo país de origem, cota geralmente em moeda estrangeira — cada vez mais
acessível ao investidor brasileiro comum através de plataformas locais, mas com tributação e
regras de acesso que merecem atenção redobrada.

Ver também [Visão Geral](/FundosDeInvestimento/Visao-Geral) e
[Tipos de Fundos](/FundosDeInvestimento/Tipos-de-Fundos).

> Conteúdo educacional baseado em regulação pública (CVM) e material de mercado, reescrito para
> fins didáticos — regras tributárias mudam com frequência, sempre confirme a versão vigente.
EOF

cat > "$ROOT/FundosDeInvestimento/Regulacao-CVM-175.md" <<'EOF'
---
tags: [fundos, educacional, regulacao]
---
# Resolução CVM 175 — o novo marco regulatório dos fundos

A **Resolução CVM 175** (2022, com entrada em vigor escalonada até 2024) revogou a antiga
**Instrução CVM 555** e outras cerca de 38 normas esparsas, consolidando num único normativo as
regras gerais de constituição, administração e funcionamento de fundos de investimento no Brasil.
O objetivo declarado do regulador foi reduzir divergência de interpretação entre gestoras/
administradoras e aumentar a segurança jurídica do setor.

## A mudança mais visível: classes e subclasses de cotas

Antes da 175, para oferecer condições diferentes a grupos de investidores diferentes (ex.: uma
taxa menor para investidores institucionais vs. varejo), o mercado recorria a uma estrutura de
**fundo master + vários FICs** (Fundos de Investimento em Cotas) satélites, cada um com sua própria
CNPJ, replicando a carteira do master.

A Resolução 175 permite estruturar isso dentro de um **único fundo**, com **classes** e
**subclasses de cotas** segregadas patrimonialmente entre si — cada classe pode ter sua própria
política de taxa, público-alvo e até estratégia, sem precisar de um CNPJ e um registro completo
separado para cada satélite. Essa estrutura de multiclasses entrou em vigor em **1º de abril de
2024**.

## Outras mudanças relevantes

- **Responsabilidade limitada do cotista**: o regulamento do fundo deve definir explicitamente que
  a responsabilidade do cotista está limitada ao valor de suas cotas — e fundos que adotam essa
  limitação devem incluir a expressão "Responsabilidade Limitada" no próprio nome do fundo.
- **Consolidação normativa**: em vez de precisar cruzar dezenas de instruções (555 para fundos
  "comuns", 356 para FIDC, 472 para FII, etc.), grande parte das regras gerais está agora num só
  normativo estruturado em anexos por tipo de veículo.
- **Atualização de sistemas**: a CVM atualizou o sistema Fundos.NET (onde administradoras enviam
  informações periódicas obrigatórias) para já receber dados no novo formato de classes/subclasses.

## Por que isso importa pra quem consome dados de fundos

Sistemas e integrações que leem o cadastro público de fundos da CVM (como o schema documentado em
[Banco de Dados — Fundos de Investimento](/Arquitetura/Banco-de-Dados-Fundos)) passaram a conviver
com fundos que têm múltiplas classes de cota sob o mesmo CNPJ-mãe — um detalhe que qualquer modelo
de dados construído "pré-175" (um fundo = uma cota = uma taxa) precisa saber que já não é
universalmente verdade no mercado real, mesmo que o modelo simplificado desta POC não implemente
classes/subclasses.

Ver também [Visão Geral](/FundosDeInvestimento/Visao-Geral) e
[Tributação](/FundosDeInvestimento/Tributacao).

> Resumo baseado em normativos públicos da CVM e análises de mercado sobre a Resolução 175,
> reescrito para fins didáticos — consulte o texto oficial da resolução para qualquer decisão de
> compliance ou estruturação de fundo.
EOF

cat > "$ROOT/FundosDeInvestimento/Tributacao.md" <<'EOF'
---
tags: [fundos, educacional, tributacao]
---
# Tributação de Fundos de Investimento

A tributação varia por **tipo de fundo** (ações vs. os demais) e por **prazo de permanência do
investidor**, com regras específicas para categorias como FII. Este resumo é sobre a lógica geral
— sempre confirme a alíquota vigente antes de qualquer decisão, porque a legislação muda.

## Tabela regressiva de Imposto de Renda

Para fundos que não são de ações (Renda Fixa, Multimercado, Cambial), o IR sobre o rendimento
segue uma tabela regressiva por prazo de aplicação — quanto mais tempo o dinheiro fica aplicado,
menor a alíquota:

| Prazo de aplicação | Alíquota de IR |
|---|---|
| Até 180 dias | 22,5% |
| De 181 a 360 dias | 20% |
| De 361 a 720 dias | 17,5% |
| Acima de 720 dias | 15% |

Fundos são classificados como **curto prazo** (carteira com prazo médio ≤ 365 dias) ou **longo
prazo** (> 365 dias) para fins de come-cotas — fundos de curto prazo recolhem come-cotas à
alíquota mínima de 20%, fundos de longo prazo à alíquota mínima de 15%, com ajuste final na hora
do resgate conforme o tempo real de permanência do investidor.

## Come-cotas: como funciona na prática

Duas vezes por ano — último dia útil de maio e de novembro — o fundo (não de ações) recolhe
antecipadamente parte do IR devido sobre o rendimento do período, na alíquota mínima da tabela
regressiva (15% ou 20%, dependendo da classificação de prazo do fundo). Isso acontece reduzindo a
quantidade de cotas do investidor pelo equivalente ao imposto — o valor da cota não muda, o número
de cotas do investidor é que diminui. No resgate, se a alíquota final devida for maior (por causa
do prazo real de permanência), a diferença é cobrada; a antecipação já paga é sempre abatida.

## Fundos de ações: sem come-cotas

Fundos de ações têm alíquota única de **15%** sobre o ganho, cobrada só no resgate — não têm
come-cotas. Essa é uma das razões pelas quais a distinção "fundo de ações vs. os demais" importa
tributariamente, além de importar para a classificação ANBIMA.

## FII (Fundos Imobiliários) — mudança relevante a partir de 2026

Historicamente, rendimentos de FII distribuídos a pessoas físicas eram isentos de IR (sob certas
condições: fundo com no mínimo 50 cotistas, cotas negociadas em bolsa, entre outras). A partir de
**1º de janeiro de 2026**, cotas **emitidas a partir dessa data** passam a sofrer alíquota de 5%
sobre os rendimentos distribuídos; cotas emitidas até 31/12 do ano anterior mantêm a isenção
mesmo se negociadas depois no mercado secundário. Ganho de capital na venda da cota (diferença
entre preço de compra e venda) segue como tributo unificado de 17,5%.

## FIDC — tributação por cota (sênior/subordinada)

Cotistas de FIDC seguem a tabela regressiva comum de fundos de renda fixa/multimercado (come-cotas
incluído), com a particularidade de que cotas subordinadas, ao absorverem mais risco de crédito,
tendem a ter rendimento (e portanto imposto devido) mais variável que cotas seniores.

## Offshore: tributação do investidor brasileiro

Ver detalhamento em [Onshore vs. Offshore](/FundosDeInvestimento/Onshore-vs-Offshore) — desde a
Lei 14.754/2023, fundos exclusivos e certas estruturas offshore controladas por pessoas físicas
brasileiras passaram a ter tributação automática periódica (came-cotas anual em 31/12), reduzindo
a vantagem de diferimento indefinido de imposto que essas estruturas ofereciam antes.

Ver também [Visão Geral](/FundosDeInvestimento/Visao-Geral),
[Tipos de Fundos](/FundosDeInvestimento/Tipos-de-Fundos) e o
[Glossário](/FundosDeInvestimento/Glossario).

> Conteúdo educacional baseado em regras públicas vigentes em 2026 — tributação de investimentos
> muda com frequência (inclusive por medida provisória); não é aconselhamento tributário, confirme
> sempre a regra em vigor antes de decidir.
EOF

cat > "$ROOT/FundosDeInvestimento/Distribuicao-e-Mercado.md" <<'EOF'
---
tags: [fundos, educacional, mercado, distribuicao]
---
# Distribuição de Fundos e Tamanho do Mercado

## Tamanho da indústria

A indústria brasileira de fundos de investimento fechou o primeiro semestre de 2026 com
patrimônio líquido total de aproximadamente **R$ 11,1 trilhões**, segundo a ANBIMA — alta de cerca
de 10% frente ao mesmo período do ano anterior — com captação líquida de **R$ 184,7 bilhões** no
semestre, mais que o dobro do mesmo período do ano anterior. É um dos maiores mercados de fundos
do mundo em proporção ao PIB, refletindo décadas de juros reais altos que tornaram fundos de renda
fixa/DI um destino natural de poupança no Brasil.

## Como o dinheiro chega da gestora até o investidor

Um fundo raramente vende cotas diretamente ao público. A cadeia típica de distribuição é:

```mermaid
flowchart LR
    G[Gestora] -->|estrutura o fundo| A[Administradora]
    A -->|disponibiliza cotas| D1[Banco / rede de agências]
    A -->|disponibiliza cotas| D2[Corretora / plataforma]
    D1 -->|gerente ou consultor| I[Investidor de varejo]
    D2 -->|assessor de investimento EAI| I
    A -->|distribuição direta/institucional| INST[Institucional: fundo de pensão, RPPS, seguradora]
```

- **Bancos**: distribuição tradicional via agência/gerente de conta, cada vez mais migrando para
  um modelo de consultor de investimentos dentro da própria rede. Grandes bancos cobrem balcão de
  distribuição em mais de 90% dos municípios brasileiros — o canal de maior alcance geográfico.
- **Corretoras e plataformas de investimento**: distribuição via **assessor de investimento**,
  profissional certificado vinculado a uma **EAI** (Empresa de Assessoria de Investimento)
  registrada na CVM. É o canal que mais cresceu na última década, ligado à popularização das
  plataformas de investimento independentes.
- **Distribuição institucional**: para fundos de pensão (EFPC) e RPPS, existe uma cadeia
  específica — consultorias especializadas em investimentos institucionais, processos de
  credenciamento formais e, no caso de RPPS, limites de alocação definidos por resolução do
  Conselho Monetário Nacional.

## Quem são os maiores investidores

Investidores institucionais (fundos de pensão, seguradoras, e os próprios fundos investindo em
outros fundos) respondem por uma fatia historicamente **acima de 39%** do patrimônio total da
indústria — tornando-os, em conjunto, o maior grupo de investidor em fundos brasileiros, à frente
até do varejo isoladamente.

## Por que essa cadeia importa para o schema de dados

O modelo relacional documentado em
[Banco de Dados — Fundos de Investimento](/Arquitetura/Banco-de-Dados-Fundos) modela `Gestora` e
`Administradora` como entidades separadas justamente porque a cadeia real de distribuição tem mais
elos do que só "quem decide onde investir" — a administradora, o distribuidor e o canal de acesso
(banco/corretora/institucional) são papéis distintos que, num sistema mais completo, mereceriam
suas próprias tabelas (`Distribuidor`, `CanalDeAcesso`) se o caso de uso precisasse rastrear por
onde uma cota específica foi vendida.

Ver também [Visão Geral](/FundosDeInvestimento/Visao-Geral) e
[Tipos de Fundos](/FundosDeInvestimento/Tipos-de-Fundos).

> Dados de mercado (patrimônio, captação) citados a partir de estatísticas públicas da ANBIMA;
> conteúdo sobre a cadeia de distribuição reescrito com base em fontes de mercado, para fins
> didáticos.
EOF

cat > "$ROOT/FundosDeInvestimento/Fundos-Cadastrados-CVM.md" <<'EOF'
---
tags: [arquitetura, fundos, dados, cvm]
---
# Fundos Cadastrados (dados reais da CVM)

Lista dos 26 fundos reais usados nesta POC, extraídos do registro público da CVM (`registro_fundo_classe.zip`, consultado em 2026-08-31). CNPJ, nome e classificação são exatamente os do cadastro oficial — nenhum dado inventado.

| Nome | CNPJ | Tipo | Administradora | PL (R$) |
|---|---|---|---|---|
| BRASILPREV TOP TPF FUNDO DE INVESTIMENTO FINANCEIRO RENDA FIXA RESPONS | 07.593.972/0001-86 | Renda Fixa | BB GESTAO DE RECURSOS - DISTRIBUIDORA DE | 293.917.237.064 |
| BB RENDA FIXA CURTO PRAZO AUTOMÁTICO FIC FIF RESPONSABILIDADE LIMITADA | 42.592.315/0001-15 | Renda Fixa | BB GESTAO DE RECURSOS - DISTRIBUIDORA DE | 204.315.846.851 |
| BB TOP RENDA FIXA CURTO PRAZO AUTOMÁTICO II FUNDO DE INVESTIMENTO FINA | 46.133.770/0001-03 | Renda Fixa | BB GESTAO DE RECURSOS - DISTRIBUIDORA DE | 204.300.804.220 |
| BB RF IV FUNDO DE INVESTIMENTO FINANCEIRO RENDA FIXA LONGO PRAZO RESP  | 00.822.055/0001-87 | Renda Fixa | BB GESTAO DE RECURSOS - DISTRIBUIDORA DE | 155.506.323.382 |
| SPECIAL RENDA FIXA REFERENCIADO DI FUNDO DE INVESTIMENTO FINANCEIRO RE | 01.597.187/0001-15 | Renda Fixa | ITAU UNIBANCO S.A. | 142.617.101.658 |
| FUNDO DE INVESTIMENTO EM DIREITOS CREDITÓRIOS DO SISTEMA PETROBRAS RES | 09.195.235/0001-50 | FIDC | BB GESTAO DE RECURSOS - DISTRIBUIDORA DE | 61.976.224.291 |
| ITAÚ FLEXPREV HIGH YIELD II FUNDO DE INVESTIMENTO FINANCEIRO MULT CRÉD | 42.814.944/0001-42 | Multimercado | ITAU UNIBANCO S.A. | 47.597.132.192 |
| ITAÚ FLEXPREV HIGH YIELD II FIF CIC MULT CRED PRIV - RESP LIMITADA | 42.860.483/0001-44 | Multimercado | ITAU UNIBANCO S.A. | 47.533.564.759 |
| TAPSO FUNDO DE INVESTIMENTO EM DIREITOS CREDITÓRIOS RESPONSABILIDADE L | 26.287.464/0001-14 | FIDC | OLIVEIRA TRUST DISTRIBUIDORA DE TITULOS  | 42.401.969.320 |
| MAIA 95 FUNDO DE INVESTIMENTO FINANCEIRO  MULTIMERCADO CRÉDITO PRIVADO | 43.810.237/0001-40 | Multimercado | CBSF DISTRIBUIDORA DE TITULOS E VALORES  | 36.430.646.447 |
| NIMROD FIF MULTIMERCADO CRÉDITO PRIVADO INVESTIMENTO NO EXTERIOR | 37.553.253/0001-00 | Multimercado | BNY MELLON SERVICOS FINANCEIROS DISTRIBU | 36.074.157.807 |
| DINÂMICA ENERGIA FUNDO DE INVESTIMENTO FINANCEIRO EM AÇÕES - RESPONSAB | 08.196.003/0001-54 | Multimercado | BANCO CLASSICO SA | 34.316.656.537 |
| OITI FIF MULTIMERCADO CRÉDITO PRIVADO INVESTIMENTO NO EXTERIOR RESPONS | 08.771.962/0001-56 | Multimercado | INTRAG DISTR DE TITULOS EVALORES MOBILIA | 34.037.567.377 |
| OPPORTUNITY SLQ FUNDO DE INVESTIMENTO FINANCEIRO EM AÇÕES - RESPONSABI | 52.298.374/0001-39 | Multimercado | BNY MELLON SERVICOS FINANCEIROS DISTRIBU | 33.745.943.746 |
| OPP I FUNDO DE INVESTIMENTO FINANCEIRO EM AÇÕES - RESPONSABILIDADE LIM | 00.083.181/0001-67 | Multimercado | BNY MELLON SERVICOS FINANCEIROS DISTRIBU | 28.857.475.511 |
| OPPORTUNITY AÇÕES FUNDO DE INVESTIMENTO FINANCEIRO EM AÇÕES - RESPONSA | 28.260.437/0001-83 | Multimercado | BNY MELLON SERVICOS FINANCEIROS DISTRIBU | 27.628.422.242 |
| PAN AUTO FUNDO DE INVESTIMENTO EM DIREITOS CREDITÓRIOS RESPONSABILIDAD | 65.473.848/0001-83 | FIDC | BTG PACTUAL SERVICOS FINANCEIROS S.A. DI | 19.002.095.254 |
| SANTOS FUNDO DE INVESTIMENTO FINANCEIRO | 15.831.754/0001-60 | Multimercado | OLIVEIRA TRUST DISTRIBUIDORA DE TITULOS  | 17.747.951.579 |
| ANNA FUNDO DE INVESTIMENTO EM COTAS DE FUNDO DE INVESTIMENTO EM DIREIT | 53.273.475/0001-18 | FIDC | CBSF DISTRIBUIDORA DE TITULOS E VALORES  | 15.688.258.736 |
| KINEA RENDIMENTOS IMOBILIÁRIOS FUNDO DE INVESTIMENTO IMOBILIÁRIO RESPO | 16.706.958/0001-32 | FII | INTRAG DISTR DE TITULOS EVALORES MOBILIA | 10.978.689.825 |
| PROLOGIS BRAZIL LOGISTICS VENTURE FUNDO DE INVESTIMENTO IMOBILIÁRIO DE | 31.962.875/0001-06 | FII | BRL TRUST DISTRIBUIDORA DE TITULOS E VAL | 7.920.679.193 |
| PÁTRIA LOG - FUNDO DE INVESTIMENTO IMOBILIÁRIO - RESPONSABILIDADE LIMI | 11.728.688/0001-47 | FII | BANCO GENIAL S.A. | 7.589.554.105 |
| BTG PACTUAL LOGÍSTICA FUNDO DE INVESTIMENTO IMOBILIÁRIO RESPONSABILIDA | 11.839.593/0001-09 | FII | BTG PACTUAL SERVICOS FINANCEIROS S.A. DI | 7.580.921.711 |
| ITAÚ EXCHANGE CAMBIAL FUNDO DE INVESTIMENTO FINANCEIRO RESPONSABILIDAD | 02.290.279/0001-10 | Cambial | ITAU UNIBANCO S.A. | 1.573.886.156 |
| ITAÚ CAMBIAL FUNDO DE INVESTIMENTO FINANCEIRO DA CLASSE DE INVESTIMENT | 01.623.535/0001-81 | Cambial | ITAU UNIBANCO S.A. | 1.250.910.705 |
| ITAÚ CAMBIAL MASTER FUNDO DE INVESTIMENTO FINANCEIRO RESPONSABILIDADE  | 28.046.800/0001-62 | Cambial | ITAU UNIBANCO S.A. | 944.898.029 |

Consulte via MCP: `list_funds`, `get_fund(id)`, `get_fund_performance(id)`. Ver também [Banco de Dados — Fundos de Investimento](/Arquitetura/Banco-de-Dados-Fundos) para o schema, e [Visão Geral](/FundosDeInvestimento/Visao-Geral) para os conceitos de negócio.

> Fonte: CVM Dados Abertos (dados.cvm.gov.br), arquivo `registro_fundo_classe.zip`. Amostra selecionada entre os fundos de maior patrimônio líquido por classe ANBIMA na data da consulta.
EOF

cat > "$ROOT/FundosDeInvestimento/Administradoras-Reais.md" <<'EOF'
---
tags: [arquitetura, fundos, dados, cvm]
---
# Administradoras Reais

Administradoras dos fundos listados em [Fundos Cadastrados (CVM)](/FundosDeInvestimento/Fundos-Cadastrados-CVM), com CNPJ e situação cadastral verificados via BrasilAPI (`brasilapi.com.br/api/cnpj`) em 2026-08-31.

| Nome | CNPJ | Situação Cadastral |
|---|---|---|
| BANCO CLASSICO SA | 31.597.552/0001-52 | ATIVA |
| BANCO GENIAL S.A. | 45.246.410/0001-55 | ATIVA |
| BB GESTAO DE RECURSOS - DISTRIBUIDORA DE TITULOS E VALORES M | 30.822.936/0001-69 | ATIVA |
| BNY MELLON SERVICOS FINANCEIROS DISTRIBUIDORA DE TITULOS E V | 02.201.501/0001-61 | ATIVA |
| BRL TRUST DISTRIBUIDORA DE TITULOS E VALORES MOBILIARIOS S.A | 13.486.793/0001-42 | ATIVA |
| BTG PACTUAL SERVICOS FINANCEIROS S.A. DISTRIBUIDORA DE TITUL | 59.281.253/0001-23 | ATIVA |
| CBSF DISTRIBUIDORA DE TITULOS E VALORES MOBILIARIOS SA | 34.829.992/0001-86 | ATIVA |
| INTRAG DISTR DE TITULOS EVALORES MOBILIARIOS LTDA | 62.418.140/0001-31 | ATIVA |
| ITAU UNIBANCO S.A. | 60.701.190/0001-04 | ATIVA |
| OLIVEIRA TRUST DISTRIBUIDORA DE TITULOS E VALORES MOBILIARIO | 36.113.876/0001-91 | ATIVA |

Gestoras (quem toma as decisões de investimento, papel distinto da administradora — ver [Banco de Dados — Fundos de Investimento](/Arquitetura/Banco-de-Dados-Fundos)):

| Nome | CNPJ |
|---|---|
| BANCO CLASSICO SA | 31.597.552/0001-52 |
| BB GESTAO DE RECURSOS - DISTRIBUIDORA DE TITULOS E VALORES M | 30.822.936/0001-69 |
| BRL TRUST DISTRIBUIDORA DE TITULOS E VALORES MOBILIARIOS S.A | 13.486.793/0001-42 |
| BTG PACTUAL GESTORA DE INVESTIMENTOS ALTERNATIVOS LTDA | 07.625.159/0001-40 |
| BTG PACTUAL GESTORA DE RECURSOS LTDA. | 09.631.542/0001-37 |
| CAIXA DE PREVIDENCIA DOS FUNCIONARIOS DO BANCO DO BRASIL - P | 33.754.482/0001-24 |
| CBSF TRUST ADMINISTRADORA DE RECURSOS LTDA | 23.863.529/0001-34 |
| ITAU UNIBANCO ASSET MANAGEMENT LTDA | 40.430.971/0001-96 |
| ITAU UNIBANCO S.A. | 60.701.190/0001-04 |
| KINEA INVESTIMENTOS LTDA. | 08.604.187/0001-44 |
| OLIVEIRA TRUST SERVICER S/A | 02.150.453/0001-20 |
| OPPORTUNITY GESTORA DE RECURSOS LTDA | 01.608.570/0001-21 |
| OPPORTUNITY HDF ADMINISTRADORA DE RECURSOS LTDA | 33.857.830/0001-99 |
| PATRIA INVESTIMENTOS LTDA | 12.461.756/0001-17 |
| REAG JUS GESTAO DE ATIVOS JUDICIAIS LTDA. | 46.356.742/0001-55 |
| SUESTE CAPITAL GESTAO DE RECURSOS LTDA. | 29.036.872/0001-91 |
| XP INVESTIMENTOS CORRETORA DE CAMBIO, TITULOS E VALORES MOBI | 02.332.886/0001-04 |

> Fonte: CVM Dados Abertos + BrasilAPI. Estas são instituições financeiras reais — os dados exibidos (nome, CNPJ, situação cadastral) são públicos e factuais; qualquer conteúdo de demonstração da POC (ex.: tickets de suporte) foi deliberadamente escrito para não atribuir nenhuma falha ou evento negativo a essas instituições reais.
EOF

cat > "$ROOT/FundosDeInvestimento/Glossario.md" <<'EOF'
---
tags: [fundos, educacional, glossario]
---
# Glossário de Fundos de Investimento

| Termo | Definição |
|---|---|
| **Cota** | Menor fração do patrimônio líquido de um fundo; o investidor compra cotas, não "pedaços" diretos dos ativos |
| **Patrimônio Líquido (PL)** | Valor total dos ativos do fundo menos suas obrigações; base de cálculo do valor da cota |
| **Gestora** | Empresa/profissional responsável pelas decisões de investimento do fundo |
| **Administradora** | Responsável legal pelo fundo perante o regulador; controla cotistas e calcula a cota diária |
| **Custodiante** | Instituição que guarda os ativos do fundo e liquida as operações de compra/venda |
| **Distribuidor** | Quem vende as cotas ao investidor final (banco, corretora, plataforma) |
| **Cotista** | O investidor — dono de cotas do fundo |
| **Benchmark** | Indicador de referência usado para avaliar o desempenho do fundo (ex.: CDI, Ibovespa, IPCA) |
| **Taxa de Administração** | Percentual anual sobre o PL, cobrado proporcionalmente todo dia útil, embutido no valor da cota |
| **Taxa de Performance** | Cobrança adicional só quando o fundo supera o benchmark, geralmente 20% do excedente |
| **Come-cotas** | Cobrança antecipada de IR em fundos de renda fixa/multimercado, em maio e novembro, que reduz a quantidade de cotas do investidor |
| **Resgate** | Solicitação de venda de cotas pelo investidor, convertendo-as de volta em dinheiro |
| **Liquidação (D+n)** | Prazo entre a solicitação de resgate e o dinheiro efetivamente cair na conta do investidor |
| **Carência** | Prazo mínimo de permanência ou janela específica exigida para solicitar resgate em certos fundos |
| **Fundo Aberto** | Permite aplicações e resgates a qualquer momento (dentro das regras de liquidação do fundo) |
| **Fundo Fechado** | Só permite resgate em datas específicas (ou no encerramento do fundo) — comum em fundos de ativos ilíquidos (imóveis, private equity) |
| **Investidor Qualificado** | Pessoa física/jurídica com pelo menos R$ 1 milhão em investimentos financeiros, ou certificação reconhecida pela CVM |
| **Investidor Profissional** | Patamar acima do qualificado — a partir de R$ 10 milhões em investimentos financeiros, ou categorias institucionais específicas |
| **Onshore** | Fundo constituído e regulado no Brasil (CVM), cota em reais |
| **Offshore** | Fundo constituído fora do Brasil, regulado pela jurisdição local, cota geralmente em moeda estrangeira |
| **CVM** | Comissão de Valores Mobiliários — regulador do mercado de capitais brasileiro, incluindo fundos de investimento |
| **ANBIMA** | Associação Brasileira das Entidades dos Mercados Financeiro e de Capitais — entidade autorreguladora que define, entre outras coisas, a classificação padrão de fundos usada pelo mercado |
| **Classificação ANBIMA** | Sistema de 3 níveis (classe de ativo, tipo de gestão/risco, estratégia) usado para categorizar fundos de forma comparável |
| **Renda Fixa** | Classe de fundos que investe majoritariamente em títulos de renda fixa (públicos e/ou privados) |
| **Multimercado** | Classe de fundos com liberdade para combinar juros, câmbio, ações e ativos no exterior numa única carteira |
| **Long & Short** | Estratégia que compra uma posição e vende outra a descoberto, buscando lucrar com a diferença entre elas |
| **Fundo Exclusivo** | Fundo com um único cotista (pessoa física ou jurídica), usado para planejamento patrimonial/sucessório |
| **Come-cotas** | Ver linha acima — termo repetido de propósito porque é frequentemente mal compreendido: não é um imposto extra, é antecipação do IR já devido |
| **EAI** | Empresa de Assessoria de Investimento — empresa de assessores de investimento registrados na CVM, vinculada a uma corretora, que distribui cotas de fundos ao investidor final |
| **RPPS** | Regime Próprio de Previdência Social — fundo de previdência de servidores públicos municipais/estaduais, um dos maiores tipos de investidor institucional em fundos |
| **EFPC** | Entidade Fechada de Previdência Complementar — fundo de pensão de empregados de uma empresa ou grupo |
| **Classes e Subclasses de Cota** | Estrutura trazida pela Resolução CVM 175: um único fundo pode segregar patrimonialmente diferentes classes de cotistas (cada classe com sua própria política/taxa), substituindo o antigo modelo de fundos master + FICs separados |
| **Fundo Master/Feeder** | Estrutura em que um "fundo feeder" capta recursos e investe quase tudo num único "fundo master", que executa a estratégia de fato — comum para replicar a mesma carteira entre vários fundos distribuidores |
| **FIDC** | Fundo de Investimento em Direitos Creditórios — investe em recebíveis (duplicatas, cartão de crédito, financiamentos); tem cotas seniores e subordinadas |
| **FII** | Fundo de Investimento Imobiliário — investe em imóveis/recebíveis imobiliários, cotas negociadas em bolsa, distribuição de renda periódica |
| **Cota Sênior / Subordinada** | Em fundos estruturados como FIDC: a cota sênior tem prioridade de pagamento (menor risco); a subordinada absorve perdas primeiro (maior risco/retorno potencial) |
| **PGBL / VGBL** | Envelopes de previdência privada que investem, por trás, em fundos de investimento com sufixo "Previdência" — diferem entre si na regra tributária, não na carteira |

Ver também [Visão Geral](/FundosDeInvestimento/Visao-Geral),
[Tipos de Fundos](/FundosDeInvestimento/Tipos-de-Fundos) e
[Onshore vs. Offshore](/FundosDeInvestimento/Onshore-vs-Offshore).
EOF

cd "$ROOT"
git init -q
git add -A
git -c user.name="Fake Wiki" -c user.email="fake@example.com" commit -q -m "Conteudo inicial da wiki (fundos: dados reais CVM/ANBIMA; demais secoes: demo da POC)"
echo "fake-azure-wiki/ recriada em $ROOT"
