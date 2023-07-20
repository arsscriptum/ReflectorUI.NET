using Reflector;
using Reflector.CodeModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml;

namespace Reflector.UI
{
	internal class Bookmarks : ReflecWindow, IReflecService
	{
		public readonly static Bookmarks Instance;

		private IReflecWindow window;

		private Bookmarks.ProxyBookmarkCollection proxy;

		public ObservableCollection<string> Bookmark
		{
			get;
			private set;
		}

		static Bookmarks()
		{
			Bookmarks.Instance = new Bookmarks();
			FrameworkElement.DefaultStyleKeyProperty.OverrideMetadata(typeof(Bookmarks), new FrameworkPropertyMetadata(typeof(Bookmarks)));
			ReflecWindow.TitlePropertyKey.OverrideMetadata(typeof(Bookmarks), new PropertyMetadata("Bookmarks"));
			ReflecWindow.IconPropertyKey.OverrideMetadata(typeof(Bookmarks), new PropertyMetadata(Application.Current.Resources["bookmark"]));
			ReflecWindow.IdPropertyKey.OverrideMetadata(typeof(Bookmarks), new PropertyMetadata("Bookmarks"));
		}

		private Bookmarks()
		{
			base.SetValue(ReflecWindow.ParentServicePropertyKey, this);
			this.Bookmark = new ObservableCollection<string>();
		}

		public object Exec(string name, params object[] args)
		{
			if (name == "Bookmarks.Add")
			{
				this.Bookmark.Add((string)args[0]);
				return null;
			}
			if (name == "Bookmarks.Remove")
			{
				this.Bookmark.Remove((string)args[0]);
				return null;
			}
			if (name == "Bookmarks.Toggle")
			{
				if (!this.Bookmark.Contains((string)args[0]))
				{
					this.Bookmark.Add((string)args[0]);
				}
				else
				{
					this.Bookmark.Remove((string)args[0]);
				}
				return null;
			}
			if (name != "Bookmarks.Show")
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
			return Bookmarks.FindParent<T>(parentObject);
		}

		public object GetProp(string name)
		{
			if (name != "Bookmarks.Count")
			{
				return null;
			}
			return this.Bookmark.Count;
		}

		public void LoadSettings(XmlNode node)
		{
			bool s;
			if (node.SelectSingleNode("@show") != null && bool.TryParse(node.SelectSingleNode("@show").Value, out s) && s)
			{
				this.Exec("Bookmarks.Show", new object[0]);
			}
			foreach (XmlNode asm in node.SelectNodes("bookmark/@id"))
			{
				object[] value = new object[] { asm.Value };
				this.Exec("Bookmarks.Add", value);
			}
		}

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			ListBox templateChild = (ListBox)base.GetTemplateChild("PART_listBox");
			ListBox listBox = templateChild;
			Bookmarks.ProxyBookmarkCollection proxyBookmarkCollections = new Bookmarks.ProxyBookmarkCollection(this.Bookmark);
			Bookmarks.ProxyBookmarkCollection proxyBookmarkCollections1 = proxyBookmarkCollections;
			this.proxy = proxyBookmarkCollections;
			listBox.ItemsSource = proxyBookmarkCollections1;
			templateChild.MouseDoubleClick += new MouseButtonEventHandler((object sender, MouseButtonEventArgs e) => {
				if (e.ChangedButton == MouseButton.Left)
				{
					ListBoxItem item = Bookmarks.FindParent<ListBoxItem>((DependencyObject)templateChild.InputHitTest(e.GetPosition(templateChild)));
					if (item != null && ((Bookmarks.ProxyBookmarkCollection.Bookmark)item.Content).Obj != null)
					{
						this._App.GetService("AsmMgr").SetProp("AsmMgr.Selected", ((Bookmarks.ProxyBookmarkCollection.Bookmark)item.Content).Obj);
						this._App.GetService("AsmMgr").Exec("AsmMgr.Show", new object[0]);
					}
				}
			});
			templateChild.KeyDown += new KeyEventHandler((object sender, KeyEventArgs e) => {
				if (e.Key == Key.Delete && templateChild.SelectedItem != null)
				{
					this.Bookmark.Remove(((Bookmarks.ProxyBookmarkCollection.Bookmark)templateChild.SelectedItem).Link);
				}
			});
			EventHandler<AssemblyEventArgs> handler = (object sender, AssemblyEventArgs e) => base.Dispatcher.Invoke(new Action(this.proxy.RaiseReset), new object[0]);
			((IAssemblyManager)base._App.GetService("AsmMgr")).AssemblyLoaded += handler;
			((IAssemblyManager)base._App.GetService("AsmMgr")).AssemblyUnloaded += handler;
		}

