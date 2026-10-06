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

**生成器 → 载具生成器** —— 第一项是 **`分类浏览 [全部]`**，进去后是按载具类别分组的列表
（全部 / 轿车 / 跑车 / 超级跑车 / 摩托车 / 越野车 / 直升机 / 军用车辆 / 紧急车辆 …），
每类后面标着该类实际有多少辆。选定分类会自动回到载具列表并过滤好。

> 分类与数量都来自游戏自己的 `VehicleClass` 枚举，**不是硬编码清单**，所以 DLC 载具和
> 线上专属载具也会自动落进对应分类。

选中任意载具回车 → **直接刷出来并让你坐进驾驶位**。

> 载具数据库是**分帧建立**的（几百个模型分摊到多帧，避免卡顿）。第一次打开生成器时
> 可能看到"载具数据库正在建立… 已索引 N 个模型"，**建完会自动刷新成分类列表**，不用重开面板。

**生成器 → 角色皮肤** —— 穷举本安装的全部人物模型，按分类（警察 / 军人 / 平民 / 动物…）分组。
回车在**你身旁生成一个 NPC**（用于查看模型长什么样）。

**人物与武器** —— 顶部有一个 **`目标：作用于队友`** 复选框。勾上之后，本页所有项目
（回血、无敌、枪法、发武器…）都作用于**队友**而不是你自己。共 35 项，分组如下：

| 分组 | 内容 |
|---|---|
| 血量与护甲 | 回满 / 上限 200 / 500 / 2000；护甲 50 / 100；清除血迹 |
| 无敌与生存 | 无敌开关；免疫要害伤害开关 |
| 战斗能力 | 枪法 100% / 50% / 0%；视野极远；听力极远；移速 1.0x / 1.5x / 2.5x；允许被击倒开关 |
| 武器与弹药 | 弹药补满；无限弹药开关；发放卡宾/突击/冲锋/霰弹/狙击/加特林/RPG/手雷；收缴全部武器 |
| 状态与通缉 | 消除通缉；五档通缉（测试用）；报告当前状态 |

**队友皮肤** —— 这一页和"生成器 → 角色皮肤"**语义不同**：它把**队友本人**换成选中的模型，
而不是生成一个 NPC。换完之后队友仍然跟随、战斗、开车，只是换了张脸。

> 实现上模型要先流式加载完才能换（否则会生成空壳），所以点选后约半秒生效。
> 换的时候会**记下队友当前位置和朝向**，让新模型直接出现在原地（不瞬移），
> 并**重新套用全部配置**（血量 / 护甲 / 关系组 / 战斗姿态 / 配枪）——不然换完会退化成普通路人。
> 第一项 `恢复默认外观` 可以换回初始模型 `s_m_m_security_01`。

