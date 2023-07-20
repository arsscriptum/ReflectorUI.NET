using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;

namespace Reflector.UI
{
	internal class InstantiatedByNode : AnalyzeNode
	{
		private AssemblyDefinition scope;

		private MembersEnumerator enumerator;

		public InstantiatedByNode(TypeReference obj, AssemblyDefinition scope) : base(obj)
		{
			base.SetValue(BaseNode.TextPropertyKey, (scope == null ? "Instantiated By" : string.Concat("Instantiated In '", scope.Name.Name, "' By")));
			this.scope = scope;
		}

		protected override Freezable CreateInstanceCore()
		{
			return new InstantiatedByNode((TypeReference)this.obj, this.scope);
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
			List<MethodDefinition> list = new List<MethodDefinition>();
			AssemblyDefinition[] scopes = null;
			scopes = (this.scope == null ? ((IEnumerable<AssemblyDefinition>)App.Reflector.GetService("AsmMgr").GetProp("AsmMgr.Assemblies")).ToArray<AssemblyDefinition>() : new AssemblyDefinition[] { this.scope });
			this.enumerator = new MembersEnumerator(scopes, false, true);
			while (this.enumerator.MoveNext())
			{
				MethodDefinition current = this.enumerator.Current as MethodDefinition;
				if (current == null || !current.HasBody)
				{
					continue;
				}
				foreach (Instruction instruction in current.Body.Instructions)
				{
					TypeReference type = null;
					if (instruction.OpCode == OpCodes.Newobj)
					{
						MethodReference method = instruction.Operand as MethodReference;
						if (method != null && method.Name == ".ctor")
						{
							type = method.DeclaringType.GetElementType();
						}
					}
					else if (instruction.OpCode == OpCodes.Initobj)
					{
						type = (instruction.Operand as TypeReference).GetElementType();
					}
					if (type == null || !type.Equals(base.ReflectorObject))
					{
						continue;
					}
					list.Add(current);
					break;
				}
			}
			list.Sort((MethodDefinition x, MethodDefinition y) => Comparer<string>.Default.Compare(AsmViewHelper.GetFullText(x), AsmViewHelper.GetFullText(y)));
			return list.Cast<object>();
		}
	}
}