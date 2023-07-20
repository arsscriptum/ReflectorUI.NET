using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace Reflector.UI
{
	public class MenuConverter : IValueConverter
	{
		public static MenuConverter Instance;

		static MenuConverter()
		{
			MenuConverter.Instance = new MenuConverter();
		}

		public MenuConverter()
		{
		}

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null)
			{
				return new ContextMenu()
				{
					IsEnabled = false,
					Visibility = Visibility.Hidden
				};
			}
			DependencyObject par = (DependencyObject)parameter;
			IReflector reflec = par as IReflector;
			if (reflec == null)
			{
				reflec = MenuConverter.FindParent<IReflector>(par);
			}
			else if (reflec == null)
			{
				reflec = MenuConverter.FindParent<ReflecWindow>(par)._App;
			}
			return reflec.BarsManager.GetBar((string)value);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
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
			return MenuConverter.FindParent<T>(parentObject);
		}
	}
}