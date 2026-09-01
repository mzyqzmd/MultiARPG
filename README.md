# MultiARPG

> ⚠️ **项目状态：实验性 / 收尾中（很可能是倒数第二版）**。这是一个**用于技术展示的架构实践沙盒**，作者已明确兴趣下降、准备收尾，后续大概率不再扩展功能。当前版本（v23 架构快照，2026-09-01）即收尾前的真实状态；下一版（如有）将以「82→84 体面归档」为目标做最小清理后封板。

> A Unity multiplayer ARPG sandbox exploring data-driven, decoupled architecture — hierarchical FSM, ScriptableObject-driven configuration, and a single centralized network surface.

一个基于 Unity 的多人 ARPG 项目，重点**不在内容量、不在玩法完成度**，而在**架构实践验证**：数据驱动配置、分层状态机、服务容器组合根，以及**领域逻辑与网络层的彻底解耦**。

![Unity](https://img.shields.io/badge/Unity-2022.3.62f3-000000?logo=unity)
![Netcode](https://img.shields.io/badge/Netcode%20for%20GameObjects-1.12.2-2196F3)
![URP](https://img.shields.io/badge/URP-14.0.12-4CAF50)
![C#](https://img.shields.io/badge/C%23-165%20files%20%2F%20%7E10.3k%20lines-239120)
![架构评分](https://img.shields.io/badge/Architecture-82%2F100%20%28B%29-21a0c0)

---

## 架构亮点

| 特性 | 说明 |
|------|------|
| **集中式网络表面（以 Hub 为主，Boss 有 1 处破例）** | RPC 主要收口 `NetworkHub`（4 ServerRpc + 4 ClientRpc）；`BossZombie` 额外自带 1 个 `[ClientRpc]`，ClientRpc 总数 **5**。`NetworkVariable` 使用量 **2**（均属 `BossZombie`：武器显隐 `_weaponVisible` + 血量镜像 `_syncedHealth`，用于 Boss 跨端数值同步）。`using Unity.Netcode` 文件 **10 个**（其中 `FakeMan.cs` 为冗余 import 待清理；AI 域类直引 `Zombie` + `BossZombie` 2 个）。属收尾阶段「时间换简洁」取舍，记录为[已知偏差](#已知限制) |
| **表现层数据驱动（v19）** | ⭐ 新增 `SO_*Presentation` 表现配置四件套（`SO_CharacterPresentation` 抽象基类 + Player/Zombie/FakeMan 子类）：受击飙血 VFX、受伤/血溅/死亡/攻击/idle/翻滚/力竭等全部音频与 VFX key 收敛为 SO 资产 Inspector 配置，**`PlayAudio("...")`/`PlayVFX("...")` 字面量全仓归零**；`Character.Presentation` 单点属性；NetworkHub 经 `HitFeedbackKeys`（`INetworkSerializeByMemcpy`）跨端携带目标表现 key，旁观端不再硬编码默认 key |
| **装备拾取系统（v19）** | ⭐ `_defaultWeapon` 测试字段彻底移除，装备系统完全契约化：`IEquippable` 接口（含运行时状态 `IsEquipped`/`RegisterOwner`，正确下沉到 `BaseWeapon`/`BaseItem` 实例类，`SO_Weapon`/`SO_BaseItem` 为纯数据资产）+ 场景 `TryPickup` 拾取（拾取/伤害互斥）+ `EPendingEquipAction` 显式意图 + `OnEquipped`/`OnUnequipped` 事件解耦 UI 快捷面板 |
| **BossZombie 僵尸 Boss 框架（v20 起，v23 两阶段已落地）** | ⭐ 僵尸 Boss 模块（18 个 `.cs` / ~1035 行）：伤害经 `NetworkHub.RequestDamageServerRpc(effectId, multiplier, isStun)` → Host 定位 → owner 权威 `EffectControl` 施加（与玩家武器同源）；动画桥/碰撞体/11 个 BT 节点/两套 SO 数据/冲击波各司其职，复用 `AINPC` + `NetworkHub` 范式。**阶段一（P1 持武器）+ 阶段二（丢武器狂暴 + 拳脚 + Ground 冲击波击晕）均已实现并可跑**；血量经 `NetworkVariable<int> _syncedHealth` 跨端同步。**⚠️ 注意**：当前 `BossZombie` 域类已引 `using Unity.Netcode` + 2 NV + 1 裸 `ClientRpc`（破「域类 NGO-free」与「RPC 收口」两条约定），属收尾取舍、非推荐范式，详见[已知限制](#已知限制) |
| **领域 NGO-free（大体；BossZombie 除外）** | Stats / Effect / Item / Weapon / Equipment / Save 组装 / Input / Audio / UI / FSM / `ZombieAnimControl` / `ZombieHandHit` 全部不引用网络库；仅 `Zombie` 一个标准敌人域类破例直引 NGO（其余 AI 类经 `NetworkHub` 外观复用伤害链路）。**`BossZombie` 在二阶段引入了 `using Unity.Netcode`，是当前唯一的域类破例** |
| **组合根 + 服务容器** | `GameRoot` 按显式依赖顺序构建并注册 **11 个服务**，构造注入而非静态单例；场景卸载时逆序注销 |
| **分层状态机** | 父层管姿态（Free / Combat），子层管动作（Move / Jump / Fall / Dodge / Attack / Hit / Dead），带优先级抢断的 `AnyTransition` |
| **数据驱动** | 角色属性、效果、物品、武器、音频、UI、VFX、**表现配置（v19）**全部由 `SO_*` 数据库按 key 查表，配置与运行时状态分离；`SO_Weapon` 蓄力倍率、武器动画字典、**武器动画覆盖控制器（`OverrideController`）均已接通运行时**；`OnValidate()` 自动同步资产名作 ItemID |
| **零 GC 事件总线** | `EventBus` 仅承载 1 个游戏级信号（`ReturningToMainMenuEvent`），参数为只读 `struct` |
| **锁定目标系统** | `ILockableTarget` 接口 + `OverlapSphere` 检测 + `Linecast` 遮挡剔除，经 `Action<bool>` 事件桥接状态机，**零反向编译期依赖** |
| **首个自定义 asmdef（v20）** | ⭐ 原 `Core/` 基础设施（FSM / Services / EventBus / ObjectPool）迁入 `Utility/` 并新增 `_MultiARPG.Utility.asmdef`（项目首个自定义程序集），`Core/` 目录自此消失；可缩短 Unity 编译/热重载耗时（见[已知限制](#已知限制)） |
| **干净可编译（编辑器 Play）** | 全仓零 `using UnityEditor`、源码层无编译级错误、无悬空引用、过期注释归零——**Unity Play 模式无障碍，且 Standalone 打包已无障碍（v21 复核 `GizmoDrawer` 实际无 `#if UNITY_EDITOR` 包裹，P0 阻断已不存在）** |

## 技术栈

**引擎与官方包**

- Unity **2022.3.62f3** LTS · Universal Render Pipeline **14.0.12**
- Netcode for GameObjects **1.12.2** — 网络同步
- Input System **1.14.2** — 多设备输入（键盘数字量 / 手柄模拟量区分处理）
- Cinemachine **2.10.7** · TextMesh Pro **3.0.9** · Timeline **1.7.7**

**第三方资产**（详见 [许可与第三方资产](#许可与第三方资产)）

- DOTween（Demigiant）· Odin Inspector（Sirenix）· **Animancer（Kybernetix，动画状态机驱动）** · **NodeCanvas（ParadoxNotion，AI 行为树 / 可视化行为编排，新增 AI 子系统依赖）** · ParrelSync（本地双开联调）· Hot Reload（本地 UPM 包）

## 架构总览

依赖方向严格自上而下，**下层不得反向引用上层**：

```
┌──────────────────────────────────────────────────────────┐
│  表现层    UI（MVP）· 动画事件 · VFX · 音频发声器          │
│            ⭐ 表现 key 全 SO 驱动（SO_*Presentation）     │
├──────────────────────────────────────────────────────────┤
│  领域层    Character / Player 能力组件 · 分层 FSM ·        │
│            Effect · Item · Weapon · Equipment            │
│            ← 大体 NGO-free（BossZombie 域类二阶段引 NGO，破例）   │
├──────────────────────────────────────────────────────────┤
│  服务层    11 × IService（Input / UI / Audio / VFX /       │
│            Scene / Save / Settings / Effect /             │
│            Camera / Network / AINpc）                      │
├──────────────────────────────────────────────────────────┤
│  数据层    SO_* 配置资产（Assets/_Data，按 key 查表）      │
│            + ⭐ SO_*Presentation 表现配置                  │
├──────────────────────────────────────────────────────────┤
│  网络层    NetworkService（唯一 NetworkManager 封装）      │
│            NetworkHub（唯一 RPC 表面）                     │
│            ClientNetworkTransform / Animator               │
│            （伤害经 Hub + owner 权威 EffectControl 施加；NetworkVariable 仅 BossZombie 2 处）│
└──────────────────────────────────────────────────────────┘
                          ↑
                  GameRoot（组合根，装配全部服务，根目录）
```

### 核心设计约定

- **网络触点最小化**：领域类不继承 `NetworkBehaviour`、不直用 `IsServer/IsOwner/NetworkObject`。跨端通信一律经 `NetworkHub`，Hub 通过 `Func<PlayerSaveData>` Provider 回调与 `OnPlayerRestore` 事件与玩家侧交互，**Hub 零 Player 类型引用**。
- **无隐式查找**：不使用 `GameObject.Find` / `FindObjectOfType` 做跨系统通信；`Camera.main` 收敛为 `PlayerCameraService` 唯一缓存点。
- **单一职责**：能力（Capabilities）与组件（Components）分离——前者承载可被状态机驱动的玩家能力，后者是自足的挂载式组件。
- **状态机中立**：状态类为 plain C#，经构造注入获得依赖，不持有网络引用，可脱离 Unity 运行时单元测试。

## 项目结构

```
Assets/
├── _Scripts/                        源码（165 文件 / 10272 行，含自动生成 1407）
│   ├── GameRoot.cs                  ⭐ 组合根：装配并注册全部 11 个服务（根目录，首个自定义 asmdef 为 _MultiARPG.Utility）
│   │
│   ├── BasicSystems/                基础设施服务（40 文件 / 3698 行，含自动生成 1396）
│   │   ├── Input/                   InputSystem 封装 + 设备类型识别（GameInputControl 自动生成 1396）
│   │   ├── UI/                      MVP 界面层（Core / MainMenu / InGame / General）
│   │   ├── Save/                    存档（Core 读写 + SaveData 纯数据结构）
│   │   ├── Network/                 NetworkService · NetworkHub（⚠️战斗+存档两域同驻）· ClientNetwork*
│   │   ├── Audio/                   音频服务 + 发声器 + SO 音频库
│   │   ├── VFX/                     特效服务 + 控制器 + SO 特效库
│   │   └── Scene/                   场景切换服务（含黑幕过渡编排）
│   │
│   ├── Character/                   角色领域（38 文件 / 2489 行）
│   │   ├── Core/                    Character 基类（⭐含 Presentation 属性）+ 通用属性控制
│   │   │   └── Presentation/        ⭐ SO_CharacterPresentation（受击 VFX/血溅音/受伤音 key）
│   │   └── Player/
│   │       ├── Animation/           动画事件薄转发器（⭐攻击音 Presentation 化）
│   │       ├── Camera/              锁定目标系统（含 ILockableTarget）
│   │       ├── Equipment/           装备挂载权威（⭐TryPickup 场景拾取 + 事件驱动 + IEquippable/EPendingEquipAction/EquipSlot）
│   │       ├── Presentation/        ⭐ SO_PlayerPresentation（idle/死亡/攻击/翻滚/力竭音）
│   │       ├── State/               分层状态机（Parent 姿态 / Child 动作 + SubCapabilities 协作类）
│   │       ├── Stats/               玩家属性计算中心（⭐力竭音 Presentation 化）
│   │       └── Save/                纯本地存档组装 / 还原
│   │
│   ├── AI/                          ⭐ 非玩家智能体（42 文件 / ~1630 行，NodeCanvas 行为树驱动，BossZombie 两阶段已落地）
│   │   ├── Core/                    AINPC（继承 Character，⭐视觉检测）· AINpcService（⭐显式 RegisterNpc；⚠️直连 NetworkManager 绕过外观）
│   │   ├── NPC/Enemy/BossZombie/    ⭐ 僵尸 Boss（BossZombie 二阶段已落地：域类**已引 `using Unity.Netcode`** + 2 NV + 1 裸 ClientRpc（破例，收尾取舍）+ BossZombieAnimControl + BossZombieColliderHit + 11 个 BT 节点 + SO_BossZombie*，两阶段 Host 权威 AI，**伤害经 NetworkHub + owner 权威 EffectControl**；阶段一 + 阶段二（丢武器狂暴/拳脚/Ground 冲击波）已实现；v23 核验 NV=2）
│   │   ├── NPC/Enemy/Zombie/        僵尸敌人（Zombie「⚠️直引 using Unity.Netcode」+ ZombieAnimControl「⭐IsServer 门控 + 表现层音效」+ ZombieHandHit + SO_ZombiePresentation + 10 个行为树节点 + SO_ZombieStats；⚠️ 霸体机制代码已移除，残留 `HitStunThreshold` 为死逻辑待清，死亡跨端 Despawn）
│   │   └── NPC/FakeMan/             训练木桩（复用 FSM / Stats 范式，⭐SO_FakeManPresentation；⚠️FakeMan.cs 冗余 using Unity.Netcode）
│   │
│   ├── GameSystems/                 游戏系统（22 文件 / 647 行，Effect 完整接线 + Item 拍平 + Weapon 独立成域）
│   │   ├── Effect/                  ⭐ 效果系统（13 文件：EffectService/EffectControl/BaseEffect + 4 Concrete + SO_EffectDatabase/TempEffectSource/ActiveEffect/接口；EffectService 注册于消费者前、ApplyEffect 三处活跃调用，v22 完整接线）
│   │   ├── Item/                    消耗品（⭐BaseItem 实例类实现 IEquippable + SO_BaseItem 纯数据资产）
│   │   ├── Weapon/                  ⭐ 武器系统（BaseWeapon 抽象基类「含 OnTriggerEnter 拾取」+ SO_Weapon 纯数据 + Concrete/Weapon_Rapier「✅ v22 数据驱动伤害链路完整：effectId+multiplier 经 NetworkHub → EffectControl.ApplyEffect → DamageEffect → TakeDamage」）
│   │   └── Stats/                   属性修饰器（ModifiableStat · StatModifier）
│   │
│   ├── Utility/                     ⭐ 工具/基础设施（14 文件 / 636 行，原 Core/ 迁入 + 首个自定义 asmdef _MultiARPG.Utility）
│   │   ├── FSM/                     泛型状态机 + 优先级迁移
│   │   ├── Services/                服务容器
│   │   ├── EventBus/                零 GC 事件总线
│   │   ├── ObjectPool/              Mono 对象池
│   │   └── Tool/                    TickService · EventTimer · ButtonScaler · GizmoDrawer（✅ v21 复核无 #if 包裹，P0 已不存在）
│   └── World/                       世界交互物（Checkpoint · ⭐ FogWall 单向门 v20 新增）
│
├── _Data/                           SO 配置资产（Character / Effects / Items / Weapons / Audio / UI / Presentation）
├── _Prefabs/                        预制体（Character / Item / Network / UI / VFX / Audio）
├── _Art/                            美术资源
└── Scenes/                          Scene_MainMenu · Scene_World
```

## 核心系统

### 分层状态机

玩家状态树分两层。父层为姿态（`PlayerFreeState` / `PlayerCombatState`），子层为具体动作。迁移基于 `ETransitionPriority`，高优先级 `AnyTransition` 可抢断当前状态（如受击、死亡）。

```
PlayerStateControl
├── FreeState  ── Movement · Jump · Fall · Dodge
└── CombatState ── NormalAttack · Hit · Dead
```

状态类均为 plain C#，依赖经构造注入，不感知网络存在。

### 网络架构

采用**集中式 Hub** 而非 per-player 同步组件：

- `NetworkService` — 全项目唯一 `NetworkManager.Singleton` 封装（启 Host / 加入房间 / 超时失败处理，已暴露 `IsHost`）
- `NetworkHub` — 唯一跨端 RPC 表面，承载战斗中继（含 ⭐跨端表现 key `HitFeedbackKeys`）与存档同步
- `ClientNetworkTransform` / `ClientNetworkAnimator` — 客户端权威的位移与动画同步
- `BossZombie` — ⚠️ 僵尸 Boss 域类在二阶段**已引入 `using Unity.Netcode`** + 2 个 `NetworkVariable`（武器显隐 `_weaponVisible`、血量镜像 `_syncedHealth`）+ 1 个裸 `ClientRpc`（雾墙停用广播），**打破了「域类 NGO-free」与「RPC 全收口 Hub」两条约定**（属收尾取舍、非推荐范式）；伤害仍经 `NetworkHub` + owner 权威 `EffectControl` 施加；阶段一 + 阶段二（丢武器狂暴/拳脚/Ground 冲击波）均已实现。v23（2026-09-01）核验：`NetworkVariable` 现有 2 处（全在 BossZombie），标准敌人/Boss 死亡仍仅 `Despawn` 跨端销毁

Hub 与玩家侧通过 Provider 回调 + 事件双向解耦，两边互不持有对方类型。

### 装备与拾取（v19）

装备系统完全由 `IEquippable` 契约驱动，无测试字段残留：

- `SO_Weapon` / `SO_BaseItem` — 纯数据资产（槽位配置 / 图标 / 效果 / 倍率 / OverrideController，`OnValidate` 自动同步 ItemID）
- `BaseWeapon` / `BaseItem` — 场景实例类，实现 `IEquippable`（含 `IsEquipped` / `RegisterOwner` 运行时状态）
- `PlayerEquipmentControl.TryPickup` — 场景拾取三级语义：占用槽全空闲→直接装备；否则→收起态挂背；均无空间→不拾取留待背包系统；拾取与伤害结算互斥
- `OnEquipped` / `OnUnequipped` 事件 — 驱动 `UI_WeaponQuickPanel` 快捷栏图标（彩色/置灰），View 零反向依赖

### 表现层配置（v19）

所有角色类型的受击/死亡/攻击/idle/翻滚/力竭等表现 key 由 `SO_*Presentation` 承载（基类 `SO_CharacterPresentation` + Player/Zombie/FakeMan 子类），经 `Character.Presentation` 单点取用；NetworkHub 在 Host 侧从目标表现配置提取 key 经 `HitFeedbackKeys` 跨端广播，旁观端表现与角色类型精确匹配。

### 数据驱动

所有可配置内容以 ScriptableObject 承载并按 key 查表：

| 数据库 | 用途 |
|--------|------|
| `SO_AudioDatabase` | 音频剪辑与播放参数 |
| `SO_UIDatabase` | UI 预制体注册与层级归属 |
| `SO_EffectDatabase` | 效果定义（治疗 / 属性修饰 / 霸体免疫） |
| `SO_VFXDatabase` | 特效预制体 |
| `SO_PlayerStats` / `SO_CharacterStats` / `SO_ZombieStats` / `SO_FakeManStats` / `SO_BossZombieStats` | 角色基础属性（含霸体阈值 / Boss 两阶段阈值） |
| `SO_Weapon` / `SO_BaseItem` | 武器与消耗品数据（槽位 / 倍率 / 动画覆盖控制器 / 效果） |
| `SO_*Presentation` | ⭐ 角色表现配置（受击 / 死亡 / 攻击 / idle 等 VFX 与音频 key） |

### 存档系统

Host 为单一数据源。客户端通过 `NetworkHub` 上报自身存档数据，Host 汇总落盘；读档时 Host 分发，客户端经 `OnPlayerRestore` 事件恢复本地状态。`LocalPlayerId` 提供跨会话稳定标识。

## 快速开始

```bash
git clone https://github.com/mzyqzmd/MultiARPG.git
```

1. 用 **Unity 2022.3.62f3**（或同 LTS 小版本）打开项目
2. 首次打开会拉取 `Packages/manifest.json` 中的官方包
3. 补齐第三方资产（见下节），否则会有编译报错
4. 打开 `Assets/Scenes/Scene_MainMenu.unity` 运行

**本地多人联调**：使用内置的 ParrelSync（菜单 `ParrelSync → Clones Manager`）创建克隆工程，一端起 Host、一端加入即可。

## 代码规模

| 模块 | 文件数 | 行数 |
|------|-------:|-----:|
| BasicSystems | 42 | ~3760 |
| Character | 37 | ~2520 |
| **AI** | **42** | **~1630** |
| GameSystems | 22 | 647 |
| Utility | 17 | ~660 |
| World | 3 | 138 |
| GameRoot.cs（根目录组合根） | 1 | 231 |
| **合计** | **165** | **10272** |

> 其中 `GameInputControl.cs`（1407 行）为 Input System 自动生成代码，手写代码约 8865 行，零编辑器工具文件、零 >300 行手写文件（最大 `NetworkHub` 267 行 / `BossZombie` 279 行）。AI 模块（42 文件 / ~1630 行）为 NodeCanvas 行为树驱动子系统：v15 新增（AINPC + Zombie + FakeMan）→ v16/v17 内聚重构（显式注册 / 视觉检测 / 霸体 / 死亡 Despawn / 去 NGO 直引）→ v18 物品/武器接口化 → **v19 表现层 Presentation 化 + 装备拾取落地** → **v20 新增 BossZombie 僵尸 Boss 模块 + Utility 程序集分离（`Core/` 迁入并新增首个 `_MultiARPG.Utility` asmdef，`Core/` 目录消失）+ World FogWall 单向门** → **v22 伤害链路数据驱动化 + Effect 子系统完整接线（BossZombie 阶段一落地）** → **v23 BossZombie 两阶段实现 + 击晕链路 + Boss 血条 UI（域类引入 NGO 破例，评分 82/100）**。最新逐文件复审见 [`架构分析报告.md`](架构分析报告.md)（v23，2026-09-01）。

## 当前进度

| 状态 | 内容 |
|------|------|
| ✅ 已具备 | SO 数据驱动体系（含 ⭐v19 表现层 `SO_*Presentation`）；组合根与服务容器（**11 个服务**）；分层状态机；零 GC 事件总线；集中式网络 Hub（Boss 有 1 处 ClientRpc 破例）；客户端权威变换/动画同步；Host 单一数据源存档；MVP 事件驱动 UI；锁定目标系统；效果与属性修饰器；VFX / 音频服务；对象池与计时器；**⭐ AI 子系统（NodeCanvas 行为树驱动，含僵尸敌人与 NPC 框架）**；**⭐ v20 BossZombie 僵尸 Boss 基础框架（两阶段 Host 权威 AI，伤害经 NetworkHub + owner 权威 EffectControl；v23 两阶段已落地，血量经 `_syncedHealth` NV 跨端同步；域类二阶段引入 NGO 破例，记录为已知偏差）**；**⭐ v20 首个自定义 asmdef `_MultiARPG.Utility`（基础设施隔离，`Core/` 目录消失）**；**⭐ v16/v17 AI 内聚重构**（显式注册 / 视觉检测 / 霸体 / 死亡 Despawn / 去 NGO 直引）；**⭐ v18 物品/武器接口化重构**；**⭐ v19 表现层数据驱动化 + 装备拾取落地**；**⭐ v20 World FogWall 单向门**；**编辑器 Play 模式无编译阻断（连续多版）**，Standalone 打包无阻断（v21 复核 `GizmoDrawer` 实际无 `#if UNITY_EDITOR` 包裹，P0 已不存在） |
| 🚧 进行中 / 收尾 | 战斗手感打磨（**连段 / 蓄力流程已下沉至 Animator Controller + 动画 Clip 事件驱动**；蓄力重击倍率 / 连段窗口）；**`SO_Weapon` 动画覆盖控制器（`OverrideController`）已接通**；**BossZombie 两阶段已落地**（P1 持武器 + P2 丢武器狂暴/拳脚/Ground 冲击波击晕 + 阶段切换演出 + 血条 UI；BT 资产接线与数值阈值调优收尾中）；**AI 敌人扩展**（僵尸已实现行为树 + NavMesh + 手部碰撞体命中走 NetworkHub 中转、死亡跨端 `Despawn`；BossZombie 两阶段已建；更多敌人类型与 BT 节点复用待扩展——**收尾阶段不计划新增**）；**装备拾取收尾**（`TryPickup` 已落地，背包/多物品管理收尾阶段不做） |
| 📋 规划中 | **✅ 已结清 v17 引入的 P0 打包阻断**（v21 复核 `GizmoDrawer` 实际无 `#if UNITY_EDITOR` 包裹，类型始终编译，Standalone 不再 CS0246，构建健康度满分）；死亡 / 血量数值的跨端同步补齐（**Boss 血量经 `_syncedHealth` NV 已补齐**；标准敌人/Zombie/FakeMan/Player 血量数值仍仅本地——属设计取舍，收尾阶段不推广）；`NetworkHub` 按域拆分（战斗中继 / 存档，v20 已 268 行续涨）；NPC 位移客户端同步（核实预制体 `NetworkTransform`）；收敛 AI 域 NGO（`Zombie` 去直引、`AINpcService` 改走 `NetworkService` 外观）；清理 v19/v20 卫生回退（Hub 热路径 `[RapierHit]` 调试 Log、`FakeMan.cs` 冗余 `using Unity.Netcode`、翻滚音 `DeathAudioGroup` 笔误、`PlayeAttackAudio` 拼写）；Animator 魔法字符串收敛（~50 处 / 17 文件）；关卡内容与示例场景 |

**架构评分**：最新一次逐文件复审（见 [`架构分析报告.md`](架构分析报告.md)，**2026-09-01 v23，基于磁盘当前代码含未提交二阶段改动**）综合评分 **82 / 100（B 档）**——「构建健康度」维度满分（6/6，v21 复核 `GizmoDrawer` 实际无 `#if UNITY_EDITOR` 包裹，P0 打包阻断已不存在），「纯设计分」**75.5/94**（≈80%）。v23 以磁盘当前工作树（含 v22 HEAD `2be9044` 之后的二阶段改动）为准全量逐文件复审，修正 v22 两处不实叙述：**Zombie 霸体机制非「注释掉」而是整体删除、残留死逻辑** + **BossZombie 已非 NGO-free（二阶段引入 2 NV + 1 裸 ClientRpc 破例）**。二阶段功能增益被两处正确性缺陷（`Character.CanBeLocked` NRE 回归、Boss 转阶段窗口归零血无法死亡）与架构回退抵销，净 −2 至 82。九维度：分层 12.5 / 边界 12.0 / SRP 9.5 / 数据 12.0 / 解耦 11.0 / 网络 8.5 / 可测 4.0 / 卫生 6.0 / 构建 6.0。**本项目很可能为倒数第二版**——收尾时执行报告 §9 的 R-1/R-2/R-3/C-* 最小清理（预计回升至 84），跑通构建 + 双端验证 + 提交后即体面归档。

## 已知限制

- **✅ Standalone 打包阻断（v17 误判 P0，v21 复核已结清）**：原以为 `GizmoDrawer`（`Utility/Tool/Gizmos/`）被 `#if UNITY_EDITOR` 包裹而调用方未守卫 → 会触发 CS0246。经 v21/v22 逐行复核，`GizmoDrawer` **实际无 `#if UNITY_EDITOR` 包裹**（仅注释谎称），类型始终编译，两个 `OnDrawGizmos` 调用方引用合法，**Standalone 构建不再 CS0246，构建健康度满分**。本项非真实缺陷，仅为历史误判，已结清。
- **数值同步缺口（设计取舍）**：v6 起 `NetworkVariable` 归零，二阶段为 Boss 引入 **2 处**（`BossZombie._weaponVisible` 武器显隐 + `_syncedHealth` 血量镜像），Boss 血量已跨端同步。标准敌人 `Zombie`/`FakeMan`/玩家 HP 仍**仅本地判定**，其他客户端不持有其数值；Boss/敌人死亡仍仅 `NetworkObject.Despawn` 跨端销毁实体。属设计取舍（owner 权威 + 表现 RPC 简化），收尾阶段不推广补齐标准敌人数值同步。
- **NPC 位移同步待确认**：敌人 AI 行为树与 NavMesh 寻路仅 Host 端运行；若 NPC 预制体未挂 `NetworkTransform`，客户端将仅见动画、不见敌人实际移动。需核对预制体并决策是否补充网络变换同步。
- **`NetworkHub` 职责偏宽**：战斗中继与存档同步两个域同驻一个类（v20 已 268 行，续涨），已规划拆分；Provider / 事件接缝已就位，拆分为零行为变化的纯重构。
- **AI 域网络约定偏离（两处破例）**：标准敌人 `Zombie` 直引 `using Unity.Netcode`（伤害路由经 `NetworkHub` 外观，属合理必要引用）；**`BossZombie` 在二阶段也引了 `using Unity.Netcode` + 2 NV + 1 裸 ClientRpc**（破「域类 NGO-free」与「RPC 收口 Hub」两条约定，属收尾阶段「时间换简洁」取舍，非推荐范式，已记录为已知偏差）——`ZombieHandHit` 已于 v17 去 import；`AINpcService` 直连 `NetworkManager.Singleton` 绕过 `NetworkService` 外观（`NetworkService` 已暴露 `IsHost` 但未用）；⚠️ **`FakeMan.cs` 顶部冗余 `using Unity.Netcode`**（无实际类型使用，一键可删，现计入 10 个 NGO `using` 文件之一）。两者均为小债，已列入改进路线（P2-8 / P2-9）。**✅ 已修复**：`AINpcService` 已去除 `FindObjectsByType` 隐式查找，改为 NPC 显式 `RegisterNpc`/`UnregisterNpc`。
- **调试残留（v23 仍有，待收尾清理）**：`NetworkHub.NotifyHitClientRpc` 内 2 处 `[RapierHit]` 调试 `Debug.Log`（热路径，联调完应删）；手写普通 `Debug.Log` 现 **5 处 / 4 文件**（较 v22 的 12 处已显著清理——`PlayerTest` 及 `Player.cs` 多处已移除）；`PlayerDodgeState` 翻滚音误用 `DeathAudioGroup`（应为 `DodgeAudioGroup`，默认值恰好同为 "Player" 组故运行时不炸）；`PlayerAnimControl.PlayeAttackAudio` 拼写瑕疵；`PlayerDeadState` 死亡弹文本 "寄!" 上线文案隐患。均为极小改动。
- **原型残留已清**：`PlayerTest` 调试入口（曾承载"返回主菜单"真实功能）**已于 v23 从磁盘删除**，债务消除；`Player` 中已无 `_defaultWeapon` 测试字段（v19 彻底移除）。
- **asmdef 隔离（可选，单人非必需）**：v20 新增首个自定义 asmdef `_MultiARPG.Utility` 隔离基础设施；其余模块仍共用 `Assembly-CSharp`。asmdef 在单人项目不计分（架构正确性无影响），但可缩短 Unity 编译/热重载耗时——后续可按需为更多模块补 asmdef（纯迭代速度取舍，非正确性缺陷）。
- **内容量有限**：项目定位为架构验证与技术展示，玩法内容仍处早期。

## 架构文档

仓库根目录的 [`架构分析报告.md`](架构分析报告.md) 是一份持续更新的**逐文件架构审阅报告**，包含九维度评分（分层 / 边界 / SRP / 数据驱动 / 解耦 / 网络 / 可测试性 / 代码卫生 / 构建）、依赖关系分析、设计模式清单与分级改进建议（P0–P3）。

## 许可与第三方资产

本仓库源码仅包含 `Assets/_Scripts` 下的自研代码。以下第三方资产**版权归各自作者所有，不随本仓库分发**，请自行通过 Asset Store 或官方渠道获取：

- **Odin Inspector**（Sirenix）— 商业授权
- **Animancer**（Kybernetix）— 商业授权，用于动画状态机驱动（HybridAnimancerComponent / ClipTransition）
- **NodeCanvas**（ParadoxNotion）— 商业授权，用于 AI 行为树与可视化行为编排（`BehaviourTreeOwner` 与 `ActionTask`/`ConditionTask` 任务节点）
- **DOTween**（Demigiant）— 免费版 / Pro 版
- **Hot Reload**（Singularity Group）— 商业授权，位于 `Packages/com.singularitygroup.hotreload`
- **ParrelSync** — MIT，位于 `Assets/Plugins/ParrelSync`

美术资源（`Assets/_Art`）为第三方低模素材，仅用于原型演示。

---

欢迎就架构设计或实现细节提 Issue 讨论。
