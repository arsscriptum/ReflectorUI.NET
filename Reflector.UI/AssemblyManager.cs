using AvalonDock;
using Microsoft.Win32;
using Mono.Cecil;
using Mono.Collections.Generic;
using Reflector;
using Reflector.CodeModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml;

namespace Reflector.UI
{
	internal class AssemblyManager : ReflecWindow, IReflecService, IAssemblyManager
	{
		private AssemblyManager.AssemblyResolver resolver;

		private bool canSkip;

		private List<string> skip = new List<string>();

		public readonly static AssemblyManager Instance;

		private Point pt;

		private bool down;

		private bool isDrag;

		private WeakReference itm;

		private bool isSelecting;

		private IReflecWindow window;

		private ItemProps prop;

		private IReflecWindow propWin;

		public IList<AssemblyDefinition> Assemblies
		{
			get
			{
				return JustDecompileGenerated_get_Assemblies();
			}
			set
			{
				JustDecompileGenerated_set_Assemblies(value);
			}
		}

		private IList<AssemblyDefinition> JustDecompileGenerated_Assemblies_k__BackingField;

		public IList<AssemblyDefinition> JustDecompileGenerated_get_Assemblies()
		{
			return this.JustDecompileGenerated_Assemblies_k__BackingField;
		}

		private void JustDecompileGenerated_set_Assemblies(IList<AssemblyDefinition> value)
		{
			this.JustDecompileGenerated_Assemblies_k__BackingField = value;
		}

		static AssemblyManager()
		{
			AssemblyManager.Instance = new AssemblyManager();
			FrameworkElement.DefaultStyleKeyProperty.OverrideMetadata(typeof(AssemblyManager), new FrameworkPropertyMetadata(typeof(AssemblyManager)));
			ReflecWindow.TitlePropertyKey.OverrideMetadata(typeof(AssemblyManager), new PropertyMetadata("Assembly Manager"));
			ReflecWindow.IconPropertyKey.OverrideMetadata(typeof(AssemblyManager), new PropertyMetadata(Application.Current.Resources["asmMgr"]));
			ReflecWindow.IdPropertyKey.OverrideMetadata(typeof(AssemblyManager), new PropertyMetadata("AsmMgr"));
		}

		private AssemblyManager()
		{
			base.SetValue(ReflecWindow.ParentServicePropertyKey, this);
			base.CommandBindings.Add(new CommandBinding(AppCommands.DisassembleCommand, new ExecutedRoutedEventHandler(this.Disassemble), new CanExecuteRoutedEventHandler(this.CanDisassemble)));
			base.CommandBindings.Add(new CommandBinding(AppCommands.AnalyzeCommand, new ExecutedRoutedEventHandler(this.Analyze), new CanExecuteRoutedEventHandler(this.CanAnalyze)));
			base.CommandBindings.Add(new CommandBinding(AppCommands.CloseAssemblyCommand, new ExecutedRoutedEventHandler(this.CloseAssembly), new CanExecuteRoutedEventHandler(this.CanCloseAssembly)));
			base.CommandBindings.Add(new CommandBinding(AppCommands.ToggleBookmarkCommand, new ExecutedRoutedEventHandler(this.ToggleBookmark), new CanExecuteRoutedEventHandler(this.CanToggleBookmark)));
			base.InputBindings.Add(new KeyBinding(AppCommands.CloseAssemblyCommand, new KeyGesture(Key.Delete)));
			base.InputBindings.Add(new KeyBinding(AppCommands.AnalyzeCommand, new KeyGesture(Key.R, ModifierKeys.Control)));
			this.Assemblies = new ObservableCollection<AssemblyDefinition>();
			this.resolver = new AssemblyManager.AssemblyResolver(this);
		}

		private void Analyze(object sender, ExecutedRoutedEventArgs e)
		{
			object obj;
			if (e.Parameter == null)
			{
				if (this.GetProp("AsmMgr.Selected") == null)
				{
					return;
				}
				obj = this.GetProp("AsmMgr.Selected");
			}
			else
			{
				obj = e.Parameter;
			}
			IReflecService analyzer = base._App.GetService("Analyzer");
			analyzer.Exec("Analyzer.Show", new object[0]);
			object[] objArray = new object[] { obj };
			analyzer.Exec("Analyzer.Add", objArray);
		}

		private void CanAnalyze(object sender, CanExecuteRoutedEventArgs e)
		{
			object obj = (e.Parameter == null ? this.GetProp("AsmMgr.Selected") : e.Parameter);
			e.CanExecute = obj != null;
			if (!e.CanExecute)
			{
				return;
			}
			if (obj is BaseNode || obj is Resource || obj is INamespace)
			{
				e.CanExecute = false;
			}
		}

