using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using Reflector.CodeModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Reflector.UI
{
	internal class Search : ReflecWindow
	{
		public readonly static DependencyProperty IsSearchingProperty;

		private Thread thread;

		private string filter;

		private Search.SearchType searchType;

		private Search.FilterType filterType;

		private Regex rex;

		private ILanguage lang;

		private byte[] pattern;

		private ObservableCollection<Search.SearchResult> results = new ObservableCollection<Search.SearchResult>();

		private RadioButton type;

		private RadioButton member;

		private RadioButton constant;

		private RadioButton code;

		private RadioButton bytes;

		private RadioButton exact;

		private RadioButton regex;

		private ListView list;

		private TextBox text;

		public bool IsSearching
		{
			get
			{
				return (bool)base.GetValue(Search.IsSearchingProperty);
			}
			set
			{
				base.SetValue(Search.IsSearchingProperty, value);
			}
		}

		static Search()
		{
			Search.IsSearchingProperty = DependencyProperty.Register("IsSearching", typeof(bool), typeof(Search), new UIPropertyMetadata(false));
			FrameworkElement.DefaultStyleKeyProperty.OverrideMetadata(typeof(Search), new FrameworkPropertyMetadata(typeof(Search)));
			ReflecWindow.TitlePropertyKey.OverrideMetadata(typeof(Search), new PropertyMetadata("Search"));
			ReflecWindow.IconPropertyKey.OverrideMetadata(typeof(Search), new PropertyMetadata(Application.Current.Resources["search"]));
			ReflecWindow.IdPropertyKey.OverrideMetadata(typeof(Search), new PropertyMetadata("Search"));
		}

		public Search()
		{
			base.SetValue(ReflecWindow.ParentServicePropertyKey, SearchService.Instance);
		}

		private void Add(Search.SearchResult result)
		{
			if (base.CheckAccess())
			{
				this.results.Add(result);
				return;
			}
			System.Windows.Threading.Dispatcher dispatcher = base.Dispatcher;
			Action<Search.SearchResult> action = new Action<Search.SearchResult>(this.Add);
			object[] objArray = new object[] { result };
			dispatcher.Invoke(action, DispatcherPriority.Background, objArray);
		}

		private bool Check(string txt, string filter)
		{
			if (this.filterType != Search.FilterType.Exact)
			{
				if (this.filterType == Search.FilterType.Regex)
				{
					if (this.rex.IsMatch(txt))
					{
						return true;
					}
					return false;
				}
				if (txt.IndexOf(filter, StringComparison.InvariantCultureIgnoreCase) != -1)
				{
					return true;
				}
				return false;
			}
			string[] strArrays = txt.Split(new char[] { ' ', '.', '+' });
			for (int i = 0; i < (int)strArrays.Length; i++)
			{
				if (string.Compare(strArrays[i], filter, StringComparison.InvariantCultureIgnoreCase) == 0)
				{
					return true;
				}
			}
			return false;
		}

		private bool Contains(byte[] buffer, byte[] pattern)
		{
			int len = (int)buffer.Length - (int)pattern.Length;
			for (int i = 0; i < len; i++)
			{
				if (buffer[i] == pattern[0])
				{
					bool ok = true;
					int j = 0;
					while (j < (int)pattern.Length)
					{
						if (buffer[i + j] == pattern[j])
						{
							j++;
						}
						else
						{
							ok = false;
							break;
						}
					}
					if (ok)
					{
						return true;
					}
				}
			}
			return false;
		}

		private string Disassemble(MethodDefinition method)
		{
			string str;
			try
			{
				IReflecService service = base._App.GetService("Disasm");
				object[] objArray = new object[] { method, this.lang };
				str = (string)service.Exec("Disasm.DisasmText", objArray);
			}
			catch
			{
				str = "";
			}
			return str;
		}

		private void DoSearch(object sender, RoutedEventArgs e)
		{
			long l;
			if (this.IsSearching)
			{
				this.IsSearching = false;
				this.thread.Abort();
				base.Dispatcher.Invoke(new Action(() => {
					base.Cursor = Cursors.Arrow;
					this.IsSearching = false;
				}), new object[0]);
				return;
			}
			this.filter = this.text.Text;
			if (this.type.IsChecked.GetValueOrDefault())
			{
				this.searchType = Search.SearchType.Type;
			}
			else if (this.member.IsChecked.GetValueOrDefault())
			{
				this.searchType = Search.SearchType.Members;
			}
			else if (this.constant.IsChecked.GetValueOrDefault())
			{
				this.searchType = Search.SearchType.Constants;
				if (this.filter.StartsWith("0x") && long.TryParse(this.filter.Substring(2), out l))
				{
					this.filter = l.ToString();
				}
			}
			else if (this.code.IsChecked.GetValueOrDefault())
			{
				this.searchType = Search.SearchType.Code;
				this.lang = base._App.ActiveLanguage;
			}
			else if (this.bytes.IsChecked.GetValueOrDefault())
			{
				this.searchType = Search.SearchType.Bytes;
				bool valid = false;
				if (this.filter.Length % 2 == 0)
				{
					this.pattern = new byte[this.filter.Length / 2];
					for (int i = 0; i < this.filter.Length && byte.TryParse(this.filter.Substring(i, 2), NumberStyles.AllowHexSpecifier, (IFormatProvider)null, out this.pattern[i / 2]); i += 2)
					{
						if (i == this.filter.Length - 2)
						{
							valid = true;
						}
					}
				}
				if (!valid)
				{
					MessageBox.Show("Not valid byte pattern!");
					return;
				}
			}
			if (this.exact.IsChecked.GetValueOrDefault())
			{
				this.filterType = Search.FilterType.Exact;
				if (this.searchType == Search.SearchType.Bytes)
				{
					MessageBox.Show("Cannot use exact match on bytes search!");
					return;
				}
			}
			else if (this.regex.IsChecked.GetValueOrDefault())
			{
				this.filterType = Search.FilterType.Regex;
				try
				{
					this.rex = new Regex(this.filter, RegexOptions.Compiled);
				}
				catch
				{
					MessageBox.Show("Invalid regex!");
					return;
				}
				if (this.searchType != Search.SearchType.Bytes)
				{
					this.IsSearching = true;
					this.results.Clear();
					this.thread = new Thread(new ThreadStart(this.SearchCore))
					{
						IsBackground = true
					};
					this.thread.Start();
					return;
				}
				MessageBox.Show("Cannot use regex on bytes search!");
				return;
			}
			this.IsSearching = true;
			this.results.Clear();
			this.thread = new Thread(new ThreadStart(this.SearchCore))
			{
				IsBackground = true
			};
			this.thread.Start();
		}

		private static T FindParent<T>(DependencyObject child)
		where T : class
		{
			if (child == null)
			{
				return default(T);
			}
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
			return Search.FindParent<T>(parentObject);
		}

		private string GetAssembly(object obj)
		{
			if (obj is TypeDefinition)
			{
				return ((TypeDefinition)obj).Module.Assembly.Name.FullName;
			}
			if (obj is IMemberDefinition)
			{
				return ((IMemberDefinition)obj).DeclaringType.Module.Assembly.Name.FullName;
			}
			if (obj is INamespace)
			{
				return ((INamespace)obj).Types[0].Module.Assembly.Name.FullName;
			}
			if (!(obj is ModuleDefinition))
			{
				return null;
			}
			return ((ModuleDefinition)obj).Assembly.Name.FullName;
		}

		private byte[] GetBodyBytes(MethodBody body)
		{
			byte[] array;
			using (MemoryStream str = new MemoryStream())
			{
				BinaryWriter wtr = new BinaryWriter(str);
				foreach (Instruction inst in body.Instructions)
				{
					OpCode op = inst.OpCode;
					if (op.Size == 1)
					{
						wtr.Write((byte)op.Value);
					}
					else if (op.Size == 2)
					{
						wtr.Write((ushort)op.Value);
					}
					switch (op.OperandType)
					{
						case OperandType.InlineBrTarget:
						{
							wtr.Write(((Instruction)inst.Operand).Offset - inst.Offset);
							continue;
						}
						case OperandType.InlineField:
						case OperandType.InlineMethod:
						case OperandType.InlineSig:
						case OperandType.InlineTok:
						case OperandType.InlineType:
						{
							wtr.Write(((MemberReference)inst.Operand).MetadataToken.ToUInt32());
							continue;
						}
						case OperandType.InlineI:
						{
							wtr.Write((int)inst.Operand);
							continue;
						}
						case OperandType.InlineI8:
						{
							wtr.Write((long)inst.Operand);
							continue;
						}
						case OperandType.InlineR:
						{
							wtr.Write((double)inst.Operand);
							continue;
						}
						case OperandType.InlineString:
						{
							wtr.Write(1895825407);
							continue;
						}
						case OperandType.InlineSwitch:
						{
							Instruction[] insts = (Instruction[])inst.Operand;
							wtr.Write((int)insts.Length);
							for (int i = 0; i < (int)insts.Length; i++)
							{
								wtr.Write(insts[i].Offset - inst.Offset);
							}
							continue;
						}
						case OperandType.InlineVar:
						{
							wtr.Write((ushort)((VariableReference)inst.Operand).Index);
							continue;
						}
						case OperandType.InlineArg:
						{
							wtr.Write((int)((ushort)((ParameterReference)inst.Operand).Index + (body.Method.IsStatic ? 0 : 1)));
							continue;
						}
						case OperandType.ShortInlineBrTarget:
						{
							wtr.Write((sbyte)(((Instruction)inst.Operand).Offset - inst.Offset));
							continue;
						}
						case OperandType.ShortInlineI:
						{
							if (op != OpCodes.Ldc_I4_S)
							{
								wtr.Write((byte)inst.Operand);
								continue;
							}
							else
							{
								wtr.Write((sbyte)inst.Operand);
								continue;
							}
						}
						case OperandType.ShortInlineR:
						{
							wtr.Write((float)inst.Operand);
							continue;
						}
						case OperandType.ShortInlineVar:
						{
							wtr.Write((byte)((VariableReference)inst.Operand).Index);
							continue;
						}
						case OperandType.ShortInlineArg:
						{
							wtr.Write((int)((byte)((ParameterReference)inst.Operand).Index + (body.Method.IsStatic ? 0 : 1)));
							continue;
						}
						default:
						{
							continue;
						}
					}
				}
				array = str.ToArray();
			}
			return array;
		}

		private string GetOwner(TypeDefinition obj)
		{
			if (obj.DeclaringType != null)
			{
				return AsmViewHelper.Escape(AsmViewHelper.GetFullText(obj.DeclaringType));
			}
			if (string.IsNullOrEmpty(obj.Namespace))
			{
				return "-";
			}
			return AsmViewHelper.Escape(obj.Namespace);
		}

		private object GetParent(object obj)
		{
			if (obj is IMemberDefinition)
			{
				return ((IMemberDefinition)obj).DeclaringType;
			}
			if (!(obj is TypeDefinition))
			{
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

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			this.type = (RadioButton)base.GetTemplateChild("PART_type");
			this.member = (RadioButton)base.GetTemplateChild("PART_member");
			this.constant = (RadioButton)base.GetTemplateChild("PART_const");
			this.code = (RadioButton)base.GetTemplateChild("PART_code");
			this.bytes = (RadioButton)base.GetTemplateChild("PART_bytes");
			this.exact = (RadioButton)base.GetTemplateChild("PART_exact");
			this.regex = (RadioButton)base.GetTemplateChild("PART_regex");
			this.list = (ListView)base.GetTemplateChild("PART_list");
			GridView view = (GridView)this.list.View;
			GridViewColumn gridViewColumn = view.Columns[0];
			GridViewColumnHeader gridViewColumnHeader = new GridViewColumnHeader()
			{
				Content = "Item",
				Command = new DelegateCommand("Search.Sort.Item", (object obj) => true, (object obj) => {
					ListSortDirection dir = ((ListView)obj).Items.SortDescriptions[0].Direction;
					dir = (((ListView)obj).Items.SortDescriptions[0].PropertyName == "Txt" ? (dir == ListSortDirection.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending) : ListSortDirection.Ascending);
					((ListView)obj).Items.SortDescriptions.Clear();
					((ListView)obj).Items.SortDescriptions.Add(new SortDescription("Txt", dir));
				}),
				CommandParameter = this.list
			};
			gridViewColumn.Header = gridViewColumnHeader;
			GridViewColumn gridViewColumn1 = view.Columns[1];
			GridViewColumnHeader gridViewColumnHeader1 = new GridViewColumnHeader()
			{
				Content = "Owner",
				Command = new DelegateCommand("Search.Sort.Owner", (object obj) => true, (object obj) => {
					ListSortDirection dir = ((ListView)obj).Items.SortDescriptions[0].Direction;
					dir = (((ListView)obj).Items.SortDescriptions[0].PropertyName == "Own" ? (dir == ListSortDirection.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending) : ListSortDirection.Ascending);
					((ListView)obj).Items.SortDescriptions.Clear();
					((ListView)obj).Items.SortDescriptions.Add(new SortDescription("Own", dir));
				}),
				CommandParameter = this.list
			};
			gridViewColumn1.Header = gridViewColumnHeader1;
			GridViewColumn gridViewColumn2 = view.Columns[2];
			GridViewColumnHeader gridViewColumnHeader2 = new GridViewColumnHeader()
			{
				Content = "Assembly",
				Command = new DelegateCommand("Search.Sort.Assembly", (object obj) => true, (object obj) => {
					ListSortDirection dir = ((ListView)obj).Items.SortDescriptions[0].Direction;
					dir = (((ListView)obj).Items.SortDescriptions[0].PropertyName == "Asm" ? (dir == ListSortDirection.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending) : ListSortDirection.Ascending);
					((ListView)obj).Items.SortDescriptions.Clear();
					((ListView)obj).Items.SortDescriptions.Add(new SortDescription("Asm", dir));
				}),
				CommandParameter = this.list
			};
			gridViewColumn2.Header = gridViewColumnHeader2;
			this.list.Items.SortDescriptions.Add(new SortDescription("Txt", ListSortDirection.Ascending));
			this.list.ItemsSource = this.results;
			this.list.MouseDoubleClick += new MouseButtonEventHandler((object sender, MouseButtonEventArgs e) => {
				ListViewItem item = Search.FindParent<ListViewItem>((DependencyObject)this.list.InputHitTest(e.GetPosition(this.list)));
				if (item != null)
				{
					Search.SearchResult result = (Search.SearchResult)item.DataContext;
					base._App.GetService("AsmMgr").SetProp("AsmMgr.Selected", result.Obj);
				}
			});
			this.text = (TextBox)base.GetTemplateChild("PART_text");
			((Button)base.GetTemplateChild("PART_search")).Click += new RoutedEventHandler(this.DoSearch);
		}

		private void SearchCore()
		{
			base.Dispatcher.Invoke(new Action(() => base.Cursor = Cursors.AppStarting), new object[0]);
			AssemblyDefinition[] array = ((IEnumerable<AssemblyDefinition>)base._App.GetService("AsmMgr").GetProp("AsmMgr.Assemblies")).ToArray<AssemblyDefinition>();
			for (int i = 0; i < (int)array.Length; i++)
			{
				AssemblyDefinition asm = array[i];
				if (this.results.Count > 1024)
				{
					break;
				}
				foreach (ModuleDefinition mod in asm.Modules)
				{
					if (this.results.Count > 1024)
					{
						break;
					}
					foreach (TypeDefinition type in mod.Types)
					{
						if (this.results.Count > 1024)
						{
							break;
						}
						this.SearchCore(type);
					}
				}
			}
			base.Dispatcher.BeginInvoke(new Action(() => {
				base.Cursor = Cursors.Arrow;
				this.IsSearching = false;
			}), DispatcherPriority.Background, new object[0]);
		}

		private void SearchCore(TypeDefinition type)
		{
			if (this.searchType == Search.SearchType.Type && this.Check(AsmViewHelper.GetFullText(type), this.filter))
			{
				Search.SearchResult searchResult = new Search.SearchResult()
				{
					Img = AsmViewHelper.GetIcon(type),
					Txt = AsmViewHelper.Escape(AsmViewHelper.GetText(type)),
					Own = this.GetOwner(type),
					Fg = AsmViewHelper.GetVisibility(type),
					Obj = type,
					Asm = this.GetAssembly(type)
				};
				this.Add(searchResult);
				if (this.results.Count > 1024)
				{
					return;
				}
			}
			foreach (TypeDefinition i in type.NestedTypes)
			{
				this.SearchCore(i);
			}
			if (this.searchType == Search.SearchType.Members)
			{
				foreach (PropertyDefinition i in type.Properties)
				{
					this.SearchCore(i);
				}
				foreach (EventDefinition i in type.Events)
				{
					this.SearchCore(i);
				}
			}
			if (this.searchType == Search.SearchType.Members || this.searchType == Search.SearchType.Constants || this.searchType == Search.SearchType.Code || this.searchType == Search.SearchType.Bytes)
			{
				foreach (MethodDefinition i in type.Methods)
				{
					this.SearchCore(i);
				}
				if (this.searchType != Search.SearchType.Code && this.searchType != Search.SearchType.Bytes)
				{
					foreach (FieldDefinition i in type.Fields)
					{
						this.SearchCore(i);
					}
				}
			}
		}

		private void SearchCore(IMemberDefinition mem)
		{
			if (this.searchType == Search.SearchType.Code && mem is MethodDefinition && this.Check(this.Disassemble((MethodDefinition)mem), this.filter))
			{
				Search.SearchResult searchResult = new Search.SearchResult()
				{
					Img = AsmViewHelper.GetIcon(mem),
					Txt = AsmViewHelper.Escape(AsmViewHelper.GetText(mem)),
					Own = AsmViewHelper.Escape(AsmViewHelper.GetFullText(mem.DeclaringType)),
					Fg = AsmViewHelper.GetVisibility(mem),
					Obj = mem,
					Asm = this.GetAssembly(mem)
				};
				this.Add(searchResult);
				if (this.results.Count > 1024)
				{
					return;
				}
			}
			if (this.searchType == Search.SearchType.Members && this.Check(AsmViewHelper.GetFullText(mem), this.filter))
			{
				Search.SearchResult searchResult1 = new Search.SearchResult()
				{
					Img = AsmViewHelper.GetIcon(mem),
					Txt = AsmViewHelper.Escape(AsmViewHelper.GetText(mem)),
					Own = AsmViewHelper.Escape(AsmViewHelper.GetFullText(mem.DeclaringType)),
					Fg = AsmViewHelper.GetVisibility(mem),
					Obj = mem,
					Asm = this.GetAssembly(mem)
				};
				this.Add(searchResult1);
				if (this.results.Count > 1024)
				{
					return;
				}
			}
			else if (mem is FieldDefinition && this.searchType == Search.SearchType.Constants && ((FieldDefinition)mem).Constant != null && this.Check(((FieldDefinition)mem).Constant.ToString(), this.filter))
			{
				Search.SearchResult searchResult2 = new Search.SearchResult()
				{
					Img = AsmViewHelper.GetIcon(mem),
					Txt = AsmViewHelper.Escape(AsmViewHelper.GetText(mem)),
					Own = AsmViewHelper.Escape(AsmViewHelper.GetFullText(mem.DeclaringType)),
					Fg = AsmViewHelper.GetVisibility(mem),
					Obj = mem,
					Asm = this.GetAssembly(mem)
				};
				this.Add(searchResult2);
				if (this.results.Count > 1024)
				{
					return;
				}
			}
			else if (mem is MethodDefinition)
			{
				MethodDefinition methodDecl = (MethodDefinition)mem;
				bool has = false;
				if (this.searchType == Search.SearchType.Members)
				{
					foreach (ParameterDefinition param in methodDecl.Parameters)
					{
						if (!this.Check(param.Name, this.filter))
						{
							continue;
						}
						has = true;
						break;
					}
				}
				else if (methodDecl.HasBody)
				{
					MethodBody body = methodDecl.Body;
					if (this.searchType == Search.SearchType.Members)
					{
						foreach (VariableDefinition var in body.Variables)
						{
							if (!this.Check(var.Name, this.filter))
							{
								continue;
							}
							has = true;
							break;
						}
					}
					else if (this.searchType == Search.SearchType.Constants)
					{
						foreach (Instruction inst in body.Instructions)
						{
							short value = inst.OpCode.Value;
							switch (value)
							{
								case 31:
								case 32:
								case 33:
								case 34:
								case 35:
								{
									if (!this.Check(inst.Operand.ToString(), this.filter))
									{
										continue;
									}
									has = true;
									continue;
								}
								default:
								{
									if (value != 114)
									{
										continue;
									}
									else
									{
										goto case 35;
									}
								}
							}
						}
					}
					else if (this.searchType == Search.SearchType.Bytes && this.Contains(this.GetBodyBytes(body), this.pattern))
					{
						has = true;
					}
				}
				if (has)
				{
					Search.SearchResult searchResult3 = new Search.SearchResult()
					{
						Img = AsmViewHelper.GetIcon(mem),
						Txt = AsmViewHelper.Escape(AsmViewHelper.GetText(mem)),
						Own = AsmViewHelper.Escape(AsmViewHelper.GetFullText(mem.DeclaringType)),
						Fg = AsmViewHelper.GetVisibility(mem),
						Obj = mem,
						Asm = this.GetAssembly(mem)
					};
					this.Add(searchResult3);
					int count = this.results.Count;
				}
			}
		}

		private enum FilterType
		{
			None,
			Exact,
			Regex
		}

		private class SearchResult
		{
			public string Asm
			{
				get;
				set;
			}

			public bool Fg
			{
				get;
				set;
			}

			public BitmapSource Img
			{
				get;
				set;
			}

			public object Obj
			{
				get;
				set;
			}

			public string Own
			{
				get;
				set;
			}

			public string Txt
			{
				get;
				set;
			}

			public SearchResult()
			{
			}
		}

		private enum SearchType
		{
			Type,
			Members,
			Constants,
			Code,
			Bytes
		}
	}
}