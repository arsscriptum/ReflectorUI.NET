using System;
using System.Windows;

namespace Reflector.UI
{
	internal class ErrorNode : BaseNode
	{
		public ErrorNode(string status)
		{
			base.SetValue(BaseNode.TextPropertyKey, status);
			base.SetValue(BaseNode.IconPropertyKey, Application.Current.Resources["err"]);
		}

		protected override Freezable CreateInstanceCore()
		{
			return new ErrorNode(base.Text);
		}
	}
}