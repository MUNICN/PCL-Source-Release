@echo off
chcp 65001 >nul
cd /d "C:\Users\Administrator\Desktop\Projects\PCL-build"

set "MSB=C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
if not exist "%MSB%" set "MSB=C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\amd64\MSBuild.exe"

echo.
echo 正在编译 PCL（Release），大概 1-2 分钟，请稍等 ...
echo.
"%MSB%" "Plain Craft Launcher 2.sln" /t:Rebuild /p:Configuration=Release /v:m /nologo > build.log 2>&1
if errorlevel 1 goto fail

set "OUT=C:\Users\Administrator\Desktop\PCL2发布"
if not exist "%OUT%" mkdir "%OUT%"
copy /y "Plain Craft Launcher 2\bin\Plain Craft Launcher 2.exe" "%OUT%\PCL开源发布版.exe" >nul
copy /y "Plain Craft Launcher 2\bin\Plain Craft Launcher 2.exe.config" "%OUT%\PCL开源发布版.exe.config" >nul

echo.
echo [OK] 编译成功，产物已放到 桌面\PCL NEW
echo.
echo 产物清单：
dir /b "%OUT%"
echo.
goto end

:fail
echo.
echo [FAILED] 编译失败，详细报错见 build.log
:end
echo.
pause
