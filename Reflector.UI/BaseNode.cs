using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Reflector.UI
{
	internal abstract class BaseNode : Animatable
	{
		protected readonly static DependencyPropertyKey IconPropertyKey;

		public readonly static DependencyProperty IconProperty;

		protected readonly static DependencyPropertyKey IsShinePropertyKey;

		public readonly static DependencyProperty IsShineProperty;

		protected readonly static DependencyPropertyKey TextPropertyKey;

		public readonly static DependencyProperty TextProperty;

		protected readonly static DependencyPropertyKey ChildrenPropertyKey;

		public readonly static DependencyProperty ChildrenProperty;

		public readonly static DependencyProperty IsExpandedProperty;

		public readonly static DependencyProperty IsSelectedProperty;

		protected readonly static DependencyPropertyKey MenuPropertyKey;

		public readonly static DependencyProperty MenuProperty;

		private Dictionary<object, object> anno;

		public Dictionary<object, object> Annotations
		{
			get
			{
				if (this.anno == null)
				{
					this.anno = new Dictionary<object, object>();
				}
				return this.anno;
			}
		}

		public IEnumerable<object> Children
		{
			get
			{
				return (IEnumerable<object>)base.GetValue(BaseNode.ChildrenProperty);
			}
		}

		public ImageSource Icon
		{
			get
			{
				return (ImageSource)base.GetValue(BaseNode.IconProperty);
			}
		}

		public bool IsExpanded
		{
			get
			{
				return (bool)base.GetValue(BaseNode.IsExpandedProperty);
			}
			set
			{
				base.SetValue(BaseNode.IsExpandedProperty, value);
			}
		}

		public bool IsSelected
		{
			get
			{
				return (bool)base.GetValue(BaseNode.IsSelectedProperty);
			}
			set
			{
				base.SetValue(BaseNode.IsSelectedProperty, value);
			}
		}

		public bool IsShine
		{
			get
			{
				return (bool)base.GetValue(BaseNode.IsShineProperty);
			}
		}

		public string Menu
		{
			get
			{
				return (string)base.GetValue(BaseNode.MenuProperty);
			}
		}

		public string Text
		{
			get
			{
				return (string)base.GetValue(BaseNode.TextProperty);
			}
		}

		static BaseNode()
		{
			BaseNode.IconPropertyKey = DependencyProperty.RegisterReadOnly("Icon", typeof(ImageSource), typeof(BaseNode), new UIPropertyMetadata(null));
			BaseNode.IconProperty = BaseNode.IconPropertyKey.DependencyProperty;
			BaseNode.IsShinePropertyKey = DependencyProperty.RegisterReadOnly("IsShine", typeof(bool), typeof(BaseNode), new UIPropertyMetadata(true));
			BaseNode.IsShineProperty = BaseNode.IsShinePropertyKey.DependencyProperty;
			BaseNode.TextPropertyKey = DependencyProperty.RegisterReadOnly("Text", typeof(string), typeof(BaseNode), new UIPropertyMetadata(""));
			BaseNode.TextProperty = BaseNode.TextPropertyKey.DependencyProperty;
			BaseNode.ChildrenPropertyKey = DependencyProperty.RegisterReadOnly("Children", typeof(IEnumerable<object>), typeof(BaseNode), new UIPropertyMetadata(Enumerable.Empty<object>()));
			BaseNode.ChildrenProperty = BaseNode.ChildrenPropertyKey.DependencyProperty;
			BaseNode.IsExpandedProperty = DependencyProperty.Register("IsExpanded", typeof(bool), typeof(BaseNode), new UIPropertyMetadata(false, (DependencyObject d, DependencyPropertyChangedEventArgs e) => ((BaseNode)d).IsExpandedChanged()));
			BaseNode.IsSelectedProperty = DependencyProperty.Register("IsSelected", typeof(bool), typeof(BaseNode), new UIPropertyMetadata(false));
			BaseNode.MenuPropertyKey = DependencyProperty.RegisterReadOnly("Menu", typeof(string), typeof(BaseNode), new UIPropertyMetadata(null));
			BaseNode.MenuProperty = BaseNode.MenuPropertyKey.DependencyProperty;
		}

		protected BaseNode()
		{
		}

		public void Detach()
		{
			foreach (object obj in this.Children)
			{
				if (!(obj is BaseNode))
				{
					continue;
				}
				((BaseNode)obj).Detach();
			}
		}

		private void IsExpandedChanged()
		{
			if (this.IsExpanded)
			{
				this.OnExpand();
				return;
			}
			this.OnCollapse();
		}

		protected virtual void OnCollapse()
		{
		}

		protected virtual void OnExpand()
		{
		}
	}
}