@echo off
setlocal
title Building DualVoice

echo ========================================================
echo   DualVoice - Building Standalone Windows Executable
echo ========================================================
echo.

cd /d "%~dp0"

set "CSC=%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
set "WPF_LIB=%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\WPF"

if exist "%CSC%" goto :CheckSource

set "CSC=%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
set "WPF_LIB=%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\WPF"

if exist "%CSC%" goto :CheckSource

echo [ERROR] .NET Framework 4.0/4.5+ C# compiler (csc.exe) not found!
echo Please make sure Microsoft .NET Framework is installed.
echo.
pause
exit /b 1

:CheckSource
if exist "DualVoice.cs" goto :CheckResource
echo [ERROR] DualVoice.cs not found in current directory!
echo.
pause
exit /b 1

:CheckResource
set "RES_PARAM="
if exist "logo.jpg" (
    set "RES_PARAM=/resource:logo.jpg,logo.jpg"
    echo [*] Found logo.jpg - embedding as internal resource.
) else (
    echo [!] logo.jpg not found - compiling without embedded logo.
)

echo [*] Compiling DualVoice.exe...
"%CSC%" /nologo /optimize+ /target:winexe /out:DualVoice.exe %RES_PARAM% /lib:"%WPF_LIB%" /r:PresentationFramework.dll,PresentationCore.dll,WindowsBase.dll,System.Xaml.dll DualVoice.cs

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ========================================================
    echo   [SUCCESS] DualVoice.exe compiled successfully!
    echo ========================================================
) else (
    echo.
    echo ========================================================
    echo   [FAILED] Compilation failed with error code %ERRORLEVEL%!
    echo ========================================================
)
echo.
pause
exit /b %ERRORLEVEL%
