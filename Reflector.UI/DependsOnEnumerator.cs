using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Mono.Collections.Generic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Reflector.UI
{
	internal class DependsOnEnumerator : IEnumerator, IProgressProvider
	{
		private object root;

		private IEnumerator internalEnum;

		private Dictionary<object, bool> processed = new Dictionary<object, bool>();

		private bool f000319;

		public object Current
		{
			get
			{
				return this.internalEnum.Current;
			}
		}

		public DependsOnEnumerator(object obj)
		{
			this.root = obj;
			MethodDefinition method = obj as MethodDefinition;
			if (method != null)
			{
				this.internalEnum = DependsOnEnumerator.ReferencesHelper.GetReferences(method).GetEnumerator();
			}
			TypeDefinition type = obj as TypeDefinition;
			if (type != null)
			{
				this.internalEnum = new DependsOnEnumerator.TypesEnumerator(type);
			}
			ModuleDefinition module = obj as ModuleDefinition;
			if (module != null)
			{
				this.internalEnum = new DependsOnEnumerator.TypesEnumerator(module);
			}
			AssemblyDefinition assembly = obj as AssemblyDefinition;
			if (assembly != null)
			{
				this.internalEnum = new DependsOnEnumerator.TypesEnumerator(assembly);
			}
		}

		public int GetProgress()
		{
			IProgressProvider i = this.internalEnum as IProgressProvider;
			if (i == null)
			{
				return 0;
			}
			return i.GetProgress();
		}

		public bool m000245()
		{
			return this.f000319;
		}

		public bool MoveNext()
		{
			bool ok = this.internalEnum.MoveNext();
			if (ok)
			{
				object current = this.internalEnum.Current;
				if (this.processed.ContainsKey(current))
				{
					this.f000319 = false;
					return ok;
				}
				this.processed.Add(current, true);
				this.f000319 = !DependsOnEnumerator.ContainerHelper.Contains(this.root, current);
			}
			return ok;
		}

		public void Reset()
		{
			this.internalEnum.Reset();
			this.processed.Clear();
		}

		private static class ContainerHelper
		{
			public static bool Contains(AssemblyDefinition assembly, object obj)
			{
				MemberReference mem = obj as MemberReference;
				if (mem != null)
				{
					TypeReference decl = mem.DeclaringType;
					if (decl == null)
					{
						return false;
					}
					return DependsOnEnumerator.ContainerHelper.Contains(assembly, decl);
				}
				TypeReference type = obj as TypeReference;
				if (type == null)
				{
					return false;
				}
				TypeReference owner = type.DeclaringType;
				if (owner != null)
				{
					return DependsOnEnumerator.ContainerHelper.Contains(assembly, owner);
				}
				return assembly == type.Resolve().Module.Assembly;
			}

			public static bool Contains(MethodReference mtd, object obj)
			{
				MethodReference method = obj as MethodReference;
				if (method == null)
				{
					return false;
				}
				return mtd.FullName == method.FullName;
			}

			public static bool Contains(TypeReference type, object obj)
			{
				MemberReference mem = obj as MemberReference;
				if (mem != null)
				{
					TypeReference decl = mem.DeclaringType;
					if (decl == null)
					{
						return false;
					}
					return DependsOnEnumerator.ContainerHelper.Contains(type, decl);
				}
				TypeReference typeRef = obj as TypeReference;
				if (typeRef != null)
				{
					if (type.Equals(typeRef))
					{
						return true;
					}
					TypeReference decl = typeRef.DeclaringType;
					if (decl != null)
					{
						return DependsOnEnumerator.ContainerHelper.Contains(type, decl);
					}
				}
				return false;
			}

			public static bool Contains(object cont, object obj)
			{
				AssemblyDefinition assembly = cont as AssemblyDefinition;
				if (assembly != null)
				{
					return DependsOnEnumerator.ContainerHelper.Contains(assembly, obj);
				}
				ModuleDefinition module = cont as ModuleDefinition;
				if (module != null)
				{
					return DependsOnEnumerator.ContainerHelper.Contains(module.Assembly, obj);
				}
				TypeReference reference = cont as TypeReference;
				if (reference != null)
				{
					return DependsOnEnumerator.ContainerHelper.Contains(reference, obj);
				}
				MethodReference reference2 = cont as MethodReference;
				if (reference2 == null)
				{
					return false;
				}
				return DependsOnEnumerator.ContainerHelper.Contains(reference2, obj);
			}
		}

		private static class ReferencesHelper
		{
			private static IList<object> GetCstAttrReferences(ICustomAttributeProvider provider)
			{
				List<object> list = new List<object>();
				foreach (CustomAttribute attr in provider.CustomAttributes)
				{
					list.Add(attr.Constructor);
				}
				return list;
			}

			private static IList<object> GetFldRefReferences(FieldReference fieldRef)
			{
				List<object> list = new List<object>(0);
				list.AddRange(DependsOnEnumerator.ReferencesHelper.GetTypeRefReferences(fieldRef.FieldType));
				return list;
			}

			private static IList<object> GetMtdRefReferences(MethodReference methodRef)
			{
				List<object> list = new List<object>();
				if (methodRef.ReturnType != null)
				{
					list.AddRange(DependsOnEnumerator.ReferencesHelper.GetTypeRefReferences(methodRef.ReturnType));
				}
				foreach (ParameterDefinition param in methodRef.Parameters)
				{
					list.AddRange(DependsOnEnumerator.ReferencesHelper.GetTypeRefReferences(param.ParameterType));
				}
				return list;
			}

			public static IList<object> GetReferences(TypeDefinition type)
			{
				List<object> list = new List<object>();
				list.AddRange(DependsOnEnumerator.ReferencesHelper.GetCstAttrReferences(type));
				if (type.BaseType != null)
				{
					list.AddRange(DependsOnEnumerator.ReferencesHelper.GetTypeRefReferences(type.BaseType));
				}
				foreach (TypeReference iface in type.Interfaces)
				{
					list.AddRange(DependsOnEnumerator.ReferencesHelper.GetTypeRefReferences(iface));
				}
				foreach (TypeDefinition nestedType in type.NestedTypes)
				{
					list.AddRange(DependsOnEnumerator.ReferencesHelper.GetReferences(nestedType));
				}
				foreach (MethodDefinition mtd in type.Methods)
				{
					list.AddRange(DependsOnEnumerator.ReferencesHelper.GetReferences(mtd));
				}
				foreach (FieldDefinition fld in type.Fields)
				{
					list.AddRange(DependsOnEnumerator.ReferencesHelper.GetReferences(fld));
				}
				return list;
			}

			public static IList<object> GetReferences(MethodDefinition method)
			{
				List<object> list = new List<object>();
				list.AddRange(DependsOnEnumerator.ReferencesHelper.GetMtdRefReferences(method));
				list.AddRange(DependsOnEnumerator.ReferencesHelper.GetCstAttrReferences(method));
				if (method.HasBody)
				{
					MethodBody body = method.Body;
					foreach (VariableDefinition var in body.Variables)
					{
						list.AddRange(DependsOnEnumerator.ReferencesHelper.GetTypeRefReferences(var.VariableType));
					}
					foreach (ExceptionHandler handler in body.ExceptionHandlers)
					{
						list.AddRange(DependsOnEnumerator.ReferencesHelper.GetTypeRefReferences(handler.CatchType));
					}
					foreach (Instruction inst in body.Instructions)
					{
						FieldReference field = inst.Operand as FieldReference;
						if (field != null)
						{
							list.AddRange(DependsOnEnumerator.ReferencesHelper.GetFldRefReferences(field));
							list.Add(field);
						}
						MethodReference mtd = inst.Operand as MethodReference;
						if (mtd != null)
						{
							list.AddRange(DependsOnEnumerator.ReferencesHelper.GetMtdRefReferences(mtd));
							if (mtd is GenericInstanceMethod)
							{
								mtd = ((GenericInstanceMethod)mtd).ElementMethod;
							}
							if (!(mtd.DeclaringType is TypeSpecification) && !(mtd.DeclaringType is GenericParameter))
							{
								list.Add(mtd);
							}
						}
						TypeReference type = inst.Operand as TypeReference;
						if (type == null)
						{
							continue;
						}
						list.AddRange(DependsOnEnumerator.ReferencesHelper.GetTypeRefReferences(type));
					}
				}
				return list;
			}

			private static IList<object> GetReferences(FieldDefinition field)
			{
				List<object> list = new List<object>();
				list.AddRange(DependsOnEnumerator.ReferencesHelper.GetCstAttrReferences(field));
				list.AddRange(DependsOnEnumerator.ReferencesHelper.GetTypeRefReferences(field.FieldType));
				return list;
			}

			private static IList<object> GetTypeRefReferences(TypeReference typeRef)
			{
				if (typeRef == null)
				{
					return new List<object>();
				}
				List<object> list = new List<object>();
				if (!(typeRef is TypeSpecification) && !(typeRef is GenericParameter))
				{
					list.Add(typeRef);
				}
				ArrayType type = typeRef as ArrayType;
				if (type != null)
				{
					list.AddRange(DependsOnEnumerator.ReferencesHelper.GetTypeRefReferences(type.ElementType));
				}
				PointerType type2 = typeRef as PointerType;
				if (type2 != null)
				{
					list.AddRange(DependsOnEnumerator.ReferencesHelper.GetTypeRefReferences(type2.ElementType));
				}
				ByReferenceType type3 = typeRef as ByReferenceType;
				if (type3 != null)
				{
					list.AddRange(DependsOnEnumerator.ReferencesHelper.GetTypeRefReferences(type3.ElementType));
				}
				OptionalModifierType modifier = typeRef as OptionalModifierType;
				if (modifier != null)
				{
					list.AddRange(DependsOnEnumerator.ReferencesHelper.GetTypeRefReferences(modifier.ElementType));
					list.AddRange(DependsOnEnumerator.ReferencesHelper.GetTypeRefReferences(modifier.ModifierType));
				}
				RequiredModifierType modifier2 = typeRef as RequiredModifierType;
				if (modifier2 != null)
				{
					list.AddRange(DependsOnEnumerator.ReferencesHelper.GetTypeRefReferences(modifier2.ElementType));
					list.AddRange(DependsOnEnumerator.ReferencesHelper.GetTypeRefReferences(modifier2.ModifierType));
				}
				FunctionPointerType pointer = typeRef as FunctionPointerType;
				if (pointer != null)
				{
					list.AddRange(DependsOnEnumerator.ReferencesHelper.GetTypeRefReferences(pointer.ReturnType));
					for (int i = 0; i < pointer.Parameters.Count; i++)
					{
						list.AddRange(DependsOnEnumerator.ReferencesHelper.GetTypeRefReferences(pointer.Parameters[i].ParameterType));
					}
				}
				GenericInstanceType genInst = typeRef as GenericInstanceType;
				if (genInst != null)
				{
					list.Add(genInst.ElementType);
				}
				return list;
			}
		}

		private class TypesEnumerator : IEnumerator, IProgressProvider
		{
			private TypeDefinition[] scope;

			private int progress;

			private IEnumerator internalEnum;

			public object Current
			{
				get
				{
					return this.internalEnum.Current;
				}
			}

			public TypesEnumerator(AssemblyDefinition assembly)
			{
				List<TypeDefinition> types = new List<TypeDefinition>();
				foreach (ModuleDefinition module in assembly.Modules)
				{
					types.AddRange(module.GetAllTypes());
				}
				this.scope = types.ToArray();
				this.Reset();
			}

			public TypesEnumerator(ModuleDefinition module)
			{
				this.scope = module.GetAllTypes().ToArray<TypeDefinition>();
				this.Reset();
			}

			public TypesEnumerator(TypeDefinition type)
			{
				this.scope = new TypeDefinition[] { type };
				this.Reset();
			}

			public int GetProgress()
			{
				if (this.progress == -1)
				{
					return 0;
				}
				return this.progress * 100 / (int)this.scope.Length;
			}

			public bool MoveNext()
			{
				while (this.internalEnum == null || !this.internalEnum.MoveNext())
				{
					if (this.progress >= (int)this.scope.Length)
					{
						return false;
					}
					this.progress++;
					if (this.progress >= (int)this.scope.Length)
					{
						return false;
					}
					this.internalEnum = DependsOnEnumerator.ReferencesHelper.GetReferences(this.scope[this.progress]).GetEnumerator();
				}
				return true;
			}

			public void Reset()
			{
				this.progress = -1;
				this.internalEnum = null;
			}
		}
	}
}