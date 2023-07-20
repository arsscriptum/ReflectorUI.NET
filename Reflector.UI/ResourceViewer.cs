using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Editing;
using Mono.Cecil;
using Reflector;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using System.Xml;

namespace Reflector.UI
{
	public class ResourceViewer : ReflecWindow
	{
		public readonly static DependencyProperty ContentProperty;

		public object Content
		{
			get
			{
				return base.GetValue(ResourceViewer.ContentProperty);
			}
			set
			{
				base.SetValue(ResourceViewer.ContentProperty, value);
			}
		}

		static ResourceViewer()
		{
			ResourceViewer.ContentProperty = DependencyProperty.Register("Content", typeof(object), typeof(ResourceViewer), new UIPropertyMetadata(null));
			FrameworkElement.DefaultStyleKeyProperty.OverrideMetadata(typeof(ResourceViewer), new FrameworkPropertyMetadata(typeof(ResourceViewer)));
		}

		public ResourceViewer()
		{
			base.SetValue(ReflecWindow.ParentServicePropertyKey, DisassemblyService.Instance);
		}

		private ResourceViewer.ResxItem LoadItemV1(BinaryReader rdr, string[] types)
		{
			string type = types[this.Read7BitEncodedInt(rdr)];
			ResourceViewer.ResxItem ret = new ResourceViewer.ResxItem()
			{
				Type = type
			};
			if (type.StartsWith("System.String, mscorlib, Version="))
			{
				ret.Data = rdr.ReadString();
			}
			else if (type.StartsWith("System.Byte, mscorlib, Version="))
			{
				ret.Data = rdr.ReadByte();
			}
			else if (type.StartsWith("System.SByte, mscorlib, Version="))
			{
				ret.Data = rdr.ReadSByte();
			}
			else if (type.StartsWith("System.Int16, mscorlib, Version="))
			{
				ret.Data = rdr.ReadInt16();
			}
			else if (type.StartsWith("System.UInt16, mscorlib, Version="))
			{
				ret.Data = rdr.ReadUInt16();
			}
			else if (type.StartsWith("System.Int32, mscorlib, Version="))
			{
				ret.Data = rdr.ReadInt32();
			}
			else if (type.StartsWith("System.UInt32, mscorlib, Version="))
			{
				ret.Data = rdr.ReadUInt32();
			}
			else if (type.StartsWith("System.Int64, mscorlib, Version="))
			{
				ret.Data = rdr.ReadInt64();
			}
			else if (type.StartsWith("System.UInt64, mscorlib, Version="))
			{
				ret.Data = rdr.ReadUInt64();
			}
			else if (type.StartsWith("System.Single, mscorlib, Version="))
			{
				ret.Data = rdr.ReadSingle();
			}
			else if (type.StartsWith("System.Double, mscorlib, Version="))
			{
				ret.Data = rdr.ReadDouble();
			}
			else if (type.StartsWith("System.DateTime, mscorlib, Version="))
			{
				ret.Data = new DateTime(rdr.ReadInt64());
			}
			else if (type.StartsWith("System.TimeSpan, mscorlib, Version="))
			{
				ret.Data = new TimeSpan(rdr.ReadInt64());
			}
			else if (!type.StartsWith("System.Decimal, mscorlib, Version="))
			{
				ret.Data = this.TryDeserialize(rdr);
			}
			else
			{
				int[] numArray = new int[] { rdr.ReadInt32(), rdr.ReadInt32(), rdr.ReadInt32(), rdr.ReadInt32() };
				ret.Data = new decimal(numArray);
			}
			return ret;
		}

