@echo off
cd /d "%~dp0"
echo ========== dotnet build ========== > build_output.txt 2>&1
dotnet build >> build_output.txt 2>&1
echo. >> build_output.txt
echo ========== DONE ========== >> build_output.txt
echo.
echo ========== dotnet run ==========
dotnet run
