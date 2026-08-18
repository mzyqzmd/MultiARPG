# MultiARPG

> A Unity multiplayer ARPG sandbox exploring data-driven, decoupled architecture — hierarchical FSM, ScriptableObject-driven configuration, and a single centralized network surface.

一个基于 Unity 的多人 ARPG 项目，重点不在内容量，而在**架构实践**：数据驱动配置、分层状态机、服务容器组合根，以及**领域逻辑与网络层的彻底解耦**。

![Unity](https://img.shields.io/badge/Unity-2022.3.62f3-000000?logo=unity)
![Netcode](https://img.shields.io/badge/Netcode%20for%20GameObjects-1.12.2-2196F3)
![URP](https://img.shields.io/badge/URP-14.0.12-4CAF50)
![C#](https://img.shields.io/badge/C%23-132%20files%20%2F%208.1k%20lines-239120)
![架构评分](https://img.shields.io/badge/Architecture-80%2F100%20%28B%29-21a0c0)

---

## 架构亮点

| 特性 | 说明 |
|------|------|
| **单一网络表面** | 全项目 **8 个 RPC 全部收口在 `NetworkHub`**（4 ServerRpc + 4 ClientRpc），`NetworkVariable` 使用量为 **0**；仅 **9 个文件** `using Unity.Netcode`（含 AI 的 `Zombie` 域类、`AINpcService` 服务，详见已知限制；`ZombieHandHit` 已于 v17 去除 NGO 直引） |
| **领域 NGO-free（大体）** | Stats / Effect / Item / Save 组装 / Input / Audio / UI / FSM / `ZombieAnimControl` / `ZombieHandHit` 全部不引用网络库；仅 `Zombie` 一个敌人域类破例直引 NGO（`ZombieHandHit` 已于 v17 去除 NGO 直引，改经类型化引用 `_owner` 传递 NGO 成员）；其余 AI 类均经 `NetworkHub` 外观复用伤害链路 |
| **组合根 + 服务容器** | `GameRoot` 按显式依赖顺序构建并注册 **12 个服务**（含 v15 新增的 `AINpcService`），构造注入而非静态单例；场景卸载时逆序注销 |
| **分层状态机** | 父层管姿态（Free / Combat），子层管动作（Move / Jump / Fall / Dodge / Attack / Hit / Dead），带优先级抢断的 `AnyTransition` |
| **数据驱动** | 角色属性、效果、物品、音频、UI、VFX 全部由 `SO_*` 数据库按 key 查表，配置与运行时状态分离；`SO_Weapon` 蓄力倍率、武器动画字典、**武器动画覆盖控制器（`OverrideController`）均已接通运行时** |
| **零 GC 事件总线** | `EventBus` 仅承载 3 个游戏级信号，参数为只读 `struct`，避免退化为「事件版服务定位器」 |
| **锁定目标系统** | `ILockableTarget` 接口 + `OverlapSphere` 检测 + `Linecast` 遮挡剔除，经 `Action<bool>` 事件桥接状态机，**零反向编译期依赖** |
| **干净可编译（编辑器 Play）** | 全仓零 `using UnityEditor`、源码层无编译级错误、无悬空引用——**Unity Play 模式无障碍**；但 **Standalone 打包存在 1 处 P0 阻断**（`GizmoDrawer` 被 `#if UNITY_EDITOR` 包裹而其两个 `OnDrawGizmos` 调用方未守卫，见[已知限制](#已知限制)），修复成本极低（一行级） |

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
├──────────────────────────────────────────────────────────┤
│  领域层    Character / Player 能力组件 · 分层 FSM ·        │
│            Effect · Item · Stats        ← 全部 NGO-free   │
├──────────────────────────────────────────────────────────┤
│  服务层    12 × IService（Input / UI / Audio / VFX /       │
│            Scene / Save / Settings / Effect / Item /       │
│            Camera / Network / AINpc）                      │
├──────────────────────────────────────────────────────────┤
│  数据层    SO_* 配置资产（Assets/_Data，按 key 查表）      │
├──────────────────────────────────────────────────────────┤
│  网络层    NetworkService（唯一 NetworkManager 封装）      │
│            NetworkHub（唯一 RPC 表面）                     │
│            ClientNetworkTransform / Animator               │
└──────────────────────────────────────────────────────────┘
                          ↑
                  GameRoot（组合根，装配全部服务）
```

### 核心设计约定

- **网络触点最小化**：领域类不继承 `NetworkBehaviour`、不直用 `IsServer/IsOwner/NetworkObject`。跨端通信一律经 `NetworkHub`，Hub 通过 `Func<PlayerSaveData>` Provider 回调与 `OnPlayerRestore` 事件与玩家侧交互，**Hub 零 Player 类型引用**。
- **无隐式查找**：不使用 `GameObject.Find` / `FindObjectOfType` 做跨系统通信；`Camera.main` 收敛为 `PlayerCameraService` 唯一缓存点。
- **单一职责**：能力（Capabilities）与组件（Components）分离——前者承载可被状态机驱动的玩家能力，后者是自足的挂载式组件。
- **状态机中立**：状态类为 plain C#，经构造注入获得依赖，不持有网络引用，可脱离 Unity 运行时单元测试。

## 项目结构

```
Assets/
├── _Scripts/                        源码（132 文件 / 8127 行）
│   ├── BasicSystems/                基础设施服务（39 文件 / 3675 行）
│   │   ├── Input/                   Input System 封装 + 设备类型识别
│   │   ├── UI/                      MVP 界面层（Core / MainMenu / InGame / General）
│   │   ├── Save/                    存档（Core 读写 + SaveData 纯数据结构）
│   │   ├── Network/                 NetworkService · NetworkHub · ClientNetwork*
│   │   ├── Audio/                   音频服务 + 发声器 + SO 音频库
│   │   ├── VFX/                     特效服务 + 控制器 + SO 特效库
│   │   └── Scene/                   场景切换服务（含黑幕过渡编排）
│   │
│   ├── Character/                   角色领域（33 文件 / 2208 行，FakeMan 已迁出）
│   │   ├── Core/                    Character 基类 + 通用属性控制
│   │   └── Player/
│   │       ├── Animation/           动画事件薄转发器
│   │       ├── Camera/              锁定目标系统（含 ILockableTarget）
│   │       ├── Equipment/           装备挂载权威（含武器动画覆盖控制器切换）
│   │       ├── State/               分层状态机（Parent 姿态 / Child 动作 + SubCapabilities 协作类）
│   │       ├── Stats/               玩家属性计算中心
│   │       └── Save/                纯本地存档组装 / 还原
│   │
│   ├── AI/                          ⭐ 非玩家智能体（22 文件 / 816 行，NodeCanvas 行为树驱动，v16 重构 / v17 增强：AI 视觉检测重生 + 僵尸霸体）
│   │   ├── Core/                    AINPC（继承 Character）· AINpcService（第 12 个 IService，⭐显式 RegisterNpc 替代 FindObjectsByType）
│   │   ├── Enemy/NormalZombie/      僵尸敌人（Zombie + ZombieAnimControl + ZombieHandHit「已去 NGO 直引」+ 10 个行为树节点 + SO_ZombieStats「含 HitStunThreshold 霸体阈值」，死亡跨端 Despawn）
│   │   └── NPC/FakeMan/             训练木桩（由 Character/FakeMan 迁入，复用 FSM / Stats 范式）
│   │
│   ├── Core/                        零业务基础设施（11 文件 / 544 行）
│   │   ├── GameRoot.cs              组合根：服务装配与生命周期（现注册 12 个服务）
│   │   ├── FSM/                     泛型状态机 + 优先级迁移
│   │   ├── Services/                服务容器
│   │   ├── EventBus/                零 GC 事件总线
│   │   └── ObjectPool/              Mono 对象池
│   │
│   ├── GameSystems/                 游戏系统（22 文件 / 531 行）
│   │   ├── Effect/                  效果系统（Core / Concrete / DamageInfo）
│   │   ├── Item/                    物品与武器（BaseItem · Weapon_Rapier · SO_Weapon 蓄力倍率）
│   │   └── Stats/                   属性修饰器（ModifiableStat · StatModifier）
│   │
│   ├── Utility/                     工具（TickService · EventTimer · ButtonScaler · GizmoDrawer）
│   └── World/                       世界交互物（Checkpoint）
│
├── _Data/                           SO 配置资产（Character / Effects / Items / Audio / UI）
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

- `NetworkService` — 全项目唯一 `NetworkManager.Singleton` 封装（启 Host / 加入房间 / 超时失败处理）
- `NetworkHub` — 唯一跨端 RPC 表面，承载战斗中继与存档同步
- `ClientNetworkTransform` / `ClientNetworkAnimator` — 客户端权威的位移与动画同步

Hub 与玩家侧通过 Provider 回调 + 事件双向解耦，两边互不持有对方类型。

### 数据驱动

所有可配置内容以 ScriptableObject 承载并按 key 查表：

| 数据库 | 用途 |
|--------|------|
| `SO_AudioDatabase` | 音频剪辑与播放参数 |
| `SO_UIDatabase` | UI 预制体注册与层级归属 |
| `SO_EffectDatabase` | 效果定义（治疗 / 属性修饰） |
| `SO_ItemDatabase` | 物品与武器定义 |
| `SO_VFXDatabase` | 特效预制体 |
| `SO_PlayerStats` / `SO_CharacterStats` | 角色基础属性 |

武器专属数据 `SO_Weapon` 额外承载蓄力重击倍率（`ShortChargeMulti` / `LongChargeMulti`）与动画字典（`WeaponAnimations`），均已在运行时被状态机与武器逻辑实际消费。

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
| BasicSystems | 39 | 3675 |
| Character | 33 | 2208 |
| **AI** | **22** | **816** |
| Core | 11 | 544 |
| GameSystems | 22 | 531 |
| Utility | 4 | 280 |
| World | 1 | 29 |
| **合计** | **132** | **8127** |

> 其中 `GameInputControl.cs`（1396 行）为 Input System 自动生成代码，手写代码约 6731 行。AI 模块（22 文件 / 816 行）为 NodeCanvas 行为树驱动子系统，2026-08-17 经 v16 重构（去 `FindObjectsByType`、拆 `ZombieAnimControl`·`ZombieHandHit`、僵尸死亡跨端 `Despawn`），2026-08-18 v17 进一步增强（AI 视觉检测重生：前/后视锥 + 遮挡 + 3s 记忆；僵尸受击霸体 `SO_ZombieStats.HitStunThreshold`；`ZombieHandHit` 去除 NGO 直引）。

## 当前进度

| 状态 | 内容 |
|------|------|
| ✅ 已具备 | SO 数据驱动体系；组合根与服务容器（12 个服务）；分层状态机；零 GC 事件总线；集中式网络 Hub；客户端权威变换/动画同步；Host 单一数据源存档；MVP 事件驱动 UI；锁定目标系统；效果与属性修饰器；VFX / 音频服务；对象池与计时器；**⭐ AI 子系统（NodeCanvas 行为树驱动，含僵尸敌人与 NPC 框架）**；**⭐ v16 AI 内聚重构**：`AINpcService` 去除 `FindObjectsByType`（NPC 显式注册）、僵尸手部命中与动画编排拆为单职责类、僵尸死亡跨端 `Despawn`；**⭐ v17 AI 增强 + 偿债**：AI 视觉检测重生（前/后视锥 + 遮挡 + 3s 记忆，逆转 v16 退化）、僵尸受击霸体（`SO_ZombieStats.HitStunThreshold`）、玩家轻击卡死修复、战斗态相机动画修复、`ZombieHandHit` 去除 NGO 直引（`BaseItem.GetItemData<T>` 泛型替代向下转型）；**编辑器 Play 模式无编译阻断（连续多版）**，Standalone 打包存在 1 处 P0 阻断（v17 新引入，见[已知限制](#已知限制)） |
| 🚧 进行中 | 战斗手感打磨（**连段 / 蓄力流程已下沉至 Animator Controller + 动画 Clip 事件驱动，攻击态薄化为胶水**；蓄力重击倍率 / 连段窗口）；**`SO_Weapon` 动画覆盖控制器（`OverrideController`）已接通**（装备时切换武器专属动画）；**AI 敌人扩展**（僵尸已实现行为树 + NavMesh + 手部碰撞体命中走 NetworkHub 中转、死亡跨端 `Despawn`；更多敌人类型与 BT 节点复用待扩展）；**技能 / 连招系统重建**（v13 移除了 v12 的实验性实现，连段现由 Animator Controller 本地驱动，待以更稳妥方案重新设计） |
| 📋 规划中 | 死亡 / 血量数值的跨端同步补齐（僵尸死亡已 Despawn，但血量数值仍仅本地）；`NetworkHub` 按域拆分（战斗中继 / 存档）；NPC 位移客户端同步（核实预制体 `NetworkTransform`）；`Zombie` 域类去 NGO 直引（v17 已去掉 `ZombieHandHit` 的直引，仅剩 `Zombie`）、收敛 `AINpcService` 网络入口（P2-8 / P2-9）；**修复 v17 新引入的 P0 打包阻断**（`GizmoDrawer` `#if UNITY_EDITOR` 不对称，一行级）；关卡内容与示例场景 |

**架构评分**：最新一次逐文件复审（见 [`架构分析报告.md`](架构分析报告.md)，2026-08-18 v17）综合评分 **80 / 100（B 档）**——其中「构建健康度」维度因新 P0 打包阻断扣 2（4/6），「纯设计分」**76.0/94**（≈81%，较 v16 的 74.0/94 升 2.0）。v17 为「AI 视觉/霸体增强 + 僵尸域去 NGO 直引 + 玩家战斗健壮性修复」迭代：在 v16 骨架上清偿了 P2-8（`ZombieHandHit` 去 NGO import，域 NGO 类 2→1，NGO `using` 文件 10→9）、P3-1（过期注释 6→5）、P3-11（`BaseItem` 泛型）、SRP 瑕疵（`AINPC` 改 `abstract` 去 virtual 空实现），并新增 AI 视觉检测（逆转 v16 退化）、僵尸霸体、玩家轻击卡死/相机动画修复；但 `GizmoDrawer` 被 `#if UNITY_EDITOR` 包裹而其两个 `OnDrawGizmos` 调用方未守卫，引入**全新 Standalone 打包阻断（P0）**。设计维增益 +2.0 恰被构建健康 −2.0 抵消，**总分稳在 80（B 档，净变化 0）**，纯设计分升至 76.0/94。

## 已知限制

- **🔴 Standalone 打包阻断（v17 新增 P0）**：`GizmoDrawer`（`Utility/Tool/Gizmos/`）被 `#if UNITY_EDITOR` 包裹，仅编辑器编译可见；但其两个调用方 `AINPC.OnDrawGizmos` 与 `PlayerLockOnTarget.OnDrawGizmos` **未做同等守卫** → Standalone 构建时 `GizmoDrawer` 类型不存在，引用处触发 **CS0246 编译失败**。**编辑器 Play 模式不受影响**（`UNITY_EDITOR` 已定义、全仓 `using UnityEditor` 仍为 0）。修复成本极低：在两个 `OnDrawGizmos` 外包一层 `#if UNITY_EDITOR`（或把 `GizmoDrawer` 调用整体移入 `#if` 块）即可恢复发布构建，预计 +2.0 分（构建健康度 4→6 → 总分 82）。
- **数值同步缺口**：`NetworkVariable` 移除后，血量的**数值**尚未补充替代同步方案，远端目前仅能通过动画层感知状态变化；僵尸死亡已新增 `NetworkObject.Despawn` 跨端销毁（远端能感知实体消失），但**血量数值仍仅本地判定**、玩家 / `FakeMan` 死亡仍仅本地动画。
- **NPC 位移同步待确认**：敌人 AI 行为树与 NavMesh 寻路仅 Host 端运行；若 NPC 预制体未挂 `NetworkTransform`，客户端将仅见动画、不见敌人实际移动。需核对预制体并决策是否补充网络变换同步。
- **`NetworkHub` 职责偏宽**：战斗中继与存档同步两个域同驻一个类（约 242 行），已规划拆分；Provider / 事件接缝已就位，拆分为零行为变化的纯重构。
- **AI 域网络约定偏离**：仅 **`Zombie` 一个**敌人域类直引 `using Unity.Netcode`（自 v6 网络重构以来首例域类破例，路由伤害经 `NetworkHub` 外观）——**`ZombieHandHit` 已于 v17 去除 NGO 直引**（改经类型化引用 `_owner` 传递 NGO 成员，NGO `using` 文件 10→9）；`AINpcService` 直连 `NetworkManager.Singleton` 绕过 `NetworkService` 外观（引入第二个 NGO 入口）。两者均为小债，已列入改进路线（P2-8 / P2-9）。**✅ 已修复**：v16 中 `AINpcService` 已去除 `FindObjectsByType` 隐式查找，改为 NPC 显式 `RegisterNpc`/`UnregisterNpc`（恢复"零隐式查找"记录）。
- **原型残留**：`Player` 中仍保留少量 Inspector 测试字段（如 `_defaultWeapon`，作者确认是有意保留的测试脚手架，待拾取系统落地后替换为数据库驱动）与 `TestButton` 调试入口（承载了"返回主菜单"真实功能），正式数据链路完善后清理。
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
