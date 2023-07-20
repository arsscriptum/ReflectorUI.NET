using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.GAC;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;

namespace Reflector.UI
{
	public partial class GACSelector : Window
	{
		private static IAssemblyCache cache;

		private ICollectionView source;

		private string fliter = "";

		static GACSelector()
		{
			GACSelector.cache = AssemblyCache.CreateAssemblyCache();
		}

		public GACSelector()
		{
			this.InitializeComponent();
			this.source = CollectionViewSource.GetDefaultView(GACSelector.GACAssemblies.Instance);
			this.listBox.Items.Filter = (object _) => {
				GACSelector.GACAssemblyName name = (GACSelector.GACAssemblyName)_;
				if (string.IsNullOrEmpty(this.fliter))
				{
					return true;
				}
				return name.Name.IndexOf(this.fliter, 0, StringComparison.InvariantCultureIgnoreCase) != -1;
			};
			this.listBox.Items.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
			this.textBox.TextChanged += new TextChangedEventHandler((object sender, TextChangedEventArgs e) => {
				this.fliter = this.textBox.Text;
				this.source.Refresh();
			});
		}

		private void DoubleClick(object sender, MouseButtonEventArgs e)
		{
			ListViewItem item = (ListViewItem)sender;
			IReflecService service = App.Reflector.GetService("AsmMgr");
			object[] location = new object[] { ((GACSelector.GACAssemblyName)item.DataContext).Location };
			service.Exec("AsmMgr.OpenAsm", location);
			base.DialogResult = new bool?(true);
		}

		public class GACAssemblies : Collection<GACSelector.GACAssemblyName>
		{
			public static GACSelector.GACAssemblies Instance;

			static GACAssemblies()
			{
				GACSelector.GACAssemblies.Instance = new GACSelector.GACAssemblies();
			}

			private GACAssemblies()
			{
				GACSelector.GACEnumerator etor = GACSelector.GACEnumerator.Instance;
				etor.Reset();
				while (etor.MoveNext())
				{
					base.Add(etor.Current);
				}
			}
		}

		public struct GACAssemblyName
		{
			private IAssemblyName name;

			private string fn;

			private string n;

			private string ver;

			private string pkt;

			private string cult;

			private string loc;

			public string Culture
			{
				get
				{
					if (this.cult == null)
					{
						this.cult = AssemblyCache.GetCulture(this.name).Name;
					}
					return this.cult;
				}
			}

			public string FullName
			{
				get
				{
					if (this.fn == null)
					{
						this.fn = AssemblyCache.GetDisplayName(this.name, ASM_DISPLAY_FLAGS.VERSION | ASM_DISPLAY_FLAGS.CULTURE | ASM_DISPLAY_FLAGS.PUBLIC_KEY_TOKEN | ASM_DISPLAY_FLAGS.PUBLIC_KEY | ASM_DISPLAY_FLAGS.CUSTOM | ASM_DISPLAY_FLAGS.PROCESSORARCHITECTURE | ASM_DISPLAY_FLAGS.LANGUAGEID);
					}
					return this.fn;
				}
			}

			public string Location
			{
				get
				{
					if (this.loc == null)
					{
						ASSEMBLY_INFO info = new ASSEMBLY_INFO()
						{
							cchBuf = 1024,
							pszCurrentAssemblyPathBuf = new string('\0', 1024)
						};
						GACSelector.cache.QueryAssemblyInfo(0, this.FullName, ref info);
						this.loc = info.pszCurrentAssemblyPathBuf;
					}
					return this.loc;
				}
			}

			public string Name
			{
				get
				{
					if (this.n == null)
					{
						this.n = AssemblyCache.GetName(this.name);
					}
					return this.n;
				}
			}

			public string PKT
			{
				get
				{
					if (this.pkt == null)
					{
						this.pkt = BitConverter.ToString(AssemblyCache.GetPublicKeyToken(this.name)).Replace("-", "").ToLower();
					}
					return this.pkt;
				}
			}

			public string Version
			{
				get
				{
					if (this.ver == null)
					{
						this.ver = AssemblyCache.GetVersion(this.name).ToString();
					}
					return this.ver;
				}
			}

			public GACAssemblyName(IAssemblyName n)
			{
				this.name = n;
				object obj = null;
				string str = (string)obj;
				this.loc = (string)obj;
				string str1 = str;
				string str2 = str1;
				this.cult = str1;
				string str3 = str2;
				string str4 = str3;
				this.pkt = str3;
				string str5 = str4;
				string str6 = str5;
				this.ver = str5;
				string str7 = str6;
				string str8 = str7;
				this.fn = str7;
				this.n = str8;
			}
		}

		private class GACEnumerator : IEnumerator<GACSelector.GACAssemblyName>, IDisposable, IEnumerator
		{
			public static GACSelector.GACEnumerator Instance;

			private IAssemblyEnum enumerator;

			private GACSelector.GACAssemblyName current;

			public GACSelector.GACAssemblyName Current
			{
				get
				{
					return this.current;
				}
			}

			object System.Collections.IEnumerator.Current
			{
				get
				{
					return this.current;
				}
			}

			static GACEnumerator()
			{
				GACSelector.GACEnumerator.Instance = new GACSelector.GACEnumerator();
			}

			private GACEnumerator()
			{
				this.enumerator = AssemblyCache.CreateGACEnum();
			}

			public void Dispose()
			{
				this.enumerator.Reset();
			}

			public bool MoveNext()
			{
				IAssemblyName n;
				bool ret = this.enumerator.GetNextAssembly(IntPtr.Zero, out n, 0) == 0;
				if (ret)
				{
					this.current = new GACSelector.GACAssemblyName(n);
				}
				return ret;
			}

			public void Reset()
			{
				this.enumerator.Reset();
			}
		}
	}
}