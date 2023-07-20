using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Reflector.UI
{
	internal class UsedByNode : AnalyzeNode
	{
		private AssemblyDefinition scope;

		private UsedByEnumerator enumerator;

		public UsedByNode(object obj, AssemblyDefinition scope) : base(obj)
		{
			base.SetValue(BaseNode.TextPropertyKey, (scope == null ? "Used By" : string.Concat("Used In '", scope.Name.Name, "' By")));
			this.scope = scope;
		}

		protected override Freezable CreateInstanceCore()
		{
			return new UsedByNode(this.obj, this.scope);
		}

		protected override int GetProgress()
		{
			if (this.enumerator == null)
			{
				return 0;
			}
			return this.enumerator.GetProgress();
		}

		protected override IEnumerable<object> InitializeItems()
		{
			IEnumerable<object> values;
			AssemblyDefinition[] scopes = null;
			scopes = (this.scope == null ? ((IEnumerable<AssemblyDefinition>)App.Reflector.GetService("AsmMgr").GetProp("AsmMgr.Assemblies")).ToArray<AssemblyDefinition>() : new AssemblyDefinition[] { this.scope });
			try
			{
				this.enumerator = new UsedByEnumerator(base.ReflectorObject, scopes);
				if (this.enumerator == null)
				{
					values = new BaseNode[0];
				}
				else
				{
					SortedList<string, object> list = new SortedList<string, object>();
					while (this.enumerator.MoveNext())
					{
						try
						{
							if (this.enumerator.UsedByRoot() && !this.IsParentOf(base.ReflectorObject as TypeReference, this.enumerator.Current as MemberReference))
							{
								list[AsmViewHelper.GetFullText(this.enumerator.Current)] = this.enumerator.Current;
							}
						}
						catch (Exception exception1)
						{
							Exception exception = exception1;
							int hashCode = exception.GetHashCode();
							list[hashCode.ToString()] = new ErrorNode(exception.Message);
						}
					}
					values = list.Values;
				}
			}
			catch (Exception exception2)
			{
				values = new BaseNode[] { new ErrorNode(exception2.Message) };
			}
			return values;
		}

		private bool IsParentOf(TypeReference typeRef, MemberReference memRef)
		{
			if (typeRef == null || memRef == null)
			{
				return false;
			}
			return typeRef.Equals(memRef.DeclaringType);
		}
	}
}