using Mono.Cecil;
using Reflector.CodeModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Windows.Data;

namespace Reflector.UI
{
	internal class AsmNodesConverter : IMultiValueConverter
	{
		public static AsmNodesConverter Instance;

		static AsmNodesConverter()
		{
			AsmNodesConverter.Instance = new AsmNodesConverter();
		}

		public AsmNodesConverter()
		{
		}

		public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
		{
			return new AsmNodesConverter.ObservableMap<object>((object[])value[0], (BaseNode)value[1]);
		}

		public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}

		public class ObservableMap<T> : ObservableCollection<object>
		{
			private static List<Type> Base;

			private ObservableCollection<T> coll;

			private ReadOnlyObservableCollection<T> outer;

			private BaseNode parent;

			public ReadOnlyObservableCollection<T> Inner
			{
				get
				{
					return this.outer;
				}
			}

			static ObservableMap()
			{
				List<Type> types = new List<Type>()
				{
					typeof(AssemblyDefinition),
					typeof(AssemblyNameReference),
					typeof(ModuleReference),
					typeof(Resource),
					typeof(INamespace),
					typeof(TypeDefinition),
					typeof(MethodDefinition),
					typeof(PropertyDefinition),
					typeof(EventDefinition),
					typeof(FieldDefinition)
				};
				AsmNodesConverter.ObservableMap<T>.Base = types;
			}

			public ObservableMap(T[] arr, BaseNode parent) : this(new ObservableCollection<T>(), parent)
			{
				T[] tArray = arr;
				for (int num = 0; num < (int)tArray.Length; num++)
				{
					T i = tArray[num];
					this.coll.Add(i);
				}
			}

			public ObservableMap(ObservableCollection<T> coll, BaseNode parent)
			{
				this.coll = coll;
				this.parent = parent;
				if (parent != null)
				{
					parent.Annotations["AsmView.Map"] = this;
				}
				this.outer = new ReadOnlyObservableCollection<T>(coll);
				coll.CollectionChanged += new NotifyCollectionChangedEventHandler(this.OnCollectionChanged);
				foreach (T i in coll)
				{
					base.Items.Add(this.GetNode(i));
				}
			}

			public static AsmNodesConverter.ObservableMap<T> GetMap(BaseNode node)
			{
				return (AsmNodesConverter.ObservableMap<T>)node.Annotations["AsmView.Map"];
			}

			private object GetNode(object val)
			{
				Type type = val.GetType();
				object obj = val;
				foreach (Type iF in AsmNodesConverter.ObservableMap<T>.Base)
				{
					if (!iF.IsAssignableFrom(type))
					{
						continue;
					}
					obj = new ReflectorNode(val);
					break;
				}
				return obj;
			}

			private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
			{
				switch (e.Action)
				{
					case NotifyCollectionChangedAction.Add:
					{
						base.Add(this.GetNode(e.NewItems[0]));
						return;
					}
					case NotifyCollectionChangedAction.Remove:
					{
						base.RemoveAt(e.OldStartingIndex);
						return;
					}
					case NotifyCollectionChangedAction.Replace:
					{
						base.SetItem(e.OldStartingIndex, this.GetNode(e.NewItems[0]));
						return;
					}
					case NotifyCollectionChangedAction.Move:
					{
						base.Move(e.OldStartingIndex, e.NewStartingIndex);
						return;
					}
				}
				this.OnCollectionChanged(e);
			}
		}
	}
}