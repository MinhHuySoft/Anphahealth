$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $PSScriptRoot
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project src/AnphaHealth.Web --no-launch-profile --urls http://127.0.0.1:5080
