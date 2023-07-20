using Mono.Cecil;
using Mono.Collections.Generic;
using Reflector;
using Reflector.CodeModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml;

namespace Reflector.UI
{
	internal class Analyzer : ReflecWindow, IReflecService
	{
		private ICommand removeCmd;

		private ICommand gotoCmd;

		public readonly static Analyzer Instance;

		private ObservableCollection<object> Objects = new ObservableCollection<object>();

		private IReflecWindow window;

		private TreeView view;

		static Analyzer()
		{
			Analyzer.Instance = new Analyzer();
			Control.BackgroundProperty.OverrideMetadata(typeof(Analyzer), new FrameworkPropertyMetadata(Application.Current.Resources["ControlBackgroundBrush"]));
			FrameworkElement.DefaultStyleKeyProperty.OverrideMetadata(typeof(Analyzer), new FrameworkPropertyMetadata(typeof(AssemblyManager)));
			ReflecWindow.TitlePropertyKey.OverrideMetadata(typeof(Analyzer), new PropertyMetadata("Analyzer"));
			ReflecWindow.IconPropertyKey.OverrideMetadata(typeof(Analyzer), new PropertyMetadata(Application.Current.Resources["analyze"]));
			ReflecWindow.IdPropertyKey.OverrideMetadata(typeof(Analyzer), new PropertyMetadata("Analyzer"));
		}

		private Analyzer()
		{
			base.SetValue(ReflecWindow.ParentServicePropertyKey, this);
			this.gotoCmd = new DelegateCommand("Analyzer.GotoMember", (object obj) => obj != null, (object obj) => AssemblyManager.Instance.SetProp("AsmMgr.Selected", obj));
			this.removeCmd = new DelegateCommand("Analyzer.Remove", (object obj) => {
				if (obj != null || this.view == null || !(this.view.SelectedItem is AnalyzeReflectorNode))
				{
					return this.Objects.Contains(obj);
				}
				return this.Objects.Contains(((AnalyzeReflectorNode)this.view.SelectedItem).ReflectorObject);
			}, (object obj) => {
				if (obj != null || this.view == null || !(this.view.SelectedItem is AnalyzeReflectorNode))
				{
					this.Objects.Remove(obj);
					return;
				}
				this.Objects.Remove(((AnalyzeReflectorNode)this.view.SelectedItem).ReflectorObject);
			});
			base.InputBindings.Add(new KeyBinding(this.removeCmd, new KeyGesture(Key.Delete)));
		}

		public object Exec(string name, params object[] args)
		{
			if (name == "Analyzer.Add")
			{
				this.Objects.Add(args[0]);
				return null;
			}
			if (name == "Analyzer.Remove")
			{
				this.Objects.Remove(args[0]);
				return null;
			}
			if (name != "Analyzer.Show")
			{
				throw new InvalidOperationException(name);
			}
			if (this.window == null)
			{
				this.window = base._App.CreateWindow(this);
				this.window.Initialize(true);
				this.window.ShowDocument();
			}
			this.window.Activate();
			return null;
		}

		private static T FindChild<T>(DependencyObject parent, string childName)
		where T : DependencyObject
		{
			if (parent == null)
			{
				return default(T);
			}
			T foundChild = default(T);
			int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
			for (int i = 0; i < childrenCount; i++)
			{
				DependencyObject child = VisualTreeHelper.GetChild(parent, i);
				if ((T)(child as T) == null)
				{
					foundChild = Analyzer.FindChild<T>(child, childName);
					if (foundChild != null)
					{
						break;
					}
				}
				else if (string.IsNullOrEmpty(childName))
				{
					foundChild = (T)child;
					break;
				}
				else
				{
					FrameworkElement frameworkElement = child as FrameworkElement;
					if (frameworkElement != null && frameworkElement.Name == childName)
					{
						foundChild = (T)child;
						break;
					}
				}
			}
			return foundChild;
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
			return Analyzer.FindParent<T>(parentObject);
		}

		private AssemblyDefinition GetAssembly(object obj)
		{
			if (obj is IMemberDefinition)
			{
				return ((IMemberDefinition)obj).DeclaringType.Module.Assembly;
			}
			if (obj is TypeDefinition)
			{
				return ((TypeDefinition)obj).Module.Assembly;
			}
			if (obj is INamespace)
			{
				return ((INamespace)obj).Types[0].Module.Assembly;
			}
			if (obj is ModuleDefinition)
			{
				return ((ModuleDefinition)obj).Assembly;
			}
			if (!(obj is AssemblyDefinition))
			{
				return null;
			}
			return (AssemblyDefinition)obj;
		}

		public object GetProp(string name)
		{
			if (name != "Analyzer.Count")
			{
				return null;
			}
			return this.Objects.Count;
		}

		public void LoadSettings(XmlNode node)
		{
			bool s;
			if (node.SelectSingleNode("@show") != null && bool.TryParse(node.SelectSingleNode("@show").Value, out s) && s)
			{
				this.Exec("Analyzer.Show", new object[0]);
			}
			foreach (XmlNode asm in node.SelectNodes("disasm/@id"))
			{
				CodeIdentifier id = new CodeIdentifier(asm.Value);
				object obj = id.Resolve(AssemblyManager.Instance);
				if (obj == null)
				{
					continue;
				}
				object[] objArray = new object[] { obj };
				this.Exec("Analyzer.Add", objArray);
			}
		}

