using AvalonDock;
using Microsoft.Win32;
using Reflector;
using Reflector.CodeModel;
using Reflector.Languages;
using Reflector.Translators;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml;

namespace Reflector.UI
{
	public partial class MainWindow : System.Windows.Window, IReflector, ILanguageManager
	{
		private Dictionary<string, IReflecService> services;

		private bool isRouting;

		private bool isRoutingExe;

		private Reflector.UI.BarsManager menus;

		public readonly static DependencyProperty ActiveLanguageProperty;

		private TranslatorManager mgr;

		private ObservableCollection<ILanguage> languages = new ObservableCollection<ILanguage>();

		public ILanguage ActiveLanguage
		{
			get
			{
				return JustDecompileGenerated_get_ActiveLanguage();
			}
			set
			{
				JustDecompileGenerated_set_ActiveLanguage(value);
			}
		}

		public ILanguage JustDecompileGenerated_get_ActiveLanguage()
		{
			return (ILanguage)base.GetValue(MainWindow.ActiveLanguageProperty);
		}

		public void JustDecompileGenerated_set_ActiveLanguage(ILanguage value)
		{
			base.SetValue(MainWindow.ActiveLanguageProperty, value);
		}

		public Reflector.UI.BarsManager BarsManager
		{
			get
			{
				if (this.menus == null)
				{
					this.menus = new Reflector.UI.BarsManager();
					this.InitializeBars();
				}
				return this.menus;
			}
		}

		public ICollection<ILanguage> Languages
		{
			get
			{
				return this.languages;
			}
		}

		public ITranslatorManager Translator
		{
			get
			{
				if (this.mgr == null)
				{
					this.mgr = new TranslatorManager();
				}
				return this.mgr;
			}
		}

		static MainWindow()
		{
			MainWindow.ActiveLanguageProperty = DependencyProperty.Register("ActiveLanguage", typeof(ILanguage), typeof(MainWindow), new UIPropertyMetadata(null));
		}

		public MainWindow()
		{
			if (App.Reflector != null)
			{
				throw new InvalidOperationException();
			}
			App.Reflector = this;
			this.services = new Dictionary<string, IReflecService>();
			this.InitializeComponent();
			this.RegisterLanguage(new ILLanguage());
			this.RegisterLanguage(new BytesLanguage());
			this.RegisterLanguage(new CSharpLanguage());
			this.RegisterLanguage(new VBLanguage());
			this.Register(Options.Instance);
			this.Register(AssemblyManager.Instance);
			this.Register(DisassemblyService.Instance);
			this.Register(Analyzer.Instance);
			this.Register(Bookmarks.Instance);
			this.Register(SearchService.Instance);
			DockableContent content = new DockableContent()
			{
				HideOnClose = false
			};
			content.Show(this.dock);
			content.Close(true);
			CommandManager.AddCanExecuteHandler(this, new CanExecuteRoutedEventHandler(this.OnCanExecute));
			CommandManager.AddExecutedHandler(this, new ExecutedRoutedEventHandler(this.OnExecuted));
			DispatcherTimer tmr = new DispatcherTimer(DispatcherPriority.Background);
			tmr.Tick += new EventHandler((object sender, EventArgs e) => {
				GC.Collect();
				GC.WaitForPendingFinalizers();
			});
			tmr.Interval = TimeSpan.FromSeconds(5);
			tmr.Start();
			this.dock.Loaded += new RoutedEventHandler((object sender, RoutedEventArgs e) => this.OnLoaded());
		}

		private void CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = true;
		}

