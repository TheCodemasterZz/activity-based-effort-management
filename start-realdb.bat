@echo off
echo Starting Mesainame Application against real PostgreSQL (RealDb profile)...
echo.

REM Start backend in a new window, using the RealDb launch profile (real Postgres, not in-memory)
echo [1/2] Starting Backend (dotnet, RealDb profile)...
start "Backend - EforTakip.Api (RealDb)" cmd /k "cd backend && dotnet run --project src/EforTakip.Api/EforTakip.Api.csproj --launch-profile RealDb"

REM Wait a moment for backend to start
timeout /t 3 /nobreak

REM Start frontend in a new window
echo [2/2] Starting Frontend (React)...
start "Frontend - React" cmd /k "cd frontend && npm run dev"

echo.
echo Both services are starting:
echo   Frontend: http://localhost:5180
echo   Backend:  http://localhost:5298 (RealDb - PostgreSQL)
echo.
pause
