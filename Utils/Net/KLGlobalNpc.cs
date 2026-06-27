namespace KL.Utils.Net;

public abstract class KLGlobalNpc : GlobalNPC
{
    public override void Load()
    {
        if(GetType().FullName!=null) KL.NetInstance.Add(GetType().FullName, this);
        else Log(GetType().Name);
        base.Load();
    }
    
    protected void RPC(string methodName,NPC npc, object[] parameters = null,KLNetModule.NetSendType netSendType = KLNetModule.NetSendType.ClientToAll)
    {
        KLNetModule.RPC(GetType().FullName,npc.whoAmI, methodName, parameters,netSendType);
    }
    protected void RPC(string methodName,NPC npc,KLNetModule.NetSendType netSendType = KLNetModule.NetSendType.ClientToAll)
    {
        KLNetModule.RPC(GetType().FullName,npc.whoAmI, methodName,[],netSendType);
    }
}