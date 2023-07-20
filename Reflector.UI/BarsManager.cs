using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Reflector.UI
{
	public class BarsManager
	{
		private Dictionary<string, object> bars = new Dictionary<string, object>();

		public BarsManager()
		{
		}

		public void AddBar(string key, object bar)
		{
			if (bar is MenuItem)
			{
				MenuItem item = (MenuItem)bar;
				DependencyProperty fontFamilyProperty = Control.FontFamilyProperty;
				Binding binding = new Binding("UIFont")
				{
					Source = Options.Instance
				};
				item.SetBinding(fontFamilyProperty, binding);
				DependencyProperty fontSizeProperty = Control.FontSizeProperty;
				Binding binding1 = new Binding("UIFontSize")
				{
					Source = Options.Instance
				};
				item.SetBinding(fontSizeProperty, binding1);
			}
			else if (!(bar is ToolBar))
			{
				ContextMenu m = (ContextMenu)bar;
				DependencyProperty dependencyProperty = Control.FontFamilyProperty;
				Binding binding2 = new Binding("UIFont")
				{
					Source = Options.Instance
				};
				m.SetBinding(dependencyProperty, binding2);
				DependencyProperty fontSizeProperty1 = Control.FontSizeProperty;
				Binding binding3 = new Binding("UIFontSize")
				{
					Source = Options.Instance
				};
				m.SetBinding(fontSizeProperty1, binding3);
			}
			else
			{
				ToolBar m = (ToolBar)bar;
				DependencyProperty fontFamilyProperty1 = Control.FontFamilyProperty;
				Binding binding4 = new Binding("UIFont")
				{
					Source = Options.Instance
				};
				m.SetBinding(fontFamilyProperty1, binding4);
				DependencyProperty dependencyProperty1 = Control.FontSizeProperty;
				Binding binding5 = new Binding("UIFontSize")
				{
					Source = Options.Instance
				};
				m.SetBinding(dependencyProperty1, binding5);
			}
			this.bars.Add(key, bar);
		}

		public object GetBar(string key)
		{
			if (!this.bars.ContainsKey(key))
			{
				return null;
			}
			return this.bars[key];
		}

		internal static MenuItem GetMenuItem(string text, ICommand command, string imgKey)
		{
			object ico;
			if (imgKey != null)
			{
				Bitmap bitmap = new Bitmap()
				{
					Source = (BitmapSource)Application.Current.Resources[imgKey]
				};
				Bitmap img = bitmap;
				Border border = new Border()
				{
					Child = img,
					Width = 16,
					Height = 16
				};
				ico = border;
				RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.NearestNeighbor);
			}
			else
			{
				Control control = new Control()
				{
					Width = 16,
					Height = 16
				};
				ico = control;
			}
			TextBlock textBlock = new TextBlock()
			{
				Text = text,
				Margin = new Thickness(2)
			};
			TextBlock header = textBlock;
			MenuItem menuItem = new MenuItem()
			{
				Header = header,
				Command = command,
				Icon = ico
			};
			return menuItem;
		}

		internal static Button GetToolBarButton(string imgKey, ICommand command)
		{
			object ico;
			if (imgKey != null)
			{
				Bitmap bitmap = new Bitmap()
				{
					Source = (BitmapSource)Application.Current.Resources[imgKey]
				};
				Bitmap img = bitmap;
				Border border = new Border()
				{
					Child = img,
					Width = 16,
					Height = 16
				};
				ico = border;
				RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.NearestNeighbor);
			}
			else
			{
				Control control = new Control()
				{
					Width = 16,
					Height = 16
				};
				ico = control;
			}
			Button button = new Button()
			{
				Content = ico,
				Margin = new Thickness(2),
				Command = command,
				Width = 20,
				Height = 20
			};
			return button;
		}
	}
}