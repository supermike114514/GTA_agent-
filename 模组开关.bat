@echo off
rem ===========================================================================
rem  GTA5Buddy 队友模组 - 禁用 / 启用模组（排查启动故障用）
rem
rem  用途：游戏启动卡住时，用它把 ASI 加载器关掉，从而判断
rem        "问题出在模组" 还是 "问题出在游戏本身"。
rem
rem  原理：ScriptHookV 是靠 dinput8.dll 这个 ASI 加载器注入的。
rem        把 dinput8.dll 改名 -> 加载器失效 -> SHV 和所有依赖它的模组都不会加载。
rem        这不是删除，改名即可，随时可以改回来。
rem
rem  支持参数：disable / enable / status
rem ===========================================================================
setlocal EnableDelayedExpansion
title GTA5Buddy 模组开关

set "GAMEDIR=E:\SteamLibrary\steamapps\common\Grand Theft Auto V Enhanced"
set "LOADER=%GAMEDIR%\dinput8.dll"
set "LOADER_OFF=%GAMEDIR%\dinput8.dll.off"
set "ARGS=%GAMEDIR%\args.txt"
set "ARGS_OFF=%GAMEDIR%\args.txt.off"
set "ACTION=%~1"

if not exist "%GAMEDIR%\GTA5_Enhanced.exe" (
    echo.
    echo   [错误] 在默认位置找不到 GTA5_Enhanced.exe：
    echo          %GAMEDIR%
    echo   如果你的游戏装在别处，请用记事本改本文件里的 GAMEDIR。
    echo.
    pause
    exit /b 1
)

if /i "%ACTION%"=="disable" goto do_disable
if /i "%ACTION%"=="enable"  goto do_enable
if /i "%ACTION%"=="status"  goto do_status

:menu
cls
echo.
echo   ============================================================
echo      GTA5Buddy 模组开关   ^|   启动故障排查
echo   ============================================================
echo.
echo       [1]  禁用全部模组    游戏启动卡住时先试这个
echo.
echo       [2]  启用全部模组    恢复正常游玩
echo.
echo       [3]  查看当前状态
echo.
echo       [0]  退出
echo.
echo   ------------------------------------------------------------
echo      禁用只是把 dinput8.dll 改名，不删除任何文件。
echo   ------------------------------------------------------------
echo.
set "C="
set /p "C=   请输入数字后回车: "
if "%C%"=="1" goto do_disable
if "%C%"=="2" goto do_enable
if "%C%"=="3" goto do_status
if "%C%"=="0" exit /b 0
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
echo   现在可以启动游戏试试。如果能进，问题在模组；如果还卡，问题在游戏本身。
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
echo   === 当前状态 ===
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
    echo     ^(如果启动卡住，请先在任务管理器结束它再重试^)
)
echo.
pause
goto menu
