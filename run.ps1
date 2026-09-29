# Starts everything: infrastructure in Docker, the four .NET processes on the host (each in its own window).
#   ./run.ps1
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

Write-Host "1/3 Starting infrastructure (SQL Server, Seq, RabbitMQ, Azurite)..." -ForegroundColor Cyan
docker compose -f platform/docker-compose.yml up -d --wait

Write-Host "2/3 Building the solution..." -ForegroundColor Cyan
dotnet build Atlas.sln
if ($LASTEXITCODE -ne 0) { throw "Build failed." }

Write-Host "3/3 Starting services..." -ForegroundColor Cyan
$services = @(
    'src/Providers/Atlas.Providers.Mock',
    'src/Onboarding/Atlas.Onboarding.Api',
    'src/Verification/Atlas.Verification.Worker',
    'src/Backoffice/Atlas.Backoffice.Api'
)
foreach ($service in $services) {
    Start-Process dotnet -ArgumentList "run --no-build --project $service"
}

Write-Host ""
Write-Host "Onboarding API   http://localhost:5100/swagger" -ForegroundColor Green
Write-Host "Backoffice API   http://localhost:5200/swagger" -ForegroundColor Green
Write-Host "Providers mock   http://localhost:5300/swagger" -ForegroundColor Green
Write-Host "Seq (logs)       http://localhost:5341" -ForegroundColor Green
Write-Host "RabbitMQ UI      http://localhost:15672  (guest/guest)" -ForegroundColor Green
Write-Host "Then run the scenarios in requests/atlas.http"
