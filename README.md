# GTA5Buddy — GTA V 故事模式 AI 队友

一个 ScriptHookVDotNet 脚本：把一具有独立状态机的 AI 队友塞进 GTA V 线下，全程陪伴，
不误伤你、会自己战斗、会开车接你、倒地能救。

- 目标版本：**GTA V Enhanced 1.0.1158.16**（`E:\SteamLibrary\steamapps\common\Grand Theft Auto V Enhanced`）
- 运行时：ScriptHookV **3889.0 / 1158.13** + ScriptHookVDotNet Enhanced **v1.1.0.6**
- 产物：`build\GTA5Buddy.dll`（net48 / x64，入口类 `GTA5Buddy.BuddyMain : GTA.Script`）
- **运行范围：仅故事模式**。模组自带模式闸门，判定到线上会话会拒绝装载（见 5.4）

---

## 1. 安装

双击 **`一键安装.bat`**（会弹 UAC，必须给管理员）。

> 为什么必须管理员：游戏装在 Steam 库 `E:\SteamLibrary\...` 下，普通用户令牌对该目录
> 没有写权限（本机实测 `dell` 是非管理员受限令牌，对 `E:\SteamLibrary` 的写入被系统拒绝）。
> 安装脚本已先备份所有会被覆盖的同名文件到
> `游戏目录\_mod_backup\<时间戳>\`。

安装脚本做三件事：

| 步骤 | 内容 |
|---|---|
| 1 | 复制 ScriptHookV：`ScriptHookV.dll`、`dinput8.dll`（ASI 加载器）、`xinput1_4.dll`、`args.txt`（自动关反作弊）、`NativeTrainer.asi` |
| 2 | 复制 SHVDNE：`ScriptHookVDotNet.asi`、`ScriptHookVDotNet3.dll`、`ScriptHookVDotNet.ini`、`MinHook.x64.dll` |
| 3 | 把 `GTA5Buddy.dll` **和 `LemonUI.SHVDN3.dll`** 一起放进 `游戏目录\scripts\` |

> **`LemonUI.SHVDN3.dll` 是必需的，不能只放 `GTA5Buddy.dll`。**
> 图形面板由 [LemonUI](https://github.com/LemonUI/LemonUI) 渲染（走游戏自己的 Scaleform UI 系统），
> 少了它脚本会因为找不到依赖而直接加载失败。
> 两个文件都在 `_runtime\` 下，安装程序会自动一起复制。

加 `-Junction` 参数（`powershell -File install-buddy.ps1 -Junction`）会把 `scripts` 做成指向
`build\` 的目录联接，之后重新编译即时生效、不用再次提权。

**安装后必须验证版本**（这是本项目最大的不确定点，见第 5 节）：

```powershell
powershell -ExecutionPolicy Bypass -File tools\check-logs.ps1
```

---

## 2. 按键

| 键 | 作用 |
|---|---|
| **`F5`** | **打开 / 关闭控制面板**（图形界面，见第 2.1 节） |
| **`F6`** | **导出诊断报告**到 `GTA5Buddy.log`（下面详述） |
| `J` | 召唤 / 收回队友 |
| `K` | 原地待命 ↔ 恢复跟随（待命时被攻击仍会还击） |
| `E` 或 `H` | 在倒地的队友旁施救（GTA 原生把 E 绑给了"上车"，所以额外给了 H） |
| `F4` | 打开 SHVDN 控制台 |

### F6：诊断报告（出问题时先按这个）

这是专门为"模组在游戏里跑，而你没法把画面贴给我"这个约束做的。按 `F6` 后，
`GTA5Buddy.log` 里会出现一整块机器可读的快照：

```
================ DIAGNOSTIC REPORT ================
time          : 2026-10-06 16:40:12
gameTime      : 123456 ms   frame=7890
fps           : 59.8
game version  : enhanced=True
mode gate     : STORY (companion allowed)
mode signals  : sessionStarted=False inSession=False ... verdict=Story
config file   : E:\...\GTA5Buddy.ini
config values : hud=True menu=True font=Default panel=0.03,0.06 scale=1
log file      : E:\...\GTA5Buddy.log
vehicle cat.  : built=True models=NNN categories=NN active=全部
skin cat.     : built=True models=NNN categories=NN active=全部
panel         : open=False page=Root
companion     : state=Follow downed=False
companion ped : health=220/220 armor=50 persistent=True dead=False invincible=False
companion pos : X:... Y:... Z:...
player        : health=200/200 armor=0 inVehicle=False wanted=0
player weapon : 卡宾步枪  clip=30/400
world         : peds=NN/NNN vehicles=NN/NNN weather=Clear
===================================================
```

`F6` **在面板打开时也能用**——恰恰是出问题的时候最需要它。

这块报告能一次性回答：模式闸门判成了什么、载具/皮肤目录到底枚举出多少个模型、分类有几个、
队友在哪个状态、有没有生成成功、配置读到没有、日志落在哪个文件。比截图和口述都准。

### 控制面板操作

面板是**图形界面**，由 [LemonUI](https://github.com/LemonUI/LemonUI) 渲染——它走的是游戏自己的
Scaleform UI 系统，也就是游戏内置菜单那一套，所以是原生外观（标题栏、蓝色高亮条、子菜单箭头），
而不是叠加的文字。

| 键 | 作用 |
|---|---|
| `↑` / `↓` | 上下移动选择 |
| `Enter` | 进入子菜单 / 执行当前项 |
| `Backspace` 或 `Esc` | 返回上一级（在根菜单则是关闭） |
| `F5` | 关闭面板 |
| 鼠标 | 可直接点击（LemonUI 自带光标支持） |

队友上车自动、进过场自动隐藏、被杀会自动补一个，都不需要你操作。

---

## 2.1 控制面板的五个功能页

```
GTA5Buddy 控制台
├── 队友指令          —— 跟随 / 待命 / 上车 / 攻击 / 紧急归位 等 10 条指令
├── 生成器
│   ├── 载具生成器    —— 分类浏览 + 穷举式载具数据库，回车直接刷出并坐进驾驶位
│   └── 角色皮肤      —— 穷举式人物模型，回车在身旁生成一个 NPC
├── 人物与武器        —— 血量 / 护甲 / 无敌 / 枪法 / 武器 / 通缉，共 35 项
├── 队友皮肤          —— 把**队友本人**换成任意人物模型
└── 队友武器          —— 为队友配枪、开无限弹药、补状态、收缴武器
```

### 页面详解

**队友指令** —— 10 条，作用于队友：跟随我 / 原地待命 / 前往地图路径点 / 上我的车 / 下车 /
攻击我瞄准的目标 / 停止战斗并归队 / 紧急归位 / 召唤队友 / 收回队友。

**生成器 → 载具生成器** —— 
>

选中任意载具回车

>
**生成器 → 角色皮肤** —

**人物与武器** —— 顶部有一个 **`目标：作用于队友`



---

## 2.2 配置文件 GTA5Buddy.ini（改完不用重编译）

安装程序会把模板放到**游戏根目录**（和 `ScriptHookV.log` 同级），脚本也会在 `scripts\`
里找。**如果你不改就不会被覆盖**——安装程序只在文件不存在时才放置模板。

| 键 | 默认 | 作用 |
|---|---|---|
| `hud` | `true` | 队友 HUD（名字/状态/血量/护甲）显示开关 |
| `menu` | `true` | F5 控制面板开关 |
| `font` | `default` | 面板字体：`default` / `labeled` / `monospace` / `leaderboard` |
| `panelx` `panely` | `0.03` `0.06` | 面板左上角位置（0–1 屏幕比例） |
| `panelscale` | `1.0` | 面板整体缩放 |

### ⚠ 中文显示成方块怎么办

面板文字由 **LemonUI 通过游戏的 Scaleform UI 系统**渲染，用的是游戏内置字体——也就是
游戏主菜单、暂停菜单同一套字体。**游戏装了中文语言包，所以中文能正常显示**（实测截图确认）。

如果个别字仍然显示成方块或空缺，那是**那个字在该字体里没有字形**（通常是极生僻字）。
把 `font=` 依次改成 `labeled` → `monospace` → `leaderboard` 可以换一套字体试试。

> 这一段在早期版本里风险高得多：当时面板是脚本自己叠加文字画的，用的十种装饰性字体
> （ChaletLondon / Pricedown / Taxi …）**是否带 CJK 字形无法验证**（字体打包在加密 RPF 里）。
> 换成 LemonUI 之后这个问题不复存在。

> 配置文件解析逻辑我用反射独立测过：`true/false/1/0/yes/no` 都能正确识别，非法值
> （如 `abc`、乱码）会**优雅回退到默认值**而不是崩溃。

---

## 2.3 各功能页的实现细节

> 各页的**入口与操作方式**见 2.1；这一节只写实现层面的东西——数据库怎么建、
> 生成流程为什么这么写、有哪些 native 缺失需要绕开。

### 队友指令：每条指令的精确行为

| 指令 | 行为 |
|---|---|
| 跟随我 | 恢复伴随，清掉手动待命标记 |
| 原地待命 | 停在当前位置不动（被攻击仍会还击） |
| 前往地图路径点 | 队友跑向你在**地图上设置的路径点**；没设路径点会明确提示失败，不会静默什么都不做 |
| 上我的车 / 下车 | 进出你当前的载具 |
| 攻击我瞄准的目标 | 打你正在瞄准/锁定的目标；取不到就退化为"打最近正在和你交战的敌人" |
| 停止战斗并归队 | 清掉交战目标，回到跟随 |
| 紧急归位 | 队友卡死/跟丢时的兜底：直接挪到你身后 |
| 召唤 / 收回队友 | 同 `J` |

### 载具数据库：运行时穷举，不是硬编码清单


```




