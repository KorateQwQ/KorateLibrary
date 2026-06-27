namespace KL.Utils.Net;

public abstract class KLModPlayer : ModPlayer
{
    public override void Load()
    {
        if(GetType().FullName!=null) KL.NetInstance.Add(GetType().FullName, this);
        else Log(GetType().Name);
        base.Load();
    }
    
    protected void RPC(string methodName, object[] parameters = null,KLNetModule.NetSendType netSendType = KLNetModule.NetSendType.ClientToAll)
    {
        KLNetModule.RPC(GetType().FullName,Player.whoAmI, methodName, parameters,netSendType);
    }
    protected void RPC(string methodName,KLNetModule.NetSendType netSendType = KLNetModule.NetSendType.ClientToAll)
    {
        KLNetModule.RPC(GetType().FullName,Player.whoAmI, methodName,[],netSendType);
    }
}