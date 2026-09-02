# MultiARPG

> 🏁 **项目状态：已定版（Final）**。这是一个**用于技术展示的架构实践项目**——数据驱动配置、分层状态机、服务容器组合根、领域逻辑与网络层的解耦实践。**当前版本为最终版本，不再继续开发**；架构质量以根目录 [`架构分析报告.md`](架构分析报告.md)（v24 终版快照，2026-09-02）的全量逐文件审阅为准，遗留问题已全量登记、随终版归档。

> A Unity multiplayer ARPG sandbox exploring data-driven, decoupled architecture — hierarchical FSM, ScriptableObject-driven configuration, and a single centralized network surface.

一个基于 Unity 的多人 ARPG 项目，重点**不在内容量、不在玩法完成度**，而在**架构实践验证**：数据驱动配置、分层状态机、服务容器组合根，以及**领域逻辑与网络层的彻底解耦**。

![Unity](https://img.shields.io/badge/Unity-2022.3.62f3-000000?logo=unity)
![Netcode](https://img.shields.io/badge/Netcode%20for%20GameObjects-1.12.2-2196F3)
![URP](https://img.shields.io/badge/URP-14.0.12-4CAF50)
![C#](https://img.shields.io/badge/C%23-163%20files%20%2F%20%7E10.3k%20lines-239120)
![架构评分](https://img.shields.io/badge/Architecture-82%2F100%20%28B%2C%20Final%29-21a0c0)

---

## 架构亮点

| 特性 | 说明 |
|------|------|
| **集中式网络表面（以 Hub 为主，Boss 有 1 处破例）** | RPC 收口 `NetworkHub`（4 ServerRpc + 4 ClientRpc）；`BossZombie` 额外自带 1 个 `[ClientRpc]`（总数 5）。`NetworkVariable` 仅 2 处（均属 `BossZombie`：武器显隐 + 血量镜像）。`using Unity.Netcode` 文件 10 个。属收尾阶段「时间换简洁」取舍，记录为[已知偏差](#已知限制) |
| **被动断线检测闭环（终版新增）** | ⭐ 客户端断线 → `NetworkService` 区分主动退出/被动断线（一次性标志互斥）→ 事件解耦至 `SceneControlService`（渐黑 + 明确提示）→ NGO 有序 Shutdown（等待真完成再切场景，5s 超时兜底）→ 返回主菜单。v23 完全缺失断线处理，终版补齐为生产级健壮度 |
| **表现层数据驱动** | `SO_*Presentation` 表现配置四件套（`SO_CharacterPresentation` 抽象基类 + Player/Zombie/FakeMan/BossZombie 子类）：受击飙血 VFX、受伤/血溅/死亡/攻击/idle 等全部音频与 VFX key 收敛为 SO 资产配置，**`PlayAudio("...")`/`PlayVFX("...")` 字面量全仓归零**；NetworkHub 经 `HitFeedbackKeys`（`INetworkSerializeByMemcpy`）跨端携带目标表现 key |
| **装备系统契约化** | `IEquippable` 接口（运行时状态 `IsEquipped`/`RegisterOwner` 下沉到 `BaseWeapon` 实例类，`SO_Weapon` 为纯数据资产）+ `OnEquipped`/`OnUnequipped` 事件驱动 UI。初始装备经 `PlayerEquipmentControl` Inspector 直配（拾取系统已随范围裁剪移除，Item 模块整体删除） |
| **BossZombie 僵尸 Boss（两阶段）** | 18 个 `.cs`：伤害经 `NetworkHub.RequestDamageServerRpc(effectId, multiplier, isStun)` → Host 定位 → owner 权威 `EffectControl` 施加（与玩家武器同源）；阶段一（P1 持武器）+ 阶段二（丢武器狂暴 + 拳脚 + Ground 冲击波击晕）均已实现；血量经 `NetworkVariable<int> _syncedHealth` 跨端同步；表现侧细节完备（Root Motion 的 NavMesh 钳制防穿墙/升天、冲击波薄壳检测 + 去重防逐帧秒杀）。域类引 NGO 属[已知偏差](#已知限制) |
| **领域 NGO-free（大体；BossZombie 除外）** | Stats / Effect / Weapon / Save 组装 / Input / Audio / UI / FSM / `ZombieAnimControl` / `ZombieHandHit` 全部不引用网络库；标准敌人仅 `Zombie` 直引 NGO（合理必要引用），其余 AI 类经 `NetworkHub` 外观复用伤害链路 |
| **组合根 + 服务容器** | `GameRoot` 按显式依赖顺序构建并注册 **12 个服务**，构造注入而非静态单例，场景卸载时逆序注销；终版新增 `_initialized` 防双实例标志（消除主菜单内嵌 GameRoot 的注销链错乱） |
| **分层状态机** | 父层管姿态（Free / Combat），子层管动作（Move / Jump / Fall / Dodge / Attack / Hit / Dead），带优先级抢断的 `AnyTransition`；击晕链路完整自洽（`IsStunInterruptible` 抢断集 + `StunState` 进态消费残留请求） |
| **数据驱动** | 角色属性、效果、武器、音频、UI、VFX、表现配置全部由 `SO_*` 数据库按 key 查表；`SO_Weapon` 蓄力倍率与动画覆盖控制器（`OverrideController`）已接通运行时；伤害数值端到端同口径（飘字与扣血共用 `DamageEffect.Compute`） |
| **零 GC 事件总线** | `EventBus` 仅承载 1 个游戏级信号（`ReturningToMainMenuEvent`），参数为只读 `struct` |
| **锁定目标系统** | `ILockableTarget` 接口 + `OverlapSphere` 检测 + `Linecast` 遮挡剔除，经 `Action<bool>` 事件桥接状态机，**零反向编译期依赖** |
| **干净可编译** | 全仓零 `using UnityEditor`、零隐式查找（`Find*`/`StringToHash`）、源码层无编译级错误。历史误判的 `GizmoDrawer` 打包 P0 经逐行复核**澄清为误报**（该文件全部为运行时 API，Standalone 可正常编译） |

## 技术栈

**引擎与官方包**

- Unity **2022.3.62f3** LTS · Universal Render Pipeline **14.0.12**
- Netcode for GameObjects **1.12.2** — 网络同步
- Input System **1.14.2** — 多设备输入（键盘数字量 / 手柄模拟量区分处理）
- Cinemachine **2.10.7** · TextMesh Pro **3.0.9** · Timeline **1.7.7**

**第三方资产**（详见 [许可与第三方资产](#许可与第三方资产)）

- DOTween（Demigiant）· Odin Inspector（Sirenix）· Animancer（Kybernetix，动画驱动）· NodeCanvas（ParadoxNotion，AI 行为树）· ParrelSync（本地双开联调）· Hot Reload（本地 UPM 包）

## 架构总览

依赖方向严格自上而下，**下层不得反向引用上层**：

```
┌──────────────────────────────────────────────────────────┐
│  表现层    UI（MVP）· 动画事件 · VFX · 音频发声器          │
│            ⭐ 表现 key 全 SO 驱动（SO_*Presentation）     │
├──────────────────────────────────────────────────────────┤
│  领域层    Character / Player 能力组件 · 分层 FSM ·        │
│            Effect · Weapon · Equipment                   │
│            ← 大体 NGO-free（BossZombie 域类引 NGO，破例） │
├──────────────────────────────────────────────────────────┤
│  服务层    12 × IService（Input / UI / Audio / VFX /       │
│            Scene / Save / Settings / Effect /             │
│            DamageText / Camera / Network / AINpc）         │
├──────────────────────────────────────────────────────────┤
│  数据层    SO_* 配置资产（Assets/_Data，按 key 查表）      │
│            + SO_*Presentation 表现配置                    │
├──────────────────────────────────────────────────────────┤
│  网络层    NetworkService（唯一 NetworkManager 封装 +      │
│            被动断线编排）                                  │
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
- **状态机中立**：状态类为 plain C#，经构造注入获得依赖，不持有网络引用。

## 项目结构

```
Assets/
├── _Scripts/                        源码（163 文件 / 10266 行，含自动生成 1396）
│   ├── GameRoot.cs                  ⭐ 组合根：装配并注册全部 12 个服务（271 行，含防双实例标志）
│   │
│   ├── BasicSystems/                基础设施服务（46 文件 / 4195 行，含自动生成 1396）
│   │   ├── Input/                   InputSystem 封装 + 设备类型识别（GameInputControl 自动生成 1396）
│   │   ├── UI/                      MVP 界面层（Core / MainMenu / InGame / NPC / DamageText / Setting / Escape）
│   │   ├── Save/                    存档（Core 读写 + SaveData 纯数据结构）
│   │   ├── Network/                 NetworkService（⭐被动断线编排）· NetworkHub（⚠️战斗+存档两域同驻）· ClientNetwork*
│   │   ├── Audio/                   音频服务 + 发声器 + SO 音频库
│   │   ├── VFX/                     特效服务 + 控制器 + SO 特效库
│   │   └── Scene/                   场景切换服务（黑幕过渡 + ⭐断线表现侧编排 + BGM 编排）
│   │
│   ├── Character/                   角色领域（37 文件 / 2536 行）
│   │   ├── Core/                    Character 基类（⭐含 Presentation 属性）+ 通用属性控制
│   │   │   └── Presentation/        ⭐ SO_CharacterPresentation（受击 VFX/血溅音/受伤音 key）
│   │   └── Player/
│   │       ├── Animation/           动画事件薄转发器（攻击音 Presentation 化）
│   │       ├── Camera/              锁定目标系统（含 ILockableTarget）
│   │       ├── Equipment/           装备挂载权威（IEquippable / EquipSlot / 初始装备 Inspector 直配）
│   │       ├── Presentation/        ⭐ SO_PlayerPresentation（idle/死亡/攻击/翻滚/力竭音）
│   │       ├── State/               分层状态机（Parent 姿态 / Child 动作 + 击晕链路）
│   │       ├── Stats/               玩家属性计算中心
│   │       └── Save/                纯本地存档组装 / 还原
│   │
│   ├── AI/                          ⭐ 非玩家智能体（42 文件 / 1927 行，NodeCanvas 行为树驱动）
│   │   ├── Core/                    AINPC（继承 Character，视觉检测）· AINpcService（⚠️直连 NetworkManager）
│   │   ├── NPC/Enemy/BossZombie/    ⭐ 僵尸 Boss（18 文件：域类引 NGO 破例 + 2 NV + BossZombieAnimControl + ColliderHit + ShockWave + 9 BT 节点 + SO_BossZombie*，两阶段 Host 权威 AI）
│   │   ├── NPC/Enemy/Zombie/        僵尸敌人（Zombie「合理直引 NGO」+ ZombieAnimControl 语义桥 + ZombieHandHit + 10 BT 节点 + SO 数据）
│   │   └── NPC/FakeMan/             训练木桩（复用 FSM / Stats 范式 + SO_FakeManPresentation）
│   │
│   ├── GameSystems/                 游戏系统（20 文件 / 569 行，Effect 完整接线 + Weapon 独立成域；Item 模块已删除）
│   │   ├── Effect/                  ⭐ 效果系统（13 文件：EffectService/EffectControl/BaseEffect + 4 Concrete + SO_EffectDatabase/TempEffectSource/ActiveEffect/接口）
│   │   ├── Weapon/                  ⭐ 武器系统（BaseWeapon 抽象基类 + SO_Weapon 纯数据 + Weapon_Rapier：effectId+multiplier 经 NetworkHub → EffectControl → DamageEffect → TakeDamage）
│   │   └── Stats/                   属性修饰器（ModifiableStat_Float/Int · StatModifier）
│   │
│   ├── Utility/                     ⭐ 工具/基础设施（14 文件 / 630 行，首个自定义 asmdef _MultiARPG.Utility）
│   │   ├── FSM/                     泛型状态机 + 优先级迁移
│   │   ├── Services/                服务容器
│   │   ├── EventBus/                零 GC 事件总线
│   │   ├── ObjectPool/              Mono 对象池
│   │   └── Tool/                    TickService · EventTimer · ButtonScaler · GizmoDrawer
│   └── World/                       世界交互物（Checkpoint · FogWall 单向门 · AudioPlayBox 区域 BGM）
│
├── _Data/                           SO 配置资产（Character / Effects / Weapons / Audio / UI / Presentation）
├── _Prefabs/                        预制体（Character / Network / UI / VFX / Audio）
├── _Art/                            美术资源
└── Scenes/                          Scene_MainMenu · Scene_World
```

## 核心系统

### 分层状态机

玩家状态树分两层。父层为姿态（`PlayerFreeState` / `PlayerCombatState`），子层为具体动作。迁移基于 `ETransitionPriority`，高优先级 `AnyTransition` 可抢断当前状态（如受击、死亡、击晕）。

```
PlayerStateControl
├── FreeState  ── Movement · Jump · Fall · Dodge
└── CombatState ── NormalAttack · Hit · Stun · Dead
```

状态类均为 plain C#，依赖经构造注入，不感知网络存在。

### 网络架构

采用**集中式 Hub** 而非 per-player 同步组件：

- `NetworkService` — 全项目唯一 `NetworkManager.Singleton` 封装（启 Host / 加入房间 / 超时失败处理 / `IsHost`）+ **被动断线编排**（`_intentionalShutdown` 主动退出互斥标志、`BeginReturnToMainMenu` 有序 Shutdown 与 5s 兜底）
- `NetworkHub` — 唯一跨端 RPC 表面，承载战斗中继（含跨端表现 key `HitFeedbackKeys`）与存档同步；`OnNetworkDespawn` 清空存档字典防状态泄漏
- `ClientNetworkTransform` / `ClientNetworkAnimator` — 客户端权威的位移与动画同步
- `BossZombie` — ⚠️ 唯一域类破例：引 `using Unity.Netcode` + 2 个 `NetworkVariable`（武器显隐 / 血量镜像）+ 1 个裸 `ClientRpc`（雾墙停用广播，各端本地执行）；伤害仍经 `NetworkHub` + owner 权威 `EffectControl` 施加

Hub 与玩家侧通过 Provider 回调 + 事件双向解耦，两边互不持有对方类型。

### 被动断线处理（终版能力）

```
客户端检测连接丢失（NGO OnDisconnectedFromHost，主动退出经标志互斥不触发）
  → NetworkService 发出 OnDisconnectedFromHost 事件
  → SceneControlService：渐黑 + "Host Disconnect" 红字提示
  → BeginReturnToMainMenu：NGO 有序 Shutdown（等待 OnClientStopped/OnServerStopped 真完成，5s 超时兜底）
  → 返回主菜单（BGM 经 SceneControlService 编排恢复）
```

### 装备系统

装备系统由 `IEquippable` 契约驱动：

- `SO_Weapon` — 纯数据资产（槽位配置 / 图标 / 效果 id / 蓄力倍率 / OverrideController，`OnValidate` 自动同步 ItemID）
- `BaseWeapon` — 场景实例类，实现 `IEquippable`（含 `IsEquipped` / `RegisterOwner` 运行时状态），攻击碰撞体由动画事件在命中窗口期启停
- 初始装备经 `PlayerEquipmentControl` Inspector 直配（拾取/Item 模块已随范围裁剪移除）
- `OnEquipped` / `OnUnequipped` 事件 — 驱动快捷栏 UI，View 零反向依赖

### 表现层配置

所有角色类型的受击/死亡/攻击/idle 等表现 key 由 `SO_*Presentation` 承载（基类 `SO_CharacterPresentation` + Player/Zombie/FakeMan/BossZombie 子类），经 `Character.Presentation` 单点取用；NetworkHub 在 Host 侧从目标表现配置提取 key 经 `HitFeedbackKeys` 跨端广播，旁观端表现与角色类型精确匹配。

### 数据驱动

所有可配置内容以 ScriptableObject 承载并按 key 查表：

| 数据库 | 用途 |
|--------|------|
| `SO_AudioDatabase` | 音频剪辑与播放参数 |
| `SO_UIDatabase` | UI 预制体注册与层级归属 |
| `SO_EffectDatabase` | 效果定义（伤害 / 治疗 / 属性修饰 / 伤害倍率） |
| `SO_VFXDatabase` | 特效预制体 |
| `SO_PlayerStats` / `SO_CharacterStats` / `SO_ZombieStats` / `SO_FakeManStats` / `SO_BossZombieStats` | 角色基础属性（含 Boss 两阶段阈值与分动作攻击距离） |
| `SO_Weapon` | 武器数据（槽位 / 蓄力倍率 / 动画覆盖控制器 / 效果） |
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
| BasicSystems | 46 | 4195 |
| Character | 37 | 2536 |
| **AI** | **42** | **1927** |
| GameSystems | 20 | 569 |
| Utility | 14 | 630 |
| World | 3 | 138 |
| GameRoot.cs（根目录组合根） | 1 | 271 |
| **合计** | **163** | **10266** |

> 其中 `GameInputControl.cs`（1396 行）为 Input System 自动生成代码，手写代码约 8870 行，零编辑器工具文件、零 >300 行手写文件（最大 `BossZombie` 279 行 / `NetworkHub` 269 行）。AI 模块（42 文件）为 NodeCanvas 行为树驱动子系统。完整逐文件审阅见 [`架构分析报告.md`](架构分析报告.md)（v24 终版，2026-09-02）。

## 终版状态

| 状态 | 内容 |
|------|------|
| ✅ 已具备 | SO 数据驱动体系（含表现层 `SO_*Presentation`）；组合根与服务容器（12 个服务 + 防双实例）；分层状态机（含击晕链路）；零 GC 事件总线；集中式网络 Hub（Boss 有 1 处 ClientRpc 破例）；客户端权威变换/动画同步；**被动断线检测闭环**；Host 单一数据源存档；MVP 事件驱动 UI；锁定目标系统；效果与属性修饰器；VFX / 音频服务；对象池与计时器；AI 子系统（NodeCanvas 行为树驱动）；BossZombie 僵尸 Boss 两阶段（Host 权威 AI，血量经 `_syncedHealth` NV 跨端同步）；`_MultiARPG.Utility` 程序集隔离；World FogWall 单向门 |
| ⚠️ 已知问题（随终版归档，不再修复） | 详见 [`架构分析报告.md`](架构分析报告.md) §9 遗留债务清单。要点：Boss 转阶段演出窗口内打穿血量会导致 Boss 无法死亡且雾墙不开（P1，触发条件为 6.5s 窗口内打出过量伤害）；存档槽名显示调试口径文本（P2）；耐力上限变更 UI 错值（P2）；若干热路径调试 Log / 死代码 / 冗余 using（P3） |
| 🏁 定版 | **当前版本为最终版本，不再继续开发**。架构评分 **82 / 100（B 档）**，纯设计分 75.5/94（≈80%），九维度：分层 12.5 / 边界 12.0 / SRP 9.5 / 数据 12.0 / 解耦 11.0 / 网络 9.0 / 可测 4.0 / 卫生 5.5 / 构建 6.0。版本演进：v15 AI 子系统 → v16/v17 AI 内聚重构 → v18 物品/武器接口化 → v19 表现层 Presentation 化 + 装备拾取 → v20 BossZombie 框架 + Utility asmdef + FogWall → v22 伤害链路数据驱动化 → v23 BossZombie 两阶段 + 击晕链路 + Boss 血条 → **v24 终版（Item 模块删除 / 被动断线闭环 / 组合根防双实例 / 全量 163 文件逐行复审定档）** |

## 已知限制

- **Boss 转阶段死亡缺口（P1，终版已知问题）**：`BossZombie` 阶段切换演出（6.5s）期间血量被打至 0 时，演出结束进入二阶段后死亡事件不再触发（血量 0→0 不产生变更事件），Boss 将无法被击杀且雾墙保持关闭。触发条件为演出窗口内打出过量伤害。已登记于报告 §9，随终版归档。
- **网络约定偏离（收尾取舍，登记为已知偏差）**：`BossZombie` 域类引 `using Unity.Netcode` + 2 NV + 1 裸 ClientRpc（破「域类 NGO-free」与「RPC 收口 Hub」两条约定，用法本身自洽）；标准敌人 `Zombie` 直引 NGO（合理必要引用）；`AINpcService` 直连 `NetworkManager.Singleton` 绕过外观。
- **数值同步缺口（设计取舍）**：Boss 血量经 `_syncedHealth` NV 跨端同步；标准敌人 `Zombie`/`FakeMan`/玩家 HP 仍仅本地判定，其他客户端不持有其数值；敌人死亡经 `NetworkObject.Despawn` 跨端销毁。
- **NPC 位移同步待确认**：敌人 AI 行为树与 NavMesh 寻路仅 Host 端运行；若 NPC 预制体未挂 `NetworkTransform`，客户端将仅见动画、不见敌人实际移动。
- **`NetworkHub` 职责偏宽**：战斗中继与存档同步两域同驻（269 行）。Provider / 事件接缝已就位，拆分为零行为变化的纯重构，终版不执行。
- **调试残留（P3，未清）**：手写普通 `Debug.Log` 5 处 / 3 文件（含 `NetworkHub` 热路径 2 处 `[RapierHit]`）；死代码 2 处（`UnequipAll` / `[Obsolete]` 断线方法）；冗余 `using` 4 处；`BaseItem` 过期注释 3 处；文案与拼写小瑕疵若干。完整清单见报告 §7/§9。
- **asmdef 隔离（单人非必需）**：仅 `Utility` 模块有独立 asmdef，其余共用 `Assembly-CSharp`。单人项目不计分，可缩短热重载耗时的扩展未做。
- **内容量有限**：项目定位为架构验证与技术展示，玩法内容处于原型水平。

## 架构文档

仓库根目录的 [`架构分析报告.md`](架构分析报告.md) 是一份**全量逐文件架构审阅报告**（v24 终版：对 `Assets/_Scripts` 全部 163 个文件的逐一源码审阅），包含九维度评分（分层 / 边界 / SRP / 数据驱动 / 解耦 / 网络 / 可测试性 / 代码卫生 / 构建）、依赖关系分析、P0–P3 分级问题清单与终版遗留债务登记。

## 许可与第三方资产

本仓库源码仅包含 `Assets/_Scripts` 下的自研代码。以下第三方资产**版权归各自作者所有，不随本仓库分发**，请自行通过 Asset Store 或官方渠道获取：

- **Odin Inspector**（Sirenix）— 商业授权
- **Animancer**（Kybernetix）— 商业授权，用于动画驱动
- **NodeCanvas**（ParadoxNotion）— 商业授权，用于 AI 行为树与可视化行为编排
- **DOTween**（Demigiant）— 免费版 / Pro 版
- **Hot Reload**（Singularity Group）— 商业授权，位于 `Packages/com.singularitygroup.hotreload`
- **ParrelSync** — MIT，位于 `Assets/Plugins/ParrelSync`

美术资源（`Assets/_Art`）为第三方低模素材，仅用于原型演示。

---

欢迎就架构设计或实现细节提 Issue 讨论。
