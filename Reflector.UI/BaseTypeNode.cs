using Mono.Cecil;
using Mono.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace Reflector.UI
{
	internal class BaseTypeNode : LazyNode
	{
		protected TypeDefinition typeDecl;

		public BaseTypeNode(TypeDefinition typeDecl)
		{
			this.typeDecl = typeDecl;
			base.SetValue(BaseNode.IconPropertyKey, AsmViewHelper.GetThreadStaticIcon((ImageSource)Application.Current.Resources["folder"]));
			base.SetValue(BaseNode.TextPropertyKey, "Base Types");
			base.IsMutable = true;
		}

		protected override Freezable CreateInstanceCore()
		{
			return new BaseTypeNode(this.typeDecl);
		}

		protected override IEnumerable<object> InitializeItems()
		{
			List<TypeReference> bas = new List<TypeReference>();
			if (this.typeDecl.BaseType != null)
			{
				bas.Add(this.typeDecl.BaseType);
			}
			foreach (TypeReference i in this.typeDecl.Interfaces)
			{
				bas.Add(i);
			}
			List<object> b = new List<object>();
			foreach (TypeReference i in bas)
			{
				TypeDefinition t = i.Resolve();
				if (t != null)
				{
					List<object> objs = b;
					System.Windows.Threading.Dispatcher dispatcher = Application.Current.Dispatcher;
					Func<TypeDefinition, object> baseTypeNodeCore = (TypeDefinition x) => new BaseTypeNode.BaseTypeNodeCore(x);
					object[] objArray = new object[] { t };
					objs.Add(dispatcher.Invoke(baseTypeNodeCore, DispatcherPriority.Background, objArray));
				}
				else
				{
					List<object> objs1 = b;
					System.Windows.Threading.Dispatcher dispatcher1 = Application.Current.Dispatcher;
					Func<string, object> errorNode = (string x) => new ErrorNode(x);
					object[] str = new object[] { i.ToString() };
					objs1.Add(dispatcher1.Invoke(errorNode, DispatcherPriority.Background, str));
				}
			}
			return b.ToArray();
		}

		private class BaseTypeNodeCore : BaseTypeNode, IReflectorObjectContainer
		{
			public object ReflectorObject
			{
				get
				{
					return this.typeDecl;
				}
			}

			public BaseTypeNodeCore(TypeDefinition typeDecl) : base(typeDecl)
			{
				base.SetValue(BaseNode.IconPropertyKey, AsmViewHelper.GetIcon(typeDecl));
				base.SetValue(BaseNode.IsShinePropertyKey, AsmViewHelper.GetVisibility(typeDecl));
				base.SetValue(BaseNode.TextPropertyKey, AsmViewHelper.Escape(AsmViewHelper.GetText(typeDecl)));
				base.SetValue(BaseNode.MenuPropertyKey, "AsmMgr.TypeRef");
				if (typeDecl.BaseType == null)
				{
					base.ClearValue(BaseNode.ChildrenPropertyKey);
					base.Initalized = true;
				}
			}

			protected override Freezable CreateInstanceCore()
			{
				return new BaseTypeNode.BaseTypeNodeCore(this.typeDecl);
			}
		}
	}
}