		private ResourceViewer.ResxItem LoadItemV2(BinaryReader rdr, string[] types)
		{
			ResourceViewer.ResourceTypeCode code = (ResourceViewer.ResourceTypeCode)this.Read7BitEncodedInt(rdr);
			ResourceViewer.ResxItem ret = new ResourceViewer.ResxItem();
			switch (code)
			{
				case ResourceViewer.ResourceTypeCode.Null:
				{
					ret.Data = null;
					ret.Type = typeof(object).AssemblyQualifiedName;
					break;
				}
				case ResourceViewer.ResourceTypeCode.String:
				{
					ret.Data = rdr.ReadString();
					ret.Type = typeof(string).AssemblyQualifiedName;
					break;
				}
				case ResourceViewer.ResourceTypeCode.Boolean:
				{
					ret.Data = rdr.ReadBoolean();
					ret.Type = typeof(bool).AssemblyQualifiedName;
					break;
				}
				case ResourceViewer.ResourceTypeCode.Char:
				{
					ret.Data = (char)rdr.ReadUInt16();
					ret.Type = typeof(char).AssemblyQualifiedName;
					break;
				}
				case ResourceViewer.ResourceTypeCode.Byte:
				{
					ret.Data = rdr.ReadByte();
					ret.Type = typeof(byte).AssemblyQualifiedName;
					break;
				}
				case ResourceViewer.ResourceTypeCode.SByte:
				{
					ret.Data = rdr.ReadSByte();
					ret.Type = typeof(sbyte).AssemblyQualifiedName;
					break;
				}
				case ResourceViewer.ResourceTypeCode.Int16:
				{
					ret.Data = rdr.ReadInt16();
					ret.Type = typeof(short).AssemblyQualifiedName;
					break;
				}
				case ResourceViewer.ResourceTypeCode.UInt16:
				{
					ret.Data = rdr.ReadUInt16();
					ret.Type = typeof(ushort).AssemblyQualifiedName;
					break;
				}
				case ResourceViewer.ResourceTypeCode.Int32:
				{
					ret.Data = rdr.ReadInt32();
					ret.Type = typeof(int).AssemblyQualifiedName;
					break;
				}
				case ResourceViewer.ResourceTypeCode.UInt32:
				{
					ret.Data = rdr.ReadUInt32();
					ret.Type = typeof(uint).AssemblyQualifiedName;
					break;
				}
				case ResourceViewer.ResourceTypeCode.Int64:
				{
					ret.Data = rdr.ReadInt64();
					ret.Type = typeof(long).AssemblyQualifiedName;
					break;
				}
				case ResourceViewer.ResourceTypeCode.UInt64:
				{
					ret.Data = rdr.ReadUInt64();
					ret.Type = typeof(ulong).AssemblyQualifiedName;
					break;
				}
				case ResourceViewer.ResourceTypeCode.Single:
				{
					ret.Data = rdr.ReadSingle();
					ret.Type = typeof(float).AssemblyQualifiedName;
					break;
				}
				case ResourceViewer.ResourceTypeCode.Double:
				{
					ret.Data = rdr.ReadDouble();
					ret.Type = typeof(double).AssemblyQualifiedName;
					break;
				}
				case ResourceViewer.ResourceTypeCode.Decimal:
				{
					ret.Data = rdr.ReadDecimal();
					ret.Type = typeof(decimal).AssemblyQualifiedName;
					break;
				}
				case ResourceViewer.ResourceTypeCode.DateTime:
				{
					ret.Data = DateTime.FromBinary(rdr.ReadInt64());
					ret.Type = typeof(DateTime).AssemblyQualifiedName;
					break;
				}
				case ResourceViewer.ResourceTypeCode.LastPrimitive:
				{
					ret.Data = new TimeSpan(rdr.ReadInt64());
					ret.Type = typeof(TimeSpan).AssemblyQualifiedName;
					break;
				}
				case ResourceViewer.ResourceTypeCode.LastPrimitive | ResourceViewer.ResourceTypeCode.String | ResourceViewer.ResourceTypeCode.TimeSpan:
				case ResourceViewer.ResourceTypeCode.Boolean | ResourceViewer.ResourceTypeCode.LastPrimitive | ResourceViewer.ResourceTypeCode.TimeSpan:
				case ResourceViewer.ResourceTypeCode.Boolean | ResourceViewer.ResourceTypeCode.Char | ResourceViewer.ResourceTypeCode.LastPrimitive | ResourceViewer.ResourceTypeCode.String | ResourceViewer.ResourceTypeCode.TimeSpan:
				case ResourceViewer.ResourceTypeCode.Byte | ResourceViewer.ResourceTypeCode.LastPrimitive | ResourceViewer.ResourceTypeCode.TimeSpan:
				case ResourceViewer.ResourceTypeCode.Byte | ResourceViewer.ResourceTypeCode.LastPrimitive | ResourceViewer.ResourceTypeCode.SByte | ResourceViewer.ResourceTypeCode.String | ResourceViewer.ResourceTypeCode.TimeSpan:
				case ResourceViewer.ResourceTypeCode.Boolean | ResourceViewer.ResourceTypeCode.Byte | ResourceViewer.ResourceTypeCode.Int16 | ResourceViewer.ResourceTypeCode.LastPrimitive | ResourceViewer.ResourceTypeCode.TimeSpan:
				case ResourceViewer.ResourceTypeCode.Boolean | ResourceViewer.ResourceTypeCode.Byte | ResourceViewer.ResourceTypeCode.Char | ResourceViewer.ResourceTypeCode.Int16 | ResourceViewer.ResourceTypeCode.LastPrimitive | ResourceViewer.ResourceTypeCode.SByte | ResourceViewer.ResourceTypeCode.String | ResourceViewer.ResourceTypeCode.TimeSpan | ResourceViewer.ResourceTypeCode.UInt16:
				case ResourceViewer.ResourceTypeCode.Int32 | ResourceViewer.ResourceTypeCode.LastPrimitive | ResourceViewer.ResourceTypeCode.TimeSpan:
				case ResourceViewer.ResourceTypeCode.Int32 | ResourceViewer.ResourceTypeCode.LastPrimitive | ResourceViewer.ResourceTypeCode.String | ResourceViewer.ResourceTypeCode.TimeSpan | ResourceViewer.ResourceTypeCode.UInt32:
				case ResourceViewer.ResourceTypeCode.Boolean | ResourceViewer.ResourceTypeCode.Int32 | ResourceViewer.ResourceTypeCode.Int64 | ResourceViewer.ResourceTypeCode.LastPrimitive | ResourceViewer.ResourceTypeCode.TimeSpan:
				case ResourceViewer.ResourceTypeCode.Boolean | ResourceViewer.ResourceTypeCode.Char | ResourceViewer.ResourceTypeCode.Int32 | ResourceViewer.ResourceTypeCode.Int64 | ResourceViewer.ResourceTypeCode.LastPrimitive | ResourceViewer.ResourceTypeCode.String | ResourceViewer.ResourceTypeCode.TimeSpan | ResourceViewer.ResourceTypeCode.UInt32 | ResourceViewer.ResourceTypeCode.UInt64:
				case ResourceViewer.ResourceTypeCode.Byte | ResourceViewer.ResourceTypeCode.Int32 | ResourceViewer.ResourceTypeCode.LastPrimitive | ResourceViewer.ResourceTypeCode.Single | ResourceViewer.ResourceTypeCode.TimeSpan:
				case ResourceViewer.ResourceTypeCode.Byte | ResourceViewer.ResourceTypeCode.Double | ResourceViewer.ResourceTypeCode.Int32 | ResourceViewer.ResourceTypeCode.LastPrimitive | ResourceViewer.ResourceTypeCode.SByte | ResourceViewer.ResourceTypeCode.Single | ResourceViewer.ResourceTypeCode.String | ResourceViewer.ResourceTypeCode.TimeSpan | ResourceViewer.ResourceTypeCode.UInt32:
				case ResourceViewer.ResourceTypeCode.Boolean | ResourceViewer.ResourceTypeCode.Byte | ResourceViewer.ResourceTypeCode.Decimal | ResourceViewer.ResourceTypeCode.Int16 | ResourceViewer.ResourceTypeCode.Int32 | ResourceViewer.ResourceTypeCode.Int64 | ResourceViewer.ResourceTypeCode.LastPrimitive | ResourceViewer.ResourceTypeCode.Single | ResourceViewer.ResourceTypeCode.TimeSpan:
				case ResourceViewer.ResourceTypeCode.Boolean | ResourceViewer.ResourceTypeCode.Byte | ResourceViewer.ResourceTypeCode.Char | ResourceViewer.ResourceTypeCode.DateTime | ResourceViewer.ResourceTypeCode.Decimal | ResourceViewer.ResourceTypeCode.Double | ResourceViewer.ResourceTypeCode.Int16 | ResourceViewer.ResourceTypeCode.Int32 | ResourceViewer.ResourceTypeCode.Int64 | ResourceViewer.ResourceTypeCode.LastPrimitive | ResourceViewer.ResourceTypeCode.SByte | ResourceViewer.ResourceTypeCode.Single | ResourceViewer.ResourceTypeCode.String | ResourceViewer.ResourceTypeCode.TimeSpan | ResourceViewer.ResourceTypeCode.UInt16 | ResourceViewer.ResourceTypeCode.UInt32 | ResourceViewer.ResourceTypeCode.UInt64:
				{
					ret.Data = this.TryDeserialize(rdr);
					ret.Type = types[(int)code - (int)ResourceViewer.ResourceTypeCode.StartOfUserTypes];
					break;
				}
				case ResourceViewer.ResourceTypeCode.ByteArray:
				{
					ret.Data = rdr.ReadBytes(rdr.ReadInt32());
					ret.Type = typeof(byte[]).AssemblyQualifiedName;
					break;
				}
				case ResourceViewer.ResourceTypeCode.Stream:
				{
					ret.Data = rdr.ReadBytes(rdr.ReadInt32());
					ret.Type = typeof(Stream).AssemblyQualifiedName;
					break;
				}
				default:
				{
					goto case ResourceViewer.ResourceTypeCode.Boolean | ResourceViewer.ResourceTypeCode.Byte | ResourceViewer.ResourceTypeCode.Char | ResourceViewer.ResourceTypeCode.DateTime | ResourceViewer.ResourceTypeCode.Decimal | ResourceViewer.ResourceTypeCode.Double | ResourceViewer.ResourceTypeCode.Int16 | ResourceViewer.ResourceTypeCode.Int32 | ResourceViewer.ResourceTypeCode.Int64 | ResourceViewer.ResourceTypeCode.LastPrimitive | ResourceViewer.ResourceTypeCode.SByte | ResourceViewer.ResourceTypeCode.Single | ResourceViewer.ResourceTypeCode.String | ResourceViewer.ResourceTypeCode.TimeSpan | ResourceViewer.ResourceTypeCode.UInt16 | ResourceViewer.ResourceTypeCode.UInt32 | ResourceViewer.ResourceTypeCode.UInt64;
				}
			}
			return ret;
		}

