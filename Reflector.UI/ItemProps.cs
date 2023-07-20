using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace Reflector.UI
{
	internal class ItemProps : ReflecWindow
	{
		public readonly static DependencyProperty ItemProperty;

		public readonly static DependencyProperty ItemsProperty;

		public object Item
		{
			get
			{
				return base.GetValue(ItemProps.ItemProperty);
			}
			set
			{
				base.SetValue(ItemProps.ItemProperty, value);
			}
		}

		public ObservableCollection<object> Items
		{
			get
			{
				return (ObservableCollection<object>)base.GetValue(ItemProps.ItemsProperty);
			}
			set
			{
				base.SetValue(ItemProps.ItemsProperty, value);
			}
		}

		static ItemProps()
		{
			ItemProps.ItemProperty = DependencyProperty.Register("Item", typeof(object), typeof(ItemProps), new UIPropertyMetadata(null, new PropertyChangedCallback(ItemProps.ItemChanged)));
			ItemProps.ItemsProperty = DependencyProperty.Register("Items", typeof(ObservableCollection<object>), typeof(ItemProps), new UIPropertyMetadata(new ObservableCollection<object>()));
			FrameworkElement.DefaultStyleKeyProperty.OverrideMetadata(typeof(ItemProps), new FrameworkPropertyMetadata(typeof(ItemProps)));
			ReflecWindow.TitlePropertyKey.OverrideMetadata(typeof(ItemProps), new PropertyMetadata("Item Properties"));
			ReflecWindow.IconPropertyKey.OverrideMetadata(typeof(ItemProps), new PropertyMetadata(Application.Current.Resources["infos"]));
			ReflecWindow.IdPropertyKey.OverrideMetadata(typeof(ItemProps), new PropertyMetadata("ItemProps"));
		}

		public ItemProps()
		{
			base.SetValue(ReflecWindow.ParentServicePropertyKey, AssemblyManager.Instance);
		}

		private static void ItemChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			AssemblyNameReference name;
			try
			{
				ItemProps info = (ItemProps)sender;
				info.Items.Clear();
				object val = e.NewValue;
				if (val is MemberReference)
				{
					ObservableCollection<object> items = info.Items;
					ItemProps.NameValuePair nameValuePair = new ItemProps.NameValuePair()
					{
						Name = "Name",
						Value = ((MemberReference)val).Name
					};
					items.Add(nameValuePair);
				}
				if (val is AssemblyDefinition || val is AssemblyNameReference)
				{
					if (val is AssemblyDefinition)
					{
						name = ((AssemblyDefinition)val).Name;
					}
					else
					{
						name = (AssemblyNameReference)val;
					}
					AssemblyNameReference asm = name;
					ObservableCollection<object> observableCollection = info.Items;
					ItemProps.NameValuePair nameValuePair1 = new ItemProps.NameValuePair()
					{
						Name = "Name",
						Value = asm.FullName
					};
					observableCollection.Add(nameValuePair1);
					if (asm.PublicKey != null && (int)asm.PublicKey.Length > 0)
					{
						ObservableCollection<object> items1 = info.Items;
						ItemProps.NameValuePair nameValuePair2 = new ItemProps.NameValuePair()
						{
							Name = "Public Key",
							Value = BitConverter.ToString(asm.PublicKey).Replace("-", "").ToLower()
						};
						items1.Add(nameValuePair2);
					}
				}
				else if (val is Resource)
				{
					Resource res = (Resource)val;
					ObservableCollection<object> observableCollection1 = info.Items;
					ItemProps.NameValuePair nameValuePair3 = new ItemProps.NameValuePair()
					{
						Name = "Name",
						Value = res.Name
					};
					observableCollection1.Add(nameValuePair3);
					if (res is EmbeddedResource)
					{
						ObservableCollection<object> items2 = info.Items;
						ItemProps.NameValuePair nameValuePair4 = new ItemProps.NameValuePair()
						{
							Name = "Size"
						};
						int length = (int)((EmbeddedResource)res).GetResourceData().Length;
						nameValuePair4.Value = string.Concat("0x", length.ToString("X8"));
						items2.Add(nameValuePair4);
					}
					else if (res is LinkedResource)
					{
						ObservableCollection<object> observableCollection2 = info.Items;
						ItemProps.NameValuePair nameValuePair5 = new ItemProps.NameValuePair()
						{
							Name = "Path",
							Value = string.Concat("0x", ((LinkedResource)res).File)
						};
						observableCollection2.Add(nameValuePair5);
					}
				}
				else if (val is FieldDefinition)
				{
					FieldDefinition field = (FieldDefinition)val;
					if ((int)field.InitialValue.Length != 0)
					{
						ObservableCollection<object> items3 = info.Items;
						ItemProps.NameValuePair nameValuePair6 = new ItemProps.NameValuePair()
						{
							Name = "RVA"
						};
						int rVA = field.RVA;
						nameValuePair6.Value = string.Concat("0x", rVA.ToString("X8"));
						items3.Add(nameValuePair6);
					}
				}
				else if (val is MethodDefinition)
				{
					MethodDefinition method = (MethodDefinition)val;
					if (method.HasBody)
					{
						ObservableCollection<object> observableCollection3 = info.Items;
						ItemProps.NameValuePair nameValuePair7 = new ItemProps.NameValuePair()
						{
							Name = "RVA"
						};
						int num = method.RVA;
						nameValuePair7.Value = string.Concat("0x", num.ToString("X8"));
						observableCollection3.Add(nameValuePair7);
						try
						{
							MethodBody body = method.Body;
							if (body.LocalVarToken != MetadataToken.Zero)
							{
								ObservableCollection<object> items4 = info.Items;
								ItemProps.NameValuePair nameValuePair8 = new ItemProps.NameValuePair()
								{
									Name = "LocalVar Token"
								};
								uint num1 = body.LocalVarToken.ToUInt32();
								nameValuePair8.Value = string.Concat("0x", num1.ToString("X8"));
								items4.Add(nameValuePair8);
							}
							ObservableCollection<object> observableCollection4 = info.Items;
							ItemProps.NameValuePair nameValuePair9 = new ItemProps.NameValuePair()
							{
								Name = "Code Size"
							};
							int codeSize = body.CodeSize;
							nameValuePair9.Value = string.Concat("0x", codeSize.ToString("X8"));
							observableCollection4.Add(nameValuePair9);
						}
						catch
						{
						}
					}
				}
				else if (val is ModuleDefinition)
				{
					ModuleDefinition module = (ModuleDefinition)val;
					ObservableCollection<object> items5 = info.Items;
					ItemProps.NameValuePair nameValuePair10 = new ItemProps.NameValuePair()
					{
						Name = "Name",
						Value = module.Name
					};
					items5.Add(nameValuePair10);
					ObservableCollection<object> observableCollection5 = info.Items;
					ItemProps.NameValuePair nameValuePair11 = new ItemProps.NameValuePair()
					{
						Name = "Architecture",
						Value = module.Architecture.ToString()
					};
					observableCollection5.Add(nameValuePair11);
					if (module.FullyQualifiedName != null)
					{
						ObservableCollection<object> items6 = info.Items;
						ItemProps.NameValuePair nameValuePair12 = new ItemProps.NameValuePair()
						{
							Name = "Path",
							Value = module.FullyQualifiedName
						};
						items6.Add(nameValuePair12);
					}
					ObservableCollection<object> observableCollection6 = info.Items;
					ItemProps.NameValuePair nameValuePair13 = new ItemProps.NameValuePair()
					{
						Name = "Kind",
						Value = module.Kind.ToString()
					};
					observableCollection6.Add(nameValuePair13);
					ObservableCollection<object> items7 = info.Items;
					ItemProps.NameValuePair nameValuePair14 = new ItemProps.NameValuePair()
					{
						Name = "Mvid",
						Value = module.Mvid.ToString()
					};
					items7.Add(nameValuePair14);
					ObservableCollection<object> observableCollection7 = info.Items;
					ItemProps.NameValuePair nameValuePair15 = new ItemProps.NameValuePair()
					{
						Name = "Runtime",
						Value = module.Runtime.ToString()
					};
					observableCollection7.Add(nameValuePair15);
				}
				if (val is IMetadataTokenProvider)
				{
					ObservableCollection<object> items8 = info.Items;
					ItemProps.NameValuePair nameValuePair16 = new ItemProps.NameValuePair()
					{
						Name = "Token"
					};
					uint num2 = ((IMetadataTokenProvider)val).MetadataToken.ToUInt32();
					nameValuePair16.Value = string.Concat("0x", num2.ToString("X8"));
					items8.Add(nameValuePair16);
				}
			}
			catch
			{
			}
		}

		public override void OnApplyTemplate()
		{
			System.Windows.Controls.ContextMenu item = (System.Windows.Controls.ContextMenu)base.Style.Resources["menu"];
			((MenuItem)item.Items[0]).Click += new RoutedEventHandler((object sender, RoutedEventArgs e) => {
				ItemProps.NameValuePair pair = (ItemProps.NameValuePair)((ListViewItem)item.PlacementTarget).DataContext;
				Clipboard.SetText(string.Format("{0}    {1}", pair.Name, pair.Value));
			});
			((MenuItem)item.Items[1]).Click += new RoutedEventHandler((object sender, RoutedEventArgs e) => Clipboard.SetText(((ItemProps.NameValuePair)((ListViewItem)item.PlacementTarget).DataContext).Value));
		}

		private class NameValuePair
		{
			public string Name
			{
				get;
				set;
			}

			public string Value
			{
				get;
				set;
			}

			public NameValuePair()
			{
			}
		}
	}
}