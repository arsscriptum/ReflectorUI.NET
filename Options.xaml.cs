using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml;

namespace Reflector.UI
{
	public partial class Options : Window, IReflecService
	{
		public readonly static Options Instance;

		public readonly static DependencyProperty DisassemblerFontProperty;

		public readonly static DependencyProperty DisassemblerFontSizeProperty;

		public readonly static DependencyProperty UIFontProperty;

		public readonly static DependencyProperty UIFontSizeProperty;

		public readonly static DependencyProperty ThemeProperty;

		public readonly static DependencyProperty ShortensNameProperty;

		public readonly static DependencyProperty OptimizationProperty;

		public readonly static DependencyProperty NumberFormatProperty;

		private IReflector app;

		public IReflector _App
		{
			get
			{
				return this.app;
			}
			set
			{
				this.app = value;
			}
		}

		public System.Windows.Media.FontFamily DisassemblerFont
		{
			get
			{
				return (System.Windows.Media.FontFamily)base.GetValue(Options.DisassemblerFontProperty);
			}
			set
			{
				base.SetValue(Options.DisassemblerFontProperty, value);
			}
		}

		public int DisassemblerFontSize
		{
			get
			{
				return (int)base.GetValue(Options.DisassemblerFontSizeProperty);
			}
			set
			{
				base.SetValue(Options.DisassemblerFontSizeProperty, value);
			}
		}

		public static int[] FontSizes
		{
			get
			{
				return new int[] { 8, 9, 10, 11, 12, 14, 16, 18, 20, 22, 24, 26, 36, 48, 72 };
			}
		}

		public string Id
		{
			get
			{
				return "Options";
			}
		}

		public string NumberFormat
		{
			get
			{
				return (string)base.GetValue(Options.NumberFormatProperty);
			}
			set
			{
				base.SetValue(Options.NumberFormatProperty, value);
			}
		}

		public static string[] NumberFormats
		{
			get
			{
				return new string[] { "Auto", "Hexadecimal", "Decimal" };
			}
		}

		public string Optimization
		{
			get
			{
				return (string)base.GetValue(Options.OptimizationProperty);
			}
			set
			{
				base.SetValue(Options.OptimizationProperty, value);
			}
		}

		public static string[] Optimizations
		{
			get
			{
				return new string[] { "None", "1.0", "2.0", "3.5", "4.0" };
			}
		}

		public bool ShortensName
		{
			get
			{
				if (base.CheckAccess())
				{
					return (bool)base.GetValue(Options.ShortensNameProperty);
				}
				System.Windows.Threading.Dispatcher dispatcher = base.Dispatcher;
				Func<DependencyProperty, object> func = new Func<DependencyProperty, object>(this.GetValue);
				object[] shortensNameProperty = new object[] { Options.ShortensNameProperty };
				return (bool)dispatcher.Invoke(func, shortensNameProperty);
			}
			set
			{
				base.SetValue(Options.ShortensNameProperty, value);
			}
		}

		public string Theme
		{
			get
			{
				return (string)base.GetValue(Options.ThemeProperty);
			}
			set
			{
				base.SetValue(Options.ThemeProperty, value);
			}
		}

		public static string[] Themes
		{
			get
			{
				return new string[] { "ExpressionDark", "ExpressionLight", "Classic" };
			}
		}

		public System.Windows.Media.FontFamily UIFont
		{
			get
			{
				return (System.Windows.Media.FontFamily)base.GetValue(Options.UIFontProperty);
			}
			set
			{
				base.SetValue(Options.UIFontProperty, value);
			}
		}

		public int UIFontSize
		{
			get
			{
				return (int)base.GetValue(Options.UIFontSizeProperty);
			}
			set
			{
				base.SetValue(Options.UIFontSizeProperty, value);
			}
		}

		static Options()
		{
			Options.Instance = new Options();
			Options.DisassemblerFontProperty = DependencyProperty.Register("DisassemblerFont", typeof(System.Windows.Media.FontFamily), typeof(Options), new UIPropertyMetadata(new System.Windows.Media.FontFamily("asd")));
			Options.DisassemblerFontSizeProperty = DependencyProperty.Register("DisassemblerFontSize", typeof(int), typeof(Options), new UIPropertyMetadata((object)10));
			Options.UIFontProperty = DependencyProperty.Register("UIFont", typeof(System.Windows.Media.FontFamily), typeof(Options), new UIPropertyMetadata(new System.Windows.Media.FontFamily("Tahoma")));
			Options.UIFontSizeProperty = DependencyProperty.Register("UIFontSize", typeof(int), typeof(Options), new UIPropertyMetadata((object)11));
			Options.ThemeProperty = DependencyProperty.Register("Theme", typeof(string), typeof(Options), new UIPropertyMetadata("ExpressionDark", new PropertyChangedCallback(Options.OnUIChanged)));
			Options.ShortensNameProperty = DependencyProperty.Register("ShortenNames", typeof(bool), typeof(Options), new UIPropertyMetadata(false, new PropertyChangedCallback(Options.OnUIChanged)));
			Options.OptimizationProperty = DependencyProperty.Register("Optimization", typeof(string), typeof(Options), new FrameworkPropertyMetadata("3.5"));
			Options.NumberFormatProperty = DependencyProperty.Register("NumberFormat", typeof(string), typeof(Options), new FrameworkPropertyMetadata("Auto"));
		}

