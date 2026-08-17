using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reflection;
using KL.SkillSystem;
using KL.Utils.Net;

namespace KL.Utils;

public static class KLNetModule 
{
    private static readonly MethodInfo GetGlobalNpcGenericDefinition = typeof(NPC)
        .GetMethods(BindingFlags.Public | BindingFlags.Instance)
        .FirstOrDefault(m =>
            m.Name == "GetGlobalNPC" &&
            m.IsGenericMethodDefinition &&
            m.GetParameters().Length == 0);

    private static readonly MethodInfo GetModPlayerGenericDefinition = typeof(Player).GetMethod("GetModPlayer",
        BindingFlags.Public | BindingFlags.Instance,
        null,
        Type.EmptyTypes,
        null);

    private static readonly ConcurrentDictionary<Type, MethodInfo> CachedGlobalNpcMethods = new();
    private static readonly ConcurrentDictionary<Type, MethodInfo> CachedModPlayerMethods = new();

    public static bool IsServerOrLocalClient => Main.netMode == NetmodeID.Server || Main.netMode == NetmodeID.SinglePlayer;
    
    public enum NetMessageType : byte
    {
        RPCFunction,
        Property
    }
    
    public enum NetSendType : byte
    {
        //由客户端发起，所有端和服务器调用
        ClientToAll,
        //由客户端发起，服务器调用
        ClientToServer,
        //由服务器发起，所有端和服务器调用
        ServerToAll,
        //由服务器发起，所有客户端（不包括服务器）调用
        ServerToClients,
        None,
    }
    private static GlobalNPC GetGlobalNPCByType(NPC npc, Type globalNpcType)
    {
        if (npc == null || globalNpcType == null) return null;
        if (!typeof(GlobalNPC).IsAssignableFrom(globalNpcType)) return null;
        if (GetGlobalNpcGenericDefinition == null) return null;

        MethodInfo genericMethod = CachedGlobalNpcMethods.GetOrAdd(globalNpcType,
            type => GetGlobalNpcGenericDefinition.MakeGenericMethod(type));
        return genericMethod.Invoke(npc, null) as GlobalNPC;
    }

    private static Projectile GetProjectileByIdentity(int owner, int identity, string typeName = null)
    {
        if (owner < 0 || owner >= Main.player.Length)
        {
            Log($"GetProjectileByIdentity: Invalid owner {owner}, identity {identity}");
            return null;
        }

        for (int i = 0; i < Main.projectile.Length; i++)
        {
            Projectile projectile = Main.projectile[i];
            if (!projectile.active || projectile.owner != owner || projectile.identity != identity)
            {
                continue;
            }

            if (typeName != null)
            {
                if (projectile.ModProjectile == null)
                {
                    Log($"GetProjectileByIdentity: Projectile owner {owner}, identity {identity} missing ModProjectile for {typeName}");
                    return null;
                }

                if (projectile.ModProjectile.GetType().FullName != typeName)
                {
                    Log($"GetProjectileByIdentity: Projectile type mismatch, expect {typeName}, actual {projectile.ModProjectile.GetType().FullName}");
                    return null;
                }
            }

            return projectile;
        }

        Log($"GetProjectileByIdentity: Projectile not found, owner {owner}, identity {identity}, type {typeName ?? "Any"}");
        return null;
    }

    public static object GetNetInstance(string typeName, int instanceID = 0, int instanceOwner = -1)
    {
        KL.NetInstance.TryGetValue(typeName, out object instance);
        if (instance == null)
        {
            Log("GetNetInstance: NetInstance not found: " + typeName);
            return null;
        }
        if(instance is ModNPC modNpc)
        {
            if (instanceID < 0 || instanceID >= Main.npc.Length)
            {
                Log($"GetNetInstance: Invalid NPC instance id {instanceID} for {typeName}");
                return null;
            }

            NPC result = Main.npc[instanceID];
            if (!result.active || result.ModNPC == null)
            {
                Log($"GetNetInstance: NPC instance {instanceID} is inactive or missing ModNPC for {typeName}");
                return null;
            }
            if (result.ModNPC.GetType().FullName != typeName)
            {
                Log($"GetNetInstance: NPC type mismatch, expect {typeName}, actual {result.ModNPC.GetType().FullName}");
                return null;
            }
            return result.ModNPC;
        }
        