---

## 3. 架构：三层状态机

你的能力表是对的，但直接写一个 `switch(state)` 会因为引擎抢任务而抖动。所以分三层，
**L3 全部交给游戏引擎自己的任务系统**——自己写寻路必然卡墙。

```
L1 战略层  Companion.DecideState()     每帧按优先级产出"意图"，不调用任何任务 native
   Cutscene(Hidden) > Dead(Hidden) > Downed > Standby > Combat > InVehicle > Follow > Idle

L2 行为层  Enter*/Update*              把意图翻译成引擎任务，只在任务被抢走时重下
   UpdateHidden / UpdateDowned / UpdateCombat / UpdateVehicle / UpdateFollow / UpdateIdle

L3 任务层  引擎任务，负责一切"怎么做"
   TASK_FOLLOW_TO_OFFSET_OF_ENTITY / TASK_COMBAT_PED / CTaskVehicleMissionBase(DriveTo)
```

```




```

> 版本注意：本仓库带的 `LemonUI.SHVDN3.dll` 是对 **SHVDN 3.6.0.0** 编译的，而本机跑
> **3.9.0.0**。.NET 的版本统一机制使其正常工作（实测已确认），但这是**未来最可能出问题的一环**。
> 如果某天面板打不开，先怀疑这里：换用与本机 SHVDN 版本匹配的 LemonUI 构建。

