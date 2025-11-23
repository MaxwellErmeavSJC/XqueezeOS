@echo off
echo ============================================
echo XqueezeOS - Complete Clean Rebuild
echo ============================================
echo.

echo Step 1: Closing Visual Studio (if open)...
taskkill /F /IM devenv.exe 2>nul
if %ERRORLEVEL% EQU 0 (
    echo    Visual Studio closed
    timeout /t 3 /nobreak >nul
) else (
    echo    Visual Studio was not running
)
echo.

echo Step 2: Deleting build artifacts...
if exist bin (
    rmdir /s /q bin
    echo    - Deleted bin folder
)
if exist obj (
    rmdir /s /q obj
    echo    - Deleted obj folder
)
if exist .vs (
    rmdir /s /q .vs
    echo    - Deleted .vs folder
)
echo.

echo Step 3: Cleaning solution...
dotnet clean
echo.

echo Step 4: Restoring NuGet packages...
dotnet restore
echo.

echo Step 5: Building solution...
dotnet build
echo.

echo ============================================
if %ERRORLEVEL% EQU 0 (
    echo SUCCESS! Build completed with no errors
    echo.
    echo Next steps:
    echo   1. Open Visual Studio
    echo   2. Press F5 to run
    echo   3. Test File Manager, Camera, Gallery
) else (
    echo BUILD FAILED
    echo Please check the error messages above
)
echo ============================================
echo.

pause
