using System;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;

namespace Reflector.UI
{
	public class BarExtension : MarkupExtension
	{
		private string key;

		public string Key
		{
			get
			{
				return this.key;
			}
			set
			{
				this.key = value;
			}
		}

		public BarExtension()
		{
		}

		public BarExtension(string key)
		{
			this.key = key;
		}

		private static T FindParent<T>(DependencyObject child)
		where T : class
		{
			DependencyObject parentObject = VisualTreeHelper.GetParent(child);
			if (parentObject == null)
			{
				parentObject = LogicalTreeHelper.GetParent(child);
				if (parentObject == null)
				{
					return default(T);
				}
			}
			T parent = (T)(parentObject as T);
			if (parent != null)
			{
				return parent;
			}
			return BarExtension.FindParent<T>(parentObject);
		}

		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			if (this.key == null)
			{
				return null;
			}
			IProvideValueTarget target = (IProvideValueTarget)serviceProvider.GetService(typeof(IProvideValueTarget));
			if (target == null)
			{
				throw new NotSupportedException();
			}
			if (target.TargetObject.GetType().FullName == "System.Windows.SharedDp")
			{
				return this;
			}
			IReflector reflec = BarExtension.FindParent<IReflector>((DependencyObject)target.TargetObject) ?? BarExtension.FindParent<ReflecWindow>((DependencyObject)target.TargetObject)._App;
			return reflec.BarsManager.GetBar(this.key);
		}
	}
}