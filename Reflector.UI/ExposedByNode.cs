using Mono.Cecil;
using Mono.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;

namespace Reflector.UI
{
	internal class ExposedByNode : AnalyzeNode
	{
		private AssemblyDefinition scope;

		private MembersEnumerator enumerator;

		public ExposedByNode(TypeReference obj, AssemblyDefinition scope) : base(obj)
		{
			base.SetValue(BaseNode.TextPropertyKey, (scope == null ? "Exposed By" : string.Concat("Exposed In '", scope.Name.Name, "' By")));
			this.scope = scope;
		}

		protected override Freezable CreateInstanceCore()
		{
			return new ExposedByNode((TypeReference)this.obj, this.scope);
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
			List<FieldDefinition> fldList = new List<FieldDefinition>();
			List<MethodDefinition> mtdList = new List<MethodDefinition>();
			List<PropertyDefinition> propList = new List<PropertyDefinition>();
			List<EventDefinition> evtList = new List<EventDefinition>();
			AssemblyDefinition[] scopes = null;
			scopes = (this.scope == null ? ((IEnumerable<AssemblyDefinition>)App.Reflector.GetService("AsmMgr").GetProp("AsmMgr.Assemblies")).ToArray<AssemblyDefinition>() : new AssemblyDefinition[] { this.scope });
			this.enumerator = new MembersEnumerator(scopes, false, true);
			while (this.enumerator.MoveNext())
			{
				MemberReference current = this.enumerator.Current;
				if (current == null || current.DeclaringType.Resolve().IsEnum && ((TypeReference)base.ReflectorObject).Equals(current.DeclaringType))
				{
					continue;
				}
				FieldReference field = current as FieldReference;
				if (field != null && base.ReflectorObject.Equals(field.FieldType.GetElementType()))
				{
					fldList.Add(field.Resolve());
				}
				MethodReference method = current as MethodReference;
				if (method != null)
				{
					if (base.ReflectorObject.Equals(method.ReturnType.GetElementType()))
					{
						mtdList.Add(method.Resolve());
					}
					else
					{
						foreach (ParameterDefinition declaration4 in method.Parameters)
						{
							if (!base.ReflectorObject.Equals(declaration4.ParameterType.GetElementType()))
							{
								continue;
							}
							mtdList.Add(method.Resolve());
							break;
						}
					}
				}
				PropertyReference prop = current as PropertyReference;
				if (prop != null)
				{
					if (base.ReflectorObject.Equals(prop.PropertyType.GetElementType()))
					{
						PropertyDefinition propDef = prop.Resolve();
						propList.Add(propDef);
						mtdList.Remove(propDef.GetMethod);
						mtdList.Remove(propDef.SetMethod);
					}
					else
					{
						foreach (ParameterDefinition declaration6 in prop.Parameters)
						{
							if (!base.ReflectorObject.Equals(declaration6.ParameterType.GetElementType()))
							{
								continue;
							}
							PropertyDefinition propDef = prop.Resolve();
							propList.Add(propDef);
							mtdList.Remove(propDef.GetMethod);
							mtdList.Remove(propDef.SetMethod);
							break;
						}
					}
				}
				EventReference evt = current as EventReference;
				if (evt == null || !base.ReflectorObject.Equals(evt.EventType.GetElementType()))
				{
					continue;
				}
				EventDefinition evtDef = evt.Resolve();
				evtList.Add(evtDef);
				mtdList.Remove(evtDef.AddMethod);
				mtdList.Remove(evtDef.RemoveMethod);
				mtdList.Remove(evtDef.InvokeMethod);
			}
			fldList.Sort((FieldDefinition x, FieldDefinition y) => Comparer<string>.Default.Compare(AsmViewHelper.GetFullText(x), AsmViewHelper.GetFullText(y)));
			mtdList.Sort((MethodDefinition x, MethodDefinition y) => Comparer<string>.Default.Compare(AsmViewHelper.GetFullText(x), AsmViewHelper.GetFullText(y)));
			propList.Sort((PropertyDefinition x, PropertyDefinition y) => Comparer<string>.Default.Compare(AsmViewHelper.GetFullText(x), AsmViewHelper.GetFullText(y)));
			evtList.Sort((EventDefinition x, EventDefinition y) => Comparer<string>.Default.Compare(AsmViewHelper.GetFullText(x), AsmViewHelper.GetFullText(y)));
			return fldList.Cast<object>().Concat<object>(mtdList.Cast<object>()).Concat<object>(propList.Cast<object>()).Concat<object>(evtList.Cast<object>());
		}
	}
}