        if (instance is GlobalNPC globalNPC)
        {
            if (instanceID < 0 || instanceID >= Main.npc.Length)
            {
                Log($"GetNetInstance: Invalid GlobalNPC instance id {instanceID} for {typeName}");
                return null;
            }

            NPC result = Main.npc[instanceID];
            if (!result.active)
            {
                Log($"GetNetInstance: NPC instance {instanceID} is inactive for {typeName}");
                return null;
            }

            GlobalNPC resolvedGlobalNpc = GetGlobalNPCByType(result, globalNPC.GetType());
            if (resolvedGlobalNpc == null)
            {
                Log($"GetNetInstance: GlobalNPC not found on NPC {instanceID} for {typeName}");
                return null;
            }
            if (resolvedGlobalNpc.GetType().FullName != typeName)
            {
                Log($"GetNetInstance: GlobalNPC type mismatch, expect {typeName}, actual {resolvedGlobalNpc.GetType().FullName}");
                return null;
            }
            return resolvedGlobalNpc;
        }

        
        if(instance is ModPlayer modPlayer)
        {
            if (instanceID < 0 || instanceID >= Main.player.Length)
            {
                Log($"GetNetInstance: Invalid ModPlayer instance id {instanceID} for {typeName}");
                return null;
            }
            
            Player result = Main.player[instanceID];
            Type modPlayerType = instance.GetType();

            if (GetModPlayerGenericDefinition == null) return null;
            MethodInfo genericMethod = CachedModPlayerMethods.GetOrAdd(modPlayerType,
                type => GetModPlayerGenericDefinition.MakeGenericMethod(type));
            ModPlayer finalPlayer = (ModPlayer)genericMethod.Invoke(result, null);
            if (finalPlayer == null)
            {
                Log($"GetNetInstance: ModPlayer not found on Player {instanceID} for {typeName}");
                return null;
            }
            if (finalPlayer.GetType().FullName != typeName)
            {
                Log($"GetNetInstance: ModPlayer type mismatch, expect {typeName}, actual {finalPlayer.GetType().FullName}");
                return null;
            }
            return finalPlayer;
        }
        else if(instance is ModProjectile modProjectile)
        {
            Projectile result = GetProjectileByIdentity(instanceOwner, instanceID, typeName);
            if (result == null)
            {
                return null;
            }
            return result.ModProjectile;
        }
        return instance;
    }
    public static void RPC(string typeName,int instanceID,string methodName, object[] parameters = null,NetSendType netSendType = NetSendType.ClientToAll,int ignoreClient=-1,int instanceOwner = -1)
    {
        parameters ??= [];

        object instance = GetNetInstance(typeName,instanceID,instanceOwner);
        if(instance==null)
        {
            Log("Error: KLNetModule.GetNetInstance() return null");
            return;
        }
        
        if (Main.netMode == NetmodeID.SinglePlayer)
        {
            KL.InvokeMethodByTypeName(instance,methodName, parameters);

            return;
        }
        
        ModPacket packet = KL.KLInstance.GetPacket();
        packet.Write((byte)NetMessageType.RPCFunction);
        packet.Write((byte)netSendType);
        packet.Write(typeName);
        packet.Write(instanceID);
        packet.Write(instanceOwner);
        packet.Write(methodName);
        packet.Write(parameters.Length);

        foreach (var parameter in parameters)
        {
            packet.Write(parameter);
        }

        switch (netSendType)
        {
            case NetSendType.ClientToAll:
                KL.InvokeMethodByTypeName(instance,methodName, parameters);
                packet.Send();
                break;
            case NetSendType.ClientToServer:
                packet.Send();
                break;
            case NetSendType.ServerToAll:
                KL.InvokeMethodByTypeName(instance,methodName, parameters);
                packet.Send(-1,Main.myPlayer);
                break;
            case NetSendType.ServerToClients:
                packet.Send(-1,ignoreClient);
                break;
        }
    }
    
