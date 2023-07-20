using Mono.Cecil;
using Mono.Collections.Generic;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Reflector.UI
{
	internal class TypesEnumerator : IEnumerator<TypeDefinition>, IDisposable, IEnumerator, IProgressProvider
	{
		private int moduleIndex;

		private ModuleDefinition[] modules;

		private int index;

		private List<TypeDefinition> types;

		public TypeDefinition Current
		{
			get
			{
				return this.types[this.index];
			}
		}

		object System.Collections.IEnumerator.Current
		{
			get
			{
				return this.Current;
			}
		}

		public TypesEnumerator(AssemblyDefinition[] assemblies)
		{
			List<ModuleDefinition> list = new List<ModuleDefinition>();
			AssemblyDefinition[] assemblyDefinitionArray = assemblies;
			for (int i = 0; i < (int)assemblyDefinitionArray.Length; i++)
			{
				list.AddRange(assemblyDefinitionArray[i].Modules);
			}
			this.modules = list.ToArray();
			this.types = new List<TypeDefinition>();
			this.Reset();
		}

		public TypesEnumerator(ModuleDefinition[] modules)
		{
			this.modules = modules;
			this.types = new List<TypeDefinition>();
			this.Reset();
		}

		public TypesEnumerator(TypeDefinition[] types)
		{
			this.modules = new ModuleDefinition[1];
			this.types = new List<TypeDefinition>(types);
			this.Reset();
		}

		public void Dispose()
		{
		}

		private ICollection<TypeDefinition> GetNestedTypes(TypeDefinition type)
		{
			List<TypeDefinition> list = new List<TypeDefinition>();
			foreach (TypeDefinition nestedType in type.NestedTypes)
			{
				list.Add(nestedType);
				list.AddRange(this.GetNestedTypes(nestedType));
			}
			return list;
		}

		public int GetProgress()
		{
			if (this.types != null && this.modules != null && (int)this.modules.Length != 0)
			{
				if (this.types.Count == 0)
				{
					return (this.moduleIndex + 1) * 100 / (int)this.modules.Length;
				}
				int num = this.types.Count * this.moduleIndex + this.index;
				int num2 = this.types.Count * (int)this.modules.Length;
				if (num2 != 0)
				{
					return num * 100 / num2;
				}
			}
			return 100;
		}

		public bool MoveNext()
		{
			while (this.types == null || this.index + 1 >= this.types.Count)
			{
				if (this.moduleIndex + 1 >= (int)this.modules.Length)
				{
					return false;
				}
				this.moduleIndex++;
				ModuleDefinition module = this.modules[this.moduleIndex];
				if (module == null)
				{
					this.moduleIndex++;
					return this.MoveNext();
				}
				this.index = -1;
				this.types.Clear();
				foreach (TypeDefinition type in module.Types)
				{
					this.types.Add(type);
					this.types.AddRange(this.GetNestedTypes(type));
				}
			}
			this.index++;
			return true;
		}

		public void Reset()
		{
			if (this.modules == null || (int)this.modules.Length != 1 || this.modules[0] != null)
			{
				this.moduleIndex = -1;
				this.types.Clear();
			}
			else
			{
				this.moduleIndex = 0;
			}
			this.index = -1;
		}
	}
}