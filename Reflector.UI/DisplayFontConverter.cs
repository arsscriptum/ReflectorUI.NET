using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Reflector.UI
{
	internal class DisplayFontConverter : IValueConverter
	{
		private FontFamily def = new FontFamily("Global User Interface");

		public static DisplayFontConverter Instance;

		static DisplayFontConverter()
		{
			DisplayFontConverter.Instance = new DisplayFontConverter();
		}

		public DisplayFontConverter()
		{
		}

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			GlyphTypeface typeface2;
			object obj;
			object obj1;
			if (value == null)
			{
				return null;
			}
			using (IEnumerator<Typeface> enumerator = ((FontFamily)value).GetTypefaces().GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					Typeface typeface = enumerator.Current;
					try
					{
						if (typeface.TryGetGlyphTypeface(out typeface2))
						{
							if (typeface2.Symbol)
							{
								obj1 = this.def;
							}
							else
							{
								obj1 = value;
							}
							obj = obj1;
							return obj;
						}
					}
					catch
					{
						obj = this.def;
						return obj;
					}
				}
				return value;
			}
			return obj;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}