### 5.3 【中】Enhanced 版对部分 native 的兼容性

`ScriptHookVDotNetEnhanced` 能让 Legacy 脚本几乎原样跑在 Enhanced 上，但少数依赖
内存特征码的 API 仍可能失效。本项目只用了纯任务类 native（不碰内存扫描），风险相对低，
但下面几项如果行为异常，属于这一条：

- `SET_ENTITY_AS_MISSION_ENTITY` / `SET_PED_NEVER_LEAVES_GROUP`
- `TASK_SEEK_COVER_FROM_PED`
- `TASK_PLAY_ANIM` 的吃药动作

### 5.4 Legacy 备选路线

Steam 库里你还有 Legacy 版的入库记录（`H:\SteamLibrary\...\Grand Theft Auto V` 只剩
Steam 残留文件，主程序不在）。如果 Enhanced 这条路被 5.1 卡住，重新下载 Legacy 版是最稳的：
SHV 的 `3889.0` 与 Legacy build 精确匹配，所有 native 行为最可预测，本项目的 C# 代码
**一行都不用改**（SHVDNE 同一份 DLL 同时支持两个版本）。

### 5.5 【必读】只能线下，且要关反作弊

**这是硬性边界：本模组只允许在【故事模式】(Story Mode) 运行，绝不允许在 GTA Online 装载。**

一共三层保护，互相独立：