		private void CanCloseAssembly(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = (this.Assemblies.Count == 0 ? false : this.GetProp("AsmMgr.Selected") is AssemblyDefinition);
		}

		private void CanDisassemble(object sender, CanExecuteRoutedEventArgs e)
		{
			object obj = (e.Parameter == null ? this.GetProp("AsmMgr.Selected") : e.Parameter);
			e.CanExecute = obj != null;
			if (!e.CanExecute)
			{
				return;
			}
			if (obj is BaseNode)
			{
				e.CanExecute = false;
			}
		}

		private void CanToggleBookmark(object sender, CanExecuteRoutedEventArgs e)
		{
			object obj = (e.Parameter == null ? this.GetProp("AsmMgr.Selected") : e.Parameter);
			e.CanExecute = obj != null;
			if (!e.CanExecute)
			{
				return;
			}
			if (obj is BaseNode || obj is INamespace)
			{
				e.CanExecute = false;
			}
		}

		private void CloseAssembly(object sender, ExecutedRoutedEventArgs e)
		{
			object[] prop = new object[] { this.GetProp("AsmMgr.Selected") };
			this.Exec("AsmMgr.CloseAsm", prop);
		}

		private void Disassemble(object sender, ExecutedRoutedEventArgs e)
		{
			object obj;
			if (e.Parameter == null)
			{
				if (this.GetProp("AsmMgr.Selected") == null)
				{
					return;
				}
				obj = this.GetProp("AsmMgr.Selected");
			}
			else
			{
				obj = e.Parameter;
			}
			IReflecService service = base._App.GetService("Disasm");
			object[] objArray = new object[] { obj };
			service.Exec("Disasm.Disasm", objArray);
		}

		private void DoEvents()
		{
			System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.Background, new Action(() => {
			}));
		}

		public object Exec(string name, params object[] args)
		{
			if (name == "AsmMgr.OpenAsm")
			{
				return this.LoadFile((string)args[0]);
			}
			if (name == "AsmMgr.LoadAsm")
			{
				AssemblyDefinition i = (AssemblyDefinition)args[0];
				foreach (AssemblyDefinition asm in this.Assemblies)
				{
					if (asm.Name.FullName != i.Name.FullName)
					{
						continue;
					}
					object[] objArray = new object[] { asm };
					this.Exec("AsmMgr.CloseAsm", objArray);
					break;
				}
				this.OnAssemblyLoaded(new AssemblyEventArgs(i));
				this.SetProp("AsmMgr.Selected", i);
				return args[0];
			}
			if (name == "AsmMgr.CloseAsm")
			{
				int idx = this.Assemblies.IndexOf((AssemblyDefinition)args[0]);
				this.Unload((AssemblyDefinition)args[0]);
				if (idx < this.Assemblies.Count)
				{
					this.SetProp("AsmMgr.Selected", this.Assemblies[idx]);
				}
				else if (this.Assemblies.Count == 0)
				{
					this.SetProp("AsmMgr.Selected", null);
				}
				else
				{
					this.SetProp("AsmMgr.Selected", this.Assemblies[this.Assemblies.Count - 1]);
				}
				return null;
			}
			if (name == "AsmMgr.SaveAsm")
			{
				AssemblyDefinition asm = (AssemblyDefinition)args[0];
				SaveFileDialog sfd = new SaveFileDialog()
				{
					Filter = ".NET Assembly (*.exe; *.dll)|*.exe;*.dll|All Files|*.*",
					FileName = asm.MainModule.Name
				};
				if (sfd.ShowDialog().GetValueOrDefault())
				{
					try
					{
						asm.Write(sfd.FileName);
					}
					catch (Exception exception)
					{
						Exception e = exception;
						MessageBox.Show(string.Format("Cannot save \"{0}\"!!!\r\nException Message : {0}\r\nStack Trace : {1}", asm.Name.FullName, e.Message, e.StackTrace));
					}
				}
				return null;
			}
			if (name == "AsmMgr.Show")
			{
				if (this.window == null)
				{
					this.window = base._App.CreateWindow(this);
					this.window.Dock(AnchorStyle.Left);
				}
				this.window.Activate();
				return null;
			}
			if (name == "AsmMgr.ShowProps")
			{
				if (this.prop == null)
				{
					this.prop = new ItemProps();
					this.propWin = base._App.CreateWindow(this.prop);
					this.propWin.Dock(AnchorStyle.Bottom);
				}
				this.propWin.Activate();
				this.prop.Item = this.GetProp("AsmMgr.Selected");
				return null;
			}
			if (name != "AsmMgr.Refresh")
			{
				throw new InvalidOperationException(name);
			}
			AssemblyDefinition[] array = this.Assemblies.ToArray<AssemblyDefinition>();
			for (int num = 0; num < (int)array.Length; num++)
			{
				AssemblyDefinition asm = array[num];
				string path = asm.MainModule.FullyQualifiedName;
				object[] objArray1 = new object[] { asm };
				this.Exec("AsmMgr.CloseAsm", objArray1);
				object[] objArray2 = new object[] { path };
				this.Exec("AsmMgr.OpenAsm", objArray2);
			}
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
					foundChild = AssemblyManager.FindChild<T>(child, childName);
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
			return AssemblyManager.FindParent<T>(parentObject);
		}

