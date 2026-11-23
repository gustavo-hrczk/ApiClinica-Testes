# Sobe a API para os testes de carga: build Release, porta 5052 e um banco
# descartavel (src/ApiClinica/carga.db), recriado do zero a cada execucao.
# Uso (na raiz do repositorio):  .\scripts\api-carga.ps1      (Ctrl+C para parar)
$ErrorActionPreference = 'Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)

Remove-Item src/ApiClinica/carga.db* -ErrorAction SilentlyContinue

$env:ASPNETCORE_URLS = 'http://localhost:5052'
$env:ConnectionStrings__Default = 'Data Source=carga.db'

dotnet run -c Release --no-launch-profile --project src/ApiClinica
