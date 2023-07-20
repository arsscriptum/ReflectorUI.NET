using Mono.Cecil;
using Mono.Collections.Generic;
using System;

namespace Reflector.UI
{
	internal static class AnalyzerHelper
	{
		public static AssemblyNameReference GetAssemblyName(TypeReference typeRef)
		{
			TypeReference owner = typeRef.DeclaringType;
			if (owner != null)
			{
				return AnalyzerHelper.GetAssemblyName(owner);
			}
			if (!(typeRef.Scope is ModuleReference))
			{
				AssemblyNameReference asmRef = typeRef.Scope as AssemblyNameReference;
				if (asmRef != null)
				{
					return asmRef;
				}
				return null;
			}
			ModuleReference module = (ModuleReference)typeRef.Scope;
			if (!(module is ModuleDefinition))
			{
				foreach (ModuleDefinition mod in typeRef.Module.Assembly.Modules)
				{
					if (mod.Name != module.Name)
					{
						continue;
					}
					module = mod;
					break;
				}
			}
			if (!(module is ModuleDefinition))
			{
				return null;
			}
			return ((ModuleDefinition)module).Assembly.Name;
		}

		private static bool IsBased(TypeReference type, TypeReference based)
		{
			bool flag;
			TypeDefinition typeDef = type.Resolve();
			if (typeDef != null)
			{
				if (typeDef.BaseType != null && typeDef.BaseType.Equals(based))
				{
					return true;
				}
				foreach (TypeReference iface in typeDef.Interfaces)
				{
					if (!iface.Equals(based))
					{
						continue;
					}
					flag = true;
					return flag;
				}
				if (typeDef.BaseType != null && AnalyzerHelper.IsBased(typeDef.BaseType, based))
				{
					return true;
				}
				Collection<TypeReference>.Enumerator enumerator = typeDef.Interfaces.GetEnumerator();
				try
				{
					while (enumerator.MoveNext())
					{
						if (!AnalyzerHelper.IsBased(enumerator.Current, based))
						{
							continue;
						}
						flag = true;
						return flag;
					}
					return false;
				}
				finally
				{
					((IDisposable)enumerator).Dispose();
				}
				return flag;
			}
			return false;
		}

		public static bool VirtualEquals(MethodReference a, MethodReference b)
		{
			if (a.Name != b.Name)
			{
				return false;
			}
			if (a is GenericInstanceMethod)
			{
				a = ((GenericInstanceMethod)a).ElementMethod;
			}
			if (b is GenericInstanceMethod)
			{
				b = ((GenericInstanceMethod)b).ElementMethod;
			}
			if (!a.ReturnType.Equals(b.ReturnType))
			{
				return false;
			}
			if (a.HasThis != b.HasThis && a.ExplicitThis != b.ExplicitThis && a.CallingConvention != b.CallingConvention)
			{
				return false;
			}
			if (a.Parameters.Count != b.Parameters.Count)
			{
				return false;
			}
			for (int i = 0; i < a.Parameters.Count; i++)
			{
				if (!a.Parameters[i].ParameterType.Equals(b.Parameters[i].ParameterType))
				{
					return false;
				}
			}
			MethodDefinition aDef = a.Resolve();
			MethodDefinition bDef = b.Resolve();
			if (aDef == null || bDef == null)
			{
				return false;
			}
			if (!aDef.IsVirtual || !bDef.IsVirtual)
			{
				return false;
			}
			if (!AnalyzerHelper.IsBased(aDef.DeclaringType, bDef.DeclaringType))
			{
				return false;
			}
			return true;
		}
	}
}