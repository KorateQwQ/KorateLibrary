using SilkyUIFramework;
using SilkyUIFramework.Attributes;
using SilkyUIFramework.Elements;
using SilkyUIFramework.Extensions;
using SilkyUIFramework.Layout;
using System.Linq;

namespace KL.SkillSystem.UI;

/// <summary>
/// 玩家目标筛选方式。
/// </summary>
public enum PlayerTargetFilter
{
    /// <summary>只选择本地玩家所在队伍中的玩家。</summary>
    FriendlyOnly,

    /// <summary>选择所有符合距离和存活条件的玩家。</summary>
    AnyPlayer,
}

/// <summary>
/// 玩家目标选择面板的打开参数。
/// </summary>
public sealed class PlayerTargetSelectorOptions
{
    /// <summary>玩家筛选方式。</summary>
    public PlayerTargetFilter Filter { get; init; } = PlayerTargetFilter.FriendlyOnly;

    /// <summary>是否把本地玩家加入候选列表。</summary>
    public bool IncludeSelf { get; init; } = true;

    /// <summary>最多显示的候选数量，实际值会限制在 1 到 9。</summary>
    public int MaxTargets { get; init; } = 9;

    /// <summary>候选玩家与本地玩家的最大距离。小于等于 0 表示只允许同一位置，正无穷表示不限制距离。</summary>
    public float SearchRadius { get; init; } = 1200f;
}

/// <summary>
/// 通用轮盘式玩家目标选择器。
///
/// 该类默认注册在 SilkyUI 的 Radial Hotbars 图层，但默认处于关闭状态。
/// 技能可以调用 <see cref="Open"/>，并监听 <see cref="TargetConfirmed"/> 与
/// <see cref="SelectionCancelled"/>；派生类可以覆写布局、颜色、头像绘制和动画。
/// </summary>
[RegisterUI("Vanilla: Radial Hotbars", "KL: Player Target Selector", int.MaxValue)]
public class PlayerTargetSelectorUI : BaseBody
{
    private readonly List<int> _playerIndices = [];
    private readonly List<int> _outerPlayerIndices = [];
    private readonly List<PlayerTargetRegionUI> _targetRegions = [];
    private Action<int> _openConfirmedCallback;
    private Action _openCancelledCallback;
    private int _centerPlayerIndex = -1;
    private int _refreshTimer;
    private uint _openedAtUpdate;
    private bool _positionAtMousePending;
    private bool _initialized;

    /// <summary>鼠标左键确认目标时触发，参数为 Terraria 玩家 index。</summary>
    public event Action<int> TargetConfirmed;

    /// <summary>选择器被主动取消时触发。</summary>
    public event Action SelectionCancelled;

    /// <summary>当前是否正在显示选择器。</summary>
    public bool IsOpen { get; private set; }

    /// <summary>当前候选玩家 index，按距离从近到远排列。</summary>
    public IReadOnlyList<int> CandidatePlayerIndices => _playerIndices;

    /// <summary>当前打开请求使用的筛选方式。</summary>
    public PlayerTargetFilter Filter { get; private set; } = PlayerTargetFilter.FriendlyOnly;

    /// <summary>当前打开请求是否包含本地玩家。</summary>
    public bool IncludeSelf { get; private set; } = true;

    /// <summary>当前打开请求的最大候选数量。</summary>
    public int MaxTargets { get; private set; } = 9;

    /// <summary>当前打开请求的搜索半径。</summary>
    public float SearchRadius { get; private set; } = 1200f;

    /// <summary>轮盘直径。</summary>
    public virtual float RingDiameter => 190f;

    /// <summary>头像圆直径。</summary>
    public virtual float AvatarDiameter => 64f;

    /// <summary>头像中心距离轮盘中心的距离。</summary>
    public virtual float AvatarRingRadius => 65f;

    /// <summary>中心目标区域的直径。</summary>
    public virtual float CenterAreaDiameter => 54f;

    /// <summary>中心圆与外圈扇区之间的径向间隙。</summary>
    public virtual float CenterOuterGap => 6f;

    /// <summary>兼容旧样式覆写点；现在作为扇区的半透明背景颜色使用。</summary>
    public virtual Color RingBackgroundColor => Color.Black * 0.28f;

    /// <summary>扇区的半透明背景颜色。</summary>
    public virtual Color FanBackgroundColor => RingBackgroundColor;

    /// <summary>外圈目标扇区悬停时的背景颜色。</summary>
    public virtual Color FanHoverBackgroundColor => Color.White * 0.22f;

