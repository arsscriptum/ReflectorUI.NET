using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace Reflector.UI
{
	public partial class About : Window
	{
		private bool canClose;

		public About()
		{
			this.InitializeComponent();
			base.Loaded += new RoutedEventHandler((object sender, RoutedEventArgs e) => this.OnLoad(e));
			string txt = "\r\n<StackPanel Orientation=\"Vertical\"\r\n            xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\r\n            xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">\r\n    <TextBlock Text=\".NET Reflector\" Margin=\"10,5,5,5\" FontSize=\"16\"/>\r\n    <TextBlock Text=\"Originally Developed By Lutz Roeder.\" Margin=\"10,2,5,2\"/>\r\n    <TextBlock Text=\"By Ki (yck) @ Black Storm TEAM\" Margin=\"0,10,5,0\" TextAlignment=\"Right\"/>\r\n</StackPanel>";
			FrameworkElement a = (FrameworkElement)XamlReader.Load(new MemoryStream(Encoding.UTF8.GetBytes(txt)));
			FrameworkElement b = (FrameworkElement)XamlReader.Load(new MemoryStream(Encoding.UTF8.GetBytes(txt)));
			b.Effect = new BlurEffect()
			{
				Radius = 10
			};
			this.txt.Children.Add(a);
			this.txt.Children.Add(b);
			a.Measure(new Size(base.Width, double.PositiveInfinity));
			About height = this;
			double num = height.Height;
			Size desiredSize = a.DesiredSize;
			height.Height = num + (desiredSize.Height + 10);
			base.Clip = new RectangleGeometry(new Rect(0, 0, base.Width, base.Height), 5, 5);
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			if (this.canClose)
			{
				return;
			}
			e.Cancel = true;
			Storyboard sb = new Storyboard();
			DoubleAnimation anim = new DoubleAnimation(0, new Duration(TimeSpan.FromSeconds(0.2)))
			{
				FillBehavior = FillBehavior.HoldEnd
			};
			Storyboard.SetTarget(sb, this);
			Storyboard.SetTargetProperty(sb, new PropertyPath(UIElement.OpacityProperty));
			sb.Children.Add(anim);
			sb.Completed += new EventHandler((object sender, EventArgs ee) => {
				this.canClose = true;
				base.Close();
			});
			sb.Begin();
		}

		private void OnLoad(EventArgs e)
		{
			Storyboard sb = new Storyboard();
			DoubleAnimation anim = new DoubleAnimation(1, new Duration(TimeSpan.FromSeconds(0.2)))
			{
				FillBehavior = FillBehavior.HoldEnd
			};
			Storyboard.SetTarget(sb, this);
			Storyboard.SetTargetProperty(sb, new PropertyPath(UIElement.OpacityProperty));
			sb.Children.Add(anim);
			sb.Begin();
		}

		protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
		{
			base.Close();
		}
	}
}