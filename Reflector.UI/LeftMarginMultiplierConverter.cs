using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace Reflector.UI
{
	public class LeftMarginMultiplierConverter : IValueConverter
	{
		public double Length
		{
			get;
			set;
		}

		public LeftMarginMultiplierConverter()
		{
		}

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			TreeViewItem item = value as TreeViewItem;
			if (item == null)
			{
				return 1;
			}
			return this.Length * (double)LeftMarginMultiplierConverter.GetDepth(item);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}

		public static T FindParent<T>(DependencyObject child)
		where T : DependencyObject
		{
			DependencyObject parentObject = VisualTreeHelper.GetParent(child);
			if (parentObject == null)
			{
				return default(T);
			}
			T parent = (T)(parentObject as T);
			if (parent != null)
			{
				return parent;
			}
			return LeftMarginMultiplierConverter.FindParent<T>(parentObject);
		}

		private static int GetDepth(TreeViewItem item)
		{
			TreeViewItem tvi = LeftMarginMultiplierConverter.FindParent<TreeViewItem>(item);
			if (tvi == null)
			{
				return 1;
			}
			return LeftMarginMultiplierConverter.GetDepth(tvi) + 1;
		}
	}
}