    /// <summary>中心目标区域的背景颜色。</summary>
    public virtual Color CenterBackgroundColor => FanBackgroundColor;

    /// <summary>中心目标区域悬停时的背景颜色。</summary>
    public virtual Color CenterHoverBackgroundColor => FanHoverBackgroundColor;

    /// <summary>中心目标区域的边框颜色。</summary>
    public virtual Color CenterBorderColor => FanSeparatorColor;

    /// <summary>中心目标区域的边框宽度。</summary>
    public virtual float CenterBorderWidth => FanSeparatorWidth;

    /// <summary>兼容旧样式覆写点。扇形轮盘默认不绘制外圈边框。</summary>
    public virtual Color RingBorderColor => Color.Transparent;

    /// <summary>扇区之间的分隔线颜色。</summary>
    public virtual Color FanSeparatorColor => Color.White * 0.72f;

    /// <summary>扇区之间的分隔线宽度。</summary>
    public virtual float FanSeparatorWidth => 2f;

    /// <summary>扇区边界处保留的角度间隙。</summary>
    public virtual float FanSeparatorGapRadians => 0.018f;

    /// <summary>实际头像绘制比例。玩家头部渲染目标本身为 84 像素，头像圆会按此比例放大。</summary>
    public virtual float AvatarRenderScale => 0.9f;

    /// <summary>外圈扇区数量；外圈候选不足三个时仍绘制三个扇区。</summary>
    protected virtual int FanSectorCount => Math.Max(3, _outerPlayerIndices.Count);

    /// <summary>头像背景颜色。</summary>
    public virtual Color AvatarBackgroundColor => Color.White * 0.18f;

    /// <summary>头像普通边框颜色。</summary>
    public virtual Color AvatarBorderColor => Color.White * 0.78f;

    /// <summary>头像悬停边框颜色。</summary>
    public virtual Color AvatarHoverBorderColor => Color.White;

    /// <summary>头像边框宽度。</summary>
    public virtual float AvatarBorderWidth => 2f;

    public PlayerTargetSelectorUI()
    {
        Enabled = false;
    }

    /// <summary>
    /// 查找已注册的默认选择器并打开它，便于技能直接调用。
    /// </summary>
    public static bool TryOpen(
        PlayerTargetSelectorOptions options = null,
        Action<int> onConfirmed = null,
        Action onCancelled = null)
    {
        if (!TryGet(out var selector))
        {
            return false;
        }

        return selector.Open(options, onConfirmed, onCancelled);
    }

    /// <summary>
    /// 获取当前已注册的选择器实例，便于订阅事件后再调用 <see cref="Open"/>。
    /// </summary>
    public static bool TryGet(out PlayerTargetSelectorUI selector)
        => SilkyUIRenderSystem.Instance.TryGetInstance(out selector);

    /// <summary>
    /// 获取已注册的派生选择器实例，便于使用自定义 UI 样式。
    /// </summary>
    public static bool TryGet<TSelector>(out TSelector selector)
        where TSelector : PlayerTargetSelectorUI
        => SilkyUIRenderSystem.Instance.TryGetInstance(out selector);

    /// <summary>
    /// 打开选择器。打开新的请求会替换当前请求，但不会清除事件订阅。
    /// </summary>
    public virtual bool Open(
        PlayerTargetSelectorOptions options = null,
        Action<int> onConfirmed = null,
        Action onCancelled = null)
    {
        options ??= new PlayerTargetSelectorOptions();

        Filter = options.Filter;
        IncludeSelf = options.IncludeSelf;
        MaxTargets = Math.Clamp(options.MaxTargets, 1, 9);
        SearchRadius = float.IsNaN(options.SearchRadius) ? 1200f : Math.Max(0f, options.SearchRadius);
        _openConfirmedCallback = onConfirmed;
        _openCancelledCallback = onCancelled;
        _refreshTimer = 0;
        _openedAtUpdate = Main.GameUpdateCount;
        _positionAtMousePending = true;
        IsOpen = true;
        Enabled = true;

        RefreshTargets();
        OnOpened();
        return true;
    }

    /// <summary>
    /// 主动中断当前选择，不会触发目标确认回调。
    /// </summary>
    public virtual void Cancel()
    {
        if (!IsOpen)
        {
            return;
        }

        IsOpen = false;
        Enabled = false;
        ClearTargetLayout();

        Action callback = _openCancelledCallback;
        _openCancelledCallback = null;
        _openConfirmedCallback = null;

        SelectionCancelled?.Invoke();
        callback?.Invoke();
        OnCancelled();
    }

