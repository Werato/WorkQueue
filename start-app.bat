@echo off
chcp 65001 > nul
echo "Starting automated build and test pipeline..."

:: 1. Run xUnit tests
echo "Running unit tests..."
dotnet test ./WorkQueue.Tests

:: Check if tests failed (errorlevel check)
if %errorlevel% neq 0 (
    echo "Tests failed! Aborting startup to prevent running broken code."
    pause
    exit /b %errorlevel%
)

echo "All tests passed successfully!"

:: 2. Start Backend API in a new window
echo "Starting Backend API..."
cd WorkQueue
start "Backend API" cmd /k "dotnet run"
cd ..

:: Give the backend a few seconds to warm up
echo "Waiting for API to start..."
timeout /t 4 /nobreak > nul

:: 3. Open Swagger in the default browser
echo "Opening Swagger..."
start https://localhost:7122/swagger

:: 4. Start Frontend UI in a new window
echo "Starting Frontend application..."
cd workqueue-ui
start "Frontend UI" cmd /k "ng serve -o"
cd ..

echo "=================================================="
echo "Everything is up and running!"
echo "To stop the servers, just close their terminal windows."
echo "=================================================="
pause