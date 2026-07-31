# ToolBoxHub 编辑器工具箱

一套零依赖（不依赖 Odin 等第三方插件）的纯 Unity Editor 扩展工具集合，统一放置在 `Assets/Editor/ToolBoxHub/` 下。

每个工具独立成文件夹，命名空间约定为 `ToolBoxHub.<工具名>`，结构统一为四个文件：

| 文件 | 作用 |
| --- | --- |
| `SO_<ToolName>Config.cs` | ScriptableObject 配置数据（懒加载单例，落盘 `.asset`） |
| `<ToolName>Hook.cs` | `[InitializeOnLoad]` 钩子，订阅编辑器事件 / 绘制 / 右键集成 |
| `<ToolName>Commands.cs` | 核心静态逻辑、公开 API、`[MenuItem]` 入口 |
| `<ToolName>Window.cs` | `EditorWindow` 界面 |

---

## 1. FolderColor（文件夹 / 资源上色）

在 Project 窗口为文件夹及其子文件夹染色背景，文件不染色。

- **入口**：Project 窗口右键文件夹 → `FolderColor / 染色`、`FolderColor / 清除染色`
- **使用**：
  1. 右键目标文件夹 → `FolderColor / 染色`
  2. 弹出颜色面板，点击任一色块即递归上色（含所有子文件夹）
  3. 右键 → `FolderColor / 清除染色` 可清除选中项及其子项颜色
- **文件**：`FolderColor/`（SO_FolderColorData、FolderColorProjectHook、FolderColorCommands、FolderColorWizard）

---

## 2. HierarchyIcons（层级图标）

在 Hierarchy 窗口每一行右侧绘制组件类型图标，未配置组件内置图标时自动取用引擎内置图标，重编译后自动自愈。

- **入口**：顶部菜单 `ToolBoxHub / 层级图标`
- **使用**：
  - 勾选「启用层级图标」开关总控
  - 规则列表：每行含「启用 / 组件类型名 / 图标 / 删除 X」
    - 组件类型名支持全名（`UnityEngine.Camera`）或短名（`Camera`）
    - 图标留空则自动使用组件内置图标；留自定义图标则优先生效
  - 「应用常用预设」一键加入常用组件规则；「+ 添加规则」手动追加
  - 改动即时生效（自动清缓存）
- **注意**：预设规则 icon 若残留失效引用，会回退到内置图标；个别引擎无注册图标的类型可手动指定图标兜底。
- **文件**：`HierarchyIcons/`（SO_HierarchyIconsConfig、HierarchyIconsHook、HierarchyIconsCommands、HierarchyIconsWindow）

---

## 3. AssetRenamer（批量重命名）

按前缀 / 后缀 / 查找替换 / 序号 / 大小写组合规则，对项目视图选中资产批量重命名，支持冲突预览。

- **入口**：顶部菜单 `ToolBoxHub / 批量重命名`
- **使用**：
  1. 在 Project 窗口选中要重命名的资产
  2. 设置前缀、后缀、查找 / 替换、序号开关与起始值、大小写模式
  3. 点「预览」查看 `旧名 → 新名`，冲突项标红 `[冲突]` 且不会被执行
  4. 点「执行重命名」写入
- **文件**：`AssetRenamer/`（SO_AssetRenamerConfig、AssetRenamerHook、AssetRenamerCommands、AssetRenamerWindow）

---

## 4. DependencyGraph（查找引用依赖）

分析选中资源的正向依赖（它引用了谁）与反向引用（谁引用了它），点击列表项可在 Project 中定位。

- **入口**：顶部菜单 `ToolBoxHub / 查找引用依赖`
- **使用**：
  - 选中目标资产后点「分析选中」，或直接拖入「目标资源」后点「分析」
  - 左栏「正向依赖」、右栏「反向引用」，分别列出资产路径，点击即 Ping 定位
- **文件**：`DependencyGraph/`（SO_DependencyGraphConfig、DependencyGraphHook、DependencyGraphCommands、DependencyGraphWindow）

---

## 开发约定

- 顶层命名空间 `ToolBoxHub`，各工具子命名空间 `ToolBoxHub.<工具名>`（旧 `ToolBoxHub.Editor.Tools` 已废弃）。
- 每个工具独立文件夹，所有 `.cs` / `.asset` 集中存放，不散落别处。
- 类命名：`SO_<ToolName>Config` / `<ToolName>Hook` / `<ToolName>Commands` / `<ToolName>Window`。
- UI 类（继承 `MonoBehaviour` 的窗口）以 `UI_` 开头（本工具箱窗口均为 `EditorWindow`，未使用 `UI_` 前缀）。
- 枚举以 `E` 为前缀；公开 API 用 `#region` 包裹，私有方法不包。