		protected override void OnDragEnter(DragEventArgs e)
		{
			if (e.Data.GetDataPresent("Reflector object") && e.Data.GetData("Reflector object") is Resource)
			{
				e.Effects = DragDropEffects.Move;
				e.Handled = true;
			}
		}

		protected override void OnDrop(DragEventArgs e)
		{
			if (e.Data.GetDataPresent("Reflector object") && e.Data.GetData("Reflector object") is Resource)
			{
				this.View((Resource)e.Data.GetData("Reflector object"));
				e.Handled = true;
			}
		}

		private int Read7BitEncodedInt(BinaryReader rdr)
		{
			byte num3;
			int num = 0;
			int num2 = 0;
			do
			{
				num3 = rdr.ReadByte();
				num = num | (num3 & 127) << (num2 & 31);
				num2 += 7;
			}
			while ((num3 & 128) != 0);
			return num;
		}

		private void ShowBytes(byte[] bytes)
		{
			TextEditor editor = (TextEditor)XamlReader.Load(XmlReader.Create(new StringReader("\r\n<ae:TextEditor xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\r\n               xmlns:ae=\"http://icsharpcode.net/sharpdevelop/avalonedit\"\r\n               IsReadOnly=\"True\" Background=\"{DynamicResource ControlBackgroundBrush}\" Margin=\"5\" Foreground=\"{DynamicResource LightTextBrush}\" FontFamily=\"Lucida Console\">\r\n    <ae:TextEditor.Options>\r\n        <ae:TextEditorOptions AllowScrollBelowDocument=\"True\" EnableHyperlinks=\"False\" EnableEmailHyperlinks=\"False\"/>\r\n    </ae:TextEditor.Options>\r\n</ae:TextEditor>")));
			editor.TextArea.SetResourceReference(TextArea.SelectionBorderProperty, "DisSelectBdr");
			editor.TextArea.SetResourceReference(TextArea.SelectionBrushProperty, "DisSelectBg");
			editor.TextArea.SetResourceReference(TextArea.SelectionForegroundProperty, "DisSelectFg");
			editor.TextArea.TextView.ClipToBounds = false;
			editor.TextArea.TextView.Margin = new Thickness(1);
			StringBuilder sb = new StringBuilder();
			int strLen = ((int)bytes.Length).ToString("X").Length;
			sb.Append(new string(' ', strLen + 2 + 3));
			for (int i = 0; i < 16; i++)
			{
				if (i != 0)
				{
					sb.Append("  ");
				}
				sb.AppendFormat("{0:X1}", i);
			}
			sb.AppendLine();
			StringBuilder txt = new StringBuilder();
			for (int i = 0; i < (int)bytes.Length; i++)
			{
				if (i % 16 == 0)
				{
					if (i != 0)
					{
						sb.Append(txt.ToString());
						txt = new StringBuilder();
						sb.AppendLine();
					}
					sb.Append("0x");
					sb.Append(i.ToString(string.Concat("X", strLen.ToString())));
					sb.Append(":  ");
				}
				sb.AppendFormat("{0:X2} ", bytes[i]);
				if (bytes[i] < 32 || bytes[i] >= 127)
				{
					txt.Append('.');
				}
				else
				{
					txt.Append((char)bytes[i]);
				}
			}
			sb.Append(new string(' ', ((int)bytes.Length % 16 == 0 ? 0 : (16 - (int)bytes.Length % 16) * 3)));
			sb.AppendLine(txt.ToString());
			editor.Text = sb.ToString();
			this.Content = editor;
		}

