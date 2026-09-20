# SUI / SilkyUIFramework 开发与复用指南

本文供在 **没有历史对话上下文** 的情况下接手 KL UI 开发的 AI 和开发者使用。先读「快速规则」与「完整入门示例」，再按任务查阅布局、输入、绘制和控件章节。

**依据：2026-09-18 本机 SilkyUIFramework 源码，`build.txt` 版本 `0.3.0`，Git HEAD `ca2cf22`。** 已阅读 `SilkyUIFramework.sln` 中的框架项目及关联的 SilkyUIAnalyzer 生成器，并对照 KL 已有实现。本文描述的是该源码快照的实际行为；升级前置后，请重新核对相关实现。示例经过源码 API 核对，第 3 节完整 C# 示例另已通过隔离编译；未在游戏中完成交互、缩放和渲染测试。

## 目录

- [1. 给下一次 AI 的快速规则](#1-给下一次-ai-的快速规则)
- [2. 项目关系、依赖与命名空间](#2-项目关系依赖与命名空间)
- [3. 完整入门示例：可拖动、可关闭的滚动窗口](#3-完整入门示例可拖动可关闭的滚动窗口)
- [4. 注册、实例获取与生命周期](#4-注册实例获取与生命周期)
- [5. 尺寸、盒模型与定位](#5-尺寸盒模型与定位)
- [6. Flexbox 布局与常用组合](#6-flexbox-布局与常用组合)
- [7. 元素树、可见性与层级](#7-元素树可见性与层级)
- [8. 鼠标、焦点、事件冒泡与拖动](#8-鼠标焦点事件冒泡与拖动)
- [9. 滚动容器](#9-滚动容器)
- [10. 常用控件](#10-常用控件)
- [11. 绘制、裁剪、缩放与动画](#11-绘制裁剪缩放与动画)
- [12. 数据绑定与命令](#12-数据绑定与命令)
- [13. 可选的 XML 布局与生成器](#13-可选的-xml-布局与生成器)
- [14. KL 现有 UI 的参考价值与边界](#14-kl-现有-ui-的参考价值与边界)
- [15. 故障排查、验证与源码导航](#15-故障排查验证与源码导航)

## 1. 给下一次 AI 的快速规则

1. **普通窗口继承 `BaseBody`，添加 `[RegisterUI(...)]`；复用组件继承 `UIElementGroup` 或具体控件。** 不要照搬旧版 `BasicBody`、`BasicElements` 命名。
2. **SUI 元素不是 `Terraria.UI.UIElement`。** 使用 `AddChild` / `.Join(parent)`，不用原版的 `Append`、`UIState.Activate()`、`UserInterface.SetState()`、`Recalculate()`。
3. **从 `SilkyUIRenderSystem.Instance.TryGetInstance<T>(out ...)` 获取当前已挂载实例。** `new MyBody()` 不会把窗口注册到绘制栈；从 DI 直接解析普通 Body 也可能新建另一份实例。
4. **注册 UI 默认开启。** 需要按键打开的窗口在构造函数设置 `Enabled = false`；关闭后自身 `Update` 不再运行，打开逻辑必须在外部。
5. **`Join()` 会同步调用子元素初始化。** 子元素 `OnInitialize()` 需要的参数应在 `Join` 之前设置；不要假设初始化推迟到第一帧。
6. **`UIView` / `UIElementGroup` 默认宽高为 0，`FitWidth` / `FitHeight` 默认 false。** 创建空容器后必须给尺寸或启用内容适应。`UITextView` 的 Fit 默认都为 true。
7. **`SetWidth(100)` 只改像素，不会清除原来的百分比。** 要固定 100，写 `SetWidth(100f, 0f)`；位置重设也应明确清除旧百分比与对齐。
8. **百分比用小数：`1f` 是 100%，`0.5f` 是 50%。** 居中通常用 `alignment: 0.5f`，不是 `percent: 0.5f`。
9. **默认 `Relative` 会叠加 Flex 的 `LayoutOffset`。** 需要按父容器坐标任意摆放时用 `Absolute`；不要靠相对偏移假装网格布局。
10. **明确一条轴上是谁决定尺寸。** 父级 `FitWidth = true` 与子级 `Width = 100%` 会形成不合理的尺寸依赖；列表通常固定宽度、内容适应高度。
11. **普通按钮优先使用 `LeftMouseClick`。** 装饰文本/图标设 `IgnoreMouseInteraction = true`，避免改变最深命中对象。
12. **滚动内容添加到 `scrollView.Container`。** `SUIScrollView` 本身已包含 Mask 和 ScrollBar，不能把它当普通列表直接塞内容。
13. **尺寸与位置属性会自动标脏。** 不要在每帧重建 UI 或无条件执行完整布局；`Bounds` 是布局结果，不是声明输入。
14. **本版实用布局是 Flexbox。** Grid 有类型定义但实现未完成；不能照搬 CSS/WPF 的完整 API 和语义。
15. **KL 的 `ToggleButton`、`DragScrollView`、`IDraggableUI`、`KLTextView` 都不是前置框架 API。** 只有确实需要其业务或视觉行为时才使用。

## 2. 项目关系、依赖与命名空间

### 2.1 工作区位置

| 角色 | 当前本机位置 | 用途 |
| --- | --- | --- |
| KL，本文所在项目 | `D:/Documents/My Games/Terraria/tModLoader/ModSources/KL` | 使用框架，包含业务 UI 和扩展控件 |
| 前置框架 | `D:/Documents/My Games/Terraria/tModLoader/ModSources/SilkyUIFramework` | 布局、控件、输入、渲染与自动注册 |
| XML 生成器 | `D:/Documents/My Games/Terraria/tModLoader/ModSources/SilkyUIAnalyzer` | 编译时将 XML 生成 C#；纯 C# 布局不需要它 |
| tModLoader 源码参考 | `D:/泰拉原版贴图与代码与其他工具/tModLoader` | 原版层名、图形与游戏 API |
| KL 实际构建引用的游戏安装 | `D:/Steam/steamapps/common/tModLoader` | 由上级 `ModSources/tModLoader.targets` 导入这里的 `tMLMod.targets` |

下文源码索引中的 `SUI/`、`Analyzer/`、`KL/` 分别表示上表的框架、生成器、KL 项目根目录。它们是**路径说明**，不是 C# 命名空间。迁移到其他机器时按这些角色重新定位即可。

### 2.2 KL 当前已完成的接入

`KL/build.txt` 已包含：

```text
modReferences = SilkyUIFramework
```

`KL/KL.csproj` 当前通过已编译 DLL 引用框架：

```xml
<Reference Include="SilkyUIFramework">
  <HintPath>..\SilkyUIFramework\bin\Debug\net8.0\SilkyUIFramework.dll</HintPath>
</Reference>
```

这意味着 **修改前置源码不等于 KL 马上使用新版本**。编译时的 DLL 与游戏加载的前置模组必须匹配。当前 KL 没有显式引入 SilkyUIAnalyzer，也没有显式配置 SUI XML 为 AdditionalFiles；新 UI 默认采用纯 C# 最直接。

框架项目目标是 `net8.0`，使用 `LangVersion=preview`，且本快照实际包含 `field` 关键字和 extension block 等新语法。**运行时目标为 .NET 8 不代表 .NET 8 SDK 能编译此源码。** 重建前置时需支持这些语法的编译器及项目要求的 ModBuilder 配置。不要为了写一个 KL UI 顺手变更前置构建链。

### 2.3 常用 using

```csharp
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SilkyUIFramework;
using SilkyUIFramework.Attributes;
using SilkyUIFramework.Elements;
using SilkyUIFramework.Extensions; // Join
using SilkyUIFramework.Layout;     // FlexDirection、各对齐枚举
using Terraria;
using Terraria.ModLoader;
```

动画在 `SilkyUIFramework.Animation`；SDF 绘图在 `SilkyUIFramework.Graphics2D`。KL 在 `KL.cs` 中已有一些全局 using，但独立示例或新模组应自行导入。`Size`、`Margin`、`Dimension`、`Anchor` 是 SUI 类型；注意避免其他 UI 库的同名类型冲突。

## 3. 完整入门示例：可拖动、可关闭的滚动窗口

下面是**建议新增的示例**，并非 KL 已存在的类。可保存为 KL 的一个新 `.cs` 文件；复制到其他模组时修改命名空间与注册名。它只依赖前置原生能力，包含默认关闭、实例查询、F8 开关、拖动标题栏和纵向列表。

```csharp
using Microsoft.Xna.Framework;
using SilkyUIFramework;
using SilkyUIFramework.Attributes;
using SilkyUIFramework.Elements;
using SilkyUIFramework.Extensions;
using SilkyUIFramework.Layout;
using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;

namespace KL.UI.Examples;

[RegisterUI("Vanilla: Radial Hotbars", "KL: SUI Example")]
public sealed class SuiExampleBody : BaseBody
{
    private SUIScrollView _list;

    public SuiExampleBody()
    {
        Enabled = false;
    }

    protected override void OnInitialize()
    {
        base.OnInitialize();
        FitWidth = false;
        FitHeight = false;
        SetSize(520f, 360f, 0f, 0f);
        SetLeft(0f, 0f, 0.5f);
        SetTop(0f, 0f, 0.5f);
        Padding = new Margin(16f);
        Gap = new Size(0f, 10f);
        FlexDirection = FlexDirection.Column;
        Border = 2f;
        BorderRadius = new Vector4(8f);
        BorderColor = new Color(90, 110, 160);
        BackgroundColor = new Color(28, 34, 52);

        var header = new SUIDraggableView(this)
        {
            Width = new Dimension(0f, 1f),
            Height = new Dimension(32f),
            Padding = new Margin(8f, 4f),
            CrossAlignment = CrossAlignment.Center
        }.Join(this);

        new UITextView
        {
            Text = "SUI 示例：拖动标题栏",
            TextScale = 0.85f,
            IgnoreMouseInteraction = true
        }.Join(header);

        _list = new SUIScrollView(Direction.Vertical)
        {
            Width = new Dimension(0f, 1f),
            Height = new Dimension(0f),
            FlexGrow = 1f,
            FlexShrink = 1f
        }.Join(this);

        // 外层视口高度由父窗口分配；内容高度由列表项决定。
        _list.Container.FlexDirection = FlexDirection.Column;
        _list.Container.FlexWrap = false;
        _list.Container.MainAlignment = MainAlignment.Start;
        _list.Container.FitWidth = false;
        _list.Container.FitHeight = true;
        _list.Container.SetWidth(0f, 1f);
        _list.Container.Gap = new Size(0f, 6f);

        for (int i = 1; i <= 20; i++)
        {
            new UITextView
            {
                Text = $"列表项 {i}",
                FitWidth = false,
                FitHeight = false,
                Width = new Dimension(0f, 1f),
                Height = new Dimension(30f),
                TextScale = 0.85f,
                TextAlign = new Vector2(0f, 0.5f),
                Padding = new Margin(8f, 0f),
                BackgroundColor = Color.White * 0.06f,
                IgnoreMouseInteraction = true
            }.Join(_list.Container);
        }

        var close = new UITextView
        {
            Text = "关闭",
            FitWidth = false,
            FitHeight = false,
            Width = new Dimension(100f),
            Height = new Dimension(32f),
            TextAlign = new Vector2(0.5f),
            BackgroundColor = new Color(65, 85, 130),
            BorderRadius = new Vector4(4f)
        }.Join(this);
        close.LeftMouseClick += (_, _) => Enabled = false;
    }

    public static bool TryToggle()
    {
        if (Main.dedServ || Main.gameMenu || SilkyUISystem.ServiceProvider == null)
            return false;
        if (!SilkyUIRenderSystem.Instance.TryGetInstance<SuiExampleBody>(out var body))
            return false;

        body.Enabled = !body.Enabled;
        return true;
    }
}

// 实际项目已有快捷键入口时，只需在那里调用 TryToggle()。
public sealed class SuiExampleKeySystem : ModSystem
{
    public static ModKeybind ToggleKey { get; private set; }

    public override void Load()
    {
        if (!Main.dedServ)
            ToggleKey = KeybindLoader.RegisterKeybind(Mod, "ToggleSuiExample", "F8");
    }

    public override void Unload() => ToggleKey = null;
}

public sealed class SuiExamplePlayer : ModPlayer
{
    public override void ProcessTriggers(TriggersSet triggersSet)
    {
        if (SuiExampleKeySystem.ToggleKey?.JustPressed == true)
            SuiExampleBody.TryToggle();
    }
}
```

进入世界后按 F8。窗口应居中，标题栏可拖动，列表可滚动，关闭后 F8 可再次打开。不要额外编写 `ModifyInterfaceLayers`、手动维护 `UserInterface` 或主动调用此 Body 的绘制入口。正式功能应为快捷键补上本地化文本。

## 4. 注册、实例获取与生命周期

### 4.1 三层结构

```text
SilkyUIRenderSystem
  ├─ 全局 SilkyUISceneStack（RegisterGlobalUI）
  └─ 按原版图层名分组的游戏内 SilkyUISceneStack（RegisterUI）
       └─ SilkyUI：RootNode、TransformMatrix、ScenePriority
            └─ BaseBody：一个窗口/界面的根
                 └─ UIElementGroup：容器
                      └─ UIView / UITextView / SUIImage / ...
```

`UIView` 是最基本的布局、绘制和事件单元。`UIElementGroup` 增加子元素、Flex 布局和裁剪。`BaseBody` 增加启用状态、原版输入占用、屏幕变化监听、独立根布局、离屏绘制等能力。

### 4.2 注册方式

```csharp
[RegisterUI("Vanilla: Radial Hotbars", "MyMod: InventoryPanel", priority: 0)]
public class InventoryPanel : BaseBody { /* ... */ }
```

`RegisterUI` 的完整参数为：

```text
layerNode, name, priority = 0, interfaceScaleType = InterfaceScaleType.UI
```

单名称重载 `[RegisterUI("MyMod: InventoryPanel")]` 默认挂在 `Vanilla: Radial Hotbars`。注意一个字符串是 **UI 名称**，不是图层节点名；需要指定节点时用两个字符串或命名参数。

游戏内 UI 插到目标原版层的**后面**；层名找不到就不会插入。不要编造原版层名。普通 UI 只在非主菜单状态更新和参与游戏内命中。

`[RegisterGlobalUI(name: "MyMod: GlobalOverlay", priority: 0)]` 是包括主菜单场景在内的全局 UI：在系统初始化时创建，借由光标绘制钩子绘制；先于游戏 UI 参与命中判断。不要把所有普通窗口都注册成全局 UI。

本版注册扫描使用反射查找 `BaseBody` 派生类，没有在收集函数中显式过滤抽象类。**把注册特性放在可实例化的具体根类上**，通用抽象基类不注册。特性具有继承语义；注册类继续派生时要核对是否产生额外注册实例。不要同时给一个 Body 添加两种注册特性。

### 4.3 实例的创建时间和寿命

1. 客户端 `SilkyUISystem.Load()` 收集模组类型并建立服务容器。
2. `PostSetupContent()` 收集注册 UI，创建全局 UI 与游戏内空栈。
3. `SilkyUIPlayer.OnEnterWorld()` 调用 `ReloadSilkyUIStacks()`：清空旧游戏栈、使旧 Body 退树、重新解析并挂载游戏内 Body。
4. `RegisterUI` Body 使用 **Transient** 生命周期，因此重新进入世界会产生新的 Body。
5. `RegisterGlobalUI` Body 使用 **Singleton** 生命周期，不会随同游戏内栈一起重建。

实际业务应在进入世界后查询普通 UI。专用服务器没有 UI 服务容器，不要访问 `SilkyUIRenderSystem.Instance` 或创建依赖 GraphicsDevice 的 Body。静态缓存普通 Body、跨世界保留事件订阅或把玩家持久数据存进窗口都不合适。

推荐获取方式：

```csharp
if (!Main.dedServ && SilkyUISystem.ServiceProvider != null &&
    SilkyUIRenderSystem.Instance.TryGetInstance<MyBody>(out var body))
{
    body.Enabled = true;
}
```

这里的 `MyBody` 表示你自己已注册的具体类。`TryGetInstance<TBody>` 使用 `is TBody` 匹配，所以也可能返回派生类；需要所有匹配实例时用 `GetInstances<TBody>()`。不要用 `ModContent.GetInstance<MyBody>()` 获取 SUI Body。

### 4.4 初始化、入树和更新

| 回调/入口 | 时机与用途 | 注意事项 |
| --- | --- | --- |
| 构造函数 | 默认配置、保存参数、创建可独立使用的组件 | Body 此时尚未连接到 SilkyUI；避免依赖当前树 |
| `protected override OnInitialize()` | 每个对象只执行一次，适合构造子树 | 调用过 `Join` 就可能已经执行，`Enabled=false` 也不阻止初始化 |
| `protected override OnEnterTree()` | 与 SilkyUI 建立连接时 | 可订阅外部事件；动态加入已挂载父级时可能先于自己的 OnInitialize |
| `protected override OnExitTree()` | 移除/旧栈清空时 | 解除外部订阅、清理本次入树状态 |
| `protected override Update(GameTime)` | 逻辑更新 | 不保证已经完成本帧布局；根关闭后不执行 |
| `protected override UpdateStatus(GameTime)` | 绘制前，第一轮布局之后 | 适合读取 Bounds、更新悬停动画；保留 base 调用 |
| `protected override Draw(GameTime, SpriteBatch)` | 绘制自身 | 通常先调用 base 绘制背景边框，再画自定义内容 |
| `public override DrawChildren(...)` | 绘制子元素 | base 负责子项顺序与裁剪；之后可画覆盖层 |

挂根时 `SetRoot()` 先 `Initialize()`，再递归入树。动态 `AddChild()` 则先设置父级和数据上下文、调用入树，再初始化子项。**不要假设所有场景下 OnInitialize 与 OnEnterTree 的先后都相同。**

`SilkyUI.Draw()` 的正常次序：

```text
Initialize
→ Enabled 检查
→ UpdateLayout → UpdatePosition → UpdateElementsOrder
→ HandleUpdateStatus（含子元素）
→ 再次 Enabled 检查
→ UpdateLayout → UpdatePosition → UpdateElementsOrder
→ HandleDraw
```

这解释了为何可在 `UpdateStatus` 中根据尺寸更新视觉配置，并在同次绘制前完成第二次布局。不要在 `Draw` 内反复变更结构或尺寸；那会错过本轮布局，容易抖动或延后一帧。

## 5. 尺寸、盒模型与定位

### 5.1 Dimension 与 Set 方法

```text
Dimension.CalculateSize(available) = Pixels + available × Percent
```

```csharp
view.Width = new Dimension(120f);          // 120 逻辑像素
view.Width = new Dimension(0f, 1f);        // 父级可用宽度的 100%
view.Width = new Dimension(-20f, 1f);      // 父级可用宽度减 20
view.SetWidth(120f, 0f);                   // 清除旧百分比，固定宽度
view.SetSize(320f, 180f, 0f, 0f);          // 宽像素、高像素、宽比例、高比例
view.SetMinWidth(80f, 0f);
view.SetMaxWidth(600f, 0f);
```

上述片段假设 `view` 是已有 `UIView`。`SetWidth` 等的参数是可空数值，省略的部分保留原值；`SetSize(100, 100)` 也不会清除已有百分比。`Dimension`、`Anchor`、`Margin` 是只读结构，应用新值、`With(...)` 或 Set 方法，不要尝试修改 `view.Width.Pixels`。

### 5.2 三套 Bounds 与盒模型

| 数据 | 含义 | 常见用途 |
| --- | --- | --- |
| `InnerBounds` | 内容区域，不含 Padding、Border、Margin | 放文字、纹理，计算子级可用区域 |
| `Bounds` | 内容 + Padding + Border | 默认背景绘制和 `ContainsPoint` 命中范围 |
| `OuterBounds` | Bounds + Margin | Flex 排列和外部占位 |

横轴关系：

```text
Bounds.Width = InnerBounds.Width + Padding.Left + Padding.Right + 2 × Border
OuterBounds.Width = Bounds.Width + Margin.Left + Margin.Right
Bounds.Position = OuterBounds.Position + Margin 的左上偏移
InnerBounds.Position = Bounds.Position + Border + Padding 的左上偏移
```

默认 `BoxSizing.Border`：声明 `Width=100` 指 `Bounds.Width=100`。`BoxSizing.Content`：声明宽度指内容宽度，因此最终 Bounds 会再加 Padding 和 Border。

`Margin` 构造参数顺序为 **左、上、右、下**，双参数为 **水平、垂直**：

```csharp
view.Margin = new Margin(8f);
view.Padding = new Margin(horizontal: 12f, vertical: 6f);
view.Padding = new Margin(left: 12f, top: 6f, right: 20f, bottom: 10f);
```

不要套用 CSS 四值的「上右下左」顺序。边框透明也仍占据布局尺寸；要取消占位，设置 `Border = 0f`。不要只把颜色改透明。

`Bounds` 是运行时布局结果，在构造或初始化阶段通常还为零或旧值。需要创建时的声明宽度可读 `Width.Pixels`，但它不等价于最终宽度；要做精确位置计算，等布局后再读 Bounds。

### 5.3 Fit 的实际含义

- 容器 `FitWidth/FitHeight=true`：该轴尺寸由流内子元素的占位及布局决定，声明 Width/Height 不再是这一轴的普通固定输入。
- 文本 Fit 由文字测量决定；图片 Fit 由原始纹理尺寸决定。
- `Absolute` / `Fixed` 子项不参与父级内容尺寸适应。
- 流内测量时，父级某轴为 Fit，该轴给子级的百分比可用尺寸会按 0 处理。这不是浏览器那样完整的循环约束求解。
- `MinWidth/MaxWidth/MinHeight/MaxHeight` 仍是尺寸约束。不要用「同时设 Fit 和 Width」含糊表达最小尺寸。

常见可靠组合：**固定宽 + FitHeight 的段落**、**固定宽高的视口 + FitHeight 的列表内容**、**FitWidth 的水平条 + 固定宽度图标**。

### 5.4 Anchor 与对齐

```text
Anchor.CalculatePosition(available, self)
    = Pixels + available × Percent + (available - self) × Alignment
```

定位使用自身 `OuterBounds` 尺寸参与对齐。`percent=0.5` 把左边缘放到父级中点；`alignment=0.5` 才将元素整体居中。

```csharp
view.Positioning = Positioning.Absolute;
view.SetLeft(0f, 0f, 0.5f);   // 相对父内容区水平居中
view.SetTop(0f, 0f, 0.5f);    // 相对父内容区垂直居中
view.SetLeft(-8f, 0f, 1f);    // 距父内容区右边缘 8
view.SetTop(8f, 0f, 0f);      // 距父内容区顶部 8
```

### 5.5 Positioning 对照

| 模式 | 是否参与 Flex/父级 Fit | 位置来源 |
| --- | --- | --- |
| `Relative`（默认） | 是 | 父 InnerBounds + 父 ScrollOffset + LayoutOffset + Anchor + DragOffset |
| `Static` | 是 | 父 InnerBounds + 父 ScrollOffset + LayoutOffset；忽略 Anchor 和 DragOffset |
| `Absolute` | 否 | 父 InnerBounds + Anchor + DragOffset |
| `Fixed` | 否 | 屏幕 UI 逻辑空间 + Anchor + DragOffset |
| `Sticky` | 是 | 先按 Relative 计算，再用 StickyType/Sticky 限制边界 |

`Sticky` 的 `Vector4` 顺序是左、上、右、下；通过 `StickyType` 位标志启用对应边界。它是本实现的钳位逻辑，不应假设等同 CSS 的全部 sticky 行为。

`Absolute` 不直接加父级 `ScrollOffset`，但仍会随父级自身位置移动；`Fixed` 的位置相对屏幕。不过 **Fixed 子项的百分比尺寸测量仍可能使用父 InnerBounds**，不能因名字就假设尺寸也完全按屏幕计算。需要屏幕百分比窗口时优先使用根 BaseBody。

`LayoutOffset` 是布局器生成的位置，通常不要手改；`DragOffset` 是布局之外的拖动/视觉平移。对流内元素写 `SetLeft(100)` 只是偏移其排列位置，后续兄弟的占位不会跟着被重新解释成绝对坐标。

## 6. Flexbox 布局与常用组合

### 6.1 默认值与含义

| 属性 | UIElementGroup 默认值 | 说明 |
| --- | --- | --- |
| `LayoutType` | `Flexbox` | 本版可实际使用的标准布局 |
| `FlexDirection` | `Row` | Row 主轴为 X，Column 主轴为 Y |
| `FlexWrap` | `false` | 是否换行/换列 |
| `Gap` | `(0,0)` | 横向/纵向间隔；类型为 `Size` |
| `MainAlignment` | `Start` | Start、End、Center、SpaceEvenly、SpaceBetween |
| `CrossAlignment` | `Start` | 单个子项在所在行的交叉轴对齐；支持 Stretch |
| `CrossContentAlignment` | `Stretch` | 多条布局线在整个交叉轴上的分布 |
| 子项 `FlexGrow` | `0` | 有剩余空间时按权重增长 |
| 子项 `FlexShrink` | `0` | 空间不足时按权重收缩 |

`BaseBody` 额外设为 **Column、Gap=10、固定屏幕定位、480×270、Border=2、白色 25% 背景**。它不是无样式的全屏透明层。写新的窗口/HUD 时明确覆盖这些默认值。

本版收缩按 `FlexShrink` 权重分配，不是完整 CSS 的 basis 加权规则。增长/收缩受 Min/Max 约束。没有 `FlexBasis`、`AlignSelf`、反向布局、baseline 和 SpaceAround API。

### 6.2 三个常用容器配方

以下是可放入已存在的 `OnInitialize` 中的片段，假设 `this` 为容器：

```csharp
// 固定宽度、自动长高的纵向内容。
var column = new UIElementGroup
{
    Width = new Dimension(0f, 1f),
    FitWidth = false,
    FitHeight = true,
    FlexDirection = FlexDirection.Column,
    FlexWrap = false,
    Gap = new Size(0f, 8f)
}.Join(this);

// 水平工具条：固定宽高，内容左右分布且垂直居中。
var toolbar = new UIElementGroup
{
    Width = new Dimension(0f, 1f),
    Height = new Dimension(40f),
    FlexDirection = FlexDirection.Row,
    MainAlignment = MainAlignment.SpaceBetween,
    CrossAlignment = CrossAlignment.Center
}.Join(this);

// 固定可用宽度的图标网格，使用 Flex 换行。
var icons = new UIElementGroup
{
    Width = new Dimension(0f, 1f),
    FitWidth = false,
    FitHeight = true,
    FlexDirection = FlexDirection.Row,
    FlexWrap = true,
    MainAlignment = MainAlignment.Start,
    CrossContentAlignment = CrossContentAlignment.Start,
    Gap = new Size(8f, 8f)
}.Join(this);
```

图标网格中的每个图标应有明确宽高。布局初次测量时，Row 换行要求非 Fit 的宽度，Column 换列要求非 Fit 的高度；后面的尺寸分配阶段还会重新计算换行，因此不要通过 Fit 与 Wrap 的组合制造不明确的主轴空间。

**填满剩余空间**：父容器该轴尺寸明确，子项主轴初始尺寸设 0，`FlexGrow=1`，必要时 `FlexShrink=1`。完整示例中的列表就是这种模式。两个各为 100% 宽度的子项并列时，必须主动给收缩能力或重新设计尺寸，否则默认会溢出。

`MainAlignment=SpaceBetween/SpaceEvenly` 会重新分配主轴间距，不要把它当作固定 Gap 的简单累加。要求间距严格固定时用 `Start + Gap`。

### 6.3 布局自动更新

常规尺寸、边距、文本、Fit、布局方向变化会 `MarkLayoutDirty()`；Anchor/DragOffset 变化主要标记位置脏。流内变化向父级传播；脱流元素通常独立布局。`BaseBody` 保证执行完整根布局管线。

完整布局依次完成：测量 → 子级宽度调整 → 因换行等重新计算高度 → 子级高度调整 → 写 LayoutOffset。随后 `UpdatePosition()` 算出坐标。

普通业务只设属性。如果自定义组件的测量依赖额外字段，字段变化时自行 `MarkLayoutDirty()`。只有确实需要**本次调用立刻使用新边界**时，才考虑从正确根节点执行布局/位置更新；不要把 `MarkLayoutDirty(true)` 或递归布局作为每帧万能修复。

## 7. 元素树、可见性与层级

### 7.1 添加与删除

```csharp
parent.AddChild(child);          // 返回 void
var label = new UITextView().Join(parent); // 返回原本的具体类型
parent.AddChild(child, index: 0);
child.RemoveFromParent();
parent.RemoveChild(child);
parent.RemoveAllChildren();
```

一个元素只有一个父级。添加已在其他容器中的元素会先移除旧父级，加入自己或祖先会抛异常。`Children` 包含全部直接子项；`ChildrenCache` 是布局后过滤 Invalid 的缓存，不能当作即时完整树。

`Join` 之后才赋值并非普遍错误，但若该属性用于 `OnInitialize` 建树，就太晚了：

```csharp
// 正确：把初始化所需配置放在 Join 前。
var toggle = new KL.UI.ToggleButton
{
    ToggleSize = new Vector2(56f, 28f),
    ThumbDiameter = 22f
}.Join(parent);
```

如果需要建树后的对象立即可用，可在组件构造函数中建树，或设计显式刷新方法；不要要求外部调用框架的 internal `Initialize()`。

### 7.2 隐藏与禁用是不同操作

| 需求 | API | 实际语义 |
| --- | --- | --- |
| 关闭整个根窗口 | `BaseBody.Enabled=false` | 根不更新、不绘制、不参与新命中 |
| 根保持绘制但暂停命中 | 覆写 `BaseBody.IsInteractable` | 命中入口跳过这个根，适合过渡动画 |
| 子项退出布局与有效更新/绘制 | `child.Invalid=true` | 下一次缓存更新时排除，不删除 Children 中的对象 |
| 仅自身不作为命中目标 | `IgnoreMouseInteraction=true` | 容器的子项仍可命中，事件仍可能从子项冒泡过来 |
| 整个子树不参与命中 | `DisableMouseInteraction=true` | 仍占布局、仍绘制 |
| 只变透明 | 设置颜色 | 仍占位、仍可命中；文本、图片等还有各自颜色 |
| 完全移除子树 | `RemoveFromParent()` | 退树并从父列表移除 |

`UIView` 没有通用 `Visible` 或 `Enabled` 属性，不能直接套用其他 UI 库的写法。`Invalid` 不是 `OnExitTree`，不要依赖隐藏动作自动解除业务订阅。正在按下的控件还可能收到原按下目标的 MouseUp；隐藏/关闭时应清理业务拖动状态。

### 7.3 层级

- **同一父容器**：`ZIndex` 为 int，越大越后绘制、越先命中；同值保持元素顺序，后添加者通常覆盖前添加者。
- **同一 UI 栈**：`ScenePriority` 从高到低排列，高优先级先命中并显示在上层。
- 点击会 `Activate()` / BringToFront，同优先级下可置顶；优先级排序仍优先于点击顺序。
- **不同原版层**：由图层插入位置决定。一个 UI 的高 priority 不代表能跨越所有原版层或全局栈。

子项 ZIndex 不能突破祖先的裁剪，也不能使其变成独立顶级窗口。`BaseBody.GetElementAt()` 始终先检查根的 `ContainsPoint()`，默认要求鼠标位于根 Bounds 内；覆写 ContainsPoint 可以改变根的命中形状。根外可见的子项不一定能点击，给浮层设计足够的根区域。

## 8. 鼠标、焦点、事件冒泡与拖动

### 8.1 事件签名

```csharp
button.LeftMouseClick += (UIView sender, UIMouseEvent evt) =>
{
    // sender：当前接收事件的节点；evt.Source：最初被命中的节点。
};

public override void OnLeftMouseDown(UIMouseEvent evt)
{
    base.OnLeftMouseDown(evt);
    if (evt.Source != this) return;
    // 只处理按在自身上的动作。
}
```

第一段是事件订阅；第二段是放在 UIView 派生类中的覆写。可用事件包括左/中/右键 Down、Up、Click，MouseEnter、MouseLeave、MouseWheel、GotFocus、LostFocus。

`MouseMove` 虽有事件和 On 方法，当前输入分发器未见主动调用，不能仅凭声明就依赖持续 MouseMove 回调。需要跟随鼠标时可在适合的更新回调中读取位置。

### 8.2 冒泡与 Click 判定

基础 `OnLeftMouseDown` 会：设置 `LeftMousePressed` → 调用事件订阅者 → 将 `evt.Previous` 更新为自己 → 调用父级同名方法 → 执行自己的 Command。其他许多事件也逐级冒泡。

- `evt.Source` 始终是最初命中者，`evt.Previous` 是冒泡路径上的上一节点。
- `sender` 不是固定的最初命中者，而是触发当前订阅的元素。
- 没有通用的 `Handled`、`StopPropagation()` 或 `CaptureMouse()` API。
- 覆写时跳过 base 会同时失去内部状态更新、事件通知和冒泡；不要为避开父级行为随意这么做。
- 父容器想区分空白区点击与子按钮点击，应检查 `evt.Source`，必要时检查目标祖先关系。

输入系统记录按下时的**最深目标**。松开时 MouseUp 发给原按下目标，即使鼠标已经移开；只有松开时 `HoverTarget` 仍为**同一对象**才会发 Click。装饰子项忽略命中能让整个按钮拥有稳定的点击目标。

悬停是沿事件链传递的状态，并不是每个控件独立检测自己的 Bounds。从一个子项移到另一个子项可能让父级收到 Leave 再 Enter；不要在父级 Leave 无条件清空整个拖动或打开状态。

### 8.3 原版输入与焦点

`BaseBody.UpdateStatus()` 在根处于悬停状态时，默认通过 `mouseInterface=true` 阻止使用物品，并锁定原版鼠标滚轮。`AvailableItem` / `AvailableScroll` 是 protected virtual 属性，默认为 false；确实需要透传时在派生根中设置或覆写。

`IgnoreMouseInteraction` 与「不冒泡」「不占用原版鼠标」不是同义词。全屏透明且自身可命中的根会拦截整屏鼠标；纯展示 HUD 通常需要让对应展示树不参与命中。

焦点由输入系统在鼠标按下时更新。`SUIEditText` 默认 `OccupyPlayerInput=true`，框架管理文字输入、IME 和原版文字输入接管。业务快捷键仍应考虑玩家正在输入的情况。

### 8.4 拖动

优先使用框架 `SUIDraggableView` 作为窗口标题栏：

```csharp
var titleBar = new SUIDraggableView(window)
{
    Width = new Dimension(0f, 1f),
    Height = new Dimension(32f)
}.Join(window);
```

这里 `window` 是要拖动的 `UIElementGroup`/`BaseBody`。`ControlTarget` 不能为空；无参数构造不会默认拖动自己。内置拖动要求 `evt.Source == titleBar`，因此标题文字和图标通常设 `IgnoreMouseInteraction=true`，关闭按钮则保留交互。

拖动写入目标 `DragOffset`，可用 `DragIncrement` 设置步进。`ConstrainInParent=true` 只在目标确实有 Parent 时限制于父 InnerBounds；顶级 Body 没有父级，不会因此自动限制在屏幕内。

KL 的 `IDraggableUI` 是另一种方案，直接修改 Left/Top，并对技能图标、滚动条等做业务优先级判断。不要同时给同一目标启用两种拖动机制。

## 9. 滚动容器

### 9.1 内部结构和默认行为

```text
SUIScrollView
  ├─ Mask : SUIScrollMask（OverflowHidden=true）
  │    └─ Container : SUIScrollContainer（实际内容添加在这里）
  └─ ScrollBar : SUIScrollbar
```

默认方向为 Vertical。**默认内容布局却是 Row + FlexWrap=true + SpaceBetween + FitHeight=true**，适合可换行的项目集合，不是单列文本列表。纵向列表请按完整示例显式改成 Column / 不换行 / Start。

滚动范围由 Mask 的布局阶段按视口和内容 OuterBounds 计算。内容必须在滚动方向真正大于视口；把内容也固定成视口高度通常没有滚动范围。

### 9.2 横向列表配方

下面片段假设外部已有明确尺寸的 `parent`：

```csharp
var horizontal = new SUIScrollView(Direction.Horizontal)
{
    Width = new Dimension(0f, 1f),
    Height = new Dimension(72f)
}.Join(parent);

horizontal.Container.FlexDirection = FlexDirection.Row;
horizontal.Container.FlexWrap = false;
horizontal.Container.MainAlignment = MainAlignment.Start;
horizontal.Container.FitWidth = true;
horizontal.Container.FitHeight = false;
horizontal.Container.SetHeight(0f, 1f);
horizontal.Container.Gap = new Size(6f, 0f);

// 固定尺寸 item.Join(horizontal.Container)，由这些项撑出总宽度。
```

`Direction.Horizontal` 会改变外壳方向和滚动条形状，但不会替你把内容的 FitWidth、FitHeight、FlexWrap 改成上面的配置。水平内容宽度自适应时，不要再让其子项靠百分比宽度撑开父级。

### 9.3 滚动控制 API

```csharp
scroll.ScrollBar.VScrollBy(40f);                // 平滑向下滚动
scroll.ScrollBar.HScrollBy(40f);                // 平滑向右滚动
scroll.ScrollBar.ScrollByTop();                 // 动画回到开头
scroll.ScrollBar.ScrollByEnd();                 // 动画到结尾
scroll.ScrollBar.SetScrollPosition(Vector2.Zero); // 同步当前/起始/目标位置
Vector2 range = scroll.ScrollBar.GetScrollRange();
```

`TargetScrollPosition` 是动画目标，`CurrentScrollPosition` 是当前值。直接只写 Current，可能在下一次滚动条绘制时被动画覆盖；立即跳转使用 `SetScrollPosition`。滚动范围尚未布局完成时可能仍为默认值，不能过早调用 ScrollByEnd 并假设已经滚到底。

容器实际 `ScrollOffset = -CurrentScrollPosition`。当前滚动动画位置在滚动条 Draw 中推进，若读取其他节点 Bounds 做拖放，要留意更新阶段与绘制阶段的时序。

嵌套滚动通过 `UIScrollWheelEvent.ScrollElement` 与 `LockScroll(view)` 协作：内层能滚时锁定，达到边界时可向上冒泡。修改滚轮处理前保留这个机制。

默认裁剪发生在 Mask；不要为显示一点阴影就随意关闭遮罩。KL 的 `DragScrollView` 和技能面板调整过裁剪边界，那是特定界面的选择，不是通用初始化步骤。

## 10. 常用控件

### 10.1 文本 UITextView

```csharp
var description = new UITextView
{
    Text = "支持 [c/ffcc66:彩色文字] 与 Terraria TextSnippet。",
    FitWidth = false,
    FitHeight = true,
    Width = new Dimension(0f, 1f),
    WordWrap = true,
    TextScale = 0.85f,
    TextBorder = 0f,
    IgnoreMouseInteraction = true
}.Join(parent);
```

- 默认 `FitWidth=true`、`FitHeight=true`，默认 Font 为 `FontAssets.MouseText.Value`。
- 自动换行应有确定的可用宽度；设置 `FitWidth=false`、Width、`WordWrap=true`，再用 `FitHeight=true` 获取段落高度。
- `TextAlign=(0.5,0.5)` 表示在当前内容区居中；要让效果可见，该区域需比文字大。
- `TextColor` 控制文本颜色，`TextBorder`/`TextBorderColor` 控制文字阴影描边；`Border` 是控件矩形边框。
- `TextOffset` 是像素偏移，`TextPercentOffset` 相对于内容区，`TextPercentOrigin` 改文字原点；不等同于控件 Left/Top。
- `MaxLines` 限制行数；`MaximumCharacters` 限制字符串长度。
- `Text` 更改会标脏，常规情况下不必手动刷新。
- `ContentChanged` 签名是 `(UITextView sender, ContentChangedEventArgs e)`，新文本在 `e.Text`。

本版 `ContentChanging` 虽暴露返回值和可写 `NewText`，Text setter 没有采用该事件的返回值/修改后的 NewText；需要过滤文本时覆写 `OnContentChanging(newText, oldText)` 并返回结果，不要依赖事件返回值改写输入。

使用 KL 的自定义字体和内嵌图标时，参考 `KLTextView` 对 snippet 字体比例与基线的处理。自定义字体的 `TextScale` 应按该字体实际度量设定，不要机械复制 KL 某个页面的 `0.3f`。

### 10.2 图片 SUIImage

```csharp
var texture = ModContent.Request<Texture2D>("MyMod/Assets/Icon",
    AssetRequestMode.ImmediateLoad);
var image = new SUIImage(texture)
{
    FitWidth = false,
    FitHeight = false,
    Width = new Dimension(40f),
    Height = new Dimension(40f),
    ImageAlign = new Vector2(0.5f),
    ImageScale = new Vector2(0.5f),
    IgnoreMouseInteraction = true
}.Join(parent);
```

替换资源路径，资源名不带扩展名。`Texture2D` 属性类型是 **`Asset<Texture2D>`**；无参构造默认 30×30、Fit 关闭，传纹理构造默认按纹理适应。

**控件 Width/Height 不会自动把图片缩放到边界。** 实际图片通过 `ImageScale` 绘制。按完整纹理等比适应内容区，可在布局完成后的 `UpdateStatus` 计算：

```csharp
if (Texture2D?.Value is { } texture)
{
    float scale = MathF.Min(InnerBounds.Width / texture.Width,
                           InnerBounds.Height / texture.Height);
    ImageScale = new Vector2(MathF.Max(0f, scale));
    ImageAlign = new Vector2(0.5f);
}
```

此片段放在 `SUIImage` 派生类的方法中，并保留 `base.UpdateStatus(gameTime)`。Fit 测量采用原纹理宽高，不乘 ImageScale。`SourceRectangle` 只改变采样区域，本版 ImageOriginalSize、默认原点/对齐和 Fit 仍参考完整纹理；画图集时需要按帧尺寸自行处理或覆写绘制。

其他属性：`ImageColor`、`ImageOffset`、`ImagePercent`、`ImageOriginPercent`。图片透明不会自动禁用命中。

### 10.3 输入、开关、滑条、物品槽

| 控件 | 主要接口 | 需要知道的行为 |
| --- | --- | --- |
| `SUIEditText : UITextView` | Text、Placeholder、MaximumCharacters、ContentChanged、`OnEnterKeyDown` | 默认接管文字输入；固定输入框通常关闭两个 Fit 并给宽高 |
| `SUIToggleSwitch : UIView` | `Status`、`OnStatusChanges += bool => ...` | 默认 36×20，**按下**左键立即切换 |
| `SUISlider : UIElementGroup` | Value、Step、ValueChanged、Drag、DragCommand、Thumb、Track | Value 限制在 0～1；Step 用于 OnDrag 路径，程序直接赋 Value 只限幅，不自动吸附 |
| `SUIItemSlot : UIView` | Item、ItemChanged、ItemInteractive、DisplayItemInfo、DisplayItemStack | 默认允许交换真实物品；构造函数未提供通用槽位宽高，应自行 SetSize |
| `SUIDraggableView` | ControlTarget、DragOffset（目标）、ConstrainInParent | 标题栏方案，见拖动章节 |
| `SUICross` | 十字/关闭图形属性 | 只是控件，关闭动作需自己订阅事件 |
| `SUIDividingLine` | `Horizontal(...)` / `Vertical(...)` 工厂 | 返回线条元素；装饰线按需忽略命中 |
| `UIHeader` | Title、CloseButton、继承自 SUIDraggableView | 命名空间为 `Elements.Components`；自行指定 ControlTarget、绑定关闭事件 |

`SUISlider.ValueChanged` 与 `Drag` 为 `EventHandler<float>`，订阅示例：

```csharp
slider.ValueChanged += (_, value) => { /* value 是 0～1 的 float */ };
edit.ContentChanged += (_, e) => { /* e.Text 是当前文本 */ };
edit.OnEnterKeyDown += () => { /* 确认输入 */ };
toggle.OnStatusChanges += enabled => { /* 新状态 */ };
```

物品需求展示通常这样配置（`slot` 是已创建的 `SUIItemSlot`）：

```csharp
slot.SetSize(48f, 48f, 0f, 0f);
slot.ItemInteractive = false;   // 禁止从需求展示中拿走或换入真实物品
slot.DisplayItemInfo = true;    // 保留悬停说明，因此通常保留鼠标命中
slot.Item = previewItem;        // 提供非 null 的 Item，例如业务物品的 Clone()
```

`ItemChanged` 主要由 Item 属性更换触发；对同一对象直接修改 stack 不等于重新赋 Item，不能把它当作完整背包事务通知。物品存储、关闭处理、持久化和多人同步由业务层负责。

## 11. 绘制、裁剪、缩放与动画

### 11.1 优先使用控件自带矩形样式

```csharp
view.BackgroundColor = new Color(30, 36, 54);
view.Border = 2f;
view.BorderColor = Color.White * 0.4f;
view.BorderRadius = new Vector4(8f);
view.RectangleRender.ShadowColor = Color.Black * 0.3f;
view.RectangleRender.ShadowSize = 6f;
view.RectangleRender.ShadowBlurSize = 10f;
```

`BorderRadius` 四角顺序：**左上、右上、右下、左下**。样式依赖 SDF 绘图，不必为普通圆角矩形创建纹理。改变边框宽度请通过 `view.Border`，直接改 `RectangleRender.Border` 会绕过 UIView 的布局脏标记。

### 11.2 自定义绘制入口

```csharp
protected override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
{
    base.Draw(gameTime, spriteBatch);
    // 在 Bounds / InnerBounds 的 UI 逻辑坐标中绘制自身内容。
}
```

`UIElementGroup.HandleDraw` 先画自身再画子项。要在所有子项上方画拖动预览或覆盖装饰，可覆写 `DrawChildren` 并在 `base.DrawChildren(...)` 之后绘制；这时已经离开本容器对子项建立的裁剪分支，但外层祖先仍可能裁剪。

不要重复从 Draw 调用 DrawChildren，也不要调用整个 SilkyUI.Draw 递归绘制自己。普通 sprite 绘制直接用传入的 SpriteBatch；只有确实需要切换 Effect、顶点绘制或 RenderTarget 时才 End/Begin。

### 11.3 坐标与 UI 缩放

- 默认 `InterfaceScaleType.UI` 对应 `Main.UIScaleMatrix`；Game 对应 `Main.GameViewMatrix.ZoomMatrix`，其他情况为单位矩阵。
- 正常 SUI 布局/绘制使用 **UI 逻辑像素**。框架在相关阶段会经 `PlayerInputHelper.SetZoom` 改写 Main 的鼠标/屏幕数值；不要对回调中的坐标一律再除一次 Main.UIScale。
- `SilkyUI.TransformMatrix` 是当前树的实际绘制矩阵（元素上通过自己的 `SilkyUI` 实例访问）。世界坐标、原始设备坐标和布局坐标必须分清。
- 根屏幕布局主要采用 `GraphicsDeviceHelper.GetBackBufferSizeByUIScale()`。非默认缩放层需要额外验证布局、绘制和输入一致性。
- 当前输入系统会先缓存鼠标坐标，然后逐个 UI 命中时调用 SetZoom；不要推断任意混合缩放、旋转和平移都获得完整逆矩阵命中支持。

自己在框架外取得原始屏幕坐标并放置浮窗时，应明确转换到窗口坐标系；不同来源的鼠标位置不要混用。至少验证 UI 缩放 100% 与一个非 100% 值，以及窗口分辨率变化。

### 11.4 裁剪

`UIElementGroup.OverflowHidden=true` 开启子项裁剪。`HiddenBox` 指定区域：Outer=OuterBounds，Middle=Bounds，Inner=InnerBounds（默认）。

`IndependentRenderTarget` 默认为 true，仅开启 OverflowHidden 时有作用：先在独立渲染目标绘制，再带圆角回贴；false 主要走矩形 Scissor 路径。嵌套裁剪还会与已有 Scissor 相交。

命中裁剪与视觉裁剪并非像素级一致：普通 ContainsPoint 仍按矩形 Bounds；圆角透明角、HiddenBox.Inner 与 Bounds 的差异需要自定义命中时主动考虑。滚动容器还对直接子项做相交粗裁剪；大量超出子项自身 Bounds 的绘制可能被跳过。

原始顶点绘制/自定义 Effect 必须尊重当前 Viewport、Scissor、RenderTarget 和变换矩阵，特别是独立 RT 的视口偏移。恢复 SpriteBatch 时使用 SUI 需要的光栅状态与当前矩阵，例如在元素类中：

```csharp
spriteBatch.Begin(SpriteSortMode.Deferred,
    BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None,
    global::SilkyUIFramework.SilkyUI.ScissorRasterizerState,
    null, SilkyUI.TransformMatrix);
```

这只是已正确结束旧 batch、恢复相应图形设备状态后的**恢复片段**，不能单独插进正在运行的 batch。不要套用世界绘制中的 GameViewMatrix。KL 的顶点绘制工具可参考，但需要单独核对其是否符合当前裁剪场景。

### 11.5 悬停与窗口过渡

所有 UIView 有 `HoverTimer`，基础 `UpdateStatus` 自动推进；继承控件可以这样复用：

```csharp
protected override void UpdateStatus(GameTime gameTime)
{
    base.UpdateStatus(gameTime);
    BackgroundColor = HoverTimer.Lerp(new Color(40, 50, 70), new Color(65, 85, 115));
}
```

自建 `AnimationTimer` 则需要自己调用 `Update(gameTime)`，并调用 `StartUpdate()` / `StartReverseUpdate()`。`reset: true` 只应在启动/切换时使用，不能每帧重置。`AutoAnimation<T>` / `IInterpolable<T>` 当前标有 Obsolete，不作为新代码的默认方案。

BaseBody 提供 `UseRenderTarget`、`Opacity`、`RenderTargetMatrix`、`EnableBlur`、`BlurElements`。**Opacity 和 RenderTargetMatrix 用于离屏结果回贴**，普通直绘路径不会因此自动改变整棵树。背景模糊还取决于框架模糊系统是否可用，且当前实现不在主菜单执行该模糊路径。

退出动画期间保持 Enabled 为 true，动画结束再关闭；可以单独覆写 IsInteractable 阻止过渡中的点击。不要先 Enabled=false 再指望自身继续推进关闭动画。`RenderTargetMatrix` 改变显示不等于改变布局/命中区域。

`RequestScreenshot()` 请求下一次绘制时截图，路径由 `ScreenshotSavePath` 控制；默认位于 Terraria/tModLoader 文档目录下、以根类名命名。根必须实际进入绘制，关闭的根不会立即产出截图。

## 12. 数据绑定与命令

可选使用框架的轻量绑定，不必为简单窗口强行引入 ViewModel。

```csharp
panel.LocalDataContext = viewModel;
label.Bind("Title", nameof(UITextView.Text));
// label 是 panel 的子孙，且没有自己的 LocalDataContext。
```

- `LocalDataContext` 优先，否则继承 Parent.DataContext；设回 null 恢复继承。
- `DataContext` 由框架计算，不是外部赋值入口。
- `Bind(sourcePropName, targetPropName)` 的第一个参数是**源属性路径**，第二个是控件目标属性。
- 支持类似 `PlayerInfo.Name` 的嵌套路径；进入树后绑定源，退出树时取消订阅。
- 首次挂源会同步一次；持续更新依赖源及相关嵌套对象的 `INotifyPropertyChanged`。
- 是**源到控件的单向绑定**，没有自动双向回写或通用类型转换器；源/目标类型要兼容。例如数字应由 ViewModel 提供字符串显示属性。
- 输入回写可在 `ContentChanged` / `ValueChanged` 中显式调用业务方法，并避免反馈循环。
- 同一目标属性再次 Bind 会替换该目标的旧绑定。不要频繁切换整个 LocalDataContext 来模拟逐帧更新。

`UIView.Command` 类型为 `System.Windows.Input.ICommand`，参数由 protected virtual `CommandParameter` 提供，默认 null。**它在左键按下时执行，不是在 MouseClick 时执行**，而且冒泡到的祖先也可能执行自己的 Command。需要松开确认的动作直接使用 LeftMouseClick 更清楚。

普通服务可用 `[Service]` 加入框架 DI，支持 Singleton/Transient；Body 的生命周期由 RegisterUI/RegisterGlobalUI 决定，不能用额外 Service 特性改写。需要构造注入时确认依赖已注册，简单 UI 优先保持公共无参构造。

## 13. 可选的 XML 布局与生成器

### 13.1 何时需要

XML 是**编译时生成 C#**，不是运行时加载 UI 配置，也不自带热重载。纯 C# 示例不需要 XML、partial 类或 InitializeComponent。

准备在 KL 新增 XML 布局时，需要在项目文件中额外配置生成器和 AdditionalFiles；应保留已有前置 DLL 引用，避免同时重复引入同一框架程序集：

```xml
<ItemGroup>
  <ProjectReference Include="..\SilkyUIAnalyzer\SilkyUIAnalyzer.csproj">
    <OutputItemType>Analyzer</OutputItemType>
    <ReferenceOutputAssembly>false</ReferenceOutputAssembly>
  </ProjectReference>
  <AdditionalFiles Include="UI\**\*.sui.xml" />
</ItemGroup>
```

生成器本身接受 `.xml` 后缀；这里限定 `.sui.xml` 是为了只包含 UI 定义。若上层构建配置已添加同一 AdditionalFiles，应调整而非重复。采用源码 ProjectReference 编译前置是另一种接入方式，需要先满足前置的编译器与构建环境要求。

### 13.2 配套示例

XML：

```xml
<?xml version="1.0" encoding="utf-8"?>
<Body Class="KL.UI.Examples.XmlExampleBody"
      Width="360px" Height="180px" Left="50#" Top="50#"
      Padding="12" Gap="8" FlexDirection="Column">
  <Style Name="Caption" TextScale="0.85" IgnoreMouseInteraction="true" />
  <TextView Name="TitleLabel" Style="Caption" Text="XML 示例" />
  <TextView Name="CloseLabel" Text="关闭"
            FitWidth="false" FitHeight="false"
            Width="90px" Height="32px" TextAlign="0.5" />
</Body>
```

对应 C#：

```csharp
using SilkyUIFramework.Attributes;
using SilkyUIFramework.Elements;

namespace KL.UI.Examples;

[RegisterUI("Vanilla: Radial Hotbars", "KL: XML Example")]
public partial class XmlExampleBody : BaseBody
{
    public XmlExampleBody() => Enabled = false;

    protected override void OnInitialize()
    {
        base.OnInitialize();
        InitializeComponent();
        CloseLabel.LeftMouseClick += (_, _) => Enabled = false;
    }
}
```

打开方式仍是查询注册实例后设 Enabled=true。生成器生成私有 `InitializeComponent()` 和 `Name` 对应的公开只读属性；它不会自动帮你调用 InitializeComponent。

### 13.3 XML 语法与边界

| 写法 | 含义 |
| --- | --- |
| `<Body Class="完整命名空间.类名">` | 绑定本次编译中派生自 UIElementGroup 的 partial 类 |
| `<ElementGroup>` / `<View>` / `<TextView>` / `<Image>` | 来自 XmlElementMapping 的类型别名 |
| `Name="TitleLabel"` | 生成控件属性，不是运行时查找字符串 |
| `<Style Name="Card" ... />` + `Style="Card Other"` | 复用属性；不是 CSS 选择器或级联引擎 |
| `<M.Container ...>` / `<M.Mask ...>` | 配置已存在的成员属性，可向它添加子节点 |
| `Bind.Text="Title"` | 生成 `Bind("Title", "Text")` |
| `Width="100px"` / `"100%"` / `"-20px 100%"` | Dimension 解析 |
| `Left="50#"` / `"0px 0% 50#"` | Anchor 的对齐/完整三项解析 |
| `Padding="12 6"` / `"12 6 20 10"` | 水平垂直 / 左上右下 |
| `TextAlign="0.5"` / `"0 0.5"` | Vector2 数值 |

`Anchor` 当前只接受单项或完整三项，别写两项 `"10px 50%"`。XML 百分比是人类形式 `50%`，C# 中则是 `0.5f`。

成员节点例如：

```xml
<ScrollView Name="List" Width="100%" Height="120px">
  <M.Container FlexDirection="Column" FlexWrap="false" MainAlignment="Start">
    <TextView Text="内容在 Container 中" />
  </M.Container>
</ScrollView>
```

生成器为普通元素生成 `new Type()`，适用于有可无参调用构造的控件；它没有通用构造参数语法。`SUIScrollView.Direction` 只读，水平滚动应由 C# 构造或具有合适默认构造的包装控件提供，不能仅写 `Direction="Horizontal"` 期望改变方向。

实际赋值逻辑处理具有 setter 的**属性**，不要仅凭框架旧 README 就认为任意 public 字段都支持。事件、纹理 Asset 及复杂业务对象建议在 C# 中赋值。成员节点处理也要求相应成员是可识别的属性。

样式属性先展开，元素显式赋值在后；同一目标若存在 `Bind.*`，其静态赋值会被跳过。别让多个样式重复定义同一个绑定。自定义 XML 标签需 `[XmlElementMapping("UniqueAlias")]`，别名应唯一。

**生成器有多处吞掉异常/跳过未知成员的逻辑。** 拼错属性不一定会得到清晰诊断。InitializeComponent 或生成属性缺失时，检查 AdditionalFiles、根 Class 全名、partial、类型别名、可写属性和生成的 `.g.cs`，不要盲目补写一个同名空方法。

## 14. KL 现有 UI 的参考价值与边界

以下是业务代码参考，不是前置库的规范。复用前阅读对应源码和调用者，避免把技能系统的类型、字体、资产路径及初始化约束带入通用组件。

| KL 源码 | 值得参考的内容 | 复用边界 |
| --- | --- | --- |
| `SkillSystem/UI/PlayerTargetSelectorUI.cs` | 默认关闭的已注册 Body、TryGet/TryOpen、回调接口、绝对布局、非矩形命中、头像与扇区绘制 | 玩家筛选与技能语义属于业务；含专门的矩阵/渲染处理 |
| `UI/ToggleButton.cs` | 可继承组件、独立数值状态、视觉缓动、`SetValue(value, notify, animate)` | 不等于 SUIToggleSwitch；尺寸等初始化配置在 Join 前设置 |
| `SkillSystem/AbstractClass/SkillPanelUI.cs` | 多列内容区、滚动区、详情区、拖动图标覆盖绘制 | 抽象基类，注册示例被注释；固定大尺寸和 Relative 流位置补偿不是通用模板 |
| `SkillSystem/AbstractClass/DragScrollView.cs` | 拖动滚动、边界弹性、滚动提示、保留滚动条占位但隐藏其颜色 | 显式依赖技能图标拖拽优先级和 KL 资源 |
| `SkillSystem/SilkyUI/IDraggableUI.cs` | 窗口/图标拖动与嵌套拖动过滤 | KL 接口，直接改 Left/Top；其命中结果使用处需要自己注意 null 情况 |
| `SkillSystem/SilkyUI/SkillToolTip.cs` | 组合文本与技能详情区域 | 技能业务与字体依赖，不是前置 Tooltip API |
| `SkillSystem/SilkyUI/SkillUnlockFooterUI.cs` | 横向物品需求条、物品预览、按钮与数值刷新 | 需求计算/解锁属于业务；容器尺寸配置需按新场景重设 |
| `SkillSystem/SilkyUI/PanelSkillIcon.cs` | 拖拽技能图标、锁定/隐藏状态、资源使用 | 包含技能面板与预览栏联动 |
| `Drawing/Snippets/KLTextView.cs` | 自定义文字 snippet 的字体缩放与基线 | 依赖 KLTextureSnippet 和 KL FontManager |
| `Drawing/DrawHelper.vertexDraw.cs` | UI 逻辑坐标顶点绘制与 SpriteBatch 恢复 | 不同于普通世界绘制；嵌套裁剪仍需验证 |

框架自己的 `UserInterfaces/Test/TestUI.cs` 被 `#if DEBUG && false` 禁用；不能把它当作正在注册的现成功能。`SkillDetailUI.cs` 也注明暂未使用且注册被注释。引用示例前区分活跃实现与实验代码。

新需求可按以下职责拆分：根 Body 管窗口启用和整体布局；组件管自己的样式与事件；业务对象管技能/物品/玩家数据；外部入口负责打开窗口。不要把所有业务更新塞进 Draw，也不要把 UI 实例当成持久数据模型。

## 15. 故障排查、验证与源码导航

### 15.1 按现象排查

| 现象 | 优先检查 |
| --- | --- |
| UI 根本不出现 | 注册是否在具体类、是否进入世界、Enabled、图层名是否真实、编译 DLL 与游戏前置是否一致 |
| TryGetInstance 返回 false | 普通 UI 是否已随 OnEnterWorld 创建；查询类型是否对应注册类 |
| 查询 UI 时空引用 | 是否专服/加载过早/卸载后访问了 ServiceProvider；不要在静态初始化里获取 Instance |
| 容器宽高为 0 | 是否给 Width/Height 或 Fit；父 Fit 与子百分比是否互相依赖 |
| SetWidth 后仍不对 | 旧 Percent 未清除、Fit 仍开启、BoxSizing/Border/Padding、FlexGrow/Shrink、Min/Max |
| 第二个元素比预期更靠右 | Relative 叠加了 LayoutOffset；检查是否本应使用 Absolute |
| 行内看似居中但整体偏移 | 不要混用主轴 Center 与 Left.Alignment 来重复补偿 |
| 长文本不换行/高度错误 | FitWidth、可用宽度、WordWrap、FitHeight、文本缩放与字体度量 |
| 图片撑出边界或尺寸不变 | Width 只改盒子；检查 ImageScale、原始纹理/图集帧、Fit |
| 内容不滚动 | 是否加入 Container；滚动方向的内容是否真正大于 Mask；横向是否设置 FitWidth |
| 滚动位置跳回去 | 是否只写 CurrentScrollPosition 却保留旧动画目标；用 SetScrollPosition |
| 点击文字导致按钮不触发 | 最深按下/松开目标是否一致；装饰子项是否 IgnoreMouseInteraction |
| 点击子按钮却拖动父面板 | 事件冒泡；父级是否检查 evt.Source 或拖动目标优先级 |
| 透明区域挡住游戏 | 透明不等于忽略命中；检查根大小及 Ignore/DisableMouseInteraction |
| 绘制在根外的浮层无法点击 | BaseBody 先检查自身 Bounds；增加合理根区域或单独设计浮层 |
| 关闭后打不开 | 打开逻辑是否错误地放在被关闭根的 Update 内 |
| 重进世界窗口失效/回调重复 | 是否缓存了旧 Body 或未在 OnExitTree 解除外部订阅 |
| UI 缩放后鼠标与图形不重合 | 混用了设备/世界/UI 坐标，重复除 UIScale，恢复了错误矩阵 |
| 圆角裁剪内自定义图形异常 | 当前 RT、Viewport 偏移、Scissor、光栅状态和投影矩阵 |
| XML 没有生成成员 | AdditionalFiles、partial、Class 全名、别名冲突、未知属性、生成器异常被吞 |

### 15.2 实际修改 UI 后的验证顺序

1. **编译检查**：只改 KL 时从 KL 根目录运行 `dotnet build .\KL.csproj`，或使用既有 tModLoader 构建流程。若错误来自前置 DLL/SDK，先确认环境，不要靠改 UI 类型名绕过依赖问题。
2. **进入世界**：检查默认关闭/打开、按键重复开关、关窗后再次打开。
3. **布局**：短/长/空文本、无项目/单项目/大量项目、百分比大小、最小分辨率及不同 UI 缩放。
4. **输入**：按下后移出再松开、装饰子项上点击、拖动和滚动条互斥、嵌套滚轮、文本输入时快捷键。
5. **生命周期**：退出并重新进入世界；确认旧窗口引用、拖动状态、外部事件不会残留。
6. **绘制**：圆角裁剪、嵌套滚动、浮层、半透明/离屏动画、其他原版界面同时打开。

文档修改本身无需启动游戏；真正新增或改动 UI 后不能仅以 C# 编译通过替代交互验证。本文入门示例未作为项目源码自动启用，复制后需按上述步骤验证。

本次文档验证：将第 3 节代码原样提取至系统临时目录，以独立 net8.0 项目引用 KL 当前使用的前置 DLL 和 tModLoader 程序集，设置 `BuildMod=false`，编译结果为 **0 错误**。出现 2 条 `MSB3277` 警告，来自安装目录的 Microsoft.Extensions.DependencyInjection / Abstractions 8.0 与前置依赖 9.0 的版本冲突；这不等于已验证游戏运行时兼容。未编译或更改前置源码，未向游戏安装示例模组，XML 示例仅作源码与语法核对。

### 15.3 查源码的最短路径

| 想确认什么 | 源码位置 |
| --- | --- |
| 依赖、目标框架、生成器 | `SUI/SilkyUIFramework.sln`、`SUI/SilkyUIFramework.csproj`、`Analyzer/SilkyUIAnalyzer.csproj`、`KL/KL.csproj` |
| 注册参数/类型收集 | `SUI/Attributes/RegisterUIAttribute.cs`、`RegisterGlobalUIAttribute.cs`、`SUI/SilkyUIRegistrar.cs` |
| 服务寿命与构造注入 | `SUI/ServiceProviderBuilder.cs`、`SUI/Attributes/ServiceAttribute.cs` |
| 何时创建/重建实例 | `SUI/SilkyUISystem.cs`、`SUI/SilkyUIRenderSystem.cs` |
| 场景优先级/置顶 | `SUI/SilkyUISceneStack.cs`、`SUI/SilkyUILayer.cs` |
| 更新/绘制总流程 | `SUI/SilkyUI.cs`、`SUI/SilkyUIManager.cs`、`SUI/Hooks/UIHookInstaller.cs` |
| 基础属性、初始化、定位 | `SUI/Elements/UIView.cs`、`SUI/Anchor.cs`、`SUI/Dimension.cs` |
| 尺寸/盒模型/约束 | `SUI/Elements/UIView.Bounds.cs`、`SUI/AxisMetrics.cs`、`SUI/Margin.cs` |
| 子树与裁剪/命中 | `SUI/Elements/UIElementGroup.cs`、`SUI/Elements/BaseBody.cs` |
| 布局与脏标记 | `SUI/Elements/UIElementGroup.Layout.cs`、`UIElementGroup.HandleMark.cs` |
| Flex 配置与实际算法 | `SUI/Elements/UIElementGroup.Flexbox.cs`、`UIView.FlexItem.cs`、`SUI/Layout/FlexboxModule.cs`、`SUI/Layout/Flexbox/` |
| 事件及派发 | `SUI/Elements/UIView.Events.cs`、`SUI/UIMouseEvent.cs`、`SUI/SilkyUIInputState.cs` |
| 滚动 | `SUI/Elements/SUIScrollView.cs`、`SUIScrollMask.cs`、`SUIScrollContainer.cs`、`SUIScrollbar.cs` |
| 基础/离屏绘制 | `SUI/Elements/UIView.Draw.cs`、`BaseBody.Draw.cs`、`SUI/Components/RectangleRender.cs` |
| 文本与图像 | `SUI/Elements/UITextView.cs`、`SUIImage.cs`、`SUI/Components/SnippetModule.cs` |
| 数据绑定 | `SUI/Elements/UIView.DataContext.cs`、`SUI/Bindings/BindingEntry.cs`、`SUI/Common/Reflection/` |
| XML 的真实支持范围 | `Analyzer/ComponentGenerator.cs`、`ComponentGeneratorLogic.cs`、`ParseHelper.cs`、`XmlExtensions.cs` |

在对应项目根目录可用 `rg -n "方法名或属性名" -g '*.cs'` 定位。前置原 README、MigrationGuide、Layout/FlexboxModule.md 可作补充，但其中旧路径、历史命名和计划中的能力应与当前源码交叉核对。需要新增一个功能时，优先读本指南对应小节与该控件文件，再参考 KL 的业务实现。
