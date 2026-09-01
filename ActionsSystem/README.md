# KL Action 动作系统

`ActionsSystem` 是一个基于帧时间轴的玩家动作框架。一个 `AnimAction` 持有启动时确定的总帧数和多个可并行执行的 `ActionNode`；`ActionModPlayer` 为每个玩家保存当前动作并逐帧推进；`AnimActionRegistry` 将动作类型映射为网络可传输的整数 ID；`ActionPlayerDrawLayer` 把动作的自定义绘制接入 Terraria 的玩家绘制层。

## 目录与职责

| 文件 | 职责 |
| --- | --- |
| `AnimAction.cs` | 动作基类、节点集合、生命周期回调、节点调度、动作射击辅助函数 |
| `ActionNode.cs` | 节点基类，以及手臂、腿部、单帧回调、单帧射击节点 |
| `ActionModPlayer.cs` | 当前动作状态、开始/中断/结束、每帧推进、帧表现 |
| `AnimActionRegistry.cs` | 扫描、注册、按 ID 创建动作实例 |
| `ActionPlayerDrawLayer.cs` | 四个玩家绘制层的桥接 |
| `TemplateActions/FocusCast.cs` | 由多个手臂节点组成的示例动作 |

## 最短使用方式

```csharp
ActionModPlayer actionPlayer = player.GetModPlayer<ActionModPlayer>();
actionPlayer.StartAction(new FocusCast(), interruptCurrentAction: true, rotation: 0f);
```

网络环境中只应在发起端调用 `StartAction`。它会在本地启动动作，并通过 KL 的 RPC 系统广播；接收端由 RPC 调用 `StartActionById`，不需要手动构造相同实例。

## 重要约定

- 帧区间使用半开区间 `[StartFrame, EndFrame)`；例如 `0, 10` 在动作帧 0 至 9 生效。
- `ActionNode.DurationFrame` 是 `EndFrame - StartFrame`。节点进度在持续时间大于 1 帧时按 `(actionFrame - StartFrame) / (DurationFrame - 1)` 线性插值，并限制在 `[0, 1]`。
- 动作总帧数必须大于 0，节点结束帧不能超过动作总帧数，节点结束帧必须大于开始帧。
- 当前动作只有一个；开始新动作时可选择中断旧动作。自然结束调用 `OnFinish`，被替换或显式中断调用 `OnInterrupt`。
- `UseItemTime` 默认是 `true`。开始动作时会把 `Player.itemAnimation`、`Player.itemTime` 以及当前物品的 `useTime`、`useAnimation` 改为动作总帧数；框架没有自动恢复物品原值，需在动作回调中自行处理。
- `StartRotation` 会随启动 RPC 保存到动作实例，但基类不会自动使用它。需要固定起始朝向的自定义节点或动作逻辑必须主动读取它。
- 动作可以通过带 `totalFrame` 的构造函数在启动前选择时长；`StartAction` 会把该时长加入 RPC，接收端在启动前应用相同的时长。动作开始后不应再修改总时长。

更详细的执行顺序见 [`生命周期与设计.md`](./生命周期与设计.md)，联机行为见 [`网络同步.md`](./网络同步.md)，扩展方式见 [`扩展指南.md`](./扩展指南.md)。