		private void ShowImage(byte[] bytes)
		{
			ListBox box = new ListBox();
			ScrollViewer.SetCanContentScroll(box, false);
			foreach (BitmapFrame i in BitmapDecoder.Create(new MemoryStream(bytes), BitmapCreateOptions.None, BitmapCacheOption.None).Frames)
			{
				Bitmap bitmap = new Bitmap()
				{
					Source = i
				};
				box.Items.Add(bitmap);
			}
			this.Content = box;
		}

		private void ShowResx(byte[] bytes)
		{
			unsafe
			{
				ListView view = new ListView();
				GridView grid = new GridView();
				view.View = grid;
				GridViewColumnCollection columns = grid.Columns;
				GridViewColumn gridViewColumn = new GridViewColumn();
				GridViewColumn gridViewColumn1 = gridViewColumn;
				GridViewColumnHeader gridViewColumnHeader = new GridViewColumnHeader()
				{
					Content = "Name",
					Command = new DelegateCommand("ResxView.Sort.Name", (object obj) => true, (object obj) => {
						ListSortDirection dir = ((ListView)obj).Items.SortDescriptions[0].Direction;
						dir = (((ListView)obj).Items.SortDescriptions[0].PropertyName == "Name" ? (dir == ListSortDirection.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending) : ListSortDirection.Ascending);
						((ListView)obj).Items.SortDescriptions.Clear();
						((ListView)obj).Items.SortDescriptions.Add(new SortDescription("Name", dir));
					}),
					CommandParameter = view
				};
				gridViewColumn1.Header = gridViewColumnHeader;
				gridViewColumn.Width = 200;
				gridViewColumn.DisplayMemberBinding = new Binding("Name");
				columns.Add(gridViewColumn);
				GridViewColumnCollection gridViewColumnCollection = grid.Columns;
				GridViewColumn gridViewColumn2 = new GridViewColumn();
				GridViewColumn gridViewColumn3 = gridViewColumn2;
				GridViewColumnHeader gridViewColumnHeader1 = new GridViewColumnHeader()
				{
					Content = "Value",
					Command = new DelegateCommand("ResxView.Sort.Value", (object obj) => true, (object obj) => {
						ListSortDirection dir = ((ListView)obj).Items.SortDescriptions[0].Direction;
						dir = (((ListView)obj).Items.SortDescriptions[0].PropertyName == "Data" ? (dir == ListSortDirection.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending) : ListSortDirection.Ascending);
						((ListView)obj).Items.SortDescriptions.Clear();
						((ListView)obj).Items.SortDescriptions.Add(new SortDescription("Data", dir));
					}),
					CommandParameter = view
				};
				gridViewColumn3.Header = gridViewColumnHeader1;
				gridViewColumn2.Width = 200;
				Binding binding = new Binding("Data")
				{
					Converter = ResourceViewer.ResxValueConverter.Instance
				};
				gridViewColumn2.DisplayMemberBinding = binding;
				gridViewColumnCollection.Add(gridViewColumn2);
				GridViewColumnCollection columns1 = grid.Columns;
				GridViewColumn binding1 = new GridViewColumn();
				GridViewColumn gridViewColumn4 = binding1;
				GridViewColumnHeader gridViewColumnHeader2 = new GridViewColumnHeader()
				{
					Content = "Type",
					Command = new DelegateCommand("ResxView.Sort.Type", (object obj) => true, (object obj) => {
						ListSortDirection dir = ((ListView)obj).Items.SortDescriptions[0].Direction;
						dir = (((ListView)obj).Items.SortDescriptions[0].PropertyName == "Type" ? (dir == ListSortDirection.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending) : ListSortDirection.Ascending);
						((ListView)obj).Items.SortDescriptions.Clear();
						((ListView)obj).Items.SortDescriptions.Add(new SortDescription("Type", dir));
					}),
					CommandParameter = view
				};
				gridViewColumn4.Header = gridViewColumnHeader2;
				binding1.Width = 200;
				binding1.DisplayMemberBinding = new Binding("Type");
				columns1.Add(binding1);
				view.Items.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
				using (BinaryReader rdr = new BinaryReader(new MemoryStream(bytes)))
				{
					if (rdr.ReadUInt32() != -1091581234)
					{
						throw new Exception();
					}
					if (rdr.ReadUInt32() <= 1)
					{
						rdr.ReadUInt32();
						rdr.ReadString();
						rdr.ReadString();
					}
					else
					{
						rdr.BaseStream.Seek((long)rdr.ReadUInt32(), SeekOrigin.Current);
					}
					uint ver = rdr.ReadUInt32();
					if (ver != 1 && ver != 2)
					{
						throw new Exception();
					}
					uint numOfRes = rdr.ReadUInt32();
					ResourceViewer.ResxItem[] items = new ResourceViewer.ResxItem[numOfRes];
					uint numTypes = rdr.ReadUInt32();
					string[] types = new string[numTypes];
					//for (int i = 0; (long)i < (ulong)numTypes; i++)
                    for (int i = 0; (long)i < (long)numTypes; i++)
                    {
                       types[i] = rdr.ReadString();
					}
					long align = rdr.BaseStream.Position & (long)7;
					if (align != (long)0)
					{
						Stream baseStream = rdr.BaseStream;
						baseStream.Position = baseStream.Position + ((long)8 - align);
					}
					Stream position = rdr.BaseStream;
					//position.Position = (long)(position.Position + (ulong)(4 * numOfRes));
                    position.Position = (long)(position.Position + (long)(4 * numOfRes));
                    uint[] namePos = new uint[numOfRes];
					//for (int i = 0; (long)i < (ulong)numOfRes; i++)
                    for (int i = 0; (long)i < (long)numOfRes; i++)
                    {
                            namePos[i] = rdr.ReadUInt32();
					}
					uint dataSect = rdr.ReadUInt32();
					uint nameSect = (uint)rdr.BaseStream.Position;
					//for (int i = 0; (long)i < (ulong)numOfRes; i++)
                    for (int i = 0; (long)i < (long)numOfRes; i++)
                    {
                        rdr.BaseStream.Seek((long)(nameSect + namePos[i]), SeekOrigin.Begin);
						string n = Encoding.Unicode.GetString(rdr.ReadBytes(this.Read7BitEncodedInt(rdr)));
						uint datPos = rdr.ReadUInt32();
						rdr.BaseStream.Seek((long)(dataSect + datPos), SeekOrigin.Begin);
						items[i] = (ver == 1 ? this.LoadItemV1(rdr, types) : this.LoadItemV2(rdr, types));
						items[i].Name = n;
					}
					view.ItemsSource = items;
				}
				this.Content = view;
			}
		}

