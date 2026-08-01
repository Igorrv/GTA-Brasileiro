# Sobe o portal Docsify localmente (http://localhost:5500)
# Uso: pwsh scripts/serve-docs.ps1

$root = Split-Path -Parent $PSScriptRoot
$docs = Join-Path $root "docs"

if (-not (Test-Path (Join-Path $docs "index.html"))) {
    Write-Error "docs/index.html nao encontrado."
    exit 1
}

Write-Host "Portal: http://localhost:5500" -ForegroundColor Yellow
Write-Host "Ctrl+C para encerrar." -ForegroundColor DarkGray
Set-Location $docs

if (Get-Command npx -ErrorAction SilentlyContinue) {
    npx --yes serve -l 5500
} elseif (Get-Command python -ErrorAction SilentlyContinue) {
    python -m http.server 5500
} else {
    Write-Error "Instale Node (npx) ou Python para servir o portal."
    exit 1
}
