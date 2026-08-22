# Build a partir da raiz do repo (contexto = OttoWikiMcp/), porque também precisamos copiar a
# wiki fake e o frontend: docker build -f src/OttoWikiMcp.McpServer/Dockerfile -t ottowikimcp-server .

# ── Frontend (Vite + React) — só usado aqui, no build. Nenhum Node na imagem final. ──────────
FROM node:22-slim AS frontend-build
WORKDIR /src
COPY src/OttoWikiMcp.Frontend/package*.json ./
RUN npm ci
COPY src/OttoWikiMcp.Frontend/ ./
RUN npm run build

# ── Backend (.NET) ────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/OttoWikiMcp.McpServer/OttoWikiMcp.McpServer.csproj OttoWikiMcp.McpServer/
RUN dotnet restore OttoWikiMcp.McpServer/OttoWikiMcp.McpServer.csproj
COPY src/OttoWikiMcp.McpServer/ OttoWikiMcp.McpServer/
RUN dotnet publish OttoWikiMcp.McpServer/OttoWikiMcp.McpServer.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
# git é necessário em runtime: GitWikiSync clona/atualiza a wiki chamando o `git` do sistema
# (ver Services/GitWikiSync.cs) — de propósito, para reaproveitar o Git Credential Manager
# contra uma wiki real do Azure DevOps sem precisar de PAT.
RUN apt-get update \
    && apt-get install -y --no-install-recommends git \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
# Frontend estático (build do Vite) — servido pelo próprio ASP.NET Core via UseStaticFiles.
COPY --from=frontend-build /src/dist/ /app/wwwroot/
# Wiki fake usada nesta POC (ver README) — troque Wiki__RepoUrl no Deployment para a URL real
# do Azure DevOps quando for usar de verdade; nada no código muda.
COPY fake-azure-wiki/ /app/fake-azure-wiki/
ENV ASPNETCORE_URLS=http://+:8080
ENV Wiki__RepoUrl=file:///app/fake-azure-wiki
EXPOSE 8080
ENTRYPOINT ["dotnet", "OttoWikiMcp.McpServer.dll"]
