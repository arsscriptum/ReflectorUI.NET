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
	internal class AnalyzeNodesConverter : IMultiValueConverter
	{
		public static AnalyzeNodesConverter Instance;

		static AnalyzeNodesConverter()
		{
			AnalyzeNodesConverter.Instance = new AnalyzeNodesConverter();
		}

		public AnalyzeNodesConverter()
		{
		}

		public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
		{
			return new AnalyzeNodesConverter.ObservableMap<object>((IEnumerable<object>)value[0], (BaseNode)value[1]);
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
					typeof(INamespace),
					typeof(TypeReference),
					typeof(MethodReference),
					typeof(PropertyReference),
					typeof(EventReference),
					typeof(FieldReference)
				};
				AnalyzeNodesConverter.ObservableMap<T>.Base = types;
			}

			public ObservableMap(IEnumerable<object> en, BaseNode parent) : this(new ObservableCollection<T>(), parent)
			{
				foreach (T i in en)
				{
					this.coll.Add(i);
				}
			}

			public ObservableMap(ObservableCollection<T> coll, BaseNode parent)
			{
				this.coll = coll;
				this.parent = parent;
				this.outer = new ReadOnlyObservableCollection<T>(coll);
				coll.CollectionChanged += new NotifyCollectionChangedEventHandler(this.OnCollectionChanged);
				foreach (T i in coll)
				{
					base.Items.Add(this.GetNode(i));
				}
			}

			private object GetNode(object val)
			{
				if (val is BaseNode)
				{
					AnalyzeReflectorNode.SetParent((BaseNode)val, this.parent);
					return val;
				}
				Type type = val.GetType();
				object obj = val;
				foreach (Type iF in AnalyzeNodesConverter.ObservableMap<T>.Base)
				{
					if (!iF.IsAssignableFrom(type))
					{
						continue;
					}
					obj = new AnalyzeReflectorNode(val, this.parent);
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