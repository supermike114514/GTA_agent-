@echo off
rem 一键卸载：通过 UAC 以管理员身份重新启动 PowerShell 卸载程序
setlocal
set "PS1=%~dp0uninstall-buddy.ps1"

net session >nul 2>&1
if %errorlevel%==0 (
    powershell -NoProfile -ExecutionPolicy Bypass -File "%PS1%" -RemoveRuntime
) else (
    echo 正在请求管理员权限...
    powershell -NoProfile -ExecutionPolicy Bypass -Command "Start-Process -Verb RunAs -FilePath powershell -ArgumentList @('-NoProfile','-ExecutionPolicy','Bypass','-File','%PS1%','-RemoveRuntime')"
)
endlocal