		private void ShowText(byte[] bytes)
		{
			TextEditor editor = (TextEditor)XamlReader.Load(XmlReader.Create(new StringReader("\r\n<ae:TextEditor xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\r\n               xmlns:ae=\"http://icsharpcode.net/sharpdevelop/avalonedit\"\r\n               IsReadOnly=\"True\" Background=\"{DynamicResource ControlBackgroundBrush}\" Margin=\"5\" Foreground=\"{DynamicResource LightTextBrush}\" FontFamily=\"Lucida Console\">\r\n    <ae:TextEditor.Options>\r\n        <ae:TextEditorOptions AllowScrollBelowDocument=\"True\" EnableHyperlinks=\"False\" EnableEmailHyperlinks=\"False\"/>\r\n    </ae:TextEditor.Options>\r\n</ae:TextEditor>")));
			editor.TextArea.SetResourceReference(TextArea.SelectionBorderProperty, "DisSelectBdr");
			editor.TextArea.SetResourceReference(TextArea.SelectionBrushProperty, "DisSelectBg");
			editor.TextArea.SetResourceReference(TextArea.SelectionForegroundProperty, "DisSelectFg");
			editor.TextArea.TextView.ClipToBounds = false;
			editor.TextArea.TextView.Margin = new Thickness(1);
			using (StreamReader rdr = new StreamReader(new MemoryStream(bytes)))
			{
				editor.Text = rdr.ReadToEnd();
			}
			this.Content = editor;
		}

