using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;

namespace Reflector.UI
{
	internal class PInvokeNode : AnalyzeNode
	{
		private MembersEnumerator enumerator;

		public PInvokeNode(object obj) : base(obj)
		{
			base.SetValue(BaseNode.TextPropertyKey, "P/Invoke Imports");
		}

		protected override Freezable CreateInstanceCore()
		{
			return new PInvokeNode(this.obj);
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
				this.enumerator = new MembersEnumerator(new ModuleDefinition[] { module });
			}
			AssemblyDefinition assembly = base.ReflectorObject as AssemblyDefinition;
			if (assembly != null)
			{
				this.enumerator = new MembersEnumerator(new AssemblyDefinition[] { assembly });
			}
			if (this.enumerator == null)
			{
				return new BaseNode[0];
			}
			try
			{
				SortedList<ModuleReference, SortedList<string, MethodDefinition>> list = new SortedList<ModuleReference, SortedList<string, MethodDefinition>>(new DelegateComparer<ModuleReference>((ModuleReference x, ModuleReference y) => Comparer<string>.Default.Compare(x.Name, y.Name)));
				while (this.enumerator.MoveNext())
				{
					ModuleReference key = null;
					MethodDefinition value = null;
					MethodDefinition method = this.enumerator.Current as MethodDefinition;
					if (method != null && method.HasPInvokeInfo)
					{
						key = method.PInvokeInfo.Module;
						value = method;
					}
					if (key == null)
					{
						continue;
					}
					SortedList<string, MethodDefinition> values = null;
					if (!list.ContainsKey(key))
					{
						values = new SortedList<string, MethodDefinition>();
						list[key] = values;
					}
					else
					{
						values = list[key];
					}
					values[AsmViewHelper.GetFullText(value)] = value;
				}
				List<object> ret = new List<object>();
				foreach (KeyValuePair<ModuleReference, SortedList<string, MethodDefinition>> moduleReference in list)
				{
					List<object> objs = new List<object>();
					foreach (KeyValuePair<string, MethodDefinition> j in moduleReference.Value)
					{
						objs.Add(j.Value);
					}
					LazyFolderNode n = (LazyFolderNode)base.Dispatcher.Invoke(new Func<LazyFolderNode>(() => {
						LazyFolderNode r = new LazyFolderNode(moduleReference.Key.Name, objs);
						r.SetValue(BaseNode.IconPropertyKey, AsmViewHelper.GetIcon(moduleReference.Key));
						AnalyzeReflectorNode.SetParent(r, this);
						return r;
					}), new object[0]);
					ret.Add(n);
				}
				errorNode = ret;
			}
			catch (Exception exception)
			{
				errorNode = new BaseNode[] { new ErrorNode(exception.Message) };
			}
			return errorNode;
		}
	}
}