		private Options()
		{
			this.InitializeComponent();
			base.DataContext = this;
		}

		public object Exec(string name, params object[] args)
		{
			if (name != "Options.Show")
			{
				throw new InvalidOperationException(name);
			}
			return base.ShowDialog();
		}

		public object GetProp(string name)
		{
			if (name == "Options.DisFont")
			{
				return this.DisassemblerFont;
			}
			if (name == "Options.DisFontSize")
			{
				return this.DisassemblerFontSize;
			}
			if (name == "Options.UIFontSize")
			{
				return this.UIFontSize;
			}
			if (name == "Options.Theme")
			{
				return this.Optimization;
			}
			if (name == "Options.Optimization")
			{
				return this.Optimization;
			}
			if (name != "Options.NumberFormat")
			{
				throw new InvalidOperationException(name);
			}
			return this.NumberFormat;
		}

		public void LoadSettings(XmlNode node)
		{
			this.DisassemblerFontSize = int.Parse(node.SelectSingleNode("fontsizes/@disasm").Value);
			this.UIFontSize = int.Parse(node.SelectSingleNode("fontsizes/@ui").Value);
			this.DisassemblerFont = new System.Windows.Media.FontFamily(node.SelectSingleNode("fonts/@disasm").Value);
			this.UIFont = new System.Windows.Media.FontFamily(node.SelectSingleNode("fonts/@ui").Value);
			this.Optimization = node.SelectSingleNode("disasm/@optimize").Value;
			this.NumberFormat = node.SelectSingleNode("disasm/@numFormat").Value;
			XmlNode n = node.SelectSingleNode("@short");
			if (n != null)
			{
				this.ShortensName = bool.Parse(n.Value);
			}
			this.Theme = node.SelectSingleNode("@theme").Value;
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			base.Visibility = System.Windows.Visibility.Hidden;
			e.Cancel = true;
		}

		private static void OnUIChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			string theme1 = "pack://application:,,,/Themes/{0}.xaml";
			string theme2 = "pack://application:,,,/AvalonDock.Themes;component/themes/{0}.xaml";
			Collection<ResourceDictionary> mergedDictionaries = Application.Current.Resources.MergedDictionaries[0].MergedDictionaries;
			ResourceDictionary resourceDictionaries = new ResourceDictionary()
			{
				Source = new Uri(string.Format(theme1, ((Options)sender).Theme))
			};
			mergedDictionaries[0] = resourceDictionaries;
			Collection<ResourceDictionary> mergedDictionaries1 = Application.Current.Resources.MergedDictionaries[0].MergedDictionaries;
			ResourceDictionary resourceDictionaries1 = new ResourceDictionary()
			{
				Source = new Uri(string.Format(theme2, ((Options)sender).Theme))
			};
			mergedDictionaries1[1] = resourceDictionaries1;
			Collection<ResourceDictionary> mergedDictionaries2 = Application.Current.Resources.MergedDictionaries;
			ResourceDictionary resourceDictionaries2 = new ResourceDictionary()
			{
				Source = new Uri("pack://application:,,,/Generic.xaml")
			};
			mergedDictionaries2[1] = resourceDictionaries2;
			if (Options.ThemeChanged != null)
			{
				Options.ThemeChanged(sender, EventArgs.Empty);
			}
		}

		public void SaveSettings(XmlDocument doc, XmlNode node)
		{
			XmlAttribute attr = doc.CreateAttribute("theme");
			attr.Value = this.Theme;
			node.Attributes.Append(attr);
			attr = doc.CreateAttribute("short");
			attr.Value = this.ShortensName.ToString();
			node.Attributes.Append(attr);
			XmlElement element = doc.CreateElement("fonts");
			attr = doc.CreateAttribute("disasm");
			attr.Value = this.DisassemblerFont.Source;
			element.Attributes.Append(attr);
			attr = doc.CreateAttribute("ui");
			attr.Value = this.UIFont.Source;
			element.Attributes.Append(attr);
			node.AppendChild(element);
			element = doc.CreateElement("fontsizes");
			attr = doc.CreateAttribute("disasm");
			attr.Value = this.DisassemblerFontSize.ToString();
			element.Attributes.Append(attr);
			attr = doc.CreateAttribute("ui");
			attr.Value = this.DisassemblerFontSize.ToString();
			element.Attributes.Append(attr);
			node.AppendChild(element);
			element = doc.CreateElement("disasm");
			attr = doc.CreateAttribute("optimize");
			attr.Value = this.Optimization;
			element.Attributes.Append(attr);
			attr = doc.CreateAttribute("numFormat");
			attr.Value = this.NumberFormat;
			element.Attributes.Append(attr);
			node.AppendChild(element);
		}

		public void SetProp(string name, object value)
		{
			throw new InvalidOperationException(name);
		}

		public static event EventHandler ThemeChanged;
	}
}