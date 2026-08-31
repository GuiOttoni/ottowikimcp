#!/bin/bash
# Builda as duas imagens, publica no Docker Hub e reinicia os deployments no k3s da VPS.
# Substitui o fluxo manual antigo (docker save -> scp -> ssh -> k3s ctr images import):
# agora o k3s puxa a imagem direto do registry (ver k8s/*.yaml, imagePullPolicy: Always).
#
# Pré-requisitos (uma vez só): `docker login -u guiottoni` e contexto kubectl `vps70119-k3s`
# configurado e apontando pro cluster com o namespace `ottowikimcp` já criado
# (kubectl apply -f k8s/namespace.yaml, se ainda não existir).
#
# Rodar a partir da raiz do repo: bash scripts/deploy.sh
set -e

REGISTRY_USER="guiottoni"
KCONTEXT="vps70119-k3s"
NAMESPACE="ottowikimcp"

echo "==> Build das imagens"
docker build -f src/OttoWikiMcp.WorkApiMock/Dockerfile -t "$REGISTRY_USER/ottowikimcp-workapi:latest" src/OttoWikiMcp.WorkApiMock
docker build -f src/OttoWikiMcp.McpServer/Dockerfile -t "$REGISTRY_USER/ottowikimcp-server:latest" .

echo "==> Push pro Docker Hub"
docker push "$REGISTRY_USER/ottowikimcp-workapi:latest"
docker push "$REGISTRY_USER/ottowikimcp-server:latest"

echo "==> Reiniciando deployments no k3s ($KCONTEXT)"
kubectl --context "$KCONTEXT" -n "$NAMESPACE" rollout restart deployment/workapi deployment/mcpserver
kubectl --context "$KCONTEXT" -n "$NAMESPACE" rollout status deployment/workapi --timeout=120s
kubectl --context "$KCONTEXT" -n "$NAMESPACE" rollout status deployment/mcpserver --timeout=120s

echo "==> Deploy concluído: http://177.153.35.66:30880"