		[DllImport("user32.dll", CharSet=CharSet.None, ExactSpelling=false)]
		private static extern bool GetCursorPos(out AssemblyManager.POINT lpPoint);

		private object GetParent(object obj)
		{
			if (obj is TypeDefinition)
			{
				TypeDefinition typeDefinition = (TypeDefinition)obj;
				if (typeDefinition.DeclaringType != null)
				{
					return typeDefinition.DeclaringType;
				}
				return AsmViewHelper.GetChildren(base.Dispatcher, typeDefinition.Module).First<object>((object ns) => {
					if (!(ns is INamespace))
					{
						return false;
					}
					return ((INamespace)ns).Name == typeDefinition.Namespace;
				});
			}
			if (!(obj is MethodDefinition) || ((MethodDefinition)obj).SemanticsAttributes == Mono.Cecil.MethodSemanticsAttributes.None)
			{
				if (obj is IMemberDefinition)
				{
					return ((IMemberDefinition)obj).DeclaringType;
				}
				if (obj is INamespace)
				{
					return ((INamespace)obj).Types[0].Module;
				}
				if (!(obj is ModuleDefinition))
				{
					return null;
				}
				return ((ModuleDefinition)obj).Assembly;
			}
			MethodDefinition methodDefinition = (MethodDefinition)obj;
			Mono.Cecil.MethodSemanticsAttributes semanticsAttributes = methodDefinition.SemanticsAttributes;
			if (semanticsAttributes > Mono.Cecil.MethodSemanticsAttributes.AddOn)
			{
				if (semanticsAttributes == Mono.Cecil.MethodSemanticsAttributes.RemoveOn)
				{
					return methodDefinition.DeclaringType.Events.FirstOrDefault<EventDefinition>((EventDefinition evt) => evt.RemoveMethod == methodDefinition);
				}
				if (semanticsAttributes == Mono.Cecil.MethodSemanticsAttributes.Fire)
				{
					return methodDefinition.DeclaringType.Events.FirstOrDefault<EventDefinition>((EventDefinition evt) => evt.InvokeMethod == methodDefinition);
				}
			}
			else
			{
				switch (semanticsAttributes)
				{
					case Mono.Cecil.MethodSemanticsAttributes.Setter:
					{
						return methodDefinition.DeclaringType.Properties.FirstOrDefault<PropertyDefinition>((PropertyDefinition prop) => prop.SetMethod == methodDefinition);
					}
					case Mono.Cecil.MethodSemanticsAttributes.Getter:
					{
						return methodDefinition.DeclaringType.Properties.FirstOrDefault<PropertyDefinition>((PropertyDefinition prop) => prop.GetMethod == methodDefinition);
					}
					case Mono.Cecil.MethodSemanticsAttributes.Setter | Mono.Cecil.MethodSemanticsAttributes.Getter:
					{
						break;
					}
					case Mono.Cecil.MethodSemanticsAttributes.Other:
					{
						object obj1 = methodDefinition.DeclaringType.Properties.FirstOrDefault<PropertyDefinition>((PropertyDefinition prop) => prop.OtherMethods.Contains(methodDefinition));
						if (obj1 == null)
						{
							obj1 = methodDefinition.DeclaringType.Events.FirstOrDefault<EventDefinition>((EventDefinition evt) => evt.OtherMethods.Contains(methodDefinition));
						}
						return obj1;
					}
					default:
					{
						if (semanticsAttributes == Mono.Cecil.MethodSemanticsAttributes.AddOn)
						{
							return methodDefinition.DeclaringType.Events.FirstOrDefault<EventDefinition>((EventDefinition evt) => evt.AddMethod == methodDefinition);
						}
						break;
					}
				}
			}
			return null;
		}

