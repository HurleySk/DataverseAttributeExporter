@echo off
echo Building and running Dataverse Attribute Exporter...
dotnet build
if %ERRORLEVEL% NEQ 0 (
    echo Build failed!
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo Running the application...
dotnet run
pause
