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
	internal class DerivedTypeNode : LazyNode
	{
		protected TypeDefinition typeDecl;

		public DerivedTypeNode(TypeDefinition typeDecl)
		{
			this.typeDecl = typeDecl;
			base.SetValue(BaseNode.IconPropertyKey, AsmViewHelper.GetThreadStaticIcon((ImageSource)Application.Current.Resources["folder"]));
			base.SetValue(BaseNode.TextPropertyKey, "Derived Types");
			base.IsMutable = true;
		}

		protected override Freezable CreateInstanceCore()
		{
			return new DerivedTypeNode(this.typeDecl);
		}

		protected override IEnumerable<object> InitializeItems()
		{
			List<TypeDefinition> de = new List<TypeDefinition>();
			List<TypeDefinition> scope = new List<TypeDefinition>();
			if (this.typeDecl.IsNotPublic || this.typeDecl.IsNestedAssembly || this.typeDecl.IsNestedFamilyAndAssembly)
			{
				this.PopulateTypes(this.typeDecl.Module.Assembly, scope);
			}
			else if (this.typeDecl.IsNestedPrivate)
			{
				this.PopulateTypes(this.typeDecl.DeclaringType, scope);
			}
			else if (this.typeDecl.IsNestedFamily || this.typeDecl.IsNestedFamilyOrAssembly || this.typeDecl.IsNestedPublic || this.typeDecl.IsPublic)
			{
				foreach (AssemblyDefinition asm in (IEnumerable<AssemblyDefinition>)App.Reflector.GetService("AsmMgr").GetProp("AsmMgr.Assemblies"))
				{
					this.PopulateTypes(asm, scope);
				}
			}
			foreach (TypeDefinition i in scope)
			{
				this.PopulateDerivedType(i, de);
			}
			List<object> b = new List<object>();
			foreach (TypeDefinition i in de)
			{
				List<object> objs = b;
				System.Windows.Threading.Dispatcher dispatcher = Application.Current.Dispatcher;
				Func<TypeDefinition, object> derivedTypeNodeCore = (TypeDefinition x) => new DerivedTypeNode.DerivedTypeNodeCore(x);
				object[] objArray = new object[] { i };
				objs.Add(dispatcher.Invoke(derivedTypeNodeCore, DispatcherPriority.Background, objArray));
			}
			return b.ToArray();
		}

		private void PopulateDerivedType(TypeDefinition type, List<TypeDefinition> derived)
		{
			if (this.typeDecl.IsInterface)
			{
				foreach (TypeReference i in type.Interfaces)
				{
					if (!i.Equals(this.typeDecl))
					{
						continue;
					}
					derived.Add(type);
					break;
				}
			}
			else if (type.BaseType == this.typeDecl)
			{
				derived.Add(type);
			}
		}

		private void PopulateTypes(AssemblyDefinition assembly, List<TypeDefinition> scope)
		{
			foreach (ModuleDefinition mod in assembly.Modules)
			{
				if (this.typeDecl.Module.Assembly != assembly)
				{
					bool hasRefer = false;
					foreach (AssemblyNameReference refer in mod.AssemblyReferences)
					{
						if (refer.FullName != this.typeDecl.Module.Assembly.Name.FullName)
						{
							continue;
						}
						hasRefer = true;
						break;
					}
					if (!hasRefer)
					{
						continue;
					}
				}
				foreach (TypeDefinition type in mod.Types)
				{
					this.PopulateTypes(type, scope);
				}
			}
		}

		private void PopulateTypes(TypeDefinition type, List<TypeDefinition> scope)
		{
			scope.Add(type);
			foreach (TypeDefinition t in type.NestedTypes)
			{
				this.PopulateTypes(t, scope);
			}
		}

		private class DerivedTypeNodeCore : DerivedTypeNode, IReflectorObjectContainer
		{
			public object ReflectorObject
			{
				get
				{
					return this.typeDecl;
				}
			}

			public DerivedTypeNodeCore(TypeDefinition typeDecl) : base(typeDecl)
			{
				base.SetValue(BaseNode.IconPropertyKey, AsmViewHelper.GetIcon(typeDecl));
				base.SetValue(BaseNode.IsShinePropertyKey, AsmViewHelper.GetVisibility(typeDecl));
				base.SetValue(BaseNode.TextPropertyKey, AsmViewHelper.Escape(AsmViewHelper.GetText(typeDecl)));
				base.SetValue(BaseNode.MenuPropertyKey, "AsmMgr.TypeRef");
				if (!typeDecl.IsSealed)
				{
					base.Initialize();
					return;
				}
				base.ClearValue(BaseNode.ChildrenPropertyKey);
				base.Initalized = true;
			}

			protected override Freezable CreateInstanceCore()
			{
				return new DerivedTypeNode.DerivedTypeNodeCore(this.typeDecl);
			}
		}
	}
}