| 层 | 机制 | 行为 |
|---|---|---|
| 1 | **ScriptHookV（C++ 侧）** | 检测到你进入 GTA Online 时**直接关闭游戏**。这是最主要的一道，因为游戏在线上会把已装 mod 列表上报给 Rockstar |
| 2 | **模组自身模式闸门** `OnlineGuard.cs` | 装载前判定会话类型；**任一线上信号为真、或判定失败，就拒绝装载**。队友不生成、不下一发任务、不碰世界 |
| 3 | **安装脚本** | 安装完成时打印边界警告；提供 `一键卸载.bat` 全量移除 |

**第 2 层的判定信号**（全部为真才算故事模式，完整数值会写进 `scripts\GTA5Buddy.log`）：

```
sessionStarted / inSession / activitySession   NETWORK_IS_SESSION_STARTED 等，已建立的线上会话
connectedPlayers > 1                           NETWORK_GET_NUM_CONNECTED_PLAYERS
mpFreemode                                    本地 ped 是 mp_m_freemode_01 / mp_f_freemode_01
extraPlayerSlot                               存在第二个有效玩家槽位（线上才有）
mpRules / transition                          NETWORK_IS_GAME_IN_PROGRESS / NETWORK_IS_TRANSITION_STARTED
```

判定策略是**保守/失败即拒绝（fail-closed）**：只要判定不出"单一玩家槽位 + 非 freemode 模型"这个
故事模式特征，就不装载。宁可你去看一行日志，也不让队友在线上冒出来。

**运行期二次保护**：装载后每帧复查。如果会话从故事模式变成线上，模组会
① 收回队友 ② 停止一切世界操作 ③ 写日志说明原因，然后什么都不做。所以即使 SHV 还没反应过来，
队友也不会跟着你走进线上。

**怎么确认闸门生效**：进故事模式后看 `scripts\GTA5Buddy.log`：

```
=== GTA5Buddy: mode gate ===
mode    : STORY (companion enabled)
signals : sessionStarted=False inSession=False activity=False signedOnline=... players=... | ... | verdict=Story
```

如果这行是 `mode : NOT STORY (companion disabled)` 而你确实在故事模式，那就是误判——
日志里的 `signals` 会指明是哪个信号触发的。`src\BuddyConfig.cs` 里的
`ForceAllowAnyMode` 可以临时绕过（**仅供排查，不要长期开着**）。

**要在线上玩**：
- 临时停用 → 把游戏目录 `dinput8.dll` 改名为 `dinput8.dll.off`（ASI 加载器停用，SHV 和模组都不再加载）
- 彻底移除 → 双击 `一键卸载.bat`

> 注意：`args.txt` 的作用是关闭 BattlEye 反作弊。它是为了让 SHV 能在故事模式里工作，
> 不是"让你偷偷在线上用"。带上这套文件进线上只会被 SHV 关掉游戏。


### 5.6 任务模式（第二阶段，本版未包含）

你原始需求里的"接入故事模式任务、监听任务事件来支援"是**最脆的一块**：任务脚本会自己
给相关 ped 下发任务、锁玩家控制、做失败判定。硬插进任务中途，最可能的结果是任务卡住
或队友被任务脚本抢走控制。

本版策略是**自由模式全功能 + 过场自动隐藏**。要做"任务内队友"，正确做法是：
监听任务状态 → 任务期间让队友自动退场（或只做非侵入式支援）→ 任务结束后回归。
这需要逐个任务验证，属于第二阶段。

---

## 6. 目录结构

