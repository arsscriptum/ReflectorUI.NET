using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Threading;

namespace Reflector.UI
{
	internal class ReflectorNode : LazyNode, IReflectorObjectContainer
	{
		private object obj;

		public object ReflectorObject
		{
			get
			{
				return this.obj;
			}
		}

		public ReflectorNode(object obj)
		{
			this.obj = obj;
			base.SetValue(BaseNode.IconPropertyKey, AsmViewHelper.GetIcon(obj));
			base.SetValue(BaseNode.IsShinePropertyKey, AsmViewHelper.GetVisibility(obj));
			base.SetValue(BaseNode.TextPropertyKey, AsmViewHelper.Escape(AsmViewHelper.GetText(obj)));
			base.SetValue(BaseNode.MenuPropertyKey, AsmViewHelper.GetMenu(obj));
			base.IsMutable = AsmViewHelper.IsMutable(obj);
			if (!AsmViewHelper.HasChildren(obj))
			{
				base.ClearValue(BaseNode.ChildrenPropertyKey);
				base.Initalized = true;
			}
		}

		protected override Freezable CreateInstanceCore()
		{
			return new ReflectorNode(this.obj);
		}

		protected override IEnumerable<object> InitializeItems()
		{
			return AsmViewHelper.GetChildren(base.Dispatcher, this.obj);
		}
	}
}