    public static void Write(this ModPacket packet, object parameter)
    {
        switch (parameter)
        {
            case null:
                packet.Write((byte)254);
                break;
            case int intValue:
                packet.Write((byte)0);
                packet.Write(intValue);
                break;
            case float floatValue:
                packet.Write((byte)1);
                packet.Write(floatValue);
                break;
            case double doubleValue:
                packet.Write((byte)2);
                packet.Write(doubleValue);
                break;
            case bool boolValue:
                packet.Write((byte)3);
                packet.Write(boolValue);
                break;
            case string stringValue:
                packet.Write((byte)4);
                packet.Write(stringValue);
                break;
            case byte byteValue:
                packet.Write((byte)5);
                packet.Write(byteValue);
                break;
            case short shortValue:
                packet.Write((byte)6);
                packet.Write(shortValue);
                break;
            case long longValue:
                packet.Write((byte)7);
                packet.Write(longValue);
                break;
            case Vector2 vector2Value:
                packet.Write((byte)8);
                packet.WriteVector2(vector2Value);
                break;
            case Color colorValue:
                packet.Write((byte)9);
                packet.WriteRGB(colorValue);
                break;
            case NPC npcValue:
                packet.Write((byte)10);
                packet.Write(npcValue.whoAmI);
                break;
            case Player playerValue:
                packet.Write((byte)11);
                packet.Write(playerValue.whoAmI);
                break;
            case Projectile projectileValue:
                packet.Write((byte)12);
                packet.Write(projectileValue.owner);
                packet.Write(projectileValue.identity);
                break;
            case List<int> intListValue:
                packet.Write((byte)20);
                packet.Write(intListValue.Count);
                foreach (var value in intListValue)
                {
                    packet.Write(value);
                }
                break;
            default:
                throw new NotSupportedException($"KLNetModule.Write 不支持参数类型: {parameter.GetType().FullName}");
        }
    }
    
    public static void HandleRPCFunction(BinaryReader reader,int messageSender)
    {
        NetSendType netSendType = (NetSendType)reader.ReadByte();
        string typeName = reader.ReadString();
        int instanceID = reader.ReadInt32();
        int instanceOwner = reader.ReadInt32();
        string methodName = reader.ReadString();
        int parameterCount = reader.ReadInt32();
        object[] parameters = new object[parameterCount];
        //PrintText($"Get RPC Function: {methodName}, NetSendType: {netSendType}, InstanceID: {instanceID}, ParameterCount: {parameterCount}");

        for (int i = 0; i < parameterCount; i++)
        {
            object parameter = null;
            switch (reader.ReadByte())
            {
                case 254:
                    parameter = null;
                    break;
                case 0:
                    parameter = reader.ReadInt32();
                    break;
                case 1:
                    parameter = reader.ReadSingle();
                    break;
                case 2:
                    parameter = reader.ReadDouble();
                    break;
                case 3:
                    parameter = reader.ReadBoolean();
                    break;
                case 4:
                    parameter = reader.ReadString();
                    break;
                case 5:
                    parameter = reader.ReadByte();
                    break;
                case 6:
                    parameter = reader.ReadInt16();
                    break;
                case 7:
                    parameter = reader.ReadInt64();
                    break;
                case 8:
                    parameter = reader.ReadVector2();
                    break;
                case 9:
                    parameter = reader.ReadRGB();
                    break;
                case 10:
                    int npcID = reader.ReadInt32();
                    NPC npc = null;
                    if (npcID >= 0 && npcID < Main.npc.Length)
                    {
                        npc = Main.npc[npcID];
                    }
                    parameter = npc;
                    break;
                case 11:
                    int playerID = reader.ReadInt32();
                    Player player = null;
                    if (playerID >= 0 && playerID < Main.player.Length)
                    {
                        player = Main.player[playerID];
                    }
                    parameter = player;
                    break;
                case 12:
                    int projectileOwner = reader.ReadInt32();
                    int projectileIdentity = reader.ReadInt32();
                    parameter = GetProjectileByIdentity(projectileOwner, projectileIdentity);
                    break;
                case 20:
                    int listCount = reader.ReadInt32();
                    List<int> intList = new List<int>(listCount);
                    for (int j = 0; j < listCount; j++)
                    {
                        intList.Add(reader.ReadInt32());
                    }
                    parameter = intList;
                    break;
                default:
                    throw new NotSupportedException($"KLNetModule.HandleRPCFunction 收到未知参数类型标记: {parameters[i]}");
            }
            parameters[i] = parameter;
        }

        object instance = GetNetInstance(typeName, instanceID, instanceOwner);
        
        if (instance != null)
        {
            KL.InvokeMethodByTypeName(instance,methodName, parameters);
            if (netSendType == NetSendType.ClientToAll)
            {
                RPC(typeName,instanceID,methodName,parameters,NetSendType.ServerToClients,messageSender,instanceOwner);
            }
        }
        else Log("RPC Failed Instance Not Found");

    }
}