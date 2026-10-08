@echo off
rem ===========================================================================
rem  GTA5Buddy 队友模组 - 故障排查工具
rem
rem  三件事：
rem    1. 结束卡死的 GTA5_Enhanced.exe
rem       —— 游戏卡在启动阶段时会留下一个 0 CPU 的僵尸进程，
rem          Steam/启动器会认为"游戏已在运行"，于是再启动就没反应。
rem          这种进程通常以管理员权限运行，普通任务管理器看不到也结束不掉，
rem          所以本脚本会先自我提权。
rem
rem    2. 禁用 / 启用全部模组
rem       —— ScriptHookV 靠 dinput8.dll 注入。把它改名，SHV 和所有依赖它的
rem          模组都不会加载，用来判断"问题在模组"还是"问题在游戏本身"。
rem          只是改名，不删除任何文件。
rem
rem  支持参数：kill / disable / enable / status
rem ===========================================================================
setlocal EnableDelayedExpansion
title GTA5Buddy 故障排查

set "GAMEDIR=E:\SteamLibrary\steamapps\common\Grand Theft Auto V Enhanced"
set "LOADER=%GAMEDIR%\dinput8.dll"
set "LOADER_OFF=%GAMEDIR%\dinput8.dll.off"
set "ARGS=%GAMEDIR%\args.txt"
set "ARGS_OFF=%GAMEDIR%\args.txt.off"
set "ACTION=%~1"

rem --- 提权：fltmc 只有管理员能成功执行 --------------------------------------
rem 结束卡死进程需要管理员权限（那种进程本身就是以管理员启动的）。
fltmc >nul 2>&1
if errorlevel 1 (
    echo.
    echo   正在请求管理员权限...
    echo   ^(会弹出 UAC 窗口，请点"是"^)
    echo.
    powershell -NoProfile -Command "Start-Process -FilePath '%~f0' -ArgumentList '%ACTION%' -Verb RunAs"
    exit /b
)

if not exist "%GAMEDIR%\GTA5_Enhanced.exe" (
    echo.
    echo   [错误] 在默认位置找不到 GTA5_Enhanced.exe：
    echo          %GAMEDIR%
    echo   如果你的游戏装在别处，请用记事本改本文件里的 GAMEDIR。
    echo.
    pause
    exit /b 1
)

if /i "%ACTION%"=="kill"    goto do_kill
if /i "%ACTION%"=="disable" goto do_disable
if /i "%ACTION%"=="enable"  goto do_enable
if /i "%ACTION%"=="status"  goto do_status

:menu
cls
echo.
echo   ============================================================
echo      GTA5Buddy 故障排查
echo   ============================================================
echo.
echo       [1]  结束卡死的游戏进程     启动没反应时先试这个
echo.
echo       [2]  禁用全部模组           判断问题是否出在模组
echo.
echo       [3]  启用全部模组           恢复正常游玩
echo.
echo       [4]  查看当前状态
echo.
echo       [0]  退出
echo.
echo   ------------------------------------------------------------
echo      卡死进程的特征：任务管理器里内存和 CPU 几乎为 0，
echo      但它占着"游戏正在运行"的状态，导致再次启动毫无反应。
echo   ------------------------------------------------------------
echo.
set "C="
set /p "C=   请输入数字后回车: "
if "%C%"=="1" goto do_kill
if "%C%"=="2" goto do_disable
if "%C%"=="3" goto do_enable
if "%C%"=="4" goto do_status
if "%C%"=="0" exit /b 0
goto menu