    /// <summary>
    /// 派生类可在打开后开始自定义动画或状态。
    /// </summary>
    protected virtual void OnOpened() { }

    /// <summary>
    /// 派生类可在取消后清理自定义动画或状态。
    /// </summary>
    protected virtual void OnCancelled() { }

    protected override void OnInitialize()
    {
        Positioning = Positioning.Fixed;
        SetLeft(alignment: 0.5f);
        SetTop(alignment: 0.5f);
        SetSize(RingDiameter, RingDiameter);
        FitWidth = false;
        FitHeight = false;
        Padding = new Margin(0f);
        Border = 0f;
        BorderRadius = new Vector4(RingDiameter * 0.5f);
        BackgroundColor = Color.Transparent;
        BorderColor = Color.Transparent;
        _initialized = true;

        base.OnInitialize();
        if (IsOpen)
        {
            RefreshTargets();
        }
    }

    protected override void Update(GameTime gameTime)
    {
        if (IsOpen && ++_refreshTimer >= 10)
        {
            _refreshTimer = 0;
            RefreshTargets();
        }

        base.Update(gameTime);
    }

    protected override void UpdateStatus(GameTime gameTime)
    {
        if (_positionAtMousePending)
        {
            _positionAtMousePending = false;
            Vector2 mousePosition = Main.MouseScreen;
            float halfDiameter = RingDiameter * 0.5f;
            SetLeft(mousePosition.X - halfDiameter, alignment: 0f);
            SetTop(mousePosition.Y - halfDiameter, alignment: 0f);
        }

        base.UpdateStatus(gameTime);
    }

