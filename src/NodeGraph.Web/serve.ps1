# NodeGraph.Web 起動スクリプト
# 使用方法: .\serve.ps1

$AppBundlePath = ".\bin\Debug\net9.0-browser\browser-wasm\AppBundle"

# ビルド
Write-Host "Building NodeGraph.Web..." -ForegroundColor Cyan
dotnet build

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

# wwwroot をコピー
Write-Host "Copying wwwroot files..." -ForegroundColor Cyan
Copy-Item -Path ".\wwwroot\*" -Destination $AppBundlePath -Force -Recurse

# AppBundle が存在するか確認
if (-not (Test-Path $AppBundlePath)) {
    Write-Host "AppBundle not found at $AppBundlePath" -ForegroundColor Red
    exit 1
}

Write-Host "Starting server at http://localhost:5000" -ForegroundColor Green
Write-Host "Press Ctrl+C to stop" -ForegroundColor Yellow

# dotnet-serve を使用
dotnet serve -d $AppBundlePath -p 5000 -o
