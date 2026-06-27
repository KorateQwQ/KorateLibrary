using System.Linq;

namespace KL.ActionsSystem;

/// <summary>
/// 负责注册和创建可通过 id 启动的动画动作类型。
/// </summary>
public sealed class AnimActionRegistry : ILoadable
{
    private readonly struct AnimActionEntry
    {
        public AnimActionEntry(int id, Type type)
        {
            Id = id;
            Type = type;
        }

        public int Id { get; }

        public Type Type { get; }
    }

    private static readonly List<AnimActionEntry> actions = new();
    private static readonly Dictionary<int, AnimActionEntry> actionsById = new();
    private static readonly Dictionary<Type, int> idsByType = new();

    /// <summary>
    /// 已注册的动画动作类型数量。
    /// </summary>
    public static int Count => actions.Count;

    /// <summary>
    /// 加载时自动注册所有动画动作子类。
    /// </summary>
    /// <param name="mod">所属 Mod 实例。</param>
    public void Load(Mod mod)
    {
        Clear();
        RegisterActionTypes(mod);
    }

    /// <summary>
    /// 卸载时清理注册信息。
    /// </summary>
    public void Unload()
    {
        Clear();
    }

    /// <summary>
    /// 注册一个动画动作类型并返回它的 id。
    /// </summary>
    /// <param name="type">动画动作类型。</param>
    /// <returns>动画动作类型 id。</returns>
    public static int Register(Type type)
    {
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (!typeof(AnimAction).IsAssignableFrom(type))
        {
            throw new ArgumentException("注册类型必须继承 AnimAction。", nameof(type));
        }

        if (type.IsAbstract)
        {
            throw new ArgumentException("不能注册抽象动画动作类型。", nameof(type));
        }

        if (type.GetConstructor(Type.EmptyTypes) == null)
        {
            throw new ArgumentException("动画动作类型必须拥有无参构造函数。", nameof(type));
        }

        if (idsByType.TryGetValue(type, out int registeredId))
        {
            return registeredId;
        }

        int id = actions.Count;
        AnimActionEntry entry = new(id, type);
        actions.Add(entry);
        actionsById.Add(id, entry);
        idsByType.Add(type, id);
        return id;
    }

    /// <summary>
    /// 注册指定 Mod 中所有可通过 id 启动的动画动作类型。
    /// </summary>
    /// <param name="mod">需要扫描动画动作类型的 Mod 实例。</param>
    public static void RegisterFromMod(Mod mod)
    {
        if (mod == null)
        {
            throw new ArgumentNullException(nameof(mod));
        }

        RegisterActionTypes(mod);
    }

    /// <summary>
    /// 尝试通过 id 创建新的动画动作实例。
    /// </summary>
    /// <param name="id">动画动作类型 id。</param>
    /// <param name="animAction">创建出的动画动作实例。</param>
    /// <returns>是否创建成功。</returns>
    public static bool TryCreate(int id, out AnimAction animAction)
    {
        animAction = null;
        if (!actionsById.TryGetValue(id, out AnimActionEntry entry))
        {
            return false;
        }

        animAction = (AnimAction)Activator.CreateInstance(entry.Type);
        return animAction != null;
    }

    /// <summary>
    /// 通过 id 创建新的动画动作实例。
    /// </summary>
    /// <param name="id">动画动作类型 id。</param>
    /// <returns>创建出的动画动作实例。</returns>
    public static AnimAction Create(int id)
    {
        if (!TryCreate(id, out AnimAction animAction))
        {
            throw new ArgumentOutOfRangeException(nameof(id), "未找到指定 id 的动画动作类型。");
        }

        return animAction;
    }

    /// <summary>
    /// 尝试获取指定动画动作类型对应的 id。
    /// </summary>
    /// <param name="type">动画动作类型。</param>
    /// <param name="id">动画动作类型 id。</param>
    /// <returns>是否找到对应 id。</returns>
    public static bool TryGetId(Type type, out int id)
    {
        id = 0;
        if (type == null)
        {
            return false;
        }

        return idsByType.TryGetValue(type, out id);
    }

    /// <summary>
    /// 获取指定动画动作类型对应的 id。
    /// </summary>
    /// <param name="type">动画动作类型。</param>
    /// <returns>动画动作类型 id。</returns>
    public static int GetId(Type type)
    {
        if (!TryGetId(type, out int id))
        {
            throw new ArgumentException("未找到指定动画动作类型对应的 id。", nameof(type));
        }

        return id;
    }

    /// <summary>
    /// 获取指定动画动作类型对应的 id。
    /// </summary>
    /// <typeparam name="T">动画动作类型。</typeparam>
    /// <returns>动画动作类型 id。</returns>
    public static int GetId<T>() where T : AnimAction
    {
        return GetId(typeof(T));
    }

    private static void RegisterActionTypes(Mod mod)
    {
        IEnumerable<Type> actionTypes = mod.Code.GetTypes()
            .Where(type => typeof(AnimAction).IsAssignableFrom(type))
            .Where(type => !type.IsAbstract)
            .Where(type => type.GetConstructor(Type.EmptyTypes) != null)
            .OrderBy(type => type.FullName, StringComparer.Ordinal);

        foreach (Type type in actionTypes)
        {
            Register(type);
        }
    }

    private static void Clear()
    {
        actions.Clear();
        actionsById.Clear();
        idsByType.Clear();
    }
}
