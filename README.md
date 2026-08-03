# MultiARPG

> A Unity-based multiplayer ARPG demonstrating a data-driven, modular architecture with a clean domain/network separation.

一个基于 Unity 的多人 ARPG 项目，侧重于**数据驱动**与**领域-网络解耦**的架构实践。项目处于早期开发阶段，当前以架构探索与技术展示为主。

## 技术栈

- **引擎**：Unity 2022 LTS（C#）
- **网络**：Unity Netcode for GameObjects（NGO）
- **数据驱动**：ScriptableObject 承载配置，配置与运行时状态分离
- **基础设施**：服务容器（`IService`）+ 组合根（`GameRoot`）、分层状态机（FSM）、进程级事件总线（EventBus，只读 `struct` 参数、零 GC）
- **第三方**：DOTween、Odin Inspector、ParrelSync（本地双开联调）、TextMesh Pro

## 架构设计

项目采用三层结构：**领域逻辑（纯 C# / MonoBehaviour）— 服务层（`IService`）— 网络层（NGO `NetworkBehaviour`）**，依赖方向自上而下，领域层不直接引用网络库。

核心设计原则：

- **数据驱动**：角色、效果、音频、UI 等配置以 ScriptableObject 承载，配置与运行时状态分离，支持编辑器内调参。
- **服务容器与组合根**：`GameRoot` 在启动期按依赖顺序构建并注册全部全局服务，生命周期自上而下统一管理。
- **分层状态机**：玩家移动 / 跳跃 / 闪避 / 攻击 / 死亡等状态由泛型状态机驱动，状态树分层（姿态层 + 动作层），与网络无关，可独立演进。
- **事件总线**：仅承载少数跨领域「游戏级」信号，参数为只读 `struct`（零堆分配），避免退化为事件版 Service Locator。
- **网络入口封装**：`NetworkService` 作为全项目唯一对 `NetworkManager` 的封装；同步逻辑下沉到独立的 `NetworkBehaviour` 组件，遵循「网络控制组件不反向持有领域引用」的约定。
- **领域-网络解耦**：游戏领域类（Stats / Effect / Save 组装 / 装备 / 武器）保持 NGO-free，可单机 / 离线运行；仅在必要的同步组件与基类处引入 NGO 依赖。

## 项目结构

代码位于 `Assets/_Scripts`，按下述模块组织（行数为当前快照统计）：

```
Assets/_Scripts/
├── Character/              角色与玩家（约 2027 行）
│   ├── Player/             玩家总控、能力（装备 / 状态机 / 动画事件）
│   ├── Stats/              属性系统 + 网络属性同步组件
│   └── Character.cs        角色基类（含网络壳）
├── Input/                  Unity Input System（自动生成 + 封装，约 1554 行）
├── UI/                     界面层（MVP 模式，约 769 行）
│   ├── MainMenuUI/         主菜单 / 存档槽
│   ├── GeneralUI/          设置面板等
│   ├── InGameUI/           血条 / 耐力条（事件驱动）
│   └── Core/               UI 基视图与表现层基类
├── Systems/                游戏系统（约 435 行）
│   ├── Effect/             效果系统 + 伤害信息数据结构
│   └── Item/               物品 / 武器基类与实例（非泛型 BaseItem）
├── Core/                   基础设施（约 523 行）
│   ├── FSM/                分层状态机
│   ├── EventBus/           泛型事件总线
│   ├── Services/           服务容器
│   └── ObjectPool/         对象池
├── Save/                   存档系统（Host 单一数据源 + Client RPC 上报）
├── Audio/                  音频系统
├── Network/                网络同步组件（ClientNetworkTransform / Animator）
├── Scene/                  场景控制服务
└── Utility/                通用工具
```

当前整体规模：**100 个 C# 文件，约 6144 行**（含 Input System 自动生成代码）。

## 当前进度

| 状态 | 内容 |
|------|------|
| ✅ 已具备 | ScriptableObject 数据驱动配置体系；服务容器与组合根；分层状态机；进程级零 GC EventBus；网络入口封装（`NetworkService`）；客户端权威的网络变换 / 动画组件；Host 单一数据源的存档系统；MVP 模式的事件驱动 UI |
| 🚧 进行中 | 领域逻辑与网络层解耦推进；战斗 / 装备系统的网络同步方案设计 |
| 📋 规划中 | 更完整的内容与战斗系统；离线冒烟测试；示例场景与配套文档 |

## 运行环境

- Unity 2022 LTS 及以上
- 安装 NGO（Netcode for GameObjects）与相关依赖包
- 使用 **ParrelSync** 可在一个编辑器内双开客户端进行本地联调
- 第三方依赖（DOTween / Odin Inspector / TextMesh Pro）通过 Package Manager 或 Asset 导入

## 已知限制

- 装备 / 武器系统目前为单机原型：武器伤害在本地结算，装备挂载走本地权威，尚未接入 NGO 的 Server/Client 同步链路；角色基类仍保留 `NetworkBehaviour` 壳以承载少量权威判定。
- 项目尚处早期阶段，部分配置以 Inspector 测试字段临时驱动，正式的内容数据驱动链路仍在完善中。

---

欢迎就架构设计或实现细节提出 Issue 与讨论。
