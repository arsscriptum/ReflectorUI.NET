using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;
using Mono.Cecil;
using Reflector;
using Reflector.CodeModel;
using Reflector.Pipelining;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace Reflector.UI
{
	internal class Disassembly : ReflecWindow, IDisassembly
	{
		private TextEditor dis;

		private static DependencyPropertyKey GoBackPropertyKey;

		public static DependencyProperty GoBackProperty;

		private static DependencyPropertyKey GoForwardPropertyKey;

		public static DependencyProperty GoForwardProperty;

		private static DependencyPropertyKey StopPropertyKey;

		public static DependencyProperty StopProperty;

		private static DependencyPropertyKey GotoPropertyKey;

		public static DependencyProperty GotoProperty;

		private static DependencyPropertyKey RefreshPropertyKey;

		public static DependencyProperty RefreshProperty;

		private Disassembly.DisTransformer transform;

		private Disassembly.DisGenerator generator = new Disassembly.DisGenerator();

		public readonly static DependencyProperty CurrentLanguageProperty;

		private LanguageWriterConfiguration cCfg;

		internal Action abort;

		private bool dising;

		private Disassembly.LinkedList history;

		private Point pt;

		private int draging;

		private WeakReference target;

		private WeakReference visualLine;

		private Popup popup;

		private TextBox searchBox;

		private string text = "";

		private int currentIdx = -1;

		public ILanguage CurrentLanguage
		{
			get
			{
				return JustDecompileGenerated_get_CurrentLanguage();
			}
			set
			{
				JustDecompileGenerated_set_CurrentLanguage(value);
			}
		}

		public ILanguage JustDecompileGenerated_get_CurrentLanguage()
		{
			if (base.CheckAccess())
			{
				return (ILanguage)base.GetValue(Disassembly.CurrentLanguageProperty);
			}
			return (ILanguage)base.Dispatcher.Invoke(new Func<object>(() => base.GetValue(Disassembly.CurrentLanguageProperty)), new object[0]);
		}

		public void JustDecompileGenerated_set_CurrentLanguage(ILanguage value)
		{
			if (base.CheckAccess())
			{
				base.SetValue(Disassembly.CurrentLanguageProperty, value);
				return;
			}
			base.Dispatcher.Invoke(new Action(() => base.SetValue(Disassembly.CurrentLanguageProperty, value)), new object[0]);
		}

		public object CurrentObject
		{
			get
			{
				if (this.history == null)
				{
					return null;
				}
				return this.history.Object;
			}
		}

		public TextEditor Text
		{
			get
			{
				return this.dis;
			}
		}

		static Disassembly()
		{
			Disassembly.GoBackPropertyKey = DependencyProperty.RegisterReadOnly("GoBack", typeof(bool), typeof(Disassembly), new PropertyMetadata(false));
			Disassembly.GoBackProperty = Disassembly.GoBackPropertyKey.DependencyProperty;
			Disassembly.GoForwardPropertyKey = DependencyProperty.RegisterReadOnly("GoForward", typeof(bool), typeof(Disassembly), new PropertyMetadata(false));
			Disassembly.GoForwardProperty = Disassembly.GoForwardPropertyKey.DependencyProperty;
			Disassembly.StopPropertyKey = DependencyProperty.RegisterReadOnly("Stop", typeof(bool), typeof(Disassembly), new PropertyMetadata(false));
			Disassembly.StopProperty = Disassembly.StopPropertyKey.DependencyProperty;
			Disassembly.GotoPropertyKey = DependencyProperty.RegisterReadOnly("Goto", typeof(bool), typeof(Disassembly), new PropertyMetadata(false));
			Disassembly.GotoProperty = Disassembly.GotoPropertyKey.DependencyProperty;
			Disassembly.RefreshPropertyKey = DependencyProperty.RegisterReadOnly("Refresh", typeof(bool), typeof(Disassembly), new PropertyMetadata(false));
			Disassembly.RefreshProperty = Disassembly.RefreshPropertyKey.DependencyProperty;
			Disassembly.CurrentLanguageProperty = DependencyProperty.Register("CurrentLanguage", typeof(ILanguage), typeof(Disassembly), new UIPropertyMetadata(null, new PropertyChangedCallback(Disassembly.OnCurrentLanguageChanged)));
			FrameworkElement.DefaultStyleKeyProperty.OverrideMetadata(typeof(Disassembly), new FrameworkPropertyMetadata(typeof(Disassembly)));
		}

		public Disassembly()
		{
			base.SetValue(ReflecWindow.ParentServicePropertyKey, DisassemblyService.Instance);
			this.transform = new Disassembly.DisTransformer();
			Disassembly.DisTransformer disTransformer = this.transform;
			Disassembly.Dis di = new Disassembly.Dis()
			{
				Colors = new Dictionary<int, Disassembly.Dis.Highlight>(),
				Text = ""
			};
			disTransformer.Disassembly = di;
			this.generator.Data.MouseDown += new MouseEventHandler(this.Data_MouseDown);
			this.generator.Data.MouseUp += new MouseEventHandler(this.Data_MouseUp);
			this.generator.Data.MouseHover += new MouseEventHandler(this.Data_MouseHover);
			this.generator.Data.MouseHoverStopped += new MouseEventHandler(this.Data_MouseHoverStopped);
			base.AllowDrop = true;
		}

		private DecompilationPipeline BuildPipeline(AssemblyNameReference assemblyRef, ILanguage lang, ILanguageWriterConfiguration cfg)
		{
			DecompilationPipeline ret = base._App.Translator.CreateDisassembler(cfg["Optimization"]).BuildPipeline(assemblyRef);
			ret.Completed += new Action<object>((object _) => {
				Disassembly.DisassmblyFormatter fmt = new Disassembly.DisassmblyFormatter();
				lang.GetWriter(fmt, cfg).WriteAssemblyReference((AssemblyNameReference)_);
				this.OnFinished(_, fmt.Result());
			});
			return ret;
		}

		private DecompilationPipeline BuildPipeline(AssemblyDefinition assembly, ILanguage lang, ILanguageWriterConfiguration cfg)
		{
			DecompilationPipeline ret = base._App.Translator.CreateDisassembler(cfg["Optimization"]).BuildPipeline(assembly, false);
			ret.Completed += new Action<object>((object _) => {
				Disassembly.DisassmblyFormatter fmt = new Disassembly.DisassmblyFormatter();
				lang.GetWriter(fmt, cfg).WriteAssembly((AssemblyDefinition)_);
				this.OnFinished(_, fmt.Result());
			});
			return ret;
		}

		private DecompilationPipeline BuildPipeline(ModuleDefinition module, ILanguage lang, ILanguageWriterConfiguration cfg)
		{
			DecompilationPipeline ret = base._App.Translator.CreateDisassembler(cfg["Optimization"]).BuildPipeline(module, false);
			ret.Completed += new Action<object>((object _) => {
				Disassembly.DisassmblyFormatter fmt = new Disassembly.DisassmblyFormatter();
				lang.GetWriter(fmt, cfg).WriteModule((ModuleDefinition)_);
				this.OnFinished(_, fmt.Result());
			});
			return ret;
		}

		private DecompilationPipeline BuildPipeline(ModuleReference moduleRef, ILanguage lang, ILanguageWriterConfiguration cfg)
		{
			DecompilationPipeline ret = base._App.Translator.CreateDisassembler(cfg["Optimization"]).BuildPipeline(moduleRef);
			ret.Completed += new Action<object>((object _) => {
				Disassembly.DisassmblyFormatter fmt = new Disassembly.DisassmblyFormatter();
				lang.GetWriter(fmt, cfg).WriteModuleReference((ModuleReference)_);
				this.OnFinished(_, fmt.Result());
			});
			return ret;
		}

		private DecompilationPipeline BuildPipeline(INamespace ns, ILanguage lang, ILanguageWriterConfiguration cfg)
		{
			DecompilationPipeline ret = base._App.Translator.CreateDisassembler(cfg["Optimization"]).BuildPipeline(ns, false);
			ret.Completed += new Action<object>((object _) => {
				Disassembly.DisassmblyFormatter fmt = new Disassembly.DisassmblyFormatter();
				lang.GetWriter(fmt, cfg).WriteNamespace((INamespace)_);
				this.OnFinished(_, fmt.Result());
			});
			return ret;
		}

		private DecompilationPipeline BuildPipeline(TypeDefinition typeDecl, ILanguage lang, ILanguageWriterConfiguration cfg)
		{
			DecompilationPipeline ret = base._App.Translator.CreateDisassembler(cfg["Optimization"]).BuildPipeline(typeDecl, true, false);
			ret.Completed += new Action<object>((object _) => {
				Disassembly.DisassmblyFormatter fmt = new Disassembly.DisassmblyFormatter();
				lang.GetWriter(fmt, cfg).WriteTypeDefinition((TypeDefinition)_);
				this.OnFinished(_, fmt.Result());
			});
			return ret;
		}

		private DecompilationPipeline BuildPipeline(MethodDefinition mtdDecl, ILanguage lang, ILanguageWriterConfiguration cfg)
		{
			DecompilationPipeline ret = base._App.Translator.CreateDisassembler(cfg["Optimization"]).BuildPipeline(mtdDecl);
			ret.Completed += new Action<object>((object _) => {
				Disassembly.DisassmblyFormatter fmt = new Disassembly.DisassmblyFormatter();
				lang.GetWriter(fmt, cfg).WriteMethodDefinition((MethodDefinition)_);
				this.OnFinished(_, fmt.Result());
			});
			return ret;
		}

		private DecompilationPipeline BuildPipeline(FieldDefinition fldDecl, ILanguage lang, ILanguageWriterConfiguration cfg)
		{
			DecompilationPipeline ret = base._App.Translator.CreateDisassembler(cfg["Optimization"]).BuildPipeline(fldDecl);
			ret.Completed += new Action<object>((object _) => {
				Disassembly.DisassmblyFormatter fmt = new Disassembly.DisassmblyFormatter();
				lang.GetWriter(fmt, cfg).WriteFieldDefinition((FieldDefinition)_);
				this.OnFinished(_, fmt.Result());
			});
			return ret;
		}

		private DecompilationPipeline BuildPipeline(EventDefinition evtDecl, ILanguage lang, ILanguageWriterConfiguration cfg)
		{
			DecompilationPipeline ret = base._App.Translator.CreateDisassembler(cfg["Optimization"]).BuildPipeline(evtDecl);
			ret.Completed += new Action<object>((object _) => {
				Disassembly.DisassmblyFormatter fmt = new Disassembly.DisassmblyFormatter();
				lang.GetWriter(fmt, cfg).WriteEventDefinition((EventDefinition)_);
				this.OnFinished(_, fmt.Result());
			});
			return ret;
		}

		private DecompilationPipeline BuildPipeline(PropertyDefinition propDecl, ILanguage lang, ILanguageWriterConfiguration cfg)
		{
			DecompilationPipeline ret = base._App.Translator.CreateDisassembler(cfg["Optimization"]).BuildPipeline(propDecl);
			ret.Completed += new Action<object>((object _) => {
				Disassembly.DisassmblyFormatter fmt = new Disassembly.DisassmblyFormatter();
				lang.GetWriter(fmt, cfg).WritePropertyDefinition((PropertyDefinition)_);
				this.OnFinished(_, fmt.Result());
			});
			return ret;
		}

		private void Data_MouseDown(object sender, MouseEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Pressed && this.transform.linkIdx != -1)
			{
				e.Handled = true;
				this.draging = 1;
				this.pt = e.GetPosition(null);
				this.target = new WeakReference(this.transform.Disassembly.References[this.transform.linkIdx].Ref);
				this.visualLine = new WeakReference(sender);
			}
		}

		private void Data_MouseHover(object sender, MouseEventArgs e)
		{
			if (this.popup == null)
			{
				this.popup = new Popup()
				{
					AllowsTransparency = true
				};
				Border border = new Border()
				{
					Background = new SolidColorBrush(Color.FromArgb(128, 32, 32, 32)),
					BorderBrush = new SolidColorBrush(Color.FromArgb(255, 221, 221, 221)),
					BorderThickness = new Thickness(0.5),
					CornerRadius = new CornerRadius(2),
					Padding = new Thickness(5)
				};
				Border bdr = border;
				Label lbl = new Label();
				lbl.SetBinding(ContentControl.ContentProperty, new Binding());
				bdr.Child = lbl;
				this.popup.Child = bdr;
				this.popup.PopupAnimation = PopupAnimation.Fade;
				this.popup.Placement = PlacementMode.Mouse;
				MouseEventHandler handler = (object _1, MouseEventArgs _2) => this.popup.IsOpen = false;
				this.popup.MouseMove += handler;
				this.popup.MouseLeave += handler;
			}
			if (this.generator.Data.Description != null)
			{
				this.popup.DataContext = this.generator.Data.Description;
				this.popup.IsOpen = true;
				this.popup.CaptureMouse();
			}
		}

		private void Data_MouseHoverStopped(object sender, MouseEventArgs e)
		{
			if (this.popup != null)
			{
				this.popup.IsOpen = false;
			}
		}

		private void Data_MouseUp(object sender, MouseEventArgs e)
		{
			if (this.transform.linkIdx != -1 && this.draging == 1 && this.target.IsAlive)
			{
				if (this.popup != null)
				{
					this.popup.IsOpen = false;
				}
				this.Disassemble(this.target.Target, this.CurrentLanguage, this.cCfg);
				e.Handled = true;
			}
			this.draging = 0;
		}

		private void dis_MouseMove(object sender, MouseEventArgs e)
		{
			if (this.draging == 1)
			{
				Point position = e.GetPosition(null);
				if ((Math.Abs(position.X - this.pt.X) > SystemParameters.MinimumHorizontalDragDistance || Math.Abs(position.Y - this.pt.Y) > SystemParameters.MinimumVerticalDragDistance) && this.target.IsAlive && this.visualLine.IsAlive)
				{
					this.draging = 2;
					VisualLineText element = (VisualLineText)this.visualLine.Target;
					TextBlock textBlock = new TextBlock()
					{
						Text = element.ParentVisualLine.Document.GetText(element.ParentVisualLine.StartOffset + element.RelativeTextOffset, element.DocumentLength)
					};
					TextBlock blk = textBlock;
					blk.Background = element.TextRunProperties.BackgroundBrush;
					blk.Foreground = element.TextRunProperties.ForegroundBrush;
					blk.FontFamily = element.TextRunProperties.Typeface.FontFamily;
					blk.FontSize = element.TextRunProperties.FontRenderingEmSize;
					blk.FontStyle = element.TextRunProperties.Typeface.Style;
					blk.FontStretch = element.TextRunProperties.Typeface.Stretch;
					blk.FontWeight = element.TextRunProperties.Typeface.Weight;
					blk.TextDecorations = element.TextRunProperties.TextDecorations;
					blk.TextEffects = element.TextRunProperties.TextEffects;
					blk.Measure(new Size(65535, 65535));
					Window window = new Window()
					{
						WindowStyle = WindowStyle.None,
						AllowsTransparency = true,
						AllowDrop = false,
						IsHitTestVisible = false,
						Width = blk.DesiredSize.Width,
						Height = element.TextRunProperties.FontRenderingEmSize,
						Topmost = true,
						ShowInTaskbar = false,
						Opacity = 0.5
					};
					Window visualBrush = window;
					visualBrush.SourceInitialized += new EventHandler((object argument0, EventArgs argument1) => {
						IntPtr handle = ((HwndSource)PresentationSource.FromVisual(visualBrush)).Handle;
						Disassembly.SetWindowLong(handle, -20, Disassembly.GetWindowLong(handle, -20) | 524288 | 32);
					});
					visualBrush.Background = new VisualBrush(blk);
					GiveFeedbackEventHandler handler = (object s, GiveFeedbackEventArgs ee) => {
						Disassembly.POINT p;
						Disassembly.GetCursorPos(out p);
						visualBrush.Left = (double)p.x;
						visualBrush.Top = (double)p.y;
					};
					this.dis.GiveFeedback += handler;
					visualBrush.Show();
					DataObject obj = new DataObject("Reflector object", this.target.Target);
					DragDrop.DoDragDrop(this.dis, obj, DragDropEffects.Move);
					obj.SetData("Reflector object", "Reflector object");
					visualBrush.Close();
					this.dis.GiveFeedback -= handler;
					this.draging = 0;
				}
				return;
			}
			if (this.transform.Disassembly.References == null)
			{
				return;
			}
			TextViewPosition? pos = this.dis.GetPositionFromPoint(e.GetPosition(this.dis));
			if (!pos.HasValue)
			{
				if (this.transform.linkIdx != -1)
				{
					int ti = this.transform.linkIdx;
					int tl = this.transform.linkLen;
					int num = -1;
					int num1 = num;
					this.transform.linkLen = num;
					this.transform.linkIdx = num1;
					this.dis.TextArea.TextView.Redraw(ti, tl, DispatcherPriority.Render);
				}
				return;
			}
			int offset = this.dis.TextArea.Document.GetOffset(pos.Value);
			int idx = -1;
			int len = -1;
			int i = offset;
			while (i != 0)
			{
				if (!this.transform.Disassembly.References.ContainsKey(i))
				{
					i--;
				}
				else
				{
					if (offset - i >= this.transform.Disassembly.References[i].Length)
					{
						break;
					}
					idx = i;
					len = this.transform.Disassembly.References[i].Length;
					break;
				}
			}
			int tmp = idx;
			idx = this.transform.linkIdx;
			this.transform.linkIdx = tmp;
			tmp = len;
			len = this.transform.linkLen;
			this.transform.linkLen = tmp;
			if (idx != -1)
			{
				this.dis.TextArea.TextView.Redraw(idx, len, DispatcherPriority.Render);
			}
			if (this.transform.linkIdx != idx)
			{
				this.dis.TextArea.TextView.Redraw(this.transform.linkIdx, this.transform.linkLen, DispatcherPriority.Render);
			}
			if (this.transform.linkIdx == -1)
			{
				this.generator.Data.Cursor = Cursors.Arrow;
			}
			else
			{
				this.generator.Data.Cursor = Cursors.Hand;
			}
			string des = null;
            //Cambio de la i por i1 (la i ya se está usando)
			int i1 = offset;
			while (i1 != 0)
			{
				if (!this.transform.Disassembly.Descriptions.ContainsKey(i1))
				{
					i1--;
				}
				else
				{
					if (offset - i1 >= this.transform.Disassembly.Descriptions[i1].Length)
					{
						break;
					}
					des = this.transform.Disassembly.Descriptions[i1].Descript;
					break;
				}
			}
			this.generator.Data.Description = des;
		}

		public void Disassemble(object obj, ILanguage lang, LanguageWriterConfiguration cfg)
		{
			if (this.abort != null)
			{
				this.abort();
			}
			if (this.history != null)
			{
				Disassembly.LinkedList linkedList = new Disassembly.LinkedList()
				{
					Object = obj,
					Previous = this.history
				};
				this.history = linkedList;
				this.history.Previous.Next = this.history;
			}
			else
			{
				this.history = new Disassembly.LinkedList()
				{
					Object = obj
				};
			}
			int num = -1;
			int num1 = num;
			this.transform.linkLen = num;
			this.transform.linkIdx = num1;
			Disassembly.DisTransformer disTransformer = this.transform;
			Disassembly.Dis di = new Disassembly.Dis()
			{
				Colors = new Dictionary<int, Disassembly.Dis.Highlight>(),
				Text = "Disassembling..."
			};
			disTransformer.Disassembly = di;
			this.dis.Document = new TextDocument(this.transform.Disassembly.Text);
			this.DisassembleCore(obj, lang, cfg, (object o) => this.history.Object = o);
		}

		private void DisassembleCore(object obj, ILanguage lang, LanguageWriterConfiguration cfg, Action<object> resolved)
		{
			object obj1 = obj;
			this.dising = true;
			this.CurrentLanguage = lang;
			this.cCfg = cfg;
			base.Dispatcher.BeginInvoke(new Action(this.RefreshToolbar), new object[0]);
			cfg["ShowMethodDefinitionBody"] = "false";
			cfg["ShowTypeDefinitionBody"] = "false";
			if (obj1 is MethodReference)
			{
				cfg["ShowMethodDefinitionBody"] = "true";
			}
			else if (obj1 is TypeReference)
			{
				cfg["ShowTypeDefinitionBody"] = "true";
			}
			else if (lang.Translate && (obj1 is PropertyReference || obj1 is EventReference))
			{
				cfg["ShowMethodDefinitionBody"] = "true";
			}
			base._App.GetService("AsmMgr").SetProp("AsmMgr.CanSkipResolve", true);
			if (obj1 is TypeReference)
			{
				TypeDefinition decl = ((TypeReference)obj1).Resolve();
				if (decl != null)
				{
					obj1 = decl;
				}
			}
			else if (obj1 is MethodReference)
			{
				MethodDefinition decl = ((MethodReference)obj1).Resolve();
				if (decl != null)
				{
					obj1 = decl;
				}
			}
			else if (obj1 is FieldReference)
			{
				FieldDefinition decl = ((FieldReference)obj1).Resolve();
				if (decl != null)
				{
					obj1 = decl;
				}
			}
			else if (obj1 is EventReference)
			{
				EventDefinition decl = ((EventReference)obj1).Resolve();
				if (decl != null)
				{
					obj1 = decl;
				}
			}
			else if (obj1 is PropertyReference)
			{
				PropertyDefinition decl = ((PropertyReference)obj1).Resolve();
				if (decl != null)
				{
					obj1 = decl;
				}
			}
			resolved(obj1);
			if (!lang.Translate)
			{
				Thread thread = new Thread((object o) => {
					try
					{
						Disassembly.DisassmblyFormatter fmt = new Disassembly.DisassmblyFormatter();
						ILanguageWriter wtr = lang.GetWriter(fmt, cfg);
						if (obj1 is AssemblyDefinition)
						{
							wtr.WriteAssembly((AssemblyDefinition)obj1);
						}
						else if (obj1 is AssemblyNameReference)
						{
							wtr.WriteAssemblyReference((AssemblyNameReference)obj1);
						}
						else if (obj1 is ModuleDefinition)
						{
							wtr.WriteModule((ModuleDefinition)obj1);
						}
						else if (obj1 is ModuleReference)
						{
							wtr.WriteModuleReference((ModuleReference)obj1);
						}
						else if (obj1 is INamespace)
						{
							wtr.WriteNamespace((INamespace)obj1);
						}
						else if (obj1 is TypeDefinition)
						{
							wtr.WriteTypeDefinition((TypeDefinition)obj1);
						}
						else if (obj1 is TypeReference)
						{
							fmt.Write(string.Concat("Could not resolve type reference '", obj1.ToString(), "'."));
						}
						else if (obj1 is MethodDefinition)
						{
							wtr.WriteMethodDefinition((MethodDefinition)obj1);
						}
						else if (obj1 is MethodReference)
						{
							fmt.Write(string.Concat("Could not resolve method reference '", obj1.ToString(), "'."));
						}
						else if (obj1 is FieldDefinition)
						{
							wtr.WriteFieldDefinition((FieldDefinition)obj1);
						}
						else if (obj1 is FieldReference)
						{
							fmt.Write(string.Concat("Could not resolve field reference '", obj1.ToString(), "'."));
						}
						else if (obj1 is EventDefinition)
						{
							wtr.WriteEventDefinition((EventDefinition)obj1);
						}
						else if (obj1 is EventReference)
						{
							fmt.Write(string.Concat("Could not resolve event reference '", obj1.ToString(), "'."));
						}
						else if (obj1 is PropertyDefinition)
						{
							wtr.WritePropertyDefinition((PropertyDefinition)obj1);
						}
						else if (obj1 is PropertyReference)
						{
							fmt.Write(string.Concat("Could not resolve property reference '", obj1.ToString(), "'."));
						}
						this.OnFinished(o, fmt.Result());
					}
					catch (ThreadAbortException threadAbortException)
					{
						this.OnFinished(obj1, new Disassembly.Dis()
						{
							Colors = new Dictionary<int, Disassembly.Dis.Highlight>(),
							Text = "Disassembly aborted."
						});
					}
					catch (Exception exception)
					{
						Exception exception1 = exception;
						this.OnFinished(obj1, new Disassembly.Dis()
						{
							Colors = new Dictionary<int, Disassembly.Dis.Highlight>(),
							Text = string.Format("Unhandled exception occured!!!\r\nException message : {0}\r\nStack Trace :\r\n{1}\r\n", exception1.Message, exception1.StackTrace)
						});
					}
				})
				{
					IsBackground = true
				};
				Thread th = thread;
				this.abort = new Action(th.Abort);
				th.Start();
				return;
			}
			DecompilationPipeline pl = null;
			string message = null;
			try
			{
				if (obj1 is AssemblyDefinition)
				{
					pl = this.BuildPipeline((AssemblyDefinition)obj1, lang, cfg);
				}
				else if (obj1 is AssemblyNameReference)
				{
					pl = this.BuildPipeline((AssemblyNameReference)obj1, lang, cfg);
				}
				else if (obj1 is ModuleDefinition)
				{
					pl = this.BuildPipeline((ModuleDefinition)obj1, lang, cfg);
				}
				else if (obj1 is ModuleReference)
				{
					pl = this.BuildPipeline((ModuleReference)obj1, lang, cfg);
				}
				else if (obj1 is INamespace)
				{
					pl = this.BuildPipeline((INamespace)obj1, lang, cfg);
				}
				else if (obj1 is TypeDefinition)
				{
					pl = this.BuildPipeline((TypeDefinition)obj1, lang, cfg);
				}
				else if (obj1 is TypeReference)
				{
					message = string.Concat("Could not resolve type reference '", obj1.ToString(), "'.");
				}
				else if (obj1 is MethodDefinition)
				{
					pl = this.BuildPipeline((MethodDefinition)obj1, lang, cfg);
				}
				else if (obj1 is MethodReference)
				{
					message = string.Concat("Could not resolve method reference '", obj1.ToString(), "'.");
				}
				else if (obj1 is FieldDefinition)
				{
					pl = this.BuildPipeline((FieldDefinition)obj1, lang, cfg);
				}
				else if (obj1 is FieldReference)
				{
					message = string.Concat("Could not resolve field reference '", obj1.ToString(), "'.");
				}
				else if (obj1 is EventDefinition)
				{
					pl = this.BuildPipeline((EventDefinition)obj1, lang, cfg);
				}
				else if (obj1 is EventReference)
				{
					message = string.Concat("Could not resolve event reference '", obj1.ToString(), "'.");
				}
				else if (!(obj1 is PropertyDefinition))
				{
					message = (!(obj1 is PropertyReference) ? "" : string.Concat("Could not resolve property reference '", obj1.ToString(), "'."));
				}
				else
				{
					pl = this.BuildPipeline((PropertyDefinition)obj1, lang, cfg);
				}
			}
			catch (Exception exception3)
			{
				Exception exception2 = exception3;
				message = string.Format("Unhandled exception occured!!!\r\nException message : {0}\r\nStack Trace :\r\n{1}\r\n", exception2.Message, exception2.StackTrace);
			}
			if (message != null)
			{
				object obj2 = obj1;
				Disassembly.Dis di = new Disassembly.Dis()
				{
					Colors = new Dictionary<int, Disassembly.Dis.Highlight>(),
					Text = message
				};
				this.OnFinished(obj2, di);
				return;
			}
			pl.Fault += new Action<DecompilationException>((DecompilationException ex) => this.OnFinished(obj1, new Disassembly.Dis()
			{
				Colors = new Dictionary<int, Disassembly.Dis.Highlight>(),
				Text = string.Format("{0}!!!\r\nException message : {1}\r\nStack Trace :\r\n{2}\r\n", ex.Message, ex.InnerException.Message, ex.InnerException.StackTrace)
			}));
			this.abort = new Action(pl.Abort);
			pl.BeginExecute();
		}

		public void DisassembleNoHist(object obj, ILanguage lang, LanguageWriterConfiguration cfg)
		{
			if (this.abort != null)
			{
				this.abort();
			}
			this.RefreshToolbar();
			int num = -1;
			int num1 = num;
			this.transform.linkLen = num;
			this.transform.linkIdx = num1;
			Disassembly.DisTransformer disTransformer = this.transform;
			Disassembly.Dis di = new Disassembly.Dis()
			{
				Colors = new Dictionary<int, Disassembly.Dis.Highlight>(),
				Text = "Disassembling..."
			};
			disTransformer.Disassembly = di;
			this.dis.Document = new TextDocument(this.transform.Disassembly.Text);
			this.DisassembleCore(obj, lang, cfg, (object _) => {
			});
		}

		private void ExeSearch(object sender, RoutedEventArgs e)
		{
			int idx;
			if (string.IsNullOrEmpty(this.searchBox.Text))
			{
				return;
			}
			idx = (this.searchBox.Text != this.text || this.currentIdx == -1 ? this.dis.Document.Text.IndexOf(this.searchBox.Text, StringComparison.OrdinalIgnoreCase) : this.dis.Document.Text.IndexOf(this.searchBox.Text, this.currentIdx + 1, StringComparison.OrdinalIgnoreCase));
			if (idx == -1)
			{
				if (this.currentIdx == -1 || this.searchBox.Text != this.text)
				{
					MessageBox.Show("Cannot find any matches!");
				}
				else
				{
					idx = this.dis.Document.Text.IndexOf(this.searchBox.Text, StringComparison.OrdinalIgnoreCase);
				}
			}
			if (idx != -1)
			{
				this.currentIdx = idx;
				this.text = this.searchBox.Text;
				this.dis.Select(idx, this.text.Length);
				TextLocation loc = this.dis.TextArea.Document.GetLocation(idx);
				this.dis.ScrollTo(loc.Line, loc.Column);
			}
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
			return Disassembly.FindParent<T>(parentObject);
		}

		[DllImport("user32.dll", CharSet=CharSet.None, ExactSpelling=false)]
		private static extern bool GetCursorPos(out Disassembly.POINT lpPoint);

		[DllImport("user32.dll", CharSet=CharSet.None, ExactSpelling=false)]
		private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

		public void GoBack()
		{
			if (this.history.Previous == null)
			{
				return;
			}
			this.DisassembleNoHist(this.history.Previous.Object, this.CurrentLanguage, this.cCfg);
			this.history = this.history.Previous;
		}

		public void GoForward()
		{
			if (this.history.Next == null)
			{
				return;
			}
			this.DisassembleNoHist(this.history.Next.Object, this.CurrentLanguage, this.cCfg);
			this.history = this.history.Next;
		}

		internal void Goto()
		{
			if (this.history.Object == null)
			{
				return;
			}
			base._App.GetService("AsmMgr").SetProp("AsmMgr.Selected", this.history.Object);
			base.Focus();
		}

		public override void OnApplyTemplate()
		{
			Button btn;
			base.OnApplyTemplate();
			this.dis = base.GetTemplateChild("PART_dis") as TextEditor;
			if (this.dis != null)
			{
				this.dis.TextArea.TextView.LineTransformers.Add(this.transform);
				this.dis.TextArea.TextView.ElementGenerators.Add(this.generator);
				this.dis.TextArea.SetResourceReference(TextArea.SelectionBorderProperty, "DisSelectBdr");
				this.dis.TextArea.SetResourceReference(TextArea.SelectionBrushProperty, "DisSelectBg");
				this.dis.TextArea.SetResourceReference(TextArea.SelectionForegroundProperty, "DisSelectFg");
				this.dis.TextArea.TextView.ClipToBounds = false;
				this.dis.TextArea.TextView.Margin = new Thickness(1);
				this.dis.MouseMove += new MouseEventHandler(this.dis_MouseMove);
			}
			ToolBar bar = (ToolBar)base.GetTemplateChild("PART_toolbar");
			foreach (DisassemblyService.DisasmBtnDesc i in DisassemblyService.Instance.btns)
			{
				if (!i.Equals(new DisassemblyService.DisasmBtnDesc()))
				{
					btn = new Button();
					DisassemblyService.DisasmBtnDesc disasmBtnDesc = i;
					btn.SetBinding(UIElement.IsEnabledProperty, disasmBtnDesc.able);
					btn.Content = new Bitmap()
					{
						Source = i.bmp
					};
					btn.Click += new RoutedEventHandler((object sender, RoutedEventArgs e) => disasmBtnDesc.exe(this));
					bar.Items.Add(btn);
				}
				else
				{
					bar.Items.Add(new Separator());
				}
			}
			this.searchBox = (TextBox)base.GetTemplateChild("PART_search");
			btn = (Button)base.GetTemplateChild("PART_doSearch");
			if (btn != null)
			{
				btn.Click += new RoutedEventHandler(this.ExeSearch);
			}
		}

		private static void OnCurrentLanguageChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			Disassembly disasm = (Disassembly)sender;
			if (disasm.history == null || disasm.history.Object == null || disasm.cCfg == null)
			{
				return;
			}
			disasm.DisassembleNoHist(disasm.history.Object, (ILanguage)e.NewValue, disasm.cCfg);
		}

		protected override void OnDragEnter(DragEventArgs e)
		{
			if (e.Data.GetDataPresent("Reflector object") && !(e.Data.GetData("Reflector object") is Resource))
			{
				e.Effects = DragDropEffects.Move;
				e.Handled = true;
			}
		}

		protected override void OnDrop(DragEventArgs e)
		{
			if (e.Data.GetDataPresent("Reflector object") && !(e.Data.GetData("Reflector object") is Resource))
			{
				this.Disassemble(e.Data.GetData("Reflector object"), this.CurrentLanguage, this.cCfg);
				e.Handled = true;
			}
		}

		private void OnFinished(object obj, Disassembly.Dis dis)
		{
			base._App.GetService("AsmMgr").SetProp("AsmMgr.CanSkipResolve", false);
			this.dising = false;
			System.Windows.Threading.Dispatcher dispatcher = base.Dispatcher;
			Action<Disassembly, Disassembly.Dis> disassembly = (Disassembly d, Disassembly.Dis s) => {
				d.transform.Disassembly = s;
				d.dis.Document = new TextDocument(s.Text);
				this.RefreshToolbar();
			};
			object[] objArray = new object[] { this, dis };
			dispatcher.BeginInvoke(disassembly, DispatcherPriority.Send, objArray);
			this.abort = null;
		}

		protected override void OnMouseLeave(MouseEventArgs e)
		{
			if (this.popup != null)
			{
				this.popup.IsOpen = false;
			}
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (this.popup != null)
			{
				this.popup.IsOpen = false;
			}
		}

		protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
		{
			base.OnMouseDown(e);
			ToolBar tb = (ToolBar)base.GetTemplateChild("PART_toolbar");
			FrameworkElement element = (FrameworkElement)tb.InputHitTest(e.GetPosition(tb));
			if (element != null)
			{
				element = Disassembly.FindParent<Button>(element);
			}
			if (element != null)
			{
				element.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
				e.Handled = true;
			}
			if (e.XButton1 == MouseButtonState.Pressed)
			{
				this.GoBack();
				return;
			}
			if (e.XButton2 == MouseButtonState.Pressed)
			{
				this.GoForward();
			}
		}

		public void Redraw()
		{
			this.dis.TextArea.TextView.Redraw();
		}

		public void Refresh()
		{
			if (this.history.Object == null)
			{
				return;
			}
			this.DisassembleNoHist(this.history.Object, this.CurrentLanguage, this.cCfg);
		}

		private void RefreshToolbar()
		{
			this.transform.linkIdx = -1;
			base.SetValue(Disassembly.GoBackPropertyKey, (this.history == null ? false : this.history.Previous != null));
			base.SetValue(Disassembly.GoForwardPropertyKey, (this.history == null ? false : this.history.Next != null));
			base.SetValue(Disassembly.StopPropertyKey, this.dising);
			base.SetValue(Disassembly.RefreshPropertyKey, !this.dising);
			base.SetValue(Disassembly.GotoPropertyKey, this.history.Object != null);
			base.SetValue(ReflecWindow.TitlePropertyKey, string.Format("{0} [{1}]", AsmViewHelper.Escape(AsmViewHelper.GetName(this.history.Object)), this.CurrentLanguage.Name));
			if (this.history.Object != null)
			{
				base.SetValue(ReflecWindow.IconPropertyKey, AsmViewHelper.GetIcon(this.history.Object));
				CodeIdentifier id = new CodeIdentifier(this.history.Object);
				base.SetValue(ReflecWindow.IdPropertyKey, id.Identifier);
			}
		}

		[DllImport("user32.dll", CharSet=CharSet.None, ExactSpelling=false)]
		private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

		public void Stop()
		{
			if (this.abort == null)
			{
				return;
			}
			this.abort();
			Disassembly.DisTransformer disTransformer = this.transform;
			Disassembly.Dis di = new Disassembly.Dis()
			{
				Colors = new Dictionary<int, Disassembly.Dis.Highlight>(),
				Text = "Aborting...."
			};
			disTransformer.Disassembly = di;
			this.dis.Document = new TextDocument(this.transform.Disassembly.Text);
		}

		public class Dis
		{
			public Dictionary<int, Disassembly.Dis.Highlight> Colors
			{
				get;
				set;
			}

			public Dictionary<int, Disassembly.Dis.Description> Descriptions
			{
				get;
				set;
			}

			public Dictionary<int, Disassembly.Dis.Reference> References
			{
				get;
				set;
			}

			public string Text
			{
				get;
				set;
			}

			public Dis()
			{
			}

			public struct Description
			{
				public int Length;

				public string Descript;
			}

			public struct Highlight
			{
				public int Length;

				public bool Bold;

				public string Color;
			}

			public struct Reference
			{
				public int Length;

				public object Ref;
			}
		}

		private class DisassmblyFormatter : IFormatter
		{
			private StringBuilder final_text;

			private Dictionary<int, Disassembly.Dis.Highlight> final_colors;

			private Dictionary<int, Disassembly.Dis.Reference> final_refs;

			private Dictionary<int, Disassembly.Dis.Description> final_des;

			private Disassembly.DisassmblyFormatter.TextType now;

			private StringBuilder sb;

			private int indent;

			private bool nl;

			public DisassmblyFormatter()
			{
				this.sb = new StringBuilder();
				this.final_text = new StringBuilder();
				this.final_colors = new Dictionary<int, Disassembly.Dis.Highlight>();
				this.final_refs = new Dictionary<int, Disassembly.Dis.Reference>();
				this.final_des = new Dictionary<int, Disassembly.Dis.Description>();
			}

			private void Check(Disassembly.DisassmblyFormatter.TextType type, string val)
			{
				if (this.now != type)
				{
					this.Flush();
					this.now = type;
				}
			}

			private void Flush()
			{
				if (this.sb.Length == 0)
				{
					return;
				}
				string str = this.sb.ToString();
				if (this.now != Disassembly.DisassmblyFormatter.TextType.Literal && this.now != Disassembly.DisassmblyFormatter.TextType.Keyword && this.now != Disassembly.DisassmblyFormatter.TextType.Comment && this.now != Disassembly.DisassmblyFormatter.TextType.Plain)
				{
					str = AsmViewHelper.Escape(str);
				}
				switch (this.now)
				{
					case Disassembly.DisassmblyFormatter.TextType.Plain:
					{
						Dictionary<int, Disassembly.Dis.Highlight> finalColors = this.final_colors;
						int length = this.final_text.Length;
						Disassembly.Dis.Highlight highlight = new Disassembly.Dis.Highlight()
						{
							Length = str.Length,
							Color = "Text"
						};
						finalColors.Add(length, highlight);
						break;
					}
					case Disassembly.DisassmblyFormatter.TextType.Comment:
					{
						Dictionary<int, Disassembly.Dis.Highlight> nums = this.final_colors;
						int num = this.final_text.Length;
						Disassembly.Dis.Highlight highlight1 = new Disassembly.Dis.Highlight()
						{
							Length = str.Length,
							Color = "Comment"
						};
						nums.Add(num, highlight1);
						break;
					}
					case Disassembly.DisassmblyFormatter.TextType.Declar:
					{
						Dictionary<int, Disassembly.Dis.Highlight> finalColors1 = this.final_colors;
						int length1 = this.final_text.Length;
						Disassembly.Dis.Highlight highlight2 = new Disassembly.Dis.Highlight()
						{
							Length = str.Length,
							Color = "Text",
							Bold = true
						};
						finalColors1.Add(length1, highlight2);
						break;
					}
					case Disassembly.DisassmblyFormatter.TextType.Keyword:
					{
						Dictionary<int, Disassembly.Dis.Highlight> nums1 = this.final_colors;
						int num1 = this.final_text.Length;
						Disassembly.Dis.Highlight highlight3 = new Disassembly.Dis.Highlight()
						{
							Length = str.Length,
							Color = "Keyword"
						};
						nums1.Add(num1, highlight3);
						break;
					}
					case Disassembly.DisassmblyFormatter.TextType.Literal:
					{
						Dictionary<int, Disassembly.Dis.Highlight> finalColors2 = this.final_colors;
						int length2 = this.final_text.Length;
						Disassembly.Dis.Highlight highlight4 = new Disassembly.Dis.Highlight()
						{
							Length = str.Length,
							Color = "Literal"
						};
						finalColors2.Add(length2, highlight4);
						break;
					}
					case Disassembly.DisassmblyFormatter.TextType.Refer:
					{
						Dictionary<int, Disassembly.Dis.Highlight> nums2 = this.final_colors;
						int num2 = this.final_text.Length;
						Disassembly.Dis.Highlight highlight5 = new Disassembly.Dis.Highlight()
						{
							Length = str.Length,
							Color = "Refer"
						};
						nums2.Add(num2, highlight5);
						break;
					}
				}
				this.final_text.Append(str);
				this.sb = new StringBuilder();
			}

			private bool IsAllSpace(string val)
			{
				string str = val;
				for (int i = 0; i < str.Length; i++)
				{
					if (str[i] != ' ')
					{
						return false;
					}
				}
				return true;
			}

			private bool IsAllSpace(StringBuilder val)
			{
				for (int i = 0; i < val.Length; i++)
				{
					if (val[i] != ' ')
					{
						return false;
					}
				}
				return true;
			}

			public Disassembly.Dis Result()
			{
				this.Flush();
				Disassembly.Dis di = new Disassembly.Dis()
				{
					Text = this.final_text.ToString(),
					Colors = this.final_colors,
					References = this.final_refs,
					Descriptions = this.final_des
				};
				return di;
			}

			public void Write(string value)
			{
				this.WriteIndentation();
				this.Check(Disassembly.DisassmblyFormatter.TextType.Plain, value);
				this.sb.Append(value);
			}

			public void WriteComment(string value)
			{
				this.WriteIndentation();
				this.Check(Disassembly.DisassmblyFormatter.TextType.Comment, value);
				this.sb.Append(value);
			}

			public void WriteDefinition(string value, object target)
			{
				this.WriteIndentation();
				this.Check(Disassembly.DisassmblyFormatter.TextType.Declar, value);
				Dictionary<int, Disassembly.Dis.Reference> finalRefs = this.final_refs;
				int length = this.final_text.Length + this.sb.Length;
				Disassembly.Dis.Reference reference = new Disassembly.Dis.Reference()
				{
					Length = AsmViewHelper.Escape(value).Length,
					Ref = target
				};
				finalRefs.Add(length, reference);
				this.sb.Append(value);
			}

			public void WriteDefinition(string value)
			{
				this.WriteIndentation();
				this.Check(Disassembly.DisassmblyFormatter.TextType.Declar, value);
				this.sb.Append(value);
			}

			public void WriteIndent()
			{
				this.indent++;
			}

			private void WriteIndentation()
			{
				if (this.nl)
				{
					this.sb.Append(' ', this.indent * 4);
				}
				this.nl = false;
			}

			public void WriteKeyword(string value)
			{
				this.WriteIndentation();
				this.Check(Disassembly.DisassmblyFormatter.TextType.Keyword, value);
				this.sb.Append(value);
			}

			public void WriteLine()
			{
				this.nl = true;
				this.Flush();
				this.final_text.Append(Environment.NewLine);
			}

			public void WriteLiteral(string value)
			{
				this.WriteIndentation();
				this.Check(Disassembly.DisassmblyFormatter.TextType.Literal, value);
				this.sb.Append(value);
			}

			public void WriteOutdent()
			{
				this.indent--;
			}

			public void WriteProperty(string name, string value)
			{
			}

			public void WriteReference(string value, string description, object target)
			{
				this.WriteIndentation();
				this.Check(Disassembly.DisassmblyFormatter.TextType.Refer, value);
				if (target != null)
				{
					Dictionary<int, Disassembly.Dis.Reference> finalRefs = this.final_refs;
					int length = this.final_text.Length + this.sb.Length;
					Disassembly.Dis.Reference reference = new Disassembly.Dis.Reference()
					{
						Length = AsmViewHelper.Escape(value).Length,
						Ref = target
					};
					finalRefs.Add(length, reference);
				}
				if (description != null)
				{
					Dictionary<int, Disassembly.Dis.Description> finalDes = this.final_des;
					int num = this.final_text.Length + this.sb.Length;
					Disassembly.Dis.Description description1 = new Disassembly.Dis.Description()
					{
						Length = AsmViewHelper.Escape(value).Length,
						Descript = AsmViewHelper.Escape(description)
					};
					finalDes.Add(num, description1);
				}
				this.sb.Append(value);
			}

			private enum TextType
			{
				Plain,
				Comment,
				Declar,
				Keyword,
				Literal,
				Refer
			}
		}

		private class DisData
		{
			public string Description;

			public System.Windows.Input.Cursor Cursor;

			public DisData()
			{
			}

			public void OnMouseDown(object sender, MouseButtonEventArgs e)
			{
				if (this.MouseDown != null)
				{
					this.MouseDown(sender, e);
				}
			}

			public void OnMouseHover(object sender, MouseEventArgs e)
			{
				if (this.MouseHover != null)
				{
					this.MouseHover(sender, e);
				}
			}

			public void OnMouseHoverStopped(object sender, MouseEventArgs e)
			{
				if (this.MouseHoverStopped != null)
				{
					this.MouseHoverStopped(sender, e);
				}
			}

			public void OnMouseUp(object sender, MouseButtonEventArgs e)
			{
				if (this.MouseUp != null)
				{
					this.MouseUp(sender, e);
				}
			}

			public event MouseEventHandler MouseDown;

			public event MouseEventHandler MouseHover;

			public event MouseEventHandler MouseHoverStopped;

			public event MouseEventHandler MouseUp;
		}

		private class DisElement : VisualLineText
		{
			public Disassembly.DisData Data
			{
				get;
				set;
			}

			public DisElement(VisualLine parentVisualLine, int length) : base(parentVisualLine, length)
			{
			}

			protected override VisualLineText CreateInstance(int length)
			{
				Disassembly.DisElement disElement = new Disassembly.DisElement(base.ParentVisualLine, length)
				{
					Data = this.Data
				};
				return disElement;
			}

			protected override void OnMouseDown(MouseButtonEventArgs e)
			{
				this.Data.OnMouseDown(this, e);
			}

			protected override void OnMouseHover(MouseEventArgs e)
			{
				this.Data.OnMouseHover(this, e);
			}

			protected override void OnMouseHoverStopped(MouseEventArgs e)
			{
				this.Data.OnMouseHoverStopped(this, e);
			}

			protected override void OnMouseUp(MouseButtonEventArgs e)
			{
				this.Data.OnMouseUp(this, e);
			}

			protected override void OnQueryCursor(QueryCursorEventArgs e)
			{
				e.Cursor = this.Data.Cursor;
				e.Handled = true;
			}
		}

		private class DisGenerator : VisualLineElementGenerator
		{
			public Disassembly.DisData Data;

			private VisualLine line;

			private TextDocument doc;

			public DisGenerator()
			{
			}

			public override VisualLineElement ConstructElement(int offset)
			{
				Disassembly.DisElement disElement = new Disassembly.DisElement(this.line, this.doc.GetLineByOffset(offset).Length)
				{
					Data = this.Data
				};
				return disElement;
			}

			public override int GetFirstInterestedOffset(int startOffset)
			{
				DocumentLine l = this.doc.GetLineByOffset(startOffset);
				while (l != null && l.Length == 0)
				{
					l = l.NextLine;
				}
				if (l == null)
				{
					return -1;
				}
				if (l.Offset < startOffset)
				{
					return -1;
				}
				return l.Offset;
			}

			public override void StartGeneration(ITextRunConstructionContext context)
			{
				this.line = context.VisualLine;
				this.doc = context.Document;
			}
		}

		private class DisTransformer : DocumentColorizingTransformer
		{
			internal int linkIdx;

			internal int linkLen;

			internal Disassembly.Dis Disassembly;

			public DisTransformer()
			{
			}

			protected override void ColorizeLine(DocumentLine line)
			{
				Disassembly.Dis.Highlight highlight;
				for (int i = line.Offset; i < line.Offset + line.Length; i++)
				{
					if (this.Disassembly.Colors.TryGetValue(i, out highlight))
					{
						base.ChangeLinePart(i, i + highlight.Length, (VisualLineElement v) => {
							v.TextRunProperties.SetForegroundBrush((Brush)Application.Current.FindResource(string.Concat("Dis", highlight.Color, "Brush")));
							if (highlight.Bold)
							{
								v.TextRunProperties.SetTypeface(new Typeface(v.TextRunProperties.Typeface.FontFamily, v.TextRunProperties.Typeface.Style, FontWeights.Bold, v.TextRunProperties.Typeface.Stretch));
							}
						});
					}
					if (i == this.linkIdx)
					{
						base.ChangeLinePart(this.linkIdx, this.linkIdx + this.linkLen, (VisualLineElement v) => v.TextRunProperties.SetTextDecorations(TextDecorations.Underline));
					}
				}
			}
		}

		private class LinkedList
		{
			public Disassembly.LinkedList Previous;

			public object Object;

			public Disassembly.LinkedList Next;

			public LinkedList()
			{
			}
		}

		private struct POINT
		{
			public int x;

			public int y;
		}
	}
}