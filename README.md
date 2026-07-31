# MultiARPG

> A Unity-based multiplayer ARPG demonstrating a data-driven, modular architecture with a clean domain/network separation.

一个基于 Unity 的多人 ARPG 项目，侧重于**数据驱动**与**领域-网络解耦**的架构实践。项目处于早期开发阶段，当前以架构探索与技术展示为主。

## 技术栈

- **引擎**：Unity 2022 LTS（C#）
- **网络**：Unity Netcode for GameObjects（NGO）
- **数据驱动**：ScriptableObject 承载配置，配置与运行时状态分离
- **基础设施**：服务容器（`IService`）+ 组合根（`GameRoot`）、分层状态机（FSM）、进程级事件总线（EventBus，零 GC）
- **第三方**：DOTween、Odin Inspector、ParrelSync、TextMesh Pro

## 架构设计

项目采用三层结构：**领域逻辑（纯 C# / MonoBehaviour）— 服务层（`IService`）— 网络层（NGO `NetworkBehaviour`）**，依赖方向自上而下，领域层不直接引用网络库。

核心设计原则：

- **数据驱动**：角色、效果、音频、UI 等配置以 ScriptableObject 承载，配置与运行时状态分离，支持编辑器内调参。
- **服务容器与组合根**：`GameRoot` 在启动期按依赖顺序构建并注册全部全局服务，生命周期自上而下统一管理。
- **分层状态机**：玩家移动 / 跳跃 / 闪避 / 死亡等状态由泛型状态机驱动，与网络无关，可独立演进。
- **事件总线**：仅承载少数跨领域「游戏级」信号，参数为只读 `struct`（零堆分配），避免退化为事件版 Service Locator。
- **网络入口封装**：`NetworkService` 作为全项目唯一对 `NetworkManager` 的封装；同步逻辑下沉到独立的 `NetworkBehaviour` 组件，遵循「网络控制组件不反向持有领域引用」的约定。
- **领域-网络解耦（进行中）**：目标是让角色等游戏类彻底 NGO-free，可在单机 / 离线运行、可独立单元测试。

## 当前进度

| 状态 | 内容 |
|------|------|
| ✅ 已具备 | ScriptableObject 数据驱动配置体系；服务容器与组合根；分层状态机；进程级 EventBus；网络入口封装（`NetworkService`）；客户端权威的网络变换 / 动画组件；存档数据组装与写盘分离 |
| 🚧 进行中 | 领域逻辑与网络层彻底解耦（角色 / 玩家类去 NGO 化、同步组件模式推广） |
| 📋 规划中 | 更完整的内容与战斗系统；离线冒烟测试；示例场景与配套文档 |