    protected override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        base.Draw(gameTime, spriteBatch);
        DrawFanWheel();
    }

    public override bool ContainsPoint(Vector2 point)
    {
        float radius = RingDiameter * 0.5f;
        Vector2 center = Bounds.Position + new Vector2(radius);
        return Vector2.DistanceSquared(point, center) <= radius * radius;
    }

    /// <summary>
    /// 绘制中心圆与外圈环形扇区。
    /// </summary>
    protected virtual void DrawFanWheel()
    {
        int sectorCount = GetOuterSectorCount();
        float outerRadius = RingDiameter * 0.5f;
        float centerRadius = Math.Clamp(CenterAreaDiameter * 0.5f, 0f, outerRadius);
        float innerRadius = Math.Clamp(centerRadius + CenterOuterGap, centerRadius, outerRadius);
        float sectorSpan = MathHelper.TwoPi / sectorCount;
        float angularGap = Math.Clamp(FanSeparatorGapRadians, 0f, sectorSpan * 0.45f);
        Vector2 center = Bounds.Position + new Vector2(outerRadius);

        for (int sector = 0; sector < sectorCount; sector++)
        {
            float centerAngle = GetSectorCenterAngle(sector, sectorCount);
            float startAngle = centerAngle - sectorSpan * 0.5f + angularGap;
            float endAngle = centerAngle + sectorSpan * 0.5f - angularGap;
            PlayerTargetRegionUI region = FindOuterRegion(sector);
            Color fillColor = region is { IsMouseHovering: true }
                ? FanHoverBackgroundColor
                : GetFanSectorColor(sector);

            DrawAnnularSectorUI(
                center,
                innerRadius,
                outerRadius,
                startAngle,
                endAngle,
                fillColor,
                FanSeparatorWidth,
                FanSeparatorColor,
                SilkyUI.TransformMatrix);
        }

        PlayerTargetRegionUI centerRegion = FindCenterRegion();
        Color centerColor = centerRegion is { IsMouseHovering: true }
            ? CenterHoverBackgroundColor
            : CenterBackgroundColor;
        DrawAnnularSectorUI(
            center,
            0f,
            centerRadius,
            -MathHelper.PiOver2,
            MathHelper.Pi * 1.5f,
            centerColor,
            CenterBorderWidth,
            CenterBorderColor,
            SilkyUI.TransformMatrix);
    }

    protected virtual Color GetFanSectorColor(int sectorIndex)
        => FanBackgroundColor;

    private int GetOuterSectorCount()
        => Math.Clamp(Math.Max(_outerPlayerIndices.Count, FanSectorCount), 3, 8);

    private static float GetSectorCenterAngle(int sectorIndex, int sectorCount)
        => -MathHelper.PiOver2 + MathHelper.TwoPi * sectorIndex / sectorCount;

    private PlayerTargetRegionUI FindCenterRegion()
        => _targetRegions.FirstOrDefault(region => region.IsCenter);

    private PlayerTargetRegionUI FindOuterRegion(int sectorIndex)
        => _targetRegions.FirstOrDefault(region => !region.IsCenter && region.SectorIndex == sectorIndex);

    protected virtual void DrawFanSeparator(
        Vector2 center,
        float angle,
        float radius,
        float width,
        Color color)
    {
        Vector2 direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
        float lineLength = Math.Max(0f, radius - width * 0.5f);
        Vector2 lineCenter = center + direction * (lineLength * 0.5f);
        DrawRectangle(
            lineCenter,
            new Vector2(lineLength, Math.Max(1f, width)),
            color,
            rotation: angle,
            corner: Math.Max(0.5f, width * 0.5f),
            border: 0f,
            borderColor: Color.Transparent);
    }

    /// <summary>
    /// 重建候选玩家。默认每十帧检查一次，以便玩家进出范围时轮盘保持最新。
    /// </summary>
    protected virtual void RefreshTargets()
    {
        if (!_initialized)
        {
            return;
        }

        List<int> next = FindTargets();
        if (_playerIndices.SequenceEqual(next))
        {
            return;
        }

        _playerIndices.Clear();
        _playerIndices.AddRange(next);
        RebuildAvatarChildren();
    }

    /// <summary>
    /// 查找并按距离排序候选玩家。派生类可覆写以加入自定义筛选规则。
    /// </summary>
    protected virtual List<int> FindTargets()
    {
        List<int> result = [];
        int localIndex = Main.myPlayer;
        if (localIndex < 0 || localIndex >= Main.maxPlayers)
        {
            return result;
        }

        Player localPlayer = Main.player[localIndex];
        if (localPlayer is null || !localPlayer.active)
        {
            return result;
        }

        float radiusSquared = float.IsPositiveInfinity(SearchRadius)
            ? float.PositiveInfinity
            : SearchRadius * SearchRadius;

        var candidates = new List<(int Index, float DistanceSquared)>();
        for (int index = 0; index < Main.maxPlayers; index++)
        {
            Player player = Main.player[index];
            if (player is null || !player.active || player.dead)
            {
                continue;
            }

            if (index == localIndex)
            {
                if (!IncludeSelf)
                {
                    continue;
                }
            }
            else
            {
                if (Filter == PlayerTargetFilter.FriendlyOnly && !IsFriendly(localPlayer, player))
                {
                    continue;
                }
            }

            float distanceSquared = Vector2.DistanceSquared(localPlayer.Center, player.Center);
            if (distanceSquared > radiusSquared)
            {
                continue;
            }

            candidates.Add((index, distanceSquared));
        }

        foreach (var candidate in candidates
                     .OrderBy(value => value.DistanceSquared)
                     .ThenBy(value => value.Index)
                     .Take(MaxTargets))
        {
            result.Add(candidate.Index);
        }

        return result;
    }

    /// <summary>
    /// 默认将同队且未开启 PvP 的玩家视为友军；无队伍玩家也可互相选择。
    /// </summary>
    protected virtual bool IsFriendly(Player localPlayer, Player candidate)
    {
        if (localPlayer.hostile || candidate.hostile)
        {
            return false;
        }

        return localPlayer.team == candidate.team;
    }

    protected virtual void RebuildAvatarChildren()
    {
        _centerPlayerIndex = -1;
        _outerPlayerIndices.Clear();
        _targetRegions.Clear();
        RemoveAllChildren();
        if (_playerIndices.Count == 0)
        {
            return;
        }

        int localIndex = Main.myPlayer;
        _centerPlayerIndex = _playerIndices.Contains(localIndex)
            ? localIndex
            : _playerIndices[0];
        foreach (int playerIndex in _playerIndices)
        {
            if (playerIndex != _centerPlayerIndex)
            {
                _outerPlayerIndices.Add(playerIndex);
            }
        }

        float wheelCenter = RingDiameter * 0.5f;
        AddTargetRegion(
            Main.player[_centerPlayerIndex],
            slotIndex: 0,
            isCenter: true,
            sectorIndex: -1,
            avatarCenter: new Vector2(wheelCenter));

        int sectorCount = GetOuterSectorCount();
        for (int outerSlot = 0; outerSlot < _outerPlayerIndices.Count; outerSlot++)
        {
            float angle = GetSectorCenterAngle(outerSlot, sectorCount);
            Vector2 avatarCenter = new Vector2(wheelCenter) +
                                   new Vector2(AvatarRingRadius, 0f).RotatedBy(angle);
            AddTargetRegion(
                Main.player[_outerPlayerIndices[outerSlot]],
                slotIndex: outerSlot + 1,
                isCenter: false,
                sectorIndex: outerSlot,
                avatarCenter: avatarCenter);
        }
    }

    private void AddTargetRegion(
        Player player,
        int slotIndex,
        bool isCenter,
        int sectorIndex,
        Vector2 avatarCenter)
    {
        PlayerTargetRegionUI region = CreateTargetRegion(player, isCenter, sectorIndex).Join(this);
        region.SetSize(RingDiameter, RingDiameter);
        region.Positioning = Positioning.Absolute;
        region.SetLeft(0f);
        region.SetTop(0f);
        _targetRegions.Add(region);

        PlayerTargetAvatarUI avatar = CreateAvatar(player, slotIndex).Join(region);
        avatar.SetSize(AvatarDiameter, AvatarDiameter);
        avatar.Positioning = Positioning.Absolute;
        avatar.SetLeft(avatarCenter.X - AvatarDiameter * 0.5f);
        avatar.SetTop(avatarCenter.Y - AvatarDiameter * 0.5f);
        avatar.IgnoreMouseInteraction = true;
    }

    /// <summary>创建目标命中区域，派生类可返回自定义区域控件。</summary>
    protected virtual PlayerTargetRegionUI CreateTargetRegion(
        Player player,
        bool isCenter,
        int sectorIndex)
        => new(this, player, isCenter, sectorIndex);

    /// <summary>创建头像控件，派生类可返回自己的头像控件实现。</summary>
    protected virtual PlayerTargetAvatarUI CreateAvatar(Player player, int slotIndex)
        => new(this, player, slotIndex);

    internal bool ContainsTargetRegionPoint(PlayerTargetRegionUI region, Vector2 point)
    {
        float outerRadius = RingDiameter * 0.5f;
        float centerRadius = Math.Clamp(CenterAreaDiameter * 0.5f, 0f, outerRadius);
        Vector2 center = Bounds.Position + new Vector2(outerRadius);
        Vector2 offset = point - center;
        float distanceSquared = offset.LengthSquared();

        if (region.IsCenter)
        {
            return distanceSquared <= centerRadius * centerRadius;
        }

        float innerRadius = Math.Clamp(centerRadius + CenterOuterGap, centerRadius, outerRadius);
        if (distanceSquared < innerRadius * innerRadius || distanceSquared > outerRadius * outerRadius)
        {
            return false;
        }

        int sectorCount = GetOuterSectorCount();
        if (region.SectorIndex < 0 || region.SectorIndex >= sectorCount)
        {
            return false;
        }

        float sectorSpan = MathHelper.TwoPi / sectorCount;
        float angularGap = Math.Clamp(FanSeparatorGapRadians, 0f, sectorSpan * 0.45f);
        float centerAngle = GetSectorCenterAngle(region.SectorIndex, sectorCount);
        float mouseAngle = MathF.Atan2(offset.Y, offset.X);
        float angularDistance = MathF.Abs(MathHelper.WrapAngle(mouseAngle - centerAngle));
        return angularDistance <= sectorSpan * 0.5f - angularGap;
    }

    private void ClearTargetLayout()
    {
        _playerIndices.Clear();
        _centerPlayerIndex = -1;
        _outerPlayerIndices.Clear();
        _targetRegions.Clear();
        RemoveAllChildren();
    }

    internal void ConfirmTarget(int playerIndex)
    {
        if (!IsOpen || !_playerIndices.Contains(playerIndex) ||
            playerIndex < 0 || playerIndex >= Main.maxPlayers ||
            Main.player[playerIndex] is not { active: true, dead: false })
        {
            return;
        }

        IsOpen = false;
        Enabled = false;
        ClearTargetLayout();
        Action<int> callback = _openConfirmedCallback;
        _openConfirmedCallback = null;
        _openCancelledCallback = null;

        TargetConfirmed?.Invoke(playerIndex);
        callback?.Invoke(playerIndex);
        OnTargetConfirmed(playerIndex);
    }

    /// <summary>
    /// 是否允许鼠标按下确认目标。默认屏蔽 UI 打开的同一更新帧(其实是打开的下一帧）；
    /// 后续加入唤出动画时可覆写并追加动画完成条件。
    /// </summary>
    protected virtual bool CanAcceptTargetInput
        => Main.GameUpdateCount != _openedAtUpdate+1;

    internal void ConfirmTargetFromMouseDown(int playerIndex)
    {
        if (CanAcceptTargetInput)
        {
            ConfirmTarget(playerIndex);
        }
    }

    /// <summary>派生类可在目标确认后清理自定义动画或状态。</summary>
    protected virtual void OnTargetConfirmed(int playerIndex) { }

    protected virtual void ConfigureAvatar(PlayerTargetAvatarUI avatar)
    {
        avatar.BackgroundColor = Color.Transparent;
        avatar.Border = 0f;
        avatar.BorderRadius = Vector4.Zero;
        avatar.BorderColor = Color.Transparent;
    }

    internal void ApplyAvatarStyle(PlayerTargetAvatarUI avatar)
        => ConfigureAvatar(avatar);

    /// <summary>
    /// 绘制头像内容。默认使用 tModLoader 的完整玩家头部渲染器。
    /// </summary>
    protected virtual void DrawPlayerAvatar(PlayerTargetAvatarUI avatar, SpriteBatch spriteBatch)
    {
        if (avatar.Player is not { active: true } player)
        {
            return;
        }

        Vector2 center = avatar.Bounds.Position + (Vector2)avatar.Bounds.Size * 0.5f;
        EndBeginDrawUI();
        try
        {
            DrawStaticPlayerHead(
                player,
                center+new Vector2(-3),
                1f,
                AvatarRenderScale,
                Color.Transparent);
        }
        finally
        {
            EndBeginDrawUI();
        }
    }

    internal void DrawAvatar(PlayerTargetAvatarUI avatar, SpriteBatch spriteBatch)
        => DrawPlayerAvatar(avatar, spriteBatch);
}

