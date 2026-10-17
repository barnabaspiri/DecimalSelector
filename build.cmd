@echo off
rem Builds build\DecimalSelector.exe and, if Inno Setup 6 is installed, dist\DecimalSelector-Setup.exe.
setlocal
cd /d "%~dp0"

if not exist build mkdir build
"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe /optimize+ /win32icon:icon.ico ^
  /r:System.Windows.Forms.dll /r:System.Drawing.dll /out:build\DecimalSelector.exe DecimalSelector.cs
if errorlevel 1 exit /b 1
echo Built build\DecimalSelector.exe

set "ISCC=%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe"
if not exist "%ISCC%" set "ISCC=%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe"
if not exist "%ISCC%" set "ISCC=%ProgramFiles%\Inno Setup 6\ISCC.exe"
if not exist "%ISCC%" goto :no_inno
"%ISCC%" /Q installer.iss
if errorlevel 1 exit /b 1
echo Built dist\DecimalSelector-Setup.exe
exit /b 0

:no_inno
echo Inno Setup 6 not found, skipped the installer. Get it from https://jrsoftware.org/isdl.php