rem ===========================================================================
:do_kill
echo.
echo   正在查找 GTA5_Enhanced.exe ...
tasklist /FI "IMAGENAME eq GTA5_Enhanced.exe" 2>nul | find /I "GTA5_Enhanced.exe" >nul
if errorlevel 1 (
    echo     [跳过] 没有正在运行的 GTA5_Enhanced.exe
    echo            游戏当前是干净状态，可以直接启动。
) else (
    echo     找到卡死的进程，正在强制结束...
    taskkill /F /IM GTA5_Enhanced.exe 2>&1 | findstr /I "SUCCESS 成功" >nul
    if errorlevel 1 (
        taskkill /F /IM GTA5_Enhanced.exe
    )
    echo.
    echo     再次确认：
    tasklist /FI "IMAGENAME eq GTA5_Enhanced.exe" 2>nul | find /I "GTA5_Enhanced.exe" >nul
    if errorlevel 1 (
        echo     [完成] 进程已结束。现在可以从 Rockstar 启动器启动游戏了。
    ) else (
        echo     [警告] 进程仍在。请重启电脑，或手动用管理员任务管理器结束它。
    )
)
echo.
pause
goto menu

rem ===========================================================================
:do_disable
echo.
echo   正在禁用全部模组...
if exist "%LOADER%" (
    if exist "%LOADER_OFF%" del "%LOADER_OFF%" >nul 2>&1
    ren "%LOADER%" "dinput8.dll.off"
    echo     [完成] dinput8.dll  -^>  dinput8.dll.off
    echo            ^(ASI 加载器已关闭，ScriptHookV 与所有模组都不会加载^)
) else (
    if exist "%LOADER_OFF%" (echo     [跳过] 已经是禁用状态) else (echo     [注意] 找不到 dinput8.dll)
)
if exist "%ARGS%" (
    ren "%ARGS%" "args.txt.off"
    echo     [完成] args.txt  -^>  args.txt.off
    echo            ^(不再向游戏传递 -nobattleye 等参数^)
)
echo.
echo   现在启动游戏试试：
echo     能进  -^> 问题在模组，回来点 [3] 恢复后再逐个排查
echo     还卡  -^> 问题在游戏本身，用 Steam 验证游戏文件完整性
echo.
pause
goto menu

rem ===========================================================================
:do_enable
echo.
echo   正在启用全部模组...
if exist "%LOADER_OFF%" (
    if exist "%LOADER%" del "%LOADER%" >nul 2>&1
    ren "%LOADER_OFF%" "dinput8.dll"
    echo     [完成] dinput8.dll.off  -^>  dinput8.dll
) else (
    if exist "%LOADER%" (echo     [跳过] 已经是启用状态) else (echo     [警告] 找不到 dinput8.dll，也没有 .off 备份)
)
if exist "%ARGS_OFF%" (
    ren "%ARGS_OFF%" "args.txt"
    echo     [完成] args.txt.off  -^>  args.txt
)
echo.
pause
goto menu

rem ===========================================================================
:do_status
echo.
echo   === 模组状态 ===
if exist "%LOADER%"     echo     dinput8.dll      存在    -^> 模组【已启用】
if exist "%LOADER_OFF%" echo     dinput8.dll.off  存在    -^> 模组【已禁用】
if exist "%ARGS%"       echo     args.txt         存在    -^> 参数【已启用】
if exist "%ARGS_OFF%"   echo     args.txt.off     存在    -^> 参数【已禁用】
echo.
echo   === 游戏进程 ===
tasklist /FI "IMAGENAME eq GTA5_Enhanced.exe" 2>nul | find /I "GTA5_Enhanced.exe" >nul
if errorlevel 1 (
    echo     游戏未运行
) else (
    echo     游戏正在运行
    echo     ^(如果启动卡住没反应，用 [1] 结束它再重试^)
)
echo.
echo   === 最新日志时间 ===
if exist "%GAMEDIR%\ScriptHookV.log" for %%F in ("%GAMEDIR%\ScriptHookV.log") do echo     ScriptHookV.log    %%~tF
if exist "%GAMEDIR%\asiloader.log"  for %%F in ("%GAMEDIR%\asiloader.log")  do echo     asiloader.log      %%~tF
echo.
echo     ^(如果这些时间停留在很早以前，说明游戏根本没走到模组加载那一步^)
echo.
pause
goto menu