		private object TryDeserialize(BinaryReader rdr)
		{
			object obj;
			try
			{
				obj = (new BinaryFormatter()).Deserialize(rdr.BaseStream);
			}
			catch
			{
				obj = null;
			}
			return obj;
		}

		public void View(Resource res)
		{
			byte[] dat;
			string ext;
			base.SetValue(ReflecWindow.TitlePropertyKey, res.Name);
			base.SetValue(ReflecWindow.IconPropertyKey, AsmViewHelper.GetIcon(res));
			CodeIdentifier id = new CodeIdentifier(res);
			base.SetValue(ReflecWindow.IdPropertyKey, id.Identifier);
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
			int idx = res.Name.LastIndexOf('.');
			ext = (idx == -1 ? res.Name : res.Name.Substring(idx));
			string str = ext;
			string str1 = str;
			if (str != null)
			{
				switch (str1)
				{
					case ".bmp":
					case ".emf":
					case ".exif":
					case ".gif":
					case ".jpeg":
					case ".jpg":
					case ".png":
					case ".tif":
					case ".tiff":
					case ".wmf":
					case ".ico":
					{
						this.ShowImage(dat);
						return;
					}
					case ".js":
					case ".cs":
					case ".vb":
					case ".txt":
					case ".xml":
					case ".xaml":
					case ".xsl":
					case ".xslt":
					case ".xsd":
					case ".dtd":
					case ".css":
					case ".htm":
					case ".html":
					case ".mht":
					case ".asp":
					case ".aspx":
					case ".ini":
					case ".manifest":
					case ".config":
					{
						this.ShowText(dat);
						return;
					}
					case ".resources":
					{
						try
						{
							this.ShowResx(dat);
							return;
						}
						catch
						{
							this.ShowBytes(dat);
							return;
						}
						break;
					}
				}
			}
			this.ShowBytes(dat);
		}