		private void CannotExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = false;
		}

		public IReflecWindow CreateWindow(ReflecWindow win)
		{
			return new MainWindow.Dw(this, win);
		}

		private void DoEvents()
		{
			System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.Background, new Action(() => {
			}));
		}

		private void ExitApp(object sender, ExecutedRoutedEventArgs e)
		{
			Application.Current.Shutdown();
		}

		private static IInputElement GetFocusableParent(DependencyObject child)
		{
			DependencyObject parentObject = VisualTreeHelper.GetParent(child);
			if (parentObject == null)
			{
				return null;
			}
			IInputElement parent = parentObject as IInputElement;
			if (parent != null && parent.Focusable)
			{
				return parent;
			}
			return MainWindow.GetFocusableParent(parentObject);
		}

		public IReflecService GetService(string id)
		{
			IReflecService ret;
			if (!this.services.TryGetValue(id, out ret))
			{
				return null;
			}
			return ret;
		}

		private void InitializeBars()
		{
			MenuItem file = new MenuItem()
			{
				Header = "File"
			};
			file.Items.Add(Reflector.UI.BarsManager.GetMenuItem("Open Assembly", AppCommands.OpenAssemblyCommand, "open"));
			file.Items.Add(Reflector.UI.BarsManager.GetMenuItem("Close Assembly", AppCommands.CloseAssemblyCommand, "close"));
			file.Items.Add(Reflector.UI.BarsManager.GetMenuItem("Open Assembly from GAC", new DelegateCommand("GAC.Select", (object _) => true, (object _) => (new GACSelector()).ShowDialog()), null));
			file.Items.Add(new Separator());
			file.Items.Add(Reflector.UI.BarsManager.GetMenuItem("Exit", AppCommands.ExitCommand, null));
			this.menus.AddBar("Main.File", file);
			MenuItem view = new MenuItem()
			{
				Header = "View"
			};
			view.Items.Add(Reflector.UI.BarsManager.GetMenuItem("Assembly Browser", AppCommands.ShowAsmMgrCommand, "asmMgr"));
			view.Items.Add(Reflector.UI.BarsManager.GetMenuItem("Item Properties", new DelegateCommand("AsmMgr.ShowProps", (object obj) => {
				if (this.GetService("AsmMgr") == null)
				{
					return false;
				}
				return (bool)this.GetService("AsmMgr").GetProp("AsmMgr.IsVisible");
			}, (object obj) => this.GetService("AsmMgr").Exec("AsmMgr.ShowProps", new object[0])), "infos"));
			view.Items.Add(Reflector.UI.BarsManager.GetMenuItem("Bookmarks", AppCommands.ShowBookmarksCommand, "bookmark"));
			view.Items.Add(Reflector.UI.BarsManager.GetMenuItem("Search", AppCommands.ShowSearchCommand, "search"));
			view.Items.Add(Reflector.UI.BarsManager.GetMenuItem("Analyzer", new DelegateCommand("Analyzer.Show", (object obj) => true, (object obj) => this.GetService("Analyzer").Exec("Analyzer.Show", new object[0])), "analyze"));
			view.Items.Add(new Separator());
			view.Items.Add(Reflector.UI.BarsManager.GetMenuItem("Refresh", AppCommands.RefreshCommand, "refresh"));
			view.Items.Add(new Separator());
			view.Items.Add(Reflector.UI.BarsManager.GetMenuItem("Options", AppCommands.OptionsCommand, "options"));
			this.menus.AddBar("Main.View", view);
			MenuItem tools = new MenuItem()
			{
				Header = "Tools"
			};
			tools.Items.Add(Reflector.UI.BarsManager.GetMenuItem("Disassemble", AppCommands.DisassembleCommand, "disasm"));
			tools.Items.Add(Reflector.UI.BarsManager.GetMenuItem("Analyze", AppCommands.AnalyzeCommand, "analyze"));
			this.menus.AddBar("Main.Tools", tools);
			MenuItem about = new MenuItem()
			{
				Header = "About"
			};
			about.Click += new RoutedEventHandler((object argument0, RoutedEventArgs argument1) => (new About()).ShowDialog());
			this.menus.AddBar("Main.About", about);
			ToolBar toolbar = new ToolBar();
			toolbar.SetResourceReference(Control.BackgroundProperty, "ControlBackgroundBrush");
			ToolBarTray.SetIsLocked(toolbar, true);
			toolbar.Items.Add(Reflector.UI.BarsManager.GetToolBarButton("open", AppCommands.OpenAssemblyCommand));
			((Button)toolbar.Items[0]).Margin = new Thickness(10, 2, 2, 2);
			toolbar.Items.Add(Reflector.UI.BarsManager.GetToolBarButton("close", AppCommands.CloseAssemblyCommand));
			toolbar.Items.Add(new Separator());
			toolbar.Items.Add(Reflector.UI.BarsManager.GetToolBarButton("asmMgr", AppCommands.ShowAsmMgrCommand));
			toolbar.Items.Add(Reflector.UI.BarsManager.GetToolBarButton("togglebk", AppCommands.ToggleBookmarkCommand));
			toolbar.Items.Add(new Separator());
			toolbar.Items.Add(Reflector.UI.BarsManager.GetToolBarButton("disasm", AppCommands.DisassembleCommand));
			toolbar.Items.Add(Reflector.UI.BarsManager.GetToolBarButton("analyze", new DelegateCommand("Analyzer.Show", (object obj) => true, (object obj) => this.GetService("Analyzer").Exec("Analyzer.Show", new object[0]))));
			ComboBox comboBox = new ComboBox()
			{
				DisplayMemberPath = "Name",
				Width = 100,
				SelectedIndex = 0,
				Margin = new Thickness(5, 2, 0, 2)
			};
			ComboBox langBox = comboBox;
			DependencyProperty itemsSourceProperty = ItemsControl.ItemsSourceProperty;
			Binding binding = new Binding("Languages")
			{
				Source = this
			};
			langBox.SetBinding(itemsSourceProperty, binding);
			DependencyProperty selectedItemProperty = Selector.SelectedItemProperty;
			Binding binding1 = new Binding("ActiveLanguage")
			{
				Source = this
			};
			langBox.SetBinding(selectedItemProperty, binding1);
			toolbar.Items.Add(langBox);
			this.menus.AddBar("Main.Toolbar", toolbar);
		}

		private bool IsInDock(DragEventArgs e)
		{
			bool flag;
			Point pt = e.GetPosition(this.dock);
			Point point = new Point();
			if (!(new Rect(point, new Size(this.dock.ActualWidth, this.dock.ActualHeight))).Contains(pt))
			{
				return false;
			}
			using (IEnumerator<DockableContent> enumerator = this.dock.DockableContents.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					DockableContent content = enumerator.Current;
					if (content.State == DockableContentState.Document || content.State == DockableContentState.Hidden)
					{
						continue;
					}
					Point point1 = new Point();
					if (!(new Rect(point1, new Size(content.ContainerPane.ActualWidth, content.ContainerPane.ActualHeight))).Contains(e.GetPosition(content.ContainerPane)))
					{
						continue;
					}
					flag = false;
					return flag;
				}
				return true;
			}
			return flag;
		}

		private void Lol(object sender, ExecutedRoutedEventArgs e)
		{
			MessageBox.Show("LOL!");
		}

		private void NotImpl(object sender, ExecutedRoutedEventArgs e)
		{
			MessageBox.Show("Not implemented!");
		}

		private void OnCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			if (this.isRouting)
			{
				return;
			}
			this.isRouting = true;
			if (this.dock.FlyoutWindow != null && ((Pane)this.dock.FlyoutWindow.Content).SelectedItem != null)
			{
				ManagedContent pane = (ManagedContent)((Pane)this.dock.FlyoutWindow.Content).SelectedItem;
				if (((RoutedCommand)e.Command).CanExecute(e.Parameter, (IInputElement)pane.Content))
				{
					e.CanExecute = true;
					e.Handled = true;
				}
			}
			foreach (DockableContent content in this.dock.DockableContents)
			{
				if (content.State == DockableContentState.AutoHide || !((RoutedCommand)e.Command).CanExecute(e.Parameter, (IInputElement)content.Content))
				{
					continue;
				}
				e.CanExecute = true;
				e.Handled = true;
			}
			if (this.dock.ActiveDocument != null && ((RoutedCommand)e.Command).CanExecute(e.Parameter, (IInputElement)this.dock.ActiveDocument.Content))
			{
				e.CanExecute = true;
				e.Handled = true;
			}
			this.isRouting = false;
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			base.OnClosing(e);
			XmlDocument doc = new XmlDocument();
			XmlElement root = doc.CreateElement("reflector");
			XmlAttribute lang = doc.CreateAttribute("lang");
			lang.Value = this.ActiveLanguage.Name;
			root.Attributes.Append(lang);
			XmlAttribute loc = doc.CreateAttribute("loc");
			loc.Value = string.Format("{0},{1}", base.Left, base.Top);
			root.Attributes.Append(loc);
			XmlAttribute size = doc.CreateAttribute("size");
			size.Value = string.Format("{0},{1}", base.Width, base.Height);
			root.Attributes.Append(size);
			XmlAttribute state = doc.CreateAttribute("state");
			state.Value = base.WindowState.ToString();
			root.Attributes.Append(state);
			doc.AppendChild(root);
			foreach (IReflecService srv in this.services.Values)
			{
				XmlElement element = doc.CreateElement("service");
				XmlAttribute id = doc.CreateAttribute("id");
				id.Value = srv.Id;
				element.Attributes.Append(id);
				srv.SaveSettings(doc, element);
				root.AppendChild(element);
			}
			foreach (DockableContent content in this.dock.DockableContents)
			{
				ReflecWindow win = (ReflecWindow)content.Content;
				content.Name = string.Concat("_", BitConverter.ToString(Encoding.UTF8.GetBytes(string.Concat(win.ParentService.Id, "::", win.Id))).Replace("-", ""));
			}
			foreach (DocumentContent content in this.dock.Documents)
			{
				ReflecWindow win = (ReflecWindow)content.Content;
				content.Name = string.Concat("_", BitConverter.ToString(Encoding.UTF8.GetBytes(string.Concat(win.ParentService.Id, "::", win.Id))).Replace("-", ""));
			}
			XmlElement layout = doc.CreateElement("layout");
			TextWriter wtr = new StringWriter();
			this.dock.SaveLayout(wtr);
			layout.InnerText = Convert.ToBase64String(Encoding.UTF8.GetBytes(wtr.ToString()));
			root.AppendChild(layout);
			doc.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "reflector.config"));
			Application.Current.Shutdown();
		}

		private void OnExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			if (this.isRoutingExe)
			{
				return;
			}
			if (e.Handled)
			{
				return;
			}
			this.isRoutingExe = true;
			if (this.dock.FlyoutWindow != null && ((Pane)this.dock.FlyoutWindow.Content).SelectedItem != null)
			{
				ManagedContent pane = (ManagedContent)((Pane)this.dock.FlyoutWindow.Content).SelectedItem;
				if (((RoutedCommand)e.Command).CanExecute(e.Parameter, (IInputElement)pane.Content))
				{
					((RoutedCommand)e.Command).Execute(e.Parameter, (IInputElement)pane.Content);
					e.Handled = true;
				}
			}
			if (e.Handled)
			{
				this.isRoutingExe = false;
				return;
			}
			foreach (DockableContent content in this.dock.DockableContents)
			{
				if (content.State == DockableContentState.AutoHide || !((RoutedCommand)e.Command).CanExecute(e.Parameter, (IInputElement)content.Content))
				{
					continue;
				}
				((RoutedCommand)e.Command).Execute(e.Parameter, (IInputElement)content.Content);
				e.Handled = true;
				break;
			}
			if (e.Handled)
			{
				this.isRoutingExe = false;
				return;
			}
			if (this.dock.ActiveDocument != null && ((RoutedCommand)e.Command).CanExecute(e.Parameter, (IInputElement)this.dock.ActiveDocument.Content))
			{
				((RoutedCommand)e.Command).CanExecute(e.Parameter, (IInputElement)this.dock.ActiveDocument.Content);
				e.Handled = true;
			}
			this.isRoutingExe = false;
		}

		private void OnLoaded()
		{
			int x;
			int y;
			int w;
			int h;
			string addins = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "addins\\");
			if (Directory.Exists(addins))
			{
				string[] directories = Directory.GetDirectories(addins);
				for (int i = 0; i < (int)directories.Length; i++)
				{
					string dir = directories[i];
					string name = (new DirectoryInfo(dir)).Name;
					string file = Path.Combine(dir, string.Concat(name, ".dll"));
					if (File.Exists(file))
					{
						try
						{
							IReflecService reflec = (IReflecService)Assembly.LoadFrom(file).GetType(string.Concat(name, ".Addin")).GetField("Instance").GetValue(null);
							this.Register(reflec);
						}
						catch (Exception exception)
						{
							Exception e = exception;
							MessageBox.Show(string.Format("Could not load addin '{0}'.\r\nMessage : {1}\r\nStack Trace: {2}", Path.GetFileName(file), e.Message, e.StackTrace));
						}
					}
				}
			}
			if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "reflector.config")))
			{
				XmlDocument xmlDocument = new XmlDocument();
				xmlDocument.Load(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "reflector.config"));
				string lang = xmlDocument.SelectSingleNode("/reflector/@lang").Value;
				foreach (ILanguage l in this.Languages)
				{
					if (l.Name != lang)
					{
						continue;
					}
					this.ActiveLanguage = l;
					break;
				}
				string loc = xmlDocument.SelectSingleNode("/reflector/@loc").Value;
				char[] chrArray = new char[] { ',' };
				if (int.TryParse(loc.Split(chrArray)[0], out x))
				{
					char[] chrArray1 = new char[] { ',' };
					if (int.TryParse(loc.Split(chrArray1)[1], out y))
					{
						base.Left = (double)x;
						base.Top = (double)y;
					}
				}
				string size = xmlDocument.SelectSingleNode("/reflector/@size").Value;
				char[] chrArray2 = new char[] { ',' };
				if (int.TryParse(size.Split(chrArray2)[0], out w))
				{
					char[] chrArray3 = new char[] { ',' };
					if (int.TryParse(size.Split(chrArray3)[1], out h))
					{
						base.Width = (double)w;
						base.Height = (double)h;
					}
				}
				string state = xmlDocument.SelectSingleNode("/reflector/@state").Value;
				if (Enum.IsDefined(typeof(System.Windows.WindowState), state))
				{
					base.WindowState = (System.Windows.WindowState)Enum.Parse(typeof(System.Windows.WindowState), state, true);
				}
				Action<IEnumerator<KeyValuePair<string, IReflecService>>> action = null;
				action = (IEnumerator<KeyValuePair<string, IReflecService>> entor) => {
					if (entor.MoveNext())
					{
						XmlNode cfg = xmlDocument.SelectSingleNode(string.Concat("/reflector/service[@id='", entor.Current.Value.Id, "']"));
						if (cfg != null)
						{
							entor.Current.Value.LoadSettings(cfg);
						}
						this.Dispatcher.BeginInvoke(action, DispatcherPriority.Loaded, new object[] { entor });
						return;
					}
					foreach (DockableContent content in this.dock.DockableContents)
					{
						ReflecWindow win = (ReflecWindow)content.Content;
						content.Name = string.Concat("_", BitConverter.ToString(Encoding.UTF8.GetBytes(string.Concat(win.ParentService.Id, "::", win.Id))).Replace("-", ""));
					}
					foreach (DocumentContent content in this.dock.Documents)
					{
						ReflecWindow win = (ReflecWindow)content.Content;
						content.Name = string.Concat("_", BitConverter.ToString(Encoding.UTF8.GetBytes(string.Concat(win.ParentService.Id, "::", win.Id))).Replace("-", ""));
					}
					string layout = xmlDocument.SelectSingleNode("/reflector/layout/text()").Value;
					layout = Encoding.UTF8.GetString(Convert.FromBase64String(layout));
					this.dock.RestoreLayout(new StringReader(layout));
				};
				System.Windows.Threading.Dispatcher dispatcher = base.Dispatcher;
				Action<IEnumerator<KeyValuePair<string, IReflecService>>> action1 = action;
				object[] enumerator = new object[] { this.services.GetEnumerator() };
				dispatcher.BeginInvoke(action1, DispatcherPriority.Loaded, enumerator);
			}
		}

		private void OpenAssembly(object sender, ExecutedRoutedEventArgs e)
		{
			OpenFileDialog ofd = new OpenFileDialog()
			{
				Filter = ".NET Assembly (*.exe; *.dll)|*.exe;*.dll|All Files|*.*",
				Multiselect = true
			};
			if (ofd.ShowDialog().GetValueOrDefault())
			{
				string[] fileNames = ofd.FileNames;
				for (int i = 0; i < (int)fileNames.Length; i++)
				{
					string f = fileNames[i];
					IReflecService service = this.GetService("AsmMgr");
					object[] objArray = new object[] { f };
					service.Exec("AsmMgr.OpenAsm", objArray);
				}
			}
			AppCommands.ShowAsmMgrCommand.Execute(null, this);
		}

		private void RefreshAssemblies(object sender, ExecutedRoutedEventArgs e)
		{
			this.GetService("AsmMgr").Exec("AsmMgr.Refresh", new object[0]);
		}

		public void Register(IReflecService srv)
		{
			if (this.services.ContainsKey(srv.Id))
			{
				throw new InvalidOperationException("Service already registered.");
			}
			this.services.Add(srv.Id, srv);
			srv._App = this;
		}

		public void RegisterLanguage(ILanguage language)
		{
			this.languages.Add(language);
		}

		private void ShowAsmMgr(object sender, ExecutedRoutedEventArgs e)
		{
			this.GetService("AsmMgr").Exec("AsmMgr.Show", new object[0]);
		}

		private void ShowBookmarks(object sender, ExecutedRoutedEventArgs e)
		{
			this.GetService("Bookmarks").Exec("Bookmarks.Show", new object[0]);
		}

		private void ShowOptions(object sender, ExecutedRoutedEventArgs e)
		{
			this.GetService("Options").Exec("Options.Show", new object[0]);
		}

		private void ShowSearch(object sender, ExecutedRoutedEventArgs e)
		{
			this.GetService("Search").Exec("Search.Show", new object[0]);
		}

		public void UnregisterLanguage(ILanguage language)
		{
			this.languages.Remove(language);
		}

		private void Window_DragOver(object sender, DragEventArgs e)
		{
			if (!e.Handled && e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				e.Effects = DragDropEffects.Link;
			}
			else if (this.IsInDock(e) && !e.Handled && e.Data.GetDataPresent("Reflector object"))
			{
				e.Effects = DragDropEffects.Move;
			}
			e.Handled = true;
		}

		private void Window_Drop(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				string[] data = (string[])e.Data.GetData(DataFormats.FileDrop);
				for (int i = 0; i < (int)data.Length; i++)
				{
					string str = data[i];
					IReflecService service = this.GetService("AsmMgr");
					object[] objArray = new object[] { str };
					service.Exec("AsmMgr.OpenAsm", objArray);
				}
				AppCommands.ShowAsmMgrCommand.Execute(null, this);
			}
			else if (this.IsInDock(e) && e.Data.GetDataPresent("Reflector object"))
			{
				MainWindow.GetFocusableParent((DependencyObject)base.InputHitTest(e.GetPosition(this))).Focus();
				AppCommands.DisassembleCommand.Execute(e.Data.GetData("Reflector object"), this);
			}
			e.Handled = true;
		}

		private void Window_GiveFeedback(object sender, GiveFeedbackEventArgs e)
		{
			e.UseDefaultCursors = false;
			if (e.Effects != DragDropEffects.None)
			{
				Mouse.SetCursor(Cursors.Arrow);
			}
			else
			{
				Mouse.SetCursor(Cursors.No);
			}
			e.Handled = true;
		}

		private class Dw : IReflecWindow
		{
			private MainWindow reflec;

			private ReflecWindow win;

			private ManagedContent content;

			public ReflecWindow Content
			{
				get
				{
					return this.win;
				}
			}

            public bool IsVisible { get; private set; }

   //         public bool IsVisible
			//{
			//	get
			//	{
			//		return get_IsVisible();
			//	}
			//	set
			//	{
			//		set_IsVisible(value);
			//	}
			//}

			//private bool _003CIsVisible_003Ek__BackingField;

			//public bool get_IsVisible()
			//{
			//	return this._003CIsVisible_003Ek__BackingField;
			//}

			//private void set_IsVisible(bool value)
			//{
			//	this._003CIsVisible_003Ek__BackingField = value;
			//}

			public Dw(MainWindow reflec, ReflecWindow win)
			{
				this.reflec = reflec;
				this.win = win;
				win._App = reflec;
			}

			public void Activate()
			{
				this.Show();
				this.content.Activate();
			}

			public void Close()
			{
				if (this.IsVisible && this.content.Close())
				{
					this.IsVisible = false;
				}
			}

			public void Dock(AnchorStyle anchor)
			{
				if (this.content == null)
				{
					this.Initialize(true);
				}
				if (!(this.content is DockableContent))
				{
					throw new InvalidOperationException("Window type mismatched.");
				}
				((DockableContent)this.content).Show(this.reflec.dock, anchor);
				this.IsVisible = true;
			}

			public void Initialize(bool isDock)
			{
				ManagedContent dockableContent;
				if (isDock)
				{
					dockableContent = new DockableContent();
				}
				else
				{
					dockableContent = new DocumentContent();
				}
				this.content = dockableContent;
				this.content.SetBinding(ManagedContent.IconProperty, "Icon");
				this.content.SetBinding(ManagedContent.TitleProperty, "Title");
				this.content.DataContext = this.win;
				this.content.Content = this.win;
				if (this.content is DockableContent)
				{
					((DockableContent)this.content).HideOnClose = true;
					((DockableContent)this.content).StateChanged += new RoutedEventHandler((object sender, RoutedEventArgs e) => {
						if (((DockableContent)this.content).State == DockableContentState.Hidden)
						{
							this.IsVisible = false;
							this.OnClosed();
						}
					});
				}
				this.content.Closed += new EventHandler((object sender, EventArgs e) => {
					this.IsVisible = false;
					this.OnClosed();
				});
			}

			private void OnClosed()
			{
				if (this.Closed != null)
				{
					this.Closed(this, EventArgs.Empty);
				}
			}

			public void Show()
			{
				if (!this.IsVisible)
				{
					this.content.Show(this.reflec.dock);
					this.IsVisible = true;
				}
			}

			public void ShowDocument()
			{
				Pane containerPane;
				if (this.content == null)
				{
					this.Initialize(false);
				}
				if (this.reflec.dock.ActiveDocument == null)
				{
					containerPane = null;
				}
				else
				{
					containerPane = this.reflec.dock.ActiveDocument.ContainerPane;
				}
				Pane pane = containerPane;
				if (!(this.content is DocumentContent))
				{
					((DockableContent)this.content).ShowAsDocument(this.reflec.dock);
				}
				else
				{
					((DocumentContent)this.content).Show(this.reflec.dock);
				}
				if (pane is DocumentPane)
				{
					this.content.ContainerPane.Items.Remove(this.content);
					pane.Items.Add(this.content);
				}
				this.IsVisible = true;
			}

			public event EventHandler Closed;
		}
	}
}