		public object GetProp(string name)
		{
			if (name == "AsmMgr.AsmCount")
			{
				return this.Assemblies.Count;
			}
			if (name == "AsmMgr.Selected")
			{
				return this.GetTrueItem(((TreeView)base.GetTemplateChild("PART_treeView")).SelectedItem);
			}
			if (name == "AsmMgr.IsVisible")
			{
				return (this.window == null ? false : this.window.IsVisible);
			}
			if (name != "AsmMgr.Assemblies")
			{
				return null;
			}
			return new ReadOnlyObservableCollection<AssemblyDefinition>((ObservableCollection<AssemblyDefinition>)this.Assemblies);
		}

		private object GetTrueItem(object val)
		{
			if (!(val is IReflectorObjectContainer))
			{
				return val;
			}
			return ((IReflectorObjectContainer)val).ReflectorObject;
		}

		[DllImport("user32.dll", CharSet=CharSet.None, ExactSpelling=false)]
		private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

		public AssemblyDefinition Load(AssemblyNameReference value)
		{
			return this.resolver.Resolve(value);
		}

		public AssemblyDefinition LoadFile(string location)
		{
			AssemblyDefinition assemblyDefinition;
			if (string.IsNullOrEmpty(location))
			{
				return null;
			}
			try
			{
				ReaderParameters readerParameter = new ReaderParameters()
				{
					AssemblyResolver = this.resolver
				};
				AssemblyDefinition ret = AssemblyDefinition.ReadAssembly(location, readerParameter);
				this.OnAssemblyLoaded(new AssemblyEventArgs(ret));
				assemblyDefinition = ret;
			}
			catch (FileNotFoundException)  // fileNotFoundException)
			{
				MessageBox.Show(string.Format("Cannot find file {0}!!!", location));
				assemblyDefinition = null;
			}
			catch (Exception exception)
			{
				Exception e = exception;
				MessageBox.Show(string.Format("\"{0}\" is an invalid .NET Assembly!!!\r\nException Message : {0}\r\nStack Trace : {1}", Path.GetFileName(location), e.Message, e.StackTrace));
				assemblyDefinition = null;
			}
			return assemblyDefinition;
		}

		public void LoadSettings(XmlNode node)
		{
			bool s;
			//bool s;
			if (node.SelectSingleNode("@show") != null && bool.TryParse(node.SelectSingleNode("@show").Value, out s) && s)
			{
				this.Exec("AsmMgr.Show", new object[0]);
			}
			if (node.SelectSingleNode("@showprop") != null && bool.TryParse(node.SelectSingleNode("@showprop").Value, out s) && s)
			{
				this.Exec("AsmMgr.ShowProps", new object[0]);
			}
			foreach (XmlNode asm in node.SelectNodes("assembly/@path"))
			{
				object[] value = new object[] { asm.Value };
				this.Exec("AsmMgr.OpenAsm", value);
			}
		}

