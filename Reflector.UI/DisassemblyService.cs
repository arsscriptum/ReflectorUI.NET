using Mono.Cecil;
using Mono.Collections.Generic;
using Reflector;
using Reflector.CodeModel;
using Reflector.Pipelining;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.Xml;

namespace Reflector.UI
{
	internal class DisassemblyService : IReflecService
	{
		public readonly static DisassemblyService Instance;

		private List<IReflecWindow> windows = new List<IReflecWindow>();

		internal List<DisassemblyService.DisasmBtnDesc> btns;

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
				((IAssemblyManager)this.app.GetService("AsmMgr")).AssemblyUnloaded += new EventHandler<AssemblyEventArgs>((object sender, AssemblyEventArgs e) => {
					List<IReflecWindow> invali = new List<IReflecWindow>();
					foreach (IReflecWindow win in this.windows)
					{
						if (!(win.Content is Disassembly))
						{
							continue;
						}
						Disassembly dis = (Disassembly)win.Content;
						if (dis.CurrentObject == null || !(this.GetAssembly(dis.CurrentObject).Name.FullName == e.Assembly.Name.FullName))
						{
							continue;
						}
						invali.Add(win);
					}
					foreach (IReflecWindow dis in invali)
					{
						dis.Close();
					}
				});
				Options.ThemeChanged += new EventHandler((object sender, EventArgs e) => {
					foreach (IReflecWindow win in this.windows)
					{
						if (!(win is Disassembly))
						{
							continue;
						}
						((Disassembly)win).Redraw();
					}
				});
				DependencyPropertyDescriptor desc = DependencyPropertyDescriptor.FromProperty(Options.OptimizationProperty, typeof(Options));
				desc.AddValueChanged(this.app.GetService("Options"), (object sender, EventArgs e) => LanguageWriterConfiguration.Instance["Optimization"] = ((Options)sender).Optimization);
                desc = DependencyPropertyDescriptor.FromProperty(Options.NumberFormatProperty, typeof(Options));
				desc.AddValueChanged(this.app.GetService("Options"), (object sender, EventArgs e) => LanguageWriterConfiguration.Instance["NumberFormat"] = ((Options)sender).NumberFormat);
			}
		}

		public string Id
		{
			get
			{
				return "Disasm";
			}
		}

		static DisassemblyService()
		{
			DisassemblyService.Instance = new DisassemblyService();
		}

		private DisassemblyService()
		{
			this.windows = new List<IReflecWindow>();
			List<DisassemblyService.DisasmBtnDesc> disasmBtnDescs = new List<DisassemblyService.DisasmBtnDesc>();
			List<DisassemblyService.DisasmBtnDesc> disasmBtnDescs1 = disasmBtnDescs;
			DisassemblyService.DisasmBtnDesc disasmBtnDesc = new DisassemblyService.DisasmBtnDesc()
			{
				bmp = (BitmapSource)Application.Current.Resources["dback"],
				exe = (IDisassembly dis) => dis.GoBack()
			};
			Binding binding = new Binding("GoBack")
			{
				RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(Disassembly), 1)
			};
			disasmBtnDesc.able = binding;
			disasmBtnDescs1.Add(disasmBtnDesc);
			List<DisassemblyService.DisasmBtnDesc> disasmBtnDescs2 = disasmBtnDescs;
			DisassemblyService.DisasmBtnDesc disasmBtnDesc1 = new DisassemblyService.DisasmBtnDesc()
			{
				bmp = (BitmapSource)Application.Current.Resources["dforward"],
				exe = (IDisassembly dis) => dis.GoForward()
			};
			Binding binding1 = new Binding("GoForward")
			{
				RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(Disassembly), 1)
			};
			disasmBtnDesc1.able = binding1;
			disasmBtnDescs2.Add(disasmBtnDesc1);
			disasmBtnDescs.Add(new DisassemblyService.DisasmBtnDesc());
			List<DisassemblyService.DisasmBtnDesc> disasmBtnDescs3 = disasmBtnDescs;
			DisassemblyService.DisasmBtnDesc disasmBtnDesc2 = new DisassemblyService.DisasmBtnDesc()
			{
				bmp = (BitmapSource)Application.Current.Resources["dstop"],
				exe = (IDisassembly dis) => dis.Stop()
			};
			Binding binding2 = new Binding("Stop")
			{
				RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(Disassembly), 1)
			};
			disasmBtnDesc2.able = binding2;
			disasmBtnDescs3.Add(disasmBtnDesc2);
			List<DisassemblyService.DisasmBtnDesc> disasmBtnDescs4 = disasmBtnDescs;
			DisassemblyService.DisasmBtnDesc disasmBtnDesc3 = new DisassemblyService.DisasmBtnDesc()
			{
				bmp = (BitmapSource)Application.Current.Resources["drefr"],
				exe = (IDisassembly dis) => dis.Refresh()
			};
			Binding binding3 = new Binding("Refresh")
			{
				RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(Disassembly), 1)
			};
			disasmBtnDesc3.able = binding3;
			disasmBtnDescs4.Add(disasmBtnDesc3);
			disasmBtnDescs.Add(new DisassemblyService.DisasmBtnDesc());
			List<DisassemblyService.DisasmBtnDesc> disasmBtnDescs5 = disasmBtnDescs;
			DisassemblyService.DisasmBtnDesc disasmBtnDesc4 = new DisassemblyService.DisasmBtnDesc()
			{
				bmp = (BitmapSource)Application.Current.Resources["dgoto"],
				exe = (IDisassembly dis) => ((Disassembly)dis).Goto()
			};
			Binding binding4 = new Binding("Goto")
			{
				RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(Disassembly), 1)
			};
			disasmBtnDesc4.able = binding4;
			disasmBtnDescs5.Add(disasmBtnDesc4);
			this.btns = disasmBtnDescs;
			//base();
		}

		public object Exec(string name, params object[] args)
		{
			ReflecWindow disassembly;
			if (name != "Disasm.Disasm")
			{
				if (name != "Disasm.DisasmText")
				{
					throw new InvalidOperationException(name);
				}
				MethodDefinition method = (MethodDefinition)args[0];
				ILanguage lang = (ILanguage)args[1];
				if (lang.Translate)
				{
					method = (MethodDefinition)App.Reflector.Translator.CreateDisassembler(LanguageWriterConfiguration.Instance["Optimization"]).BuildPipeline(method).Execute();
				}
				TextFormatter fmt = new TextFormatter();
				lang.GetWriter(fmt, LanguageWriterConfiguration.Instance).WriteMethodDefinition(method);
				return fmt.ToString();
			}
			if (this._App.ActiveLanguage == null && (int)args.Length == 1)
			{
				return null;
			}
			if (!(args[0] is Resource))
			{
				disassembly = new Disassembly();
			}
			else
			{
				disassembly = new ResourceViewer();
			}
			IReflecWindow w = this._App.CreateWindow(disassembly);
			w.ShowDocument();
			w.Closed += new EventHandler((object sender, EventArgs e) => this.windows.Remove((IReflecWindow)sender));
			if (disassembly is Disassembly)
			{
				w.Closed += new EventHandler((object sender, EventArgs e) => {
					if (((Disassembly)disassembly).abort != null)
					{
						((Disassembly)disassembly).abort();
					}
				});
			}
			this.windows.Add(w);
			disassembly.ApplyTemplate();
			disassembly.InvalidateMeasure();
			if (!(args[0] is Resource))
			{
				((Disassembly)disassembly).Disassemble(args[0], ((int)args.Length == 1 ? this._App.ActiveLanguage : (ILanguage)args[1]), LanguageWriterConfiguration.Instance);
			}
			else
			{
				((ResourceViewer)disassembly).View((Resource)args[0]);
			}
			w.Activate();
			return null;
		}

		private AssemblyDefinition GetAssembly(object obj)
		{
			if (obj is TypeDefinition)
			{
				return ((TypeDefinition)obj).Module.Assembly;
			}
			if (obj is IMemberDefinition)
			{
				return ((IMemberDefinition)obj).DeclaringType.Module.Assembly;
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
			if (name != "Disasm.Windows")
			{
				throw new InvalidOperationException(name);
			}
			return this.windows.AsReadOnly();
		}

		public void LoadSettings(XmlNode node)
		{
			Dictionary<string, ILanguage> langs = new Dictionary<string, ILanguage>();
			foreach (ILanguage lang in this._App.Languages)
			{
				langs.Add(lang.Name, lang);
			}
			foreach (XmlNode asm in node.SelectNodes("disasm"))
			{
				CodeIdentifier id = new CodeIdentifier(asm.Attributes["id"].Value);
				object obj = id.Resolve((IAssemblyManager)this._App.GetService("AsmMgr"));
				if (obj == null)
				{
					continue;
				}
				if (asm.Attributes["lang"] == null)
				{
					object[] objArray = new object[] { obj };
					this.Exec("Disasm.Disasm", objArray);
				}
				else
				{
					object[] item = new object[] { obj, langs[asm.Attributes["lang"].Value] };
					this.Exec("Disasm.Disasm", item);
				}
			}
		}

		public void SaveSettings(XmlDocument doc, XmlNode node)
		{
			foreach (IReflecWindow win in this.windows)
			{
				XmlElement element = doc.CreateElement("disasm");
				XmlAttribute attr = doc.CreateAttribute("id");
				attr.Value = win.Content.Id;
				element.Attributes.Append(attr);
				if (win.Content is Disassembly)
				{
					attr = doc.CreateAttribute("lang");
					attr.Value = ((Disassembly)win.Content).CurrentLanguage.Name;
					element.Attributes.Append(attr);
				}
				node.AppendChild(element);
			}
		}

		public void SetProp(string name, object value)
		{
			throw new InvalidOperationException(name);
		}

		internal struct DisasmBtnDesc
		{
			public BitmapSource bmp;

			public Action<IDisassembly> exe;

			public Binding able;
		}
	}
}