		private enum ResourceTypeCode
		{
			Null = 0,
			String = 1,
			Boolean = 2,
			Char = 3,
			Byte = 4,
			SByte = 5,
			Int16 = 6,
			UInt16 = 7,
			Int32 = 8,
			UInt32 = 9,
			Int64 = 10,
			UInt64 = 11,
			Single = 12,
			Double = 13,
			Decimal = 14,
			DateTime = 15,
			LastPrimitive = 16,
			TimeSpan = 16,
			ByteArray = 32,
			Stream = 33,
			StartOfUserTypes = 64
		}

		private class ResxItem
		{
			public object Data
			{
				get;
				set;
			}

			public string Name
			{
				get;
				set;
			}

			public string Type
			{
				get;
				set;
			}

			public ResxItem()
			{
			}
		}

		private class ResxValueConverter : IValueConverter
		{
			public static ResourceViewer.ResxValueConverter Instance;

			static ResxValueConverter()
			{
				ResourceViewer.ResxValueConverter.Instance = new ResourceViewer.ResxValueConverter();
			}

			public ResxValueConverter()
			{
			}

			public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
			{
				if (value == null)
				{
					return "[NULL]";
				}
				if (!(value is byte[]))
				{
					return value.ToString();
				}
				byte[] buff = (byte[])value;
				StringBuilder sb = new StringBuilder();
				int l = Math.Min((int)buff.Length, 16);
				for (int i = 0; i < l; i++)
				{
					sb.AppendFormat("{0:X2} ", buff[i]);
				}
				sb.AppendFormat("(0x{0:X} Bytes)", (int)buff.Length);
				return sb.ToString();
			}

			public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
			{
				throw new NotImplementedException();
			}
		}
	}
}