		protected override void OnAppAttached()
		{
			System.Windows.Controls.ContextMenu assembly = new System.Windows.Controls.ContextMenu();
			assembly.Items.Add(this.SetMenuItem("Go To Entry Point", new DelegateCommand("AsmMgr.Assembly.GotoEntry", (object obj) => {
				if (obj == null)
				{
					return false;
				}
				return ((AssemblyDefinition)obj).EntryPoint != null;
			}, (object obj) => this.Select(((AssemblyDefinition)obj).EntryPoint, true)), "goto"));
			assembly.Items.Add(this.SetMenuItem("Close", new DelegateCommand("AsmMgr.Assembly.Close", (object obj) => true, (object obj) => this.Exec("AsmMgr.CloseAsm", new object[] { obj })), "close"));
			assembly.Items.Add(this.SetMenuItem("Save", new DelegateCommand("AsmMgr.Assembly.Save", (object obj) => true, (object obj) => this.Exec("AsmMgr.SaveAsm", new object[] { obj })), "save"));
			this.SetDisasmAnalyze(assembly);
			base._App.BarsManager.AddBar("AsmMgr.Assembly", this.SetMenu(assembly));
			System.Windows.Controls.ContextMenu assemblyRef = new System.Windows.Controls.ContextMenu();
			assemblyRef.Items.Add(this.SetMenuItem("Go To Assembly", new DelegateCommand("AsmMgr.AssemblyRef.GotoAsm", (object obj) => obj != null, (object obj) => this.Select(obj, true)), "goto"));
			base._App.BarsManager.AddBar("AsmMgr.AssemblyRef", this.SetMenu(assemblyRef));
			System.Windows.Controls.ContextMenu module = new System.Windows.Controls.ContextMenu();
			this.SetDisasmAnalyze(module);
			base._App.BarsManager.AddBar("AsmMgr.Module", this.SetMenu(module));
			System.Windows.Controls.ContextMenu moduleRef = new System.Windows.Controls.ContextMenu();
			moduleRef.Items.Add(this.SetMenuItem("Go To Module", new DelegateCommand("AppMgr.ModuleRef.GotoMod", (object obj) => obj != null, (object obj) => this.Select(obj, true)), "goto"));
			base._App.BarsManager.AddBar("AsmMgr.ModuleRef", this.SetMenu(moduleRef));
			System.Windows.Controls.ContextMenu typeDecl = new System.Windows.Controls.ContextMenu();
			this.SetDisasmAnalyze(typeDecl);
			base._App.BarsManager.AddBar("AsmMgr.TypeDecl", this.SetMenu(typeDecl));
			System.Windows.Controls.ContextMenu typeRef = new System.Windows.Controls.ContextMenu();
			typeRef.Items.Add(this.SetMenuItem("Go To Type", new DelegateCommand("AsmMgr.TypeRef.GotoType", (object obj) => obj != null, (object obj) => this.Select(obj, true)), "goto"));
			base._App.BarsManager.AddBar("AsmMgr.TypeRef", this.SetMenu(typeRef));
			System.Windows.Controls.ContextMenu methodDecl = new System.Windows.Controls.ContextMenu();
			this.SetDisasmAnalyze(methodDecl);
			base._App.BarsManager.AddBar("AsmMgr.MethodDecl", this.SetMenu(methodDecl));
			System.Windows.Controls.ContextMenu fieldDecl = new System.Windows.Controls.ContextMenu();
			this.SetDisasmAnalyze(fieldDecl);
			base._App.BarsManager.AddBar("AsmMgr.FieldDecl", this.SetMenu(fieldDecl));
			System.Windows.Controls.ContextMenu propertyDecl = new System.Windows.Controls.ContextMenu();
			this.SetDisasmAnalyze(propertyDecl);
			base._App.BarsManager.AddBar("AsmMgr.PropertyDecl", this.SetMenu(propertyDecl));
			System.Windows.Controls.ContextMenu eventDecl = new System.Windows.Controls.ContextMenu();
			this.SetDisasmAnalyze(eventDecl);
			base._App.BarsManager.AddBar("AsmMgr.EventDecl", this.SetMenu(eventDecl));
			System.Windows.Controls.ContextMenu memberRef = new System.Windows.Controls.ContextMenu();
			base._App.BarsManager.AddBar("AsmMgr.MemberRef", this.SetMenu(memberRef));
			System.Windows.Controls.ContextMenu resource = new System.Windows.Controls.ContextMenu();
			resource.Items.Add(this.SetMenuItem("Save As...", new DelegateCommand("AsmMgr.Resource.SaveAs", (object obj) => obj != null, (object obj) => this.SaveResource((Resource)obj)), "save"));
			this.SetDisasmAnalyze(resource);
			base._App.BarsManager.AddBar("AsmMgr.Resource", this.SetMenu(resource));
		}

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			TreeView tv = (TreeView)base.GetTemplateChild("PART_treeView");
			tv.ItemContainerStyle.Setters.Add(new EventSetter(FrameworkElement.RequestBringIntoViewEvent, new RequestBringIntoViewEventHandler(this.StopScroll)));
			tv.ItemsSource = new AsmNodesConverter.ObservableMap<AssemblyDefinition>((ObservableCollection<AssemblyDefinition>)this.Assemblies, null);
			tv.PreviewMouseDown += new MouseButtonEventHandler(this.treeViewMouseDown);
			tv.PreviewMouseMove += new MouseEventHandler(this.treeViewMouseMove);
			tv.PreviewMouseUp += new MouseButtonEventHandler(this.treeViewMouseUp);
			tv.MouseDoubleClick += new MouseButtonEventHandler((object sender, MouseButtonEventArgs e) => {
				TreeView t = (TreeView)sender;
				IInputElement element = t.InputHitTest(e.GetPosition(t));
				if (element is DependencyObject)
				{
					TreeViewItem treeViewItem = AssemblyManager.FindParent<TreeViewItem>((DependencyObject)element);
					TreeViewItem item = treeViewItem;
					if (treeViewItem != null && item.DataContext is ReflectorNode && ((ReflectorNode)item.DataContext).Children.Count<object>() == 0)
					{
						AppCommands.DisassembleCommand.Execute(null, this);
					}
				}
			});
			tv.SelectedItemChanged += new RoutedPropertyChangedEventHandler<object>((object sender, RoutedPropertyChangedEventArgs<object> e) => {
				if (e.NewValue is IReflectorObjectContainer && this.prop != null && this.propWin.IsVisible)
				{
					this.prop.Item = ((IReflectorObjectContainer)e.NewValue).ReflectorObject;
				}
			});
		}

		private void OnAssemblyLoaded(AssemblyEventArgs e)
		{
			foreach (AssemblyDefinition asm in this.Assemblies)
			{
				if (asm.Name.FullName != e.Assembly.Name.FullName)
				{
					continue;
				}
				this.SetProp("AsmMgr.Selected", asm);
				return;
			}
			if (!base.CheckAccess())
			{
				System.Windows.Threading.Dispatcher dispatcher = base.Dispatcher;
				IList<AssemblyDefinition> assemblies = this.Assemblies;
				Action<AssemblyDefinition> action = new Action<AssemblyDefinition>(assemblies.Add);
				object[] assembly = new object[] { e.Assembly };
				dispatcher.BeginInvoke(action, assembly).Wait();
			}
			else
			{
				this.Assemblies.Add(e.Assembly);
			}
			if (this.AssemblyLoaded != null)
			{
				this.AssemblyLoaded(this, e);
			}
		}

		private void OnAssemblyUnloaded(AssemblyEventArgs e)
		{
			if (!base.CheckAccess())
			{
				System.Windows.Threading.Dispatcher dispatcher = base.Dispatcher;
				IList<AssemblyDefinition> assemblies = this.Assemblies;
				Func<AssemblyDefinition, bool> func = new Func<AssemblyDefinition, bool>(assemblies.Remove);
				object[] assembly = new object[] { e.Assembly };
				dispatcher.BeginInvoke(func, assembly).Wait();
			}
			else
			{
				this.Assemblies.Remove(e.Assembly);
			}
			Keyboard.Focus(this);
			if (this.AssemblyUnloaded != null)
			{
				this.AssemblyUnloaded(this, e);
			}
		}

		private AssemblyDefinition ResolveImpl(object sender, AssemblyNameReference reference)
		{
			if (this.canSkip)
			{
				if (this.skip.Contains(reference.ToString()))
				{
					return null;
				}
				this.skip.Add(reference.ToString());
			}
			System.Windows.Threading.Dispatcher dispatcher = base.Dispatcher;
			Func<AssemblyNameReference, string> func = new Func<AssemblyNameReference, string>(AssemblyResolveDialog.Resolve);
			object[] objArray = new object[] { reference };
			return this.LoadFile((string)dispatcher.Invoke(func, objArray));
		}

		private void SaveResource(Resource res)
		{
			byte[] dat;
			if (!(res is EmbeddedResource))
			{
				if (!(res is LinkedResource))
				{
					throw new NotSupportedException();
				}
				LinkedResource file = (LinkedResource)res;
				string path = Environment.ExpandEnvironmentVariables(Path.Combine(Path.GetDirectoryName(res.Module.FullyQualifiedName), file.File));
				if (File.Exists(path))
				{
					using (Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read))
					{
						//dat = new byte[checked((IntPtr)stream.Length)];
                        dat = new byte[checked(stream.Length)];
                        stream.Read(dat, 0, (int)dat.Length);
					}
				}
				else
				{
					dat = null;
				}
			}
			else
			{
				dat = ((EmbeddedResource)res).GetResourceData();
			}
			SaveFileDialog sfd = new SaveFileDialog()
			{
				FileName = res.Name,
				Filter = "All Files|*.*"
			};
			if (sfd.ShowDialog().GetValueOrDefault())
			{
				File.WriteAllBytes(sfd.FileName, dat);
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
			if (this.propWin != null && this.propWin.IsVisible)
			{
				XmlAttribute attr = doc.CreateAttribute("showprop");
				attr.Value = "true";
				node.Attributes.Append(attr);
			}
			foreach (AssemblyDefinition asm in this.Assemblies)
			{
				XmlElement element = doc.CreateElement("assembly");
				XmlAttribute attr = doc.CreateAttribute("path");
				attr.Value = asm.MainModule.FullyQualifiedName;
				element.Attributes.Append(attr);
				node.AppendChild(element);
			}
		}

		private void Select(object obj, bool sel)
		{
			object parent;
			if (obj is AssemblyNameReference)
			{
				this.Select(((AssemblyNameReference)obj).Resolve(), sel);
				return;
			}
			if (obj is TypeReference && !(obj is TypeDefinition))
			{
				this.Select(((TypeReference)obj).Resolve(), sel);
				return;
			}
			if (obj is MethodReference && !(obj is MethodDefinition))
			{
				this.Select(((MethodReference)obj).Resolve(), sel);
				return;
			}
			if (obj is FieldReference && !(obj is FieldDefinition))
			{
				this.Select(((FieldReference)obj).Resolve(), sel);
				return;
			}
			if (obj is EventReference && !(obj is EventDefinition))
			{
				this.Select(((EventReference)obj).Resolve(), sel);
				return;
			}
			if (obj is PropertyReference && !(obj is PropertyDefinition))
			{
				this.Select(((PropertyReference)obj).Resolve(), sel);
				return;
			}
			if (obj == null)
			{
				return;
			}
			List<object> path = new List<object>();
			do
			{
				path.Add(obj);
				parent = this.GetParent(obj);
				obj = parent;
			}
			while (parent != null);
			path.Reverse();
			TreeView view = (TreeView)base.GetTemplateChild("PART_treeView");
			ReflectorNode node = null;
			foreach (BaseNode n in view.ItemsSource as AsmNodesConverter.ObservableMap<AssemblyDefinition>)
			{
				if (!(n is ReflectorNode) || ((ReflectorNode)n).ReflectorObject != path[0])
				{
					continue;
				}
				node = (ReflectorNode)n;
				break;
			}
			for (int i = 1; i < path.Count; i++)
			{
				node.Initialize();
				node.IsExpanded = true;
				view.UpdateLayout();
				bool ok = false;
				foreach (object n in AsmNodesConverter.ObservableMap<object>.GetMap(node))
				{
					if (!(n is ReflectorNode) || !((ReflectorNode)n).ReflectorObject.Equals(path[i]))
					{
						continue;
					}
					node = (ReflectorNode)n;
					ok = true;
					break;
				}
				if (!ok)
				{
					throw new InvalidOperationException();
				}
			}
			this.isSelecting = true;
			node.IsSelected = true;
			((TreeViewItem)typeof(TreeView).GetProperty("SelectedContainer", BindingFlags.Instance | BindingFlags.NonPublic).GetGetMethod(true).Invoke(view, null)).BringIntoView(new Rect(new Size(0, view.ActualHeight / 2)));
			this.isSelecting = false;
		}

		private void SetDisasmAnalyze(System.Windows.Controls.ContextMenu menu)
		{
			if (menu.Items.Count != 0)
			{
				menu.Items.Add(new Separator());
			}
			menu.Items.Add(this.SetMenuItem("Disassemble", AppCommands.DisassembleCommand, "disasm"));
			menu.Items.Add(this.SetMenuItem("Analyze", AppCommands.AnalyzeCommand, "analyze"));
		}

		private System.Windows.Controls.ContextMenu SetMenu(System.Windows.Controls.ContextMenu menu)
		{
			menu.Opened += new RoutedEventHandler((object argument0, RoutedEventArgs argument1) => menu.DataContext = ((FrameworkElement)menu.PlacementTarget).DataContext);
			menu.Closed += new RoutedEventHandler((object argument2, RoutedEventArgs argument3) => {
				menu.DataContext = null;
				menu.PlacementTarget = menu;
			});
			menu.DataContext = null;
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
			if (name != "AsmMgr.Selected")
			{
				if (name != "AsmMgr.CanSkipResolve")
				{
					throw new InvalidOperationException(name);
				}
				this.canSkip = (bool)value;
				this.skip.Clear();
				return;
			}
			if (value != null)
			{
				this.Select(value, true);
				if (value is IReflectorObjectContainer && this.prop != null && this.propWin.IsVisible)
				{
					this.prop.Item = ((IReflectorObjectContainer)value).ReflectorObject;
					return;
				}
			}
			else
			{
				((TreeView)base.GetTemplateChild("PART_treeView")).SelectedValuePath = Environment.TickCount.ToString("X8");
				if (this.prop != null && this.propWin.IsVisible)
				{
					this.prop.Item = null;
					return;
				}
			}
		}

		[DllImport("user32.dll", CharSet=CharSet.None, ExactSpelling=false)]
		private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

		private void StopScroll(object sender, RequestBringIntoViewEventArgs e)
		{
			if (!this.isSelecting)
			{
				e.Handled = true;
			}
		}

		private void ToggleBookmark(object sender, ExecutedRoutedEventArgs e)
		{
			object obj;
			if (e.Parameter == null)
			{
				if (this.GetProp("AsmMgr.Selected") == null)
				{
					return;
				}
				obj = this.GetProp("AsmMgr.Selected");
			}
			else
			{
				obj = e.Parameter;
			}
			IReflecService bks = base._App.GetService("Bookmarks");
			object[] identifier = new object[] { (new CodeIdentifier(obj)).Identifier };
			bks.Exec("Bookmarks.Toggle", identifier);
			bks.Exec("Bookmarks.Show", new object[0]);
		}

		private void treeViewMouseDown(object sender, MouseButtonEventArgs e)
		{
			if (((FrameworkElement)((FrameworkElement)e.OriginalSource).TemplatedParent).TemplatedParent is TreeViewItem)
			{
				this.itm = new WeakReference(((FrameworkElement)((FrameworkElement)e.OriginalSource).TemplatedParent).TemplatedParent);
				if (((TreeViewItem)this.itm.Target).DataContext is ReflectorNode)
				{
					this.down = true;
					this.pt = e.GetPosition(null);
				}
			}
		}

		private void treeViewMouseMove(object sender, MouseEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Pressed && this.down && !this.isDrag)
			{
				Point position = e.GetPosition(null);
				if ((Math.Abs(position.X - this.pt.X) > SystemParameters.MinimumHorizontalDragDistance || Math.Abs(position.Y - this.pt.Y) > SystemParameters.MinimumVerticalDragDistance) && this.itm.IsAlive)
				{
					TreeViewItem item = (TreeViewItem)this.itm.Target;
					this.isDrag = true;
					Window window = new Window()
					{
						WindowStyle = WindowStyle.None,
						AllowsTransparency = true,
						AllowDrop = false,
						IsHitTestVisible = false,
						Height = ((FrameworkElement)item.Template.FindName("PART_Header", item)).ActualHeight,
						Width = ((FrameworkElement)item.Template.FindName("PART_Header", item)).ActualWidth,
						Topmost = true,
						ShowInTaskbar = false,
						Opacity = 0.5
					};
					Window visualBrush = window;
					visualBrush.SourceInitialized += new EventHandler((object argument0, EventArgs argument1) => {
						IntPtr handle = ((HwndSource)PresentationSource.FromVisual(visualBrush)).Handle;
						AssemblyManager.SetWindowLong(handle, -20, AssemblyManager.GetWindowLong(handle, -20) | 524288 | 32);
					});
					visualBrush.Background = new VisualBrush((Visual)item.Template.FindName("PART_Header", item));
					GiveFeedbackEventHandler handler = (object s, GiveFeedbackEventArgs ee) => {
						AssemblyManager.POINT pos;
						AssemblyManager.GetCursorPos(out pos);
						visualBrush.Left = (double)pos.x;
						visualBrush.Top = (double)pos.y;
					};
					item.GiveFeedback += handler;
					visualBrush.Show();
					DataObject obj = new DataObject("Reflector object", ((ReflectorNode)item.DataContext).ReflectorObject);
					DragDrop.DoDragDrop(item, obj, DragDropEffects.Move);
					visualBrush.Close();
					item.GiveFeedback -= handler;
					this.isDrag = false;
					this.down = false;
				}
			}
		}

		private void treeViewMouseUp(object sender, MouseButtonEventArgs e)
		{
			this.down = false;
		}

		public void Unload(AssemblyDefinition value)
		{
			this.OnAssemblyUnloaded(new AssemblyEventArgs(value));
		}

		public event EventHandler<AssemblyEventArgs> AssemblyLoaded;

		public event EventHandler<AssemblyEventArgs> AssemblyUnloaded;

		private class AssemblyResolver : BaseAssemblyResolver
		{
			private AssemblyManager mgr;

			public AssemblyResolver(AssemblyManager mgr)
			{
				this.mgr = mgr;
				base.ResolveFailure += new AssemblyResolveEventHandler(this.ResolveImpl);
				GlobalAssemblyResolver.Instance = this;
			}

			public override AssemblyDefinition Resolve(AssemblyNameReference name)
			{
				AssemblyDefinition assemblyDefinition;
				using (IEnumerator<AssemblyDefinition> enumerator = this.mgr.Assemblies.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						AssemblyDefinition asmDef = enumerator.Current;
						if (asmDef.Name.FullName != name.FullName)
						{
							continue;
						}
						assemblyDefinition = asmDef;
						return assemblyDefinition;
					}
					AssemblyDefinition ret = base.Resolve(name);
					if (ret != null)
					{
						this.mgr.OnAssemblyLoaded(new AssemblyEventArgs(ret));
					}
					return ret;
				}
				return assemblyDefinition;
			}

			private AssemblyDefinition ResolveImpl(object sender, AssemblyNameReference reference)
			{
				List<string> paths = new List<string>();
				foreach (AssemblyDefinition asm in this.mgr.Assemblies)
				{
					foreach (ModuleDefinition mod in asm.Modules)
					{
						if (File.Exists(Path.GetDirectoryName(mod.FullyQualifiedName)))
						{
							continue;
						}
						paths.Add(Path.GetDirectoryName(mod.FullyQualifiedName));
					}
				}
				AssemblyDefinition ret = base.SearchDirectory(reference, paths) ?? this.mgr.ResolveImpl(sender, reference);
				return ret;
			}
		}

		private struct POINT
		{
			public int x;

			public int y;
		}
	}
}