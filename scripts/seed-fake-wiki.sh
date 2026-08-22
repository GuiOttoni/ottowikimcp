#!/bin/bash
# Recria fake-azure-wiki/ do zero — conteúdo fictício simulando uma Wiki do Azure DevOps,
# usado pra validar o GitWikiSync (git clone/pull) sem precisar de um Azure DevOps real.
# Rodar a partir da raiz do repo OttoWikiMcp: bash scripts/seed-fake-wiki.sh
set -e

ROOT="$(dirname "$0")/../fake-azure-wiki"
rm -rf "$ROOT"
mkdir -p "$ROOT/Arquitetura"

cat > "$ROOT/.order" <<'EOF'
Home
Arquitetura
Onboarding
Runbooks
EOF

cat > "$ROOT/Home.md" <<'EOF'
# Wiki do Time — Plataforma

Bem-vindo à wiki interna. Aqui você encontra arquitetura dos sistemas, guias de onboarding
e runbooks operacionais.

## Seções

- [Arquitetura](/Arquitetura)
- [Onboarding](/Onboarding)
- [Runbooks](/Runbooks)

> Esta é uma wiki de exemplo (dados fictícios) usada para validar a POC do OttoWikiMcp antes
> de apontar para a wiki real do Azure DevOps da empresa.
EOF

cat > "$ROOT/Arquitetura.md" <<'EOF'
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
EOF

cat > "$ROOT/Arquitetura/Fluxo-de-Tickets.md" <<'EOF'
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
# Banco de Dados

PostgreSQL. Tabelas principais: `institutions`, `tickets`, `users`.

`tickets.institution_id` referencia `institutions.id`. Índice composto em
`(institution_id, status)` para consultas do painel de suporte.
EOF

cat > "$ROOT/Onboarding.md" <<'EOF'
# Onboarding

Guia de primeiros passos para novos membros do time.

1. Peça acesso ao Azure DevOps (org + projeto).
2. Clone os repositórios principais (ver Arquitetura).
3. Configure o ambiente local (`.env.example` em cada repo).
4. Leia os [Runbooks](/Runbooks) antes do seu primeiro plantão.
EOF

cat > "$ROOT/Runbooks.md" <<'EOF'
# Runbooks

## Ticket crítico sem resposta

1. Verifique o status na API de Tickets (`GET /api/tickets/{id}`).
2. Confirme a instituição afetada (`GET /api/institutions/{id}`).
3. Escale para o plantonista se o SLA estourou (ver [Fluxo de Tickets](/Arquitetura/Fluxo-de-Tickets)).

## Instituição não consegue logar

1. Confira o plano contratado da instituição (pode estar suspenso).
2. Verifique últimos tickets abertos por ela — pode já ter um ticket relacionado.
EOF

cd "$ROOT"
git init -q
git add -A
git -c user.name="Fake Wiki" -c user.email="fake@example.com" commit -q -m "Conteudo inicial da wiki (fake, para POC)"
echo "fake-azure-wiki/ recriada em $ROOT"
