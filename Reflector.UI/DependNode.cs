using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;

namespace Reflector.UI
{
	internal class DependNode : AnalyzeNode
	{
		private DependsOnEnumerator enumerator;

		public DependNode(object obj) : base(obj)
		{
			base.SetValue(BaseNode.TextPropertyKey, "Depends On");
		}

		protected override Freezable CreateInstanceCore()
		{
			return new DependNode(this.obj);
		}

		private DependsOnEnumerator GetEnumerator(object obj)
		{
			ModuleDefinition module = obj as ModuleDefinition;
			if (module != null)
			{
				return new DependsOnEnumerator(module);
			}
			AssemblyDefinition assembly = obj as AssemblyDefinition;
			if (assembly != null)
			{
				return new DependsOnEnumerator(assembly);
			}
			MethodReference mtdRef = obj as MethodReference;
			if (mtdRef != null)
			{
				MethodDefinition mtdDef = mtdRef.Resolve();
				if (mtdDef != null)
				{
					return new DependsOnEnumerator(mtdDef);
				}
			}
			TypeReference typeRef = obj as TypeReference;
			if (typeRef != null)
			{
				TypeDefinition typeDef = typeRef.Resolve();
				if (typeDef != null)
				{
					return new DependsOnEnumerator(typeDef);
				}
			}
			return null;
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
			IEnumerable<object> errorNode;
			ModuleDefinition module = base.ReflectorObject as ModuleDefinition;
			if (module != null)
			{
				AssemblyDefinition assembly = module.Assembly;
			}
			AssemblyDefinition reflectorObject = base.ReflectorObject as AssemblyDefinition;
			try
			{
				this.enumerator = this.GetEnumerator(base.ReflectorObject);
				if (this.enumerator == null)
				{
					errorNode = new BaseNode[0];
				}
				else
				{
					SortedList<AssemblyNameReference, SortedList<string, MemberReference>> list = new SortedList<AssemblyNameReference, SortedList<string, MemberReference>>(new DelegateComparer<AssemblyNameReference>((AssemblyNameReference x, AssemblyNameReference y) => Comparer<string>.Default.Compare(x.FullName, y.FullName)));
					while (this.enumerator.MoveNext())
					{
						if (!this.enumerator.m000245())
						{
							continue;
						}
						AssemblyNameReference key = null;
						MemberReference value = null;
						TypeReference typeRef = this.enumerator.Current as TypeReference;
						if (typeRef == null)
						{
							MemberReference memRef = this.enumerator.Current as MemberReference;
							if (memRef != null)
							{
								key = AnalyzerHelper.GetAssemblyName(memRef.DeclaringType);
								value = memRef;
							}
						}
						else
						{
							key = AnalyzerHelper.GetAssemblyName(typeRef);
							value = typeRef;
						}
						if (key == null)
						{
							continue;
						}
						SortedList<string, MemberReference> values = null;
						if (!list.ContainsKey(key))
						{
							values = new SortedList<string, MemberReference>();
							list.Add(key, values);
						}
						else
						{
							values = list[key];
						}
						values[AsmViewHelper.GetFullText(value)] = value;
					}
					List<object> ret = new List<object>();
					foreach (KeyValuePair<AssemblyNameReference, SortedList<string, MemberReference>> assemblyNameReference in list)
					{
						List<object> objs = new List<object>();
						foreach (KeyValuePair<string, MemberReference> j in assemblyNameReference.Value)
						{
							objs.Add(j.Value);
						}
						LazyFolderNode n = (LazyFolderNode)base.Dispatcher.Invoke(new Func<LazyFolderNode>(() => {
							LazyFolderNode r = new LazyFolderNode(assemblyNameReference.Key.Name, objs);
							r.SetValue(BaseNode.IconPropertyKey, AsmViewHelper.GetIcon(assemblyNameReference.Key));
							AnalyzeReflectorNode.SetParent(r, this);
							return r;
						}), new object[0]);
						ret.Add(n);
					}
					errorNode = ret;
				}
			}
			catch (Exception exception)
			{
				errorNode = new BaseNode[] { new ErrorNode(exception.Message) };
			}
			return errorNode;
		}
	}
}