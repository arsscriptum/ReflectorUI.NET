using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Reflector.UI
{
	internal class UsedByEnumerator : IEnumerator, IProgressProvider
	{
		private object root;

		private MembersEnumerator internalEnum;

		private IEnumerator scopeEnum;

		private int state;

		private object current;

		public object Current
		{
			get
			{
				return this.current;
			}
		}

		public UsedByEnumerator(object root, AssemblyDefinition[] scope)
		{
			this.root = root;
			List<object> list = new List<object>();
			AssemblyDefinition[] assemblyDefinitionArray = scope;
			for (int i = 0; i < (int)assemblyDefinitionArray.Length; i++)
			{
				AssemblyDefinition assembly = assemblyDefinitionArray[i];
				list.Add(assembly);
				foreach (ModuleDefinition module in assembly.Modules)
				{
					list.Add(module);
				}
			}
			this.scopeEnum = list.GetEnumerator();
			FieldDefinition field = root as FieldDefinition;
			if (field != null && field.IsPrivate)
			{
				TypeDefinition declaringType = field.DeclaringType;
				if (declaringType != null)
				{
					this.internalEnum = new MembersEnumerator(UsedByEnumerator.GetAllTypes(declaringType.Resolve()));
				}
			}
			MethodDefinition method = root as MethodDefinition;
			if (method != null && method.IsPrivate)
			{
				TypeDefinition declaringType = method.DeclaringType;
				if (declaringType != null)
				{
					this.internalEnum = new MembersEnumerator(UsedByEnumerator.GetAllTypes(declaringType.Resolve()));
				}
			}
			if (this.internalEnum == null)
			{
				List<AssemblyDefinition> list2 = new List<AssemblyDefinition>();
				AssemblyNameReference reference3 = UsedByEnumerator.GetAssemblyName(root);
				AssemblyDefinition[] assemblyDefinitionArray1 = scope;
				for (int j = 0; j < (int)assemblyDefinitionArray1.Length; j++)
				{
					AssemblyDefinition assembly2 = assemblyDefinitionArray1[j];
					if (UsedByEnumerator.IsReferenced(assembly2, reference3))
					{
						list2.Add(assembly2);
					}
				}
				this.internalEnum = new MembersEnumerator(list2.ToArray());
			}
		}

		private static TypeDefinition[] GetAllTypes(TypeDefinition type)
		{
			if (!type.HasNestedTypes)
			{
				return new TypeDefinition[] { type };
			}
			Collection<TypeDefinition> nestedTypes = type.NestedTypes;
			List<TypeDefinition> list = new List<TypeDefinition>(nestedTypes.Count + 1)
			{
				type
			};
			list.AddRange(nestedTypes);
			foreach (TypeDefinition Definition in nestedTypes)
			{
				list.AddRange(UsedByEnumerator.GetAllTypes(Definition));
			}
			return list.ToArray();
		}

		private static AssemblyNameReference GetAssemblyName(object obj)
		{
			TypeReference reference = obj as TypeReference;
			if (reference != null)
			{
				return AnalyzerHelper.GetAssemblyName(reference);
			}
			MethodReference reference2 = obj as MethodReference;
			if (reference2 != null)
			{
				return AnalyzerHelper.GetAssemblyName(reference2.DeclaringType);
			}
			FieldReference reference3 = obj as FieldReference;
			if (reference3 == null)
			{
				return null;
			}
			return AnalyzerHelper.GetAssemblyName(reference3.DeclaringType);
		}

		public int GetProgress()
		{
			if (this.state == 0)
			{
				return 0;
			}
			if (this.state != 1)
			{
				return 100;
			}
			return this.internalEnum.GetProgress();
		}

		private static bool IsReferenced(AssemblyDefinition assembly, AssemblyNameReference refer)
		{
			bool flag;
			if (assembly.Name.FullName == refer.FullName)
			{
				return true;
			}
			Collection<ModuleDefinition>.Enumerator enumerator = assembly.Modules.GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					Collection<AssemblyNameReference>.Enumerator enumerator1 = enumerator.Current.AssemblyReferences.GetEnumerator();
					try
					{
						while (enumerator1.MoveNext())
						{
							if (enumerator1.Current.FullName != refer.FullName)
							{
								continue;
							}
							flag = true;
							return flag;
						}
					}
					finally
					{
						((IDisposable)enumerator1).Dispose();
					}
				}
				return false;
			}
			finally
			{
				((IDisposable)enumerator).Dispose();
			}
			return flag;
		}

		public bool MoveNext()
		{
			if (this.state == 0)
			{
				if (this.scopeEnum.MoveNext())
				{
					this.current = this.scopeEnum.Current;
					return true;
				}
				this.state = 1;
			}
			if (this.state == 1)
			{
				if (this.internalEnum.MoveNext())
				{
					this.current = this.internalEnum.Current;
					return true;
				}
				this.state = 2;
			}
			this.current = null;
			return false;
		}

		public void Reset()
		{
			this.state = 0;
			this.internalEnum.Reset();
			this.scopeEnum.Reset();
		}

		public bool UsedByRoot()
		{
			if (this.root is TypeReference)
			{
				return this.UsedByRootType(this.current);
			}
			if (this.root is MethodReference)
			{
				return this.UsedByRootMethod(this.current);
			}
			if (!(this.root is FieldReference))
			{
				return false;
			}
			return this.UsedByRootField(this.current);
		}

		private bool UsedByRootCstAttrCtor(ICustomAttributeProvider provider)
		{
			bool flag;
			if (!(this.root is MethodReference))
			{
				return false;
			}
			Collection<CustomAttribute>.Enumerator enumerator = provider.CustomAttributes.GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					CustomAttribute attribute = enumerator.Current;
					if (((MethodReference)this.root).FullName != attribute.Constructor.FullName)
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

		private bool UsedByRootCstAttrField(ICustomAttributeProvider provider)
		{
			bool flag;
			if (!(this.root is FieldReference))
			{
				return false;
			}
			Collection<CustomAttribute>.Enumerator enumerator = provider.CustomAttributes.GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					CustomAttribute attribute = enumerator.Current;
					Collection<CustomAttributeNamedArgument>.Enumerator enumerator1 = attribute.Fields.GetEnumerator();
					try
					{
						while (enumerator1.MoveNext())
						{
							CustomAttributeNamedArgument i = enumerator1.Current;
							if (!(((FieldReference)this.root).Name == i.Name) || !((FieldReference)this.root).DeclaringType.Equals(attribute.AttributeType))
							{
								continue;
							}
							flag = true;
							return flag;
						}
					}
					finally
					{
						((IDisposable)enumerator1).Dispose();
					}
				}
				return false;
			}
			finally
			{
				((IDisposable)enumerator).Dispose();
			}
			return flag;
		}

		private bool UsedByRootCstAttrType(ICustomAttributeProvider provider)
		{
			bool flag;
			if (!(this.root is TypeReference))
			{
				return false;
			}
			Collection<CustomAttribute>.Enumerator enumerator = provider.CustomAttributes.GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					CustomAttribute attribute = enumerator.Current;
					if (!this.root.Equals(attribute.Constructor.DeclaringType))
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

		private bool UsedByRootField(object field)
		{
			bool flag;
			if (!(this.root is FieldReference))
			{
				return false;
			}
			MethodDefinition method = field as MethodDefinition;
			if (method != null)
			{
				MethodBody body = method.Body;
				if (body != null)
				{
					foreach (Instruction instruction in body.Instructions)
					{
						FieldReference fieldRef = instruction.Operand as FieldReference;
						if (fieldRef == null || !(((FieldReference)this.root).FullName == fieldRef.FullName))
						{
							continue;
						}
						flag = true;
						return flag;
					}
				}
			}
			ICustomAttributeProvider provider = field as ICustomAttributeProvider;
			if (provider != null && this.UsedByRootCstAttrField(provider))
			{
				return true;
			}
			if (method != null)
			{
				foreach (ParameterDefinition param in method.Parameters)
				{
					if (!this.UsedByRootField(param))
					{
						continue;
					}
					flag = true;
					return flag;
				}
				if (this.UsedByRootField(method.ReturnType))
				{
					return true;
				}
			}
			return false;
		}

		private bool UsedByRootMethod(object method)
		{
			bool flag;
			MethodDefinition methodDef = method as MethodDefinition;
			if (methodDef != null)
			{
				MethodBody body = methodDef.Body;
				if (methodDef.HasBody)
				{
					foreach (Instruction instruction in body.Instructions)
					{
						MethodReference reference = instruction.Operand as MethodReference;
						if (reference == null)
						{
							continue;
						}
						if (reference is GenericInstanceMethod)
						{
							reference = ((GenericInstanceMethod)reference).ElementMethod;
						}
						if (!(this.root is MethodReference) || !(((MethodReference)this.root).FullName == reference.FullName))
						{
							if (!AnalyzerHelper.VirtualEquals((MethodReference)this.root, reference))
							{
								continue;
							}
							flag = true;
							return flag;
						}
						else
						{
							flag = true;
							return flag;
						}
					}
				}
			}
			ICustomAttributeProvider provider = method as ICustomAttributeProvider;
			if (provider != null && this.UsedByRootCstAttrCtor(provider))
			{
				return true;
			}
			if (methodDef != null)
			{
				if (this.UsedByRootCstAttrCtor(methodDef.MethodReturnType))
				{
					return true;
				}
				foreach (ParameterDefinition param in methodDef.Parameters)
				{
					if (!this.UsedByRootCstAttrCtor(param))
					{
						continue;
					}
					flag = true;
					return flag;
				}
			}
			return false;
		}

		private bool UsedByRootType(object obj)
		{
			ICustomAttributeProvider provider;
			bool flag;
			MethodDefinition method = obj as MethodDefinition;
			if (method != null)
			{
				if (this.UsedByRootTypeRef(method.ReturnType))
				{
					return true;
				}
				foreach (ParameterDefinition param in method.Parameters)
				{
					if (!this.UsedByRootTypeRef(param.ParameterType))
					{
						continue;
					}
					flag = true;
					return flag;
				}
				if (this.UsedByRootCstAttrType(method.MethodReturnType))
				{
					return true;
				}
				foreach (ParameterDefinition param in method.Parameters)
				{
					if (!this.UsedByRootCstAttrType(param))
					{
						continue;
					}
					flag = true;
					return flag;
				}
			}
			FieldDefinition field = obj as FieldDefinition;
			if (field != null && this.UsedByRootTypeRef(field.FieldType))
			{
				return true;
			}
			PropertyDefinition prop = obj as PropertyDefinition;
			if (prop != null && this.UsedByRootTypeRef(prop.PropertyType))
			{
				return true;
			}
			EventDefinition evt = obj as EventDefinition;
			if (evt != null && this.UsedByRootTypeRef(evt.EventType))
			{
				return true;
			}
			TypeDefinition type = obj as TypeDefinition;
			if (type != null)
			{
				if (this.UsedByRootTypeRef(type.BaseType))
				{
					return true;
				}
				foreach (TypeReference iface in type.Interfaces)
				{
					if (!this.UsedByRootTypeRef(iface))
					{
						continue;
					}
					flag = true;
					return flag;
				}
			}
			if (method != null && method.HasBody)
			{
				MethodBody body = method.Body;
				foreach (Instruction instruction in body.Instructions)
				{
					TypeReference typeRef = instruction.Operand as TypeReference;
					if (typeRef == null || !this.UsedByRootTypeRef(typeRef))
					{
						MemberReference memRef = instruction.Operand as MemberReference;
						if (memRef == null)
						{
							continue;
						}
						if (!this.UsedByRootTypeRef(memRef.DeclaringType))
						{
							MethodReference mtdRef = memRef as MethodReference;
							if (mtdRef != null)
							{
								if (!this.UsedByRootTypeRef(mtdRef.ReturnType))
								{
									foreach (ParameterDefinition param in mtdRef.Parameters)
									{
										if (!this.UsedByRootTypeRef(param.ParameterType))
										{
											continue;
										}
										flag = true;
										return flag;
									}
								}
								else
								{
									flag = true;
									return flag;
								}
							}
							FieldReference fldRef = memRef as FieldReference;
							if (fldRef == null || !this.UsedByRootTypeRef(fldRef.FieldType))
							{
								continue;
							}
							flag = true;
							return flag;
						}
						else
						{
							flag = true;
							return flag;
						}
					}
					else
					{
						flag = true;
						return flag;
					}
				}
				foreach (VariableDefinition var in body.Variables)
				{
					if (!this.UsedByRootTypeRef(var.VariableType))
					{
						continue;
					}
					flag = true;
					return flag;
				}
				Collection<ExceptionHandler>.Enumerator enumerator = body.ExceptionHandlers.GetEnumerator();
				try
				{
					while (enumerator.MoveNext())
					{
						if (!this.UsedByRootTypeRef(enumerator.Current.CatchType))
						{
							continue;
						}
						flag = true;
						return flag;
					}
					provider = obj as ICustomAttributeProvider;
					if (provider == null)
					{
						return false;
					}
					return this.UsedByRootCstAttrType(provider);
				}
				finally
				{
					((IDisposable)enumerator).Dispose();
				}
			}
			else
			{
				provider = obj as ICustomAttributeProvider;
				if (provider == null)
				{
					return false;
				}
				return this.UsedByRootCstAttrType(provider);
			}
			return flag;
		}

		private bool UsedByRootTypeRef(TypeReference typeRef)
		{
			GenericInstanceType genInst;
			bool flag;
			if (!(typeRef is TypeSpecification) && !(typeRef is GenericParameter) && this.root.Equals(typeRef))
			{
				return true;
			}
			ArrayType type = typeRef as ArrayType;
			if (type != null)
			{
				return this.UsedByRootTypeRef(type.ElementType);
			}
			PointerType type2 = typeRef as PointerType;
			if (type2 != null)
			{
				return this.UsedByRootTypeRef(type2.ElementType);
			}
			ByReferenceType type3 = typeRef as ByReferenceType;
			if (type3 != null)
			{
				return this.UsedByRootTypeRef(type3.ElementType);
			}
			OptionalModifierType modifier = typeRef as OptionalModifierType;
			if (modifier != null)
			{
				if (this.UsedByRootTypeRef(modifier.ElementType))
				{
					return true;
				}
				return this.UsedByRootTypeRef(modifier.ModifierType);
			}
			RequiredModifierType modifier2 = typeRef as RequiredModifierType;
			if (modifier2 != null)
			{
				if (this.UsedByRootTypeRef(modifier2.ElementType))
				{
					return true;
				}
				return this.UsedByRootTypeRef(modifier2.ModifierType);
			}
			FunctionPointerType pointer = typeRef as FunctionPointerType;
			if (pointer != null)
			{
				if (this.UsedByRootTypeRef(pointer.ReturnType))
				{
					return true;
				}
				Collection<ParameterDefinition>.Enumerator enumerator = pointer.Parameters.GetEnumerator();
				try
				{
					while (enumerator.MoveNext())
					{
						if (!this.UsedByRootTypeRef(enumerator.Current.ParameterType))
						{
							continue;
						}
						flag = true;
						return flag;
					}
					genInst = typeRef as GenericInstanceType;
					if (pointer != null && this.root.Equals(genInst.ElementType))
					{
						return true;
					}
					return false;
				}
				finally
				{
					((IDisposable)enumerator).Dispose();
				}
				return flag;
			}
			genInst = typeRef as GenericInstanceType;
			if (pointer != null && this.root.Equals(genInst.ElementType))
			{
				return true;
			}
			return false;
		}
	}
}