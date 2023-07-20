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
	internal class AssignedByNode : AnalyzeNode
	{
		private AssemblyDefinition scope;

		private MembersEnumerator enumerator;

		public AssignedByNode(FieldReference obj, AssemblyDefinition scope) : base(obj)
		{
			base.SetValue(BaseNode.TextPropertyKey, (scope == null ? "Assigned By" : string.Concat("Assigned In '", scope.Name.Name, "' By")));
			this.scope = scope;
		}

		protected override Freezable CreateInstanceCore()
		{
			return new AssignedByNode((FieldReference)this.obj, this.scope);
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
					bool found = false;
					if (instruction.OpCode == OpCodes.Ldflda)
					{
						FieldReference field = instruction.Operand as FieldReference;
						if (field != null && field.FullName == ((FieldReference)base.ReflectorObject).FullName && instruction.Next != null)
						{
							Instruction next = instruction.Next;
							if (next.OpCode == OpCodes.Call || next.OpCode == OpCodes.Callvirt)
							{
								MethodReference callee = next.Operand as MethodReference;
								if (callee != null && callee.Parameters.Count > 0 && callee.Parameters[callee.Parameters.Count - 1].ParameterType is ByReferenceType)
								{
									found = true;
								}
							}
						}
					}
					else if (instruction.OpCode == OpCodes.Stfld || instruction.OpCode == OpCodes.Stsfld)
					{
						FieldReference field = instruction.Operand as FieldReference;
						if (field != null && field.FullName == ((FieldReference)base.ReflectorObject).FullName)
						{
							found = true;
						}
					}
					if (!found)
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