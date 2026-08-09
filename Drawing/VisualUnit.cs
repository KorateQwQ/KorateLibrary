namespace KL.Drawing;

/// <summary>
/// 纯表现用的视觉单元，不继承 ModProjectile，也不会进入 Main.projectile。
/// 它适合由一个真实的控制器弹幕在 OnSpawn 或 AI 中随时创建多个 unit，
/// 再由这个控制器弹幕统一更新、绘制、销毁这些 unit，从而保证本地顺序稳定。
///
/// 推荐用法：
/// 1. 在控制器 ModProjectile 中声明：private readonly List&lt;VisualUnit&gt; units = new();
/// 2. OnSpawn 或 AI 中创建：VisualUnit.Spawn(units, new MyUnit(...), Projectile);
/// 3. AI 中更新所有 unit：VisualUnit.UpdateAll(units);
/// 4. PreDraw 中绘制所有 unit：VisualUnit.DrawAll(units); return false;
/// 5. 控制器死亡、切阶段或取消技能时：VisualUnit.KillAll(units);
///
/// 网络同步建议：不要同步每个 unit。只同步控制器弹幕的随机种子、目标、阶段计时等，
/// 然后在各客户端用相同参数和相同顺序重建 unit。
/// </summary>
public class VisualUnit : ILoadable
{
    /// <summary>创建它的控制器弹幕。可为空，主要用于子类读取 owner、damage、ai 等上下文。</summary>
    public Projectile OwnerProjectile;

    /// <summary>该视觉单元在队列中的固定序号，可用于控制连续生成、连续打击和绘制前后关系。</summary>
    public int Index = -1;

    /// <summary>是否仍然参与更新和绘制。</summary>
    public bool Active = true;

    /// <summary>3D 世界坐标位置。2D 弹幕逻辑通常只使用 X/Y。</summary>
    public Vector3 Position3D;

    /// <summary>3D 世界坐标速度。2D 弹幕逻辑通常只使用 X/Y。</summary>
    public Vector3 Velocity3D;

    /// <summary>朝向，默认在 Update 中跟随 Velocity3D 的 X/Y 方向。</summary>
    public float Rotation;

    /// <summary>缩放。</summary>
    public float Scale = 1f;

    /// <summary>透明度，0 为完全透明，1 为完全不透明。</summary>
    public float Alpha = 1f;

    /// <summary>已经存在的帧数。</summary>
    public int Timer;

    /// <summary>最大存在时间。</summary>
    public int TimeLeft = 60;

    /// <summary>延迟启动时间。用于做第 0、1、2... 个 unit 连续出现。</summary>
    public int Delay;

    /// <summary>Delay 结束后才会返回 true。</summary>
    public bool Started => Timer >= Delay;

    /// <summary>Position3D 截掉 Z 后的 2D 位置。</summary>
    public Vector2 Position2D => new(Position3D.X, Position3D.Y);

    /// <summary>Velocity3D 截掉 Z 后的 2D 速度。</summary>
    public Vector2 Velocity2D => new(Velocity3D.X, Velocity3D.Y);

    public VisualUnit()
    {
    }

    public VisualUnit(Vector3 position3D, Vector3 velocity3D, int delay = 0, int timeLeft = 60)
    {
        Position3D = position3D;
        Velocity3D = velocity3D;
        Delay = delay;
        TimeLeft = timeLeft;
    }

    public VisualUnit(Vector2 position, Vector2 velocity, int delay = 0, int timeLeft = 60)
        : this(new Vector3(position, 0f), new Vector3(velocity, 0f), delay, timeLeft)
    {
    }

    public virtual void Load(Mod mod)
    {
        OnLoad(mod);
    }

    public virtual void Unload()
    {
    }

    /// <summary>Mod 加载时调用，子类可在这里加载静态贴图、模型、Effect 等资产。</summary>
    public virtual void OnLoad(Mod mod)
    {
    }

    /// <summary>获取 Position3D 截掉 Z 后的 2D 位置。</summary>
    public Vector2 GetPosition2D() => Position2D;

    /// <summary>获取 Velocity3D 截掉 Z 后的 2D 速度。</summary>
    public Vector2 GetVelocity2D() => Velocity2D;

    /// <summary>只修改 X/Y，保留当前 Z。</summary>
    public void SetPosition2D(Vector2 position) => Position3D = new Vector3(position, Position3D.Z);

    /// <summary>只修改 X/Y，保留当前 Z。</summary>
    public void SetVelocity2D(Vector2 velocity) => Velocity3D = new Vector3(velocity, Velocity3D.Z);

    /// <summary>被 Spawn 加入列表时调用，子类可以在这里初始化贴图、状态或读取控制器弹幕信息。</summary>
    public virtual void OnSpawn()
    {
    }

    /// <summary>每帧由控制器弹幕按列表顺序调用。</summary>
    public virtual void Update()
    {
        if (!Active)
        {
            return;
        }

        Timer++;
        if (!Started)
        {
            return;
        }

        Position3D += Velocity3D;

        Vector2 velocity2D = Velocity2D;
        if (velocity2D != Vector2.Zero)
        {
            Rotation = velocity2D.ToRotation();
        }

        int aliveTime = Timer - Delay;
        if (aliveTime >= TimeLeft)
        {
            Kill();
        }
    }

    /// <summary>
    /// 默认不包含任何参数和绘制内容。
    /// 子类自行决定绘制贴图、3D 模型、Bloom、BlendState、LayerDrawRequestSystem 等。
    /// </summary>
    public virtual void Draw()
    {
    }

    public virtual void Kill()
    {
        Active = false;
    }

    /// <summary>把 unit 加入某个控制器弹幕维护的列表，并自动设置 Index 与 OwnerProjectile。</summary>
    public static T Spawn<T>(List<VisualUnit> units, T unit, Projectile ownerProjectile = null) where T : VisualUnit
    {
        if (units == null || unit == null)
        {
            return unit;
        }

        unit.Index = units.Count;
        unit.OwnerProjectile = ownerProjectile;
        units.Add(unit);
        unit.OnSpawn();
        return unit;
    }

    /// <summary>按列表顺序更新所有 unit，并在更新后移除已失效 unit。</summary>
    public static void UpdateAll(List<VisualUnit> units)
    {
        if (units == null)
        {
            return;
        }

        for (int i = 0; i < units.Count; i++)
        {
            units[i]?.Update();
        }

        for (int i = units.Count - 1; i >= 0; i--)
        {
            if (units[i] == null || !units[i].Active)
            {
                units.RemoveAt(i);
            }
        }
    }

    /// <summary>按列表顺序绘制所有 unit。</summary>
    public static void DrawAll(List<VisualUnit> units)
    {
        if (units == null)
        {
            return;
        }

        for (int i = 0; i < units.Count; i++)
        {
            VisualUnit unit = units[i];
            if (unit is { Active: true, Started: true })
            {
                unit.Draw();
            }
        }
    }

    /// <summary>销毁并清空某个控制器弹幕创建的所有 unit。</summary>
    public static void KillAll(List<VisualUnit> units)
    {
        if (units == null)
        {
            return;
        }

        for (int i = 0; i < units.Count; i++)
        {
            units[i]?.Kill();
        }

        units.Clear();
    }
}