		protected override void OnDragEnter(DragEventArgs e)
		{
			if (e.Data.GetDataPresent("Reflector object") && !(e.Data.GetData("Reflector object") is INamespace))
			{
				e.Effects = DragDropEffects.Move;
				e.Handled = true;
			}
		}

		protected override void OnDrop(DragEventArgs e)
		{
			if (e.Data.GetDataPresent("Reflector object") && !(e.Data.GetData("Reflector object") is INamespace))
			{
				CodeIdentifier id = new CodeIdentifier(e.Data.GetData("Reflector object"));
				object[] identifier = new object[] { id.Identifier };
				this.Exec("Bookmarks.Add", identifier);
				e.Handled = true;
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
			foreach (string id in this.Bookmark)
			{
				XmlElement element = doc.CreateElement("bookmark");
				XmlAttribute attr = doc.CreateAttribute("id");
				attr.Value = id;
				element.Attributes.Append(attr);
				node.AppendChild(element);
			}
		}

		public void SetProp(string name, object value)
		{
			throw new InvalidOperationException(name);
		}

		private class ProxyBookmarkCollection : INotifyCollectionChanged, IEnumerable<object>, IEnumerable
		{
			private ObservableCollection<string> bks;

			public ProxyBookmarkCollection(ObservableCollection<string> bks)
			{
				this.bks = bks;
				bks.CollectionChanged += new NotifyCollectionChangedEventHandler((object sender, NotifyCollectionChangedEventArgs e) => this.CollectionChanged(sender, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset)));
			}

			private Bookmarks.ProxyBookmarkCollection.Bookmark GetBk(string str)
			{
				CodeIdentifier id = new CodeIdentifier(str);
				Bookmarks.ProxyBookmarkCollection.Bookmark ret = new Bookmarks.ProxyBookmarkCollection.Bookmark()
				{
					Link = str,
					Obj = id.Resolve(AssemblyManager.Instance)
				};
				if (ret.Obj != null)
				{
					ret.Txt = AsmViewHelper.Escape(AsmViewHelper.GetFullText(ret.Obj));
					ret.Img = AsmViewHelper.GetIcon(ret.Obj);
					ret.Fg = AsmViewHelper.GetVisibility(ret.Obj);
				}
				else
				{
					ret.Txt = id.Identifier;
					ret.Img = (BitmapSource)Application.Current.Resources["err"];
					ret.Fg = true;
				}
				return ret;
			}

			public IEnumerator<object> GetEnumerator()
			{
				return new Bookmarks.ProxyBookmarkCollection.BookmarkEnumerator(this);
			}

			public void RaiseReset()
			{
				this.CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
			}

			IEnumerator System.Collections.IEnumerable.GetEnumerator()
			{
				return new Bookmarks.ProxyBookmarkCollection.BookmarkEnumerator(this);
			}

			public event NotifyCollectionChangedEventHandler CollectionChanged;

			public class Bookmark
			{
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

				public string Link
				{
					get;
					set;
				}

				public object Obj
				{
					get;
					set;
				}

				public string Txt
				{
					get;
					set;
				}

				public Bookmark()
				{
				}
			}

			private class BookmarkEnumerator : IEnumerator<object>, IDisposable, IEnumerator
			{
				private Bookmarks.ProxyBookmarkCollection collection;

				private IEnumerator<string> ie;

				public object Current
				{
					get
					{
						return this.collection.GetBk(this.ie.Current);
					}
				}

				public BookmarkEnumerator(Bookmarks.ProxyBookmarkCollection coll)
				{
					this.collection = coll;
					this.ie = coll.bks.GetEnumerator();
				}

				public void Dispose()
				{
					this.ie.Dispose();
				}

				public bool MoveNext()
				{
					return this.ie.MoveNext();
				}

				public void Reset()
				{
					this.ie.Reset();
				}
			}
		}
	}
}