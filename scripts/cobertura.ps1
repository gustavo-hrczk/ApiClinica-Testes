# Roda os testes unitarios e gera o relatorio de cobertura em coverage-report/index.html
# Uso (na raiz do repositorio):  .\scripts\cobertura.ps1
$ErrorActionPreference = 'Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)

if (Test-Path TestResults) { Remove-Item TestResults -Recurse -Force }

dotnet tool restore
dotnet test --settings tests/ApiClinica.Tests/coverlet.runsettings --collect:"XPlat Code Coverage" --results-directory TestResults
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

dotnet reportgenerator "-reports:TestResults/*/coverage.cobertura.xml" "-targetdir:coverage-report" "-reporttypes:Html;TextSummary"
Get-Content coverage-report/Summary.txt
Write-Host "`nRelatorio completo: coverage-report\index.html"
