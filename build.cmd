@echo off
setlocal
for %%r in (recolor.exe) do (
	set recolorBuild=^| "%%~$PATH:r" darkgreen=.* green=Debug green=Release
	set recolorTest=^| "%%~$PATH:r" "magenta=.*(SUMMARY|xUnit.net Console Runner).*" "green=Total: [1-9]\d*" "red=Failed: [1-9]\d*" "red=Errors: [1-9]\d*" "yellow=Skipped: [1-9]\d*"
)
cd "%~dp0"
call :prestore         || exit /b 1
call :build Debug %*   || exit /b 1
call :build Release %* || exit /b 1
call :test  Debug %*   || exit /b 1
echo All builds successful %recolorBuild%
goto :EOF

:prestore
for /r %%s in (*.sln) do (nuget.exe restore "%%s" || exit /b 1)
goto :EOF

:build
for /r %%s in (*.sln) do (msbuild.exe "%%s" /p:Configuration=%1 /v:m %2 %3 %4 %5 %6 %7 %8 %9 || (echo error building %1 solution %%s & exit /b 1)) && echo %1 build of %%s is successful %recolorBuild%
echo %1 build is successful %recolorBuild%
goto :EOF

:test
set testrc=0
for /r %%x in (*xunit.console.exe) do set xunit.runner="%%~fx"
for /f %%t in ('dir *tests.dll /s /b ^| find /i "bin" ^| find /i "%1"') do %xunit.runner% "%%~ft" %recolorTest%
set testrc=%errorlevel%
exit /b %testrc%

