using Mono.Cecil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Reflector.UI
{
	internal class MembersEnumerator : IEnumerator<MemberReference>, IDisposable, IEnumerator, IProgressProvider
	{
		private TypesEnumerator scopeEnum;

		private IEnumerator<MemberReference> internalEnum;

		private bool enumTypes;

		private bool enumMems;

		public MemberReference Current
		{
			get
			{
				return this.internalEnum.Current;
			}
		}

		object System.Collections.IEnumerator.Current
		{
			get
			{
				return this.internalEnum.Current;
			}
		}

		public MembersEnumerator(AssemblyDefinition[] assemblies)
		{
			this.scopeEnum = new TypesEnumerator(assemblies);
			this.enumTypes = true;
			this.enumMems = true;
			this.Reset();
		}

		public MembersEnumerator(ModuleDefinition[] modules)
		{
			this.scopeEnum = new TypesEnumerator(modules);
			this.enumTypes = true;
			this.enumMems = true;
			this.Reset();
		}

		public MembersEnumerator(TypeDefinition[] types)
		{
			this.scopeEnum = new TypesEnumerator(types);
			this.enumTypes = true;
			this.enumMems = true;
			this.Reset();
		}

		public MembersEnumerator(AssemblyDefinition[] assemblies, bool enumTypes, bool enumMems)
		{
			this.scopeEnum = new TypesEnumerator(assemblies);
			this.enumTypes = enumTypes;
			this.enumMems = enumMems;
			this.Reset();
		}

		public void Dispose()
		{
		}

		public int GetProgress()
		{
			return this.scopeEnum.GetProgress();
		}

		public bool MoveNext()
		{
			while (this.internalEnum == null || !this.internalEnum.MoveNext())
			{
				if (!this.scopeEnum.MoveNext())
				{
					return false;
				}
				TypeDefinition current = this.scopeEnum.Current;
				List<MemberReference> list = new List<MemberReference>();
				if (this.enumTypes)
				{
					list.Add(current);
				}
				if (this.enumMems)
				{
					list.AddRange(current.Fields.OfType<MemberReference>());
					list.AddRange(current.Methods.OfType<MemberReference>());
					list.AddRange(current.Properties.OfType<MemberReference>());
					list.AddRange(current.Events.OfType<MemberReference>());
				}
				this.internalEnum = list.GetEnumerator();
			}
			return true;
		}

		public void Reset()
		{
			this.scopeEnum.Reset();
			this.internalEnum = null;
		}
	}
}