		protected override void OnAppAttached()
		{
			System.Windows.Controls.ContextMenu typeRef = new System.Windows.Controls.ContextMenu();
			typeRef.Items.Add(this.SetMenuItem("Go To Member", this.gotoCmd, "goto"));
			typeRef.Items.Add(this.SetMenuItem("Remove", this.removeCmd, "close"));
			typeRef.DataContext = null;
			base._App.BarsManager.AddBar("Analyzer.Menu", this.SetMenu(typeRef));
		}

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			this.view = (TreeView)base.GetTemplateChild("PART_treeView");
			this.view.ItemsSource = new AnalyzeNodesConverter.ObservableMap<object>(this.Objects, null);
			this.view.PreviewMouseDown += new MouseButtonEventHandler(this.StopScroll);
			EventHandler<AssemblyEventArgs> handler = (object sender, AssemblyEventArgs e) => base.Dispatcher.BeginInvoke(new Action(this.RefreshItems), new object[0]);
			((IAssemblyManager)base._App.GetService("AsmMgr")).AssemblyLoaded += handler;
			((IAssemblyManager)base._App.GetService("AsmMgr")).AssemblyUnloaded += handler;
		}

		protected override void OnDragEnter(DragEventArgs e)
		{
			if (e.Data.GetDataPresent("Reflector object") && !(e.Data.GetData("Reflector object") is Resource) && !(e.Data.GetData("Reflector object") is INamespace))
			{
				e.Effects = DragDropEffects.Move;
				e.Handled = true;
			}
		}

		protected override void OnDrop(DragEventArgs e)
		{
			if (e.Data.GetDataPresent("Reflector object") && !(e.Data.GetData("Reflector object") is Resource) && !(e.Data.GetData("Reflector object") is INamespace))
			{
				object[] data = new object[] { e.Data.GetData("Reflector object") };
				this.Exec("Analyzer.Add", data);
				e.Handled = true;
			}
		}

		private void RefreshItems()
		{
			List<object> invali = new List<object>();
			foreach (object obj in this.Objects)
			{
				AssemblyDefinition asm = this.GetAssembly(obj);
				if (((IList<AssemblyDefinition>)base._App.GetService("AsmMgr").GetProp("AsmMgr.Assemblies")).Contains(asm))
				{
					continue;
				}
				invali.Add(obj);
			}
			foreach (object obj in invali)
			{
				this.Objects.Remove(obj);
			}
		}

		public void SaveSettings(XmlDocument doc, XmlNode node)
		{
			if (this.window != null && this.window.IsVisible)
			{
				XmlAttribute attr = doc.CreateAttribute("show");
				attr.Value = "true";
				node.Attributes.Append(attr);
			}
			foreach (object obj in this.Objects)
			{
				XmlElement element = doc.CreateElement("item");
				XmlAttribute attr = doc.CreateAttribute("id");
				attr.Value = (new CodeIdentifier(obj)).Identifier;
				element.Attributes.Append(attr);
				node.AppendChild(element);
			}
		}

		private System.Windows.Controls.ContextMenu SetMenu(System.Windows.Controls.ContextMenu menu)
		{
			menu.Opened += new RoutedEventHandler((object argument0, RoutedEventArgs argument1) => menu.DataContext = ((FrameworkElement)menu.PlacementTarget).DataContext);
			menu.Closed += new RoutedEventHandler((object argument2, RoutedEventArgs argument3) => {
				menu.DataContext = null;
				menu.PlacementTarget = menu;
			});
			return menu;
		}

		private MenuItem SetMenuItem(string txt, ICommand cmd, string ico)
		{
			MenuItem item = BarsManager.GetMenuItem(txt, cmd, ico);
			DependencyProperty commandParameterProperty = MenuItem.CommandParameterProperty;
			Binding binding = new Binding("DataContext.ReflectorObject")
			{
				RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(System.Windows.Controls.ContextMenu), 1)
			};
			item.SetBinding(commandParameterProperty, binding);
			return item;
		}

		public void SetProp(string name, object value)
		{
			throw new InvalidOperationException(name);
		}

		private void StopScroll(object sender, MouseButtonEventArgs e)
		{
			ScrollViewer scroll = Analyzer.FindChild<ScrollViewer>(base.GetTemplateChild("PART_treeView"), null);
			if (scroll != null && Analyzer.FindParent<ScrollBar>((DependencyObject)scroll.InputHitTest(e.GetPosition(scroll))) == null)
			{
				double Hpos = scroll.HorizontalOffset;
				double Vpos = scroll.VerticalOffset;
				System.Windows.Threading.Dispatcher dispatcher = base.Dispatcher;
				Action<ScrollViewer, double, double> horizontalOffset = (ScrollViewer v, double b, double c) => {
					v.ScrollToHorizontalOffset(b);
					v.ScrollToVerticalOffset(c);
				};
				object[] objArray = new object[] { scroll, Hpos, Vpos };
				dispatcher.BeginInvoke(horizontalOffset, DispatcherPriority.DataBind, objArray);
			}
		}
	}
}