**队友武器** —— 13 项，**直接作用于队友**，不走那个目标复选框（给队友配枪和改玩家属性是两回事）：
卡宾步枪 / 突击步枪 / 冲锋枪 / 霰弹枪 / 重型狙击枪 / 加特林 / RPG / 手雷 / 武士刀；
无限弹药开关；补满队友护甲与血量；收缴队友全部武器。

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
Vehicle.GetAllModels()          → 本安装里存在的每一个载具模型
Vehicle.GetModelDisplayName()   → 游戏本地化的载具名（本身就是中文）
Vehicle.GetModelMakeName()      → 厂商
Vehicle.GetModelClass()         → 载具分类（24 个英文枚举 → Loc 里翻成中文）
```

这样做的意义正是"线下和线上的所有载具"：Enhanced 版把**线上专属载具的模型也打进了游戏档案**，
所以运行时枚举能自然覆盖到它们，不需要维护一份会过期的名单，也不会漏掉 DLC 载具。
日志里会写明本次建库数量：

```
vehicle catalogue built: NNN models in M ms
vehicle categories: NN buckets (全部, SUV, 跑车, ...)
```

生成流程是**先请求模型再生成**，不会生成半加载的模型：

1. 检查 `Model.IsValid` / `IsInCdImage`，不合格明确报错
2. `Model.Request()` 流式加载，最多等 4 秒
3. 加载成功 → 在你前方地面生成，设为 `IsPersistent`（不会消失）、引擎启动
4. 直接把你送进驾驶位

### 人物模型数据库：分类是怎么来的

用 `Ped.GetAllModels()` 穷举本安装的**全部人物模型**（市民、警察、帮派、军警、服务行业、
动物、DLC 人物模型）。分类来自 `Loc.ClassifyPed()`：

- **可靠的部分**用游戏自己的标志位 —— `Model.IsAnimalPed` / `IsHumanPed` / `IsGangPed` / `IsFemalePed`
- **更细的角色**（警察 vs 店员）游戏没有暴露字段，所以按**模型名关键字**推断
- **认不出的模型落到"男性/女性市民"**这个保守兜底，不会被错误归类

中文分类：剧情角色 · 动物 · 警察/急救 · 帮派 · 军警/保安 · 服务行业 · 女性市民 · 男性市民 · 其他

> 说明：`GET_MODEL_NAME_FOR_DEBUG` 和 `GET_LABEL_TEXT` 这两个 native 在 SHVDN v3 里
> **都不存在**，所以拿不到游戏内置的人物模型名。皮肤条目显示为 `中文分类 0x模型哈希`——
> 稳定、可搜索，而不是编一个可能误导人的名字。

生成 NPC 时会**同时请求模型和碰撞**（`RequestCollision`），否则新人物会穿地掉下去；
生成后设为持久并加入玩家小队。

### 队友换皮肤：为什么要异步、为什么换完要重配

换模型不是 `set model` 一句话能完事的：

1. **模型必须先加载**——对一个还没流式加载完的模型调用 `CreatePed`，得到的是空壳或直接失败。
   所以点选后先 `Request()`，在 `Update()` 里等 `IsLoaded`，就绪才真正换（约半秒）。
2. **新身体要接管旧位置**——先记下旧 Ped 的坐标与朝向，新模型创建后立刻搬过去，否则会看到瞬移。
3. **换完必须重新配置**——`ConfigurePed()` 要重跑一遍：血量上限、护甲、关系组、
   `KeepRelationshipGroupAfterCleanUp` 等配置标志、以及配枪。不重配的话队友会退化成普通路人
   （不打敌人、不跟随、没有武器）。

### 人物与武器页：natives 缺失导致的取舍

| 项 | 说明 |
|---|---|
| 回满血量 / 护甲 | 同时抬高 `MaxHealth` 上限，否则会被原版上限压回去 |
| 无敌开关 | `Entity.IsInvincible`；对队友同样适用 |
| 免疫要害伤害 | `Ped.CanSufferCriticalHits` |
| 枪法 / 视野 / 听力 / 移速 | `Ped.Accuracy`、`SET_PED_SEEING_RANGE`、`SET_PED_HEARING_RANGE`、`SET_PED_MOVE_RATE_OVERRIDE` |
| 全部携带武器弹药补满 | 用 `GetAllWeaponHashes()` 遍历**实际携带**的武器，不是硬编码清单 |
| 无限弹药 开/关 | 逐把武器设 `InfiniteAmmo` + `InfiniteAmmoClip` |
| 发武器 / 收缴武器 | `Weapons.Give()` / `Weapons.RemoveAll()` |

> 说明：SHVDN v3 没有 `SET_PLAYER_INFINITE_AMMO` / `SET_VEHICLE_AS_PERSONAL_VEHICLE`
> 这两个 native（逐个核对过 6460 个 Hash 成员），所以无限弹药是逐武器设置的，
> 生成的车也没有"个人载具"标记。

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

### 每条能力对应哪段代码 / 哪个 native

| 能力 | 实现 | 关键点 |
|---|---|---|
| 跟随：你跑他跑、你停他停 | `Companion.UpdateFollow` → `Task.FollowToOffsetFromEntity(offset(1.2,-1.1), speed=1.0, radius=2.0, persistFollowing=true)` | 引擎负则寻路、避障、**按玩家实际速度自动变速**。源码侧对应 `CommandTaskFollowToOffsetOfEntity`（`commands_task.cpp:714`），内部就是 `CTaskMoveFollowEntityOffset` |
| 不卡墙 / 不跟丢 | 同上 + `TeleportIfFartherThan = 45m` 归队 | 引擎导航是唯一可靠解；超远距离强制归位，避免永远卡在墙后 |
| 战斗：找掩体 | `UpdateCombat` → `Task.Combat(threat)` | `TASK_COMBAT_PED` 内部即 `CTaskCombatSeekCover`（`TaskCombat.h:128`），掩体是**评分选点**，不用自己算 |
| 补掩体压力 | `MaybeTakeCover` → `TASK_SEEK_COVER_FROM_PED` | 只在站着挨打、既没跑也没走时才推一次，避免打断引擎自己的掩体流程 |
| 换弹 | `MaybeReload` → `Task.ReloadWeapon()` | 每 900ms 检查，弹匣见底且不在掩体里才换（在掩体里换弹会送命） |
| **不误伤你** | `ConfigurePed` → `Entity.SetNotDamagedByRelGroup(playerGroup)` + `PedConfigFlagToggles.TreatAsFriendlyForTargetingAndDamage` | 伤害源过滤：玩家及其盟友关系组打不到他，敌人照常能打死他。另外 relationship group 设为 `RESPECT`，互不敌对 |
| **不自动消失** | `IsPersistent = true` + `SET_ENTITY_AS_MISSION_ENTITY` + `SET_PED_NEVER_LEAVES_GROUP` + `KeepTaskWhenMarkedAsNoLongerNeeded` + `KeepRelationshipGroupAfterCleanUp` | 光 `IsPersistent` 不够，还要拦住流式卸载和脚本清理 |
| 有名字 / HUD | `Blip`（`BlipSprite.Friend`，绿色）+ `BuddyHud` | HUD 显示 名字 / 当前状态机状态 / HP / 护甲 |
| 驾驶：自己上车 | `UpdateVehicle` → `Task.EnterVehicle(playerCar, VehicleSeat.Any, 12000, ...)` | 玩家上车后 4s 一次节流，不会反复打断上车动画 |
| 驾驶：开到你身边 | `Task.EnterVehicle(car, Driver, ..., JackAnyone)` → 坐进驾驶位后 `Task.DriveTo(...)` | 抢车到驾驶位，再交给车载任务导航；遵守交通灯、避让车辆 |
| 驾驶：下车 | `Task.LeaveVehicle` + 状态闸门 | **只在没在下车时才下命令**——每帧重下会让 ped 在车里反复抽搐 |
| 独立血量/护甲 | `MaxHealth=220 / Health / Armor`，护甲低于 20 自动补 | — |
| 会吃药 | `UseMedkit` → 回血 + `TASK_PLAY_ANIM("mp_suicide","pill", ...)` | 动画名已按源码核对：`MP_SUICIDE` 里确有 `PILL` 片段（`stripclub_drinking.sc:330` 在检测它）。战斗中血量 ≤25% 或非战斗 ≤45% 触发，12s 冷却 |
| 倒地可救 | `GuardSurvival` 拦截 + `EnterDowned` + `UpdateDowned` + `Revive` | 见下 |
| 反应：你开枪他警戒 | `FindThreat`：优先打**正在和你交战的** ped（距离加权 -25），其次打正在打他的，最后只在你 `IsInCombat` 时才扩大到"附近任何在战斗的 ped" | 刻意不用 `IsAiming` 当触发器——对着空气瞄准不该让队友进入战斗 |
| 你上车他跟车 | L1 第 5 优先级 `ShouldUseVehicle` | 玩家上车 → 同乘；玩家在远处步行 → 抢车开过去接 |
| 过场自动隐藏 | L1 第 1 优先级：`Game.IsCutsceneActive \|\| !CanControlCharacter` → `Hidden` | 隐藏时同时关碰撞、冻结位置；出过场自动恢复 |

### 为什么"倒地"必须这么写

引擎在血量归零时**立刻**走死亡流程，你冻结不住。所以：

1. `GuardSurvival` 每 100ms 检查，血量 ≤1 时抢在死亡前把状态切成 `Downed`
2. `EnterDowned` 里把血量顶到 `DownedHealthFloor=5`（保持在死亡线之上）、`SET_PED_TO_RAGDOLL`
  让其瘫倒、`CanBeTargetted=false` 让敌人停止补枪
3. `UpdateDowned` 每帧把血量顶回 5 —— 不让引擎有机会跑到 0
4. 玩家在 3m 内按 E/H → `Revive`：回血 120、给 25 护甲、3 秒无敌、`CanBeTargetted` 恢复
5. 30 秒没人救 → `DownedTimeoutMs` 到期，血量归 0 真正阵亡，`BuddyMain` 自动补一个新的

---

## 4. 验收自测清单

我没法替你启动游戏，下面每条都要你进故事模式实测。建议顺序：

| # | 能力 | 操作 | 通过标准 |
|---|---|---|---|
| 1 | 加载 | 进故事模式 | 左下角出现"队友模组已加载"ticker，且截图里能看到 `队友` 昵称和血条 HUD |
| 2 | 生成 | 等 1~2 秒 | 绿色 `Friend` 图标 + 一个保镖模型出现在你身后，不会消失（走远再回来仍在） |
| 3 | 跟随 | 慢走 → 冲刺 → 急停 | 他同步加速/停下，停在你右后方约 2m，不穿墙 |
| 4 | 不卡墙 | 绕建筑物跑一圈、翻越栅栏、进胡同 | 不撞墙原地打转；真跟丢（>45m）会自动归队 |
| 5 | 战斗 | 找警察/帮派开战 | 他会找掩体、开枪、弹匣打空会换弹 |
| 6 | **不误伤** | 对着他连开数枪 | 他**不掉血**（这是 `SetNotDamagedByRelGroup` 的效果），也不还击你 |
| 7 | 反应 | 你开枪打敌人 | 他警戒并加入战斗，优先打你正在打的那个 |
| 8 | 驾驶：同乘 | 上任意一辆四门车 | 他跑到副驾上车 |
| 9 | 驾驶：接你 | 下车后走到 22m 外招手（或走远） | 他会抢一辆车开到你身边，接近后下车 |
| 10 | 生存：吃药 | 让他在战斗中掉到 25% 血以下 | 他会回血并播吃药动作（12s 冷却） |
| 11 | 生存：独立血量 | 观察 HUD | HP/护甲独立于你，护甲低于 20 自动补 |
| 12 | **倒地可救** | 把他打到血量归零 | 他瘫倒不再被敌人打；走近出现"按 E 扶起"提示；按 E 后 120 血 + 25 护甲起身 |
| 13 | 倒地超时 | 打倒地后等 30 秒 | 提示阵亡，随后自动补一个新的队友 |
| 14 | 待命 | 按 K | 原地不动；再按 K 恢复跟随；待命时被攻击会还击 |
| 15 | 过场 | 触发任意剧情过场 | 过场时他不可见、无碰撞；过场结束自动回来 |
| 16 | 收回 | 按 J | 队友和图标消失；再按 J 重新召唤 |
| 17 | **模式闸门** | 进故事模式后打开游戏根目录的 `GTA5Buddy.log` | 出现 `mode : STORY -> companion enabled`，`verdict=Story` |
| 18 | **拒绝线上** | 若你能安全复现：在线上入口处观察 | 左下角出现"队友模组未装载：仅限故事模式"，日志写 `NOT STORY`；且 ScriptHookV 会关闭游戏 |
| 19 | **图形面板** | 按 `F5` | 出现**图形化菜单**（标题栏 + 蓝色高亮条 + 子菜单箭头），不是叠加的文字；`Esc`/`F5` 关闭 |
| 20 | **队友指令** | `F5` → 队友指令 → 逐条试 | 每条都有可见反应；「前往路径点」在没设路径点时会明确报失败 |
| 21 | **载具数据库** | `F5` → 生成器 → 载具生成器 | 列表有内容；日志出现 `vehicle catalogue built: N models`；建库期间显示"正在建立…"，建完自动刷新 |
| 22 | **载具分类浏览** | 生成器 → 载具生成器 → `分类浏览` | 列出全部分类及**每类的实际辆数**；选定一个分类后**自动返回并过滤好列表** |
| 23 | **载具生成** | 在列表里选一项回车 | 你被送进驾驶位，车不会消失；缺失模型会明确报错而非静默 |
| 24 | **角色皮肤** | `F5` → 生成器 → 角色皮肤 | 穷举本安装全部人物模型，按分类分组；回车在你身旁生成一个 NPC |
| 25 | **队友皮肤：换模型** | `F5` → 队友皮肤 → 选一个模型 | 队友**原地**变成新模型（约半秒生效，不瞬移）；换完仍**跟随你、有武器、会打架** |
| 26 | **队友皮肤：分类浏览** | 队友皮肤 → `分类浏览` | 与载具分类相同的浏览体验；选定后返回模型列表 |
| 27 | **队友皮肤：恢复默认** | 队友皮肤 → `恢复默认外观` | 队友换回初始模型 `s_m_m_security_01` |
| 28 | **队友武器** | `F5` → 队友武器 → 逐个试 | 队友掏出对应武器；无限弹药开关生效；「补满队友」后其 HUD 血量护甲回满 |
| 29 | **人物与武器：作用于玩家** | 人物与武器 → 不勾选目标 → 回满血量 | **你自己的**血条回满 |
| 30 | **人物与武器：作用于队友** | 勾选`目标：作用于队友` → 血量上限 2000 | **队友**血量上限变成 2000 并回满 |
| 31 | **战斗能力** | 勾选作用于队友 → 枪法 100% / 视野极远 | 队友命中率明显提升、更早发现敌人 |
| 32 | **通缉** | 人物与武器 → 五档通缉 | 你立刻被 5 星通缉；「消除通缉」立刻清空 |

第 6 条和第 12 条是这套实现里唯一两处"必须靠关系组/兜底拦截才能成立"的地方；
第 17/18 条是模式边界的验证点；第 21/22 条是穷举数据库与分类浏览的关键验证点；
**第 25、30 条是新增的队友皮肤与"作用于队友"能力的关键验证点**。

---

## 5. 已知风险（读一下，能省你很多时间）

### 5.1 【高】ScriptHookV 比游戏版本旧 —— 已实测澄清

**这一条我原先判断错了，实测已澄清。**

| | 版本 |
|---|---|
| `GTA5_Enhanced.exe` | `1.0.1158.16` |
| 实际 ScriptHookV 日志 | `INIT: Success, game version is VER_EN_1_0_1158_13` |

**`1.0.1158.16` 是 exe/launcher 的版本号，ScriptHookV 读的是脚本 build `1158.13`——正好在支持列表内。**
实测 SHV 池扩展、asi 注册、SHVDN 三次 hook 全部成功，模组也被成功实例化：

```
[16:10:32] [DEBUG] Loading assembly GTA5Buddy.dll ...
[16:10:32] [INFO] Found 1 script(s) in GTA5Buddy.dll resolved to API version 3.9.0
[16:10:33] [INFO] Started script GTA5Buddy.BuddyMain.
```

**教训**：判断 SHV 兼容性只能看 `ScriptHookV.log` 里那行 `Success` / `Unknown game version`，
看 exe 版本号会得出错误结论。静态辅助手段是从 `ScriptHookV.dll` 里读出它内嵌的支持列表：

```
Supported enhanced versions:
1.0.811.8, ..., 1.0.1158.13
```

### 安装时提示文件被占用怎么办

安装程序会写入 `scripts\GTA5Buddy.dll`。**如果 GTA V 正在运行，这个文件被进程占用，
复制会失败并提示"正由另一进程使用"**（这是文件锁，不是权限问题——用管理员权限也一样）。

处置：**完全退出 GTA V**（有些情况下进程会卡住、日志停止更新，这时用任务管理器直接
结束 `GTA5_Enhanced.exe`），然后重新运行 `一键安装.bat`。

模组自己也会把 DLL 锁住，所以**每次更新模组前都必须先关游戏**。

### 5.2 【重要】Enhanced 版上 SHVDN 的图形绘制全部失效 —— 因此改用 LemonUI

**这一节是本项目踩过最深的坑，写下来省得后人重走。**

在 GTA V **Enhanced** 上，ScriptHookVDotNet 能拿到的**所有脚本级图形绘制都不渲染**——
只有文字能显示。实测逐项排除过：

| 绘制方式 | 结果 |
|---|---|
| `ContainerElement` | ❌ 不渲染 |
| `DRAW_RECT` 原生 | ❌ 不渲染（调用成功、返回正常，但画面无变化） |
| `GTA.UI.Sprite`（材质管线） | ❌ 不渲染 |
| `TextElement` 文字 | ✅ 正常 |

也试过用 **C++ ASI + ScriptHookV 的 `presentCallbackRegister`** 自绘，结果更明确：

```
ScriptMain: present 回调与键盘回调已注册      ← 注册成功
ScriptMain: createTexture 成功 id=0          ← 渲染设备就绪
F8: 打开面板                                 ← 键盘 hook 正常
（此后没有任何 on_present 记录）              ← 回调从不触发
```

即 **ScriptHookV 的键盘 hook 在本机正常，但它的 Present hook 挂不上**（脚本线程注册、
按 SDK 文档要求在 `DllMain` 注册，两种都试过）。这是 ScriptHookV 在该 Enhanced build 上的
限制，不是用法问题——SDK 的 `readme.txt` 只要求"在 DllMain 注册"，照做也无效。

**最终解法：LemonUI。** 它不画像素，而是**驱动游戏自己的 Scaleform UI 系统**
（`commonmenu` 那套），等于让游戏渲染它自己的菜单——所以外观是原生的，中文由游戏字体渲染，
而且这条路走通了：

```
scripts\
  GTA5Buddy.dll         ← 模组本体
  LemonUI.SHVDN3.dll    ← 必需依赖，缺了模组根本加载不起来
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
