Write-Host "Starting Mesainame Application against real PostgreSQL (RealDb profile)..." -ForegroundColor Cyan
Write-Host ""

# Start backend, using the RealDb launch profile (real Postgres, not in-memory)
Write-Host "[1/2] Starting Backend (dotnet, RealDb profile)..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd $PSScriptRoot\backend; dotnet run --project src/EforTakip.Api/EforTakip.Api.csproj --launch-profile RealDb"

# Wait for backend to start
Start-Sleep -Seconds 3

# Start frontend
Write-Host "[2/2] Starting Frontend (React)..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd $PSScriptRoot\frontend; npm run dev"

Write-Host ""
Write-Host "Both services are starting:" -ForegroundColor Green
Write-Host "  Frontend: http://localhost:5180" -ForegroundColor Cyan
Write-Host "  Backend:  http://localhost:5298 (RealDb - PostgreSQL)" -ForegroundColor Cyan
Write-Host ""