/// <summary>
/// 轮盘中可点击的中心圆或外圈扇区。
/// </summary>
public class PlayerTargetRegionUI : UIElementGroup
{
    protected readonly PlayerTargetSelectorUI Selector;

    public Player Player { get; }

    public int PlayerIndex => Player?.whoAmI ?? -1;

    public bool IsCenter { get; }

    /// <summary>外圈扇区下标；中心区域为 -1。</summary>
    public int SectorIndex { get; }

    public PlayerTargetRegionUI(
        PlayerTargetSelectorUI selector,
        Player player,
        bool isCenter,
        int sectorIndex)
    {
        Selector = selector;
        Player = player;
        IsCenter = isCenter;
        SectorIndex = sectorIndex;
        FitWidth = false;
        FitHeight = false;
        IgnoreMouseInteraction = false;
        BackgroundColor = Color.Transparent;
        BorderColor = Color.Transparent;
        Border = 0f;
    }

    public override bool ContainsPoint(Vector2 point)
        => Selector.ContainsTargetRegionPoint(this, point);

    public override void OnLeftMouseDown(UIMouseEvent evt)
    {
        base.OnLeftMouseDown(evt);
        Selector.ConfirmTargetFromMouseDown(PlayerIndex);
    }
}

/// <summary>
/// 轮盘中的单个玩家头像。通常通过 <see cref="PlayerTargetSelectorUI.CreateAvatar"/> 创建。
/// </summary>
public class PlayerTargetAvatarUI : UIElementGroup
{
    protected readonly PlayerTargetSelectorUI Selector;

    public Player Player { get; }

    public int PlayerIndex => Player?.whoAmI ?? -1;

    public int SlotIndex { get; }

    public PlayerTargetAvatarUI(PlayerTargetSelectorUI selector, Player player, int slotIndex)
    {
        Selector = selector;
        Player = player;
        SlotIndex = slotIndex;
        FitWidth = false;
        FitHeight = false;
        IgnoreMouseInteraction = false;
    }

    protected override void UpdateStatus(GameTime gameTime)
    {
        Selector.ApplyAvatarStyle(this);
        base.UpdateStatus(gameTime);
    }

    protected override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        base.Draw(gameTime, spriteBatch);
        Selector.DrawAvatar(this, spriteBatch);
    }

    public override void OnLeftMouseDown(UIMouseEvent evt)
    {
        base.OnLeftMouseDown(evt);
        Selector.ConfirmTargetFromMouseDown(PlayerIndex);
    }
}
