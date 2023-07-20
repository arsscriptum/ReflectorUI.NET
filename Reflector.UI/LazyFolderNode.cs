using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Windows;

namespace Reflector.UI
{
	internal class LazyFolderNode : LazyNode
	{
		private IEnumerable<object> LazyChildren
		{
			get;
			set;
		}

		public LazyFolderNode(string text, IEnumerable<object> children)
		{
			base.SetValue(BaseNode.IconPropertyKey, Application.Current.Resources["folder"]);
			base.SetValue(BaseNode.TextPropertyKey, text);
			this.LazyChildren = children;
		}

		protected override Freezable CreateInstanceCore()
		{
			return new LazyFolderNode(base.Text, this.LazyChildren);
		}

		protected override IEnumerable<object> InitializeItems()
		{
			return this.LazyChildren;
		}
	}
}