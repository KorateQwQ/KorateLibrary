namespace KL.Utils.Net;

/// <summary>
/// 网络类型system基类，拥有rpc方法。
/// </summary>
public abstract class KLModSystem : ModSystem
{
    public override void Load()
    {
        if(GetType().FullName!=null) KL.NetInstance.Add(GetType().FullName, this);
        base.Load();
    }

    public int GetInstanceID => 0;

    public void RPC(string methodName, object[] parameters = null,KLNetModule.NetSendType netSendType = KLNetModule.NetSendType.ClientToAll)
    {
        KLNetModule.RPC(GetType().FullName,GetInstanceID, methodName, parameters,netSendType);
    }
}