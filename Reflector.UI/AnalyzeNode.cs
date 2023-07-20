using System;
using System.Windows;
using System.Windows.Media;

namespace Reflector.UI
{
	internal abstract class AnalyzeNode : LazyProgressNode
	{
		protected object obj;

		public object ReflectorObject
		{
			get
			{
				return this.obj;
			}
		}

		public AnalyzeNode(object obj)
		{
			this.obj = obj;
			base.SetValue(BaseNode.IconPropertyKey, AsmViewHelper.GetThreadStaticIcon((ImageSource)Application.Current.Resources["analyze"]));
			base.IsMutable = true;
		}
	}
}