global using Terraria.ModLoader;
global using Terraria.Localization;
global using Microsoft.Xna.Framework;
global using Microsoft.Xna.Framework.Graphics;
global using System.Collections.Generic;
global using Terraria.GameContent;
global using Terraria;
global using ReLogic.Content;
global using Terraria.ID;

global using KL.Drawing;
global using static KL.Drawing.DrawHelper;
global using static KL.Utils.TimeStopManager;
global using static KL.Extensions.GamePlayStatic;

global using KL.Extensions;

global using System;
global using System.Reflection;
global using Terraria.GameInput;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KL.Utils;
using ReLogic.Graphics;


namespace KL
{
	// Please read https://github.com/tModLoader/tModLoader/wiki/Basic-tModLoader-Modding-Guide#mod-skeleton-contents for more information about the various files in a mod.
	public class KL : Mod
	{
		public static Mod KLInstance;
		
		public static bool ShouldShowDebug = true;
		
		internal static Dictionary<string,object> NetInstance = new Dictionary<string, object>();
		private static readonly ConcurrentDictionary<string, MethodInfo> CachedInvokeMethods = new();
		private static readonly ConcurrentDictionary<string, FieldInfo> CachedDelegateFields = new();
		
		public override void Load()
		{
			KLInstance = this;
			
			base.Load();
		}
		
		private static string BuildMethodCacheKey(Type type, string methodName, object[] parameters)
		{
			IEnumerable<string> parameterTypes = (parameters ?? []).Select(p => p?.GetType().FullName ?? "<null>");
			return $"{type.AssemblyQualifiedName}::{methodName}::{string.Join("|", parameterTypes)}";
		}
		
		private static string BuildFieldCacheKey(Type type, string fieldName)
		{
			return $"{type.AssemblyQualifiedName}::{fieldName}";
		}
		
		private static FieldInfo GetCachedDelegateField(Type type, string fieldName)
		{
			string cacheKey = BuildFieldCacheKey(type, fieldName);
			if (CachedDelegateFields.TryGetValue(cacheKey, out FieldInfo cachedField))
			{
				return cachedField;
			}
			
			FieldInfo field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
			if (field != null && typeof(Delegate).IsAssignableFrom(field.FieldType))
			{
				CachedDelegateFields.TryAdd(cacheKey, field);
				return field;
			}
			
			return null;
		}
		
		private static MethodInfo GetCachedMethod(Type type, string methodName, object[] parameters)
		{
			string cacheKey = BuildMethodCacheKey(type, methodName, parameters);
			if (CachedInvokeMethods.TryGetValue(cacheKey, out MethodInfo cachedMethod))
			{
				return cachedMethod;
			}
			
			object[] args = parameters ?? [];
			List<(MethodInfo Method, int Score)> candidates = [];
			foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
			{
				if (method.Name != methodName)
				{
					continue;
				}
				
				ParameterInfo[] methodParameters = method.GetParameters();
				if (methodParameters.Length != args.Length)
				{
					continue;
				}
				
				bool isMatch = true;
				int score = 0;
				for (int i = 0; i < methodParameters.Length; i++)
				{
					object arg = args[i];
					Type parameterType = methodParameters[i].ParameterType;
					if (arg == null)
					{
						if (parameterType.IsValueType && Nullable.GetUnderlyingType(parameterType) == null)
						{
							isMatch = false;
							break;
						}
						
						continue;
					}
					
					Type argumentType = arg.GetType();
					if (parameterType == argumentType)
					{
						score += 2;
						continue;
					}
					
					if (parameterType.IsAssignableFrom(argumentType))
					{
						score += 1;
						continue;
					}
					
					isMatch = false;
					break;
				}
				
				if (isMatch)
				{
					candidates.Add((method, score));
				}
			}
			
			if (candidates.Count == 0)
			{
				return null;
			}
			
			candidates.Sort((left, right) => right.Score.CompareTo(left.Score));
			if (candidates.Count > 1 && candidates[0].Score == candidates[1].Score)
			{
				Log($"Fail Invoke: InvokeMethodByTypeName Function: '{methodName}' ambiguous in Type: '{type.FullName}'");
				return null;
			}
			
			MethodInfo resolvedMethod = candidates[0].Method;
			CachedInvokeMethods.TryAdd(cacheKey, resolvedMethod);
			return resolvedMethod;
		}
		
		public static void InvokeMethodByTypeName(object instance, string methodName,  object[] parameters = null)
		{
			if(instance==null)return;
			parameters ??= [];
			try
			{
				Type type = instance.GetType();

				if (typeof(Delegate).IsAssignableFrom(type))
				{
					if (instance is Delegate del)
					{
						//Log($"Success Invoke: InvokeMethodByTypeName Delegate: '{methodName}' in Type: '{type.FullName}'");
						del.DynamicInvoke(parameters);
						return;
					}
				}

				FieldInfo eventField = GetCachedDelegateField(type, methodName);
				if (eventField != null)
				{
					if (eventField.GetValue(instance) is Delegate eventDelegate)
					{
						//Log($"Success Invoke: InvokeMethodByTypeName Event: '{methodName}' in Type: '{type.FullName}'");
						eventDelegate.DynamicInvoke(parameters);
						return;
					}
					else
					{
						Log($"Fail Invoke: InvokeMethodByTypeName Event: '{methodName}' has no subscribers in Type: '{type.FullName}'");
						return;
					}
				}
				
				MethodInfo method = GetCachedMethod(type, methodName, parameters);
				if (method == null)
				{
					Log($"Fail Invoke: InvokeMethodByTypeName Function: '{methodName}' NotFind in Type: '{type.FullName}' ");
					return;
				}

				method.Invoke(instance, parameters);
				//Log($"Success Invoke: InvokeMethodByTypeName Function: '{methodName}' in Type: '{type.FullName}'");
			}
			catch (Exception ex)
			{
				Exception realException = ex is TargetInvocationException { InnerException: not null } ? ex.InnerException : ex;
				Log($"InvokeMethodByTypeName: Invoke Error: {realException.Message}");
			}
		}
		public override void HandlePacket(BinaryReader reader, int whoAmI)
		{
			KLNetModule.NetMessageType type = (KLNetModule.NetMessageType)reader.ReadByte();
			switch (type)
			{
				case KLNetModule.NetMessageType.RPCFunction:
				{
					KLNetModule.HandleRPCFunction(reader,whoAmI);
					break;
				}
			}
			base.HandlePacket(reader, whoAmI);
		}
	}
	
}