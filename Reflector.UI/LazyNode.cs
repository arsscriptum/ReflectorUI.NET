using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Reflector.UI
{
	internal abstract class LazyNode : BaseNode
	{
		public bool Initalized
		{
			get;
			protected set;
		}

		public Thread Initialization
		{
			get;
			private set;
		}

		protected bool IsMutable
		{
			get;
			set;
		}

		public LazyNode()
		{
			DependencyPropertyKey childrenPropertyKey = BaseNode.ChildrenPropertyKey;
			object[] asFrozen = new object[] { (new LazyNode.LoadingNode()).GetAsFrozen() };
			base.SetValue(childrenPropertyKey, asFrozen);
		}

		public void Initialize()
		{
			if (this.Initalized)
			{
				return;
			}
			this.Initalized = true;
			IEnumerable<object> items = this.InitializeItems();
			System.Windows.Threading.Dispatcher dispatcher = base.Dispatcher;
			Action<DependencyPropertyKey, object> action = new Action<DependencyPropertyKey, object>(this.SetValue);
			object[] childrenPropertyKey = new object[] { BaseNode.ChildrenPropertyKey, items };
			dispatcher.Invoke(action, childrenPropertyKey);
			this.Initialization = null;
		}

		protected virtual IEnumerable<object> InitializeItems()
		{
			return Enumerable.Empty<object>();
		}

		protected override void OnCollapse()
		{
			if (!this.Initalized || !this.IsMutable)
			{
				return;
			}
			foreach (BaseNode node in base.Children.OfType<BaseNode>())
			{
				node.Detach();
			}
			DependencyPropertyKey childrenPropertyKey = BaseNode.ChildrenPropertyKey;
			object[] asFrozen = new object[] { (new LazyNode.LoadingNode()).GetAsFrozen() };
			base.SetValue(childrenPropertyKey, asFrozen);
			this.Initalized = false;
		}

		protected sealed override void OnExpand()
		{
			if (this.Initalized)
			{
				return;
			}
			LazyNode.LoadingNode n = (LazyNode.LoadingNode)Application.Current.Dispatcher.Invoke(new Func<LazyNode.LoadingNode>(() => new LazyNode.LoadingNode()), new object[0]);
			base.SetValue(BaseNode.ChildrenPropertyKey, new object[] { n });
			n.Dispatcher.Invoke(new Action(n.BeginAnimate), new object[0]);
			Thread thread = new Thread(() => {
				Thread.Sleep(100);
				this.Initialize();
			})
			{
				IsBackground = true,
				ApartmentState = ApartmentState.STA
			};
			Thread thread1 = thread;
			Thread thread2 = thread1;
			this.Initialization = thread1;
			thread2.Start();
		}

		private class LoadingNode : BaseNode
		{
			public readonly static DependencyProperty FrameIdxProperty;

			private readonly static ImageSource[] Frames;

			private AnimationTimeline anim;

			public int FrameIdx
			{
				get
				{
					return (int)base.GetValue(LazyNode.LoadingNode.FrameIdxProperty);
				}
				set
				{
					base.SetValue(LazyNode.LoadingNode.FrameIdxProperty, value);
				}
			}

			static LoadingNode()
			{
				LazyNode.LoadingNode.FrameIdxProperty = DependencyProperty.Register("FrameIdx", typeof(int), typeof(LazyNode.LoadingNode), new UIPropertyMetadata((object)0, new PropertyChangedCallback(LazyNode.LoadingNode.FrameIdxChanged)));
				ImageSource[] item = new ImageSource[] { (ImageSource)Application.Current.Resources["wait1"], (ImageSource)Application.Current.Resources["wait2"], (ImageSource)Application.Current.Resources["wait3"], (ImageSource)Application.Current.Resources["wait4"], (ImageSource)Application.Current.Resources["wait5"], (ImageSource)Application.Current.Resources["wait6"], (ImageSource)Application.Current.Resources["wait7"], (ImageSource)Application.Current.Resources["wait8"] };
				LazyNode.LoadingNode.Frames = item;
			}

			public LoadingNode()
			{
				base.SetValue(BaseNode.TextPropertyKey, "Loading...");
			}

			public void BeginAnimate()
			{
				this.anim = new Int32Animation(0, (int)LazyNode.LoadingNode.Frames.Length - 1, new Duration(TimeSpan.FromMilliseconds((double)((int)LazyNode.LoadingNode.Frames.Length) * 62.5)))
				{
					RepeatBehavior = RepeatBehavior.Forever
				};
				base.BeginAnimation(LazyNode.LoadingNode.FrameIdxProperty, this.anim);
			}

			protected override Freezable CreateInstanceCore()
			{
				return new LazyNode.LoadingNode()
				{
					FrameIdx = 0
				};
			}

			private static void FrameIdxChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
			{
				((LazyNode.LoadingNode)obj).SetValue(BaseNode.IconPropertyKey, LazyNode.LoadingNode.Frames[(int)e.NewValue]);
			}

			public void StopAnimate()
			{
				base.BeginAnimation(LazyNode.LoadingNode.FrameIdxProperty, null);
			}
		}
	}
}