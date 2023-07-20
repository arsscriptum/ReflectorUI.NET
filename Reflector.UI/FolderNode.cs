using System;
using System.Collections.Generic;
using System.Windows;

namespace Reflector.UI
{
	internal class FolderNode : BaseNode
	{
		public FolderNode(string text, IEnumerable<object> children)
		{
			base.SetValue(BaseNode.IconPropertyKey, Application.Current.Resources["folder"]);
			base.SetValue(BaseNode.TextPropertyKey, text);
			base.SetValue(BaseNode.ChildrenPropertyKey, children);
		}

		protected override Freezable CreateInstanceCore()
		{
			return new FolderNode(base.Text, base.Children);
		}
	}
}