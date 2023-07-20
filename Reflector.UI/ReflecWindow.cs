using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace Reflector.UI
{
	public abstract class ReflecWindow : Control
	{
		protected static DependencyPropertyKey TitlePropertyKey;

		public static DependencyProperty TitleProperty;

		protected static DependencyPropertyKey IconPropertyKey;

		public static DependencyProperty IconProperty;

		protected static DependencyPropertyKey ParentServicePropertyKey;

		public static DependencyProperty ParentServiceProperty;

		protected static DependencyPropertyKey IdPropertyKey;

		public static DependencyProperty IdProperty;

		private IReflector app;

		public IReflector _App
		{
			get
			{
				return this.app;
			}
			set
			{
				IReflector orig = this.app;
				this.app = value;
				if (this.app != orig)
				{
					this.OnAppAttached();
				}
			}
		}

		public ImageSource Icon
		{
			get
			{
				return (ImageSource)base.GetValue(ReflecWindow.IconProperty);
			}
		}

		public string Id
		{
			get
			{
				return (string)base.GetValue(ReflecWindow.IdProperty);
			}
		}

		public IReflecService ParentService
		{
			get
			{
				return (IReflecService)base.GetValue(ReflecWindow.ParentServiceProperty);
			}
		}

		public string Title
		{
			get
			{
				return (string)base.GetValue(ReflecWindow.TitleProperty);
			}
		}

		static ReflecWindow()
		{
			ReflecWindow.TitlePropertyKey = DependencyProperty.RegisterReadOnly("Title", typeof(string), typeof(ReflecWindow), new PropertyMetadata(null));
			ReflecWindow.TitleProperty = ReflecWindow.TitlePropertyKey.DependencyProperty;
			ReflecWindow.IconPropertyKey = DependencyProperty.RegisterReadOnly("Icon", typeof(ImageSource), typeof(ReflecWindow), new PropertyMetadata(null));
			ReflecWindow.IconProperty = ReflecWindow.IconPropertyKey.DependencyProperty;
			ReflecWindow.ParentServicePropertyKey = DependencyProperty.RegisterReadOnly("ParentService", typeof(IReflecService), typeof(ReflecWindow), new PropertyMetadata(null));
			ReflecWindow.ParentServiceProperty = ReflecWindow.ParentServicePropertyKey.DependencyProperty;
			ReflecWindow.IdPropertyKey = DependencyProperty.RegisterReadOnly("Id", typeof(string), typeof(ReflecWindow), new PropertyMetadata(null));
			ReflecWindow.IdProperty = ReflecWindow.IdPropertyKey.DependencyProperty;
		}

		public ReflecWindow()
		{
			FocusManager.SetIsFocusScope(this, true);
			DependencyProperty fontFamilyProperty = Control.FontFamilyProperty;
			Binding binding = new Binding("UIFont")
			{
				Source = Options.Instance
			};
			base.SetBinding(fontFamilyProperty, binding);
			DependencyProperty fontSizeProperty = Control.FontSizeProperty;
			Binding binding1 = new Binding("UIFontSize")
			{
				Source = Options.Instance
			};
			base.SetBinding(fontSizeProperty, binding1);
		}

		protected virtual void OnAppAttached()
		{
		}
	}
}