```
GTA5Buddy\
├─ 一键安装.bat                  双击即装（含 UAC 提权）
├─ 一键卸载.bat                  双击即卸（含 UAC 提权，-RemoveRuntime 全量移除）
├─ install-buddy.ps1             安装脚本本体（需管理员，-Junction 可做联接）
├─ uninstall-buddy.ps1           卸载脚本（需管理员，支持恢复备份）
├─ build\GTA5Buddy.dll           编译产物 ← 游戏实际加载的文件
├─ README.md                     本文件
├─ .gitignore                    排除临时文件；_runtime 与 build 的 DLL 刻意收录
├─ src\                          源码
│   ├─ BuddyMain.cs              Script 入口、Tick 驱动、按键、模式闸门接线、面板托管
│   ├─ OnlineGuard.cs            ★ 故事模式 / 线上 判定闸门（失败即拒绝装载）
│   ├─ Companion.cs              核心：三层状态机 + 全部 native 调用 + 面板指令方法
│   │                            ★ 队友换皮肤（异步加载 + 原地接管 + 重配置）
│   ├─ BuddyConfig.cs            所有可调参数集中在这里
│   ├─ BuddyHud.cs               HUD 绘制
│   ├─ BuddyLog.cs               文件日志（自动探查可写落点）
│   ├─ KeyHint.cs                按键绑定
│   ├─ AssemblyInfo.cs
│   ├─ ui\                       控制面板
│   │   ├─ LemonPanel.cs         ★ **实际生效的图形面板**（LemonUI 驱动）
│   │   │                        五个页面：队友指令 / 生成器 / 人物与武器 /
│   │   │                        队友皮肤 / 队友武器
│   │   ├─ MenuController.cs     旧面板的页面栈（现仍托管载具/皮肤数据库的建立）
│   │   ├─ MenuPanel.cs          旧的手搓文字面板（已被 LemonPanel 取代，保留作回退）
│   │   ├─ HudDraw.cs            旧的 Sprite 绘制尝试（同上，保留作技术记录）
│   │   ├─ Loc.cs                ★ 中文本地化：24 个载具分类 + 人物分类推断
│   │   ├─ CommandsPanel.cs      队友指令（旧面板）
│   │   ├─ VehiclePanel.cs       载具穷举数据库 + 中文分类 + 搜索 + 生成
│   │   ├─ SkinPanel.cs          角色皮肤穷举数据库 + 中文分类 + 搜索 + 生成
│   │   └─ StatsPanel.cs         血量 / 护甲 / 弹药 / 发枪（旧面板）
│   ├─ GTA5Buddy.csproj          net48 / x64，引用 SHVDNE 的 ScriptHookVDotNet3.dll
│   ├─ build.ps1                 编译（VS2022 MSBuild；本机沙箱下可能被拒）
│   ├─ build-roslyn.ps1          ★ 进程内 Roslyn 编译（本机可用的那条路）
│   ├─ build-lemontest.ps1       只编译 LemonUI 最小验证脚本（排障用）
│   └─ build-addtype.ps1         备用：Add-Type 进程内编译（仅 C# 5 语法）
├─ cpp\                          C++ ASI 面板的尝试（未成功，保留作技术记录）
│   └─ main.cpp                  presentCallbackRegister + GDI+ 的完整实现；
│                                结论见 5.2 —— ScriptHookV 的 Present 回调在本机不触发
├─ tools\
│   ├─ check-logs.ps1            ★ 装完先跑这个（版本对照 + 模式判定 + 全部日志）
│   ├─ install-runtime.ps1       早期版本（同样需要管理员）
│   ├─ reflect.ps1               反射列出 SHVDN 类型成员（开发用）
│   ├─ listtypes.ps1             校验产物入口类（开发用）
│   ├─ peinfo.mjs                读程序集 CLR 目标框架（开发用）
│   └─ fetch*.mjs                下载器（走本机代理）
├─ _runtime\                     运行时原件（勿删，安装脚本要读）
│   ├─ shv\bin\                  ScriptHookV 3889.0/1158.13
│   └─ shvdne\                   ScriptHookVDotNet Enhanced v1.1.0.6
└─ _aclreports\                  ACL 诊断报告（说明为何安装需要提权）
```

## 7. 改参数 / 重新编译

想调跟随距离、攻击性、吃药阈值等，只改 `src\BuddyConfig.cs`，然后：

```powershell
powershell -ExecutionPolicy Bypass -File src\build.ps1
```

装了 `-Junction` 的话，重编译后直接进游戏按 `Insert` 热重载脚本即可，无需再装。

## 8. 卸载

1. 删除 `游戏目录\scripts\GTA5Buddy.dll`（或 `scripts` 联接）
2. 把 `游戏目录\_mod_backup\<时间戳>\` 里的文件复制回游戏目录
3. 最省事的"停用"方式：把 `游戏目录\dinput8.dll` 改名为 `dinput8.dll.off`
