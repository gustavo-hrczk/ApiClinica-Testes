# Executa um teste de carga do k6 e grava os resultados em k6/results/.
# A API precisa estar rodando (veja scripts\api-carga.ps1).
# Uso (na raiz do repositorio):  .\scripts\carga.ps1 smoke | average-load | stress
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('smoke', 'average-load', 'stress')]
    [string]$Teste
)

Set-Location (Split-Path $PSScriptRoot -Parent)
New-Item -ItemType Directory -Force k6/results | Out-Null

# Dashboard do k6: ao vivo em http://localhost:5665 e exportado em HTML no fim.
$env:K6_WEB_DASHBOARD = 'true'
$env:K6_WEB_DASHBOARD_EXPORT = "k6/results/$Teste-dashboard.html"

k6 run --summary-export="k6/results/$Teste-summary.json" "k6/$Teste.js"
exit $LASTEXITCODE
