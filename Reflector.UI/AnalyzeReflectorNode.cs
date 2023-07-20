using Mono.Cecil;
using Reflector.CodeModel;
using System;
using System.Collections.Generic;
using System.Windows;

namespace Reflector.UI
{
	internal class AnalyzeReflectorNode : BaseNode, IReflectorObjectContainer
	{
		private object obj;

		public BaseNode Parent
		{
			get
			{
				object ret;
				if (!base.Annotations.TryGetValue("Analyzer.Parent", out ret))
				{
					object obj = null;
					ret = obj;
					base.Annotations["Analyzer.Parent"] = obj;
				}
				return (BaseNode)ret;
			}
			set
			{
				base.Annotations["Analyzer.Parent"] = value;
			}
		}

		public object ReflectorObject
		{
			get
			{
				return this.obj;
			}
		}

		public AnalyzeReflectorNode(object obj, BaseNode parent)
		{
			if (obj is AssemblyNameReference)
			{
				obj = ((AssemblyNameReference)obj).Resolve();
			}
			else if (obj is TypeReference)
			{
				obj = ((TypeReference)obj).Resolve();
			}
			else if (obj is MethodReference)
			{
				obj = ((MethodReference)obj).Resolve();
			}
			else if (obj is FieldReference)
			{
				obj = ((FieldReference)obj).Resolve();
			}
			else if (obj is PropertyReference)
			{
				obj = ((PropertyReference)obj).Resolve();
			}
			else if (obj is EventReference)
			{
				obj = ((EventReference)obj).Resolve();
			}
			this.obj = obj;
			this.Parent = parent;
			base.SetValue(BaseNode.IconPropertyKey, AsmViewHelper.GetIcon(obj));
			base.SetValue(BaseNode.IsShinePropertyKey, AsmViewHelper.GetVisibility(obj));
			base.SetValue(BaseNode.TextPropertyKey, AsmViewHelper.Escape(AsmViewHelper.GetFullText(obj)));
			base.SetValue(BaseNode.MenuPropertyKey, "Analyzer.Menu");
			base.SetValue(BaseNode.ChildrenPropertyKey, this.GetChildren());
		}

		protected override Freezable CreateInstanceCore()
		{
			return new AnalyzeReflectorNode(this.obj, this.Parent);
		}

		private object[] GetChildren()
		{
			AssemblyDefinition reflectorObject;
			AssemblyDefinition assemblyDefinition;
			AssemblyDefinition reflectorObject1;
			AssemblyDefinition assemblyDefinition1;
			List<object> nodes = new List<object>();
			if (this.obj is PropertyDefinition)
			{
				PropertyDefinition prop = (PropertyDefinition)this.obj;
				if (prop.GetMethod != null)
				{
					nodes.Add(new AnalyzeReflectorNode(prop.GetMethod, this));
				}
				if (prop.SetMethod != null)
				{
					nodes.Add(new AnalyzeReflectorNode(prop.SetMethod, this));
				}
			}
			else if (!(this.obj is EventDefinition))
			{
				AnalyzeReflectorNode par = this.GetParentNode();
				if (this.obj is AssemblyDefinition || this.obj is ModuleDefinition || this.obj is INamespace || this.obj is TypeReference || this.obj is MethodReference)
				{
					nodes.Add(new DependNode(this.obj));
				}
				if (this.obj is TypeReference || this.obj is MethodReference || this.obj is FieldReference)
				{
					List<object> objs = nodes;
					object obj = this.obj;
					if (par == null)
					{
						reflectorObject = null;
					}
					else
					{
						reflectorObject = par.ReflectorObject as AssemblyDefinition;
					}
					objs.Add(new UsedByNode(obj, reflectorObject));
				}
				if (this.obj is TypeReference)
				{
					List<object> objs1 = nodes;
					TypeReference typeReference = (TypeReference)this.obj;
					if (par == null)
					{
						assemblyDefinition1 = null;
					}
					else
					{
						assemblyDefinition1 = par.ReflectorObject as AssemblyDefinition;
					}
					objs1.Add(new ExposedByNode(typeReference, assemblyDefinition1));
				}
				if (this.obj is FieldReference)
				{
					List<object> objs2 = nodes;
					FieldReference fieldReference = (FieldReference)this.obj;
					if (par == null)
					{
						reflectorObject1 = null;
					}
					else
					{
						reflectorObject1 = par.ReflectorObject as AssemblyDefinition;
					}
					objs2.Add(new AssignedByNode(fieldReference, reflectorObject1));
				}
				if (this.obj is TypeReference)
				{
					List<object> objs3 = nodes;
					TypeReference typeReference1 = (TypeReference)this.obj;
					if (par == null)
					{
						assemblyDefinition = null;
					}
					else
					{
						assemblyDefinition = par.ReflectorObject as AssemblyDefinition;
					}
					objs3.Add(new InstantiatedByNode(typeReference1, assemblyDefinition));
				}
				if (this.obj is AssemblyDefinition || this.obj is ModuleDefinition)
				{
					nodes.Add(new PInvokeNode(this.obj));
				}
			}
			else
			{
				EventDefinition prop = (EventDefinition)this.obj;
				if (prop.AddMethod != null)
				{
					nodes.Add(new AnalyzeReflectorNode(prop.AddMethod, this));
				}
				if (prop.RemoveMethod != null)
				{
					nodes.Add(new AnalyzeReflectorNode(prop.RemoveMethod, this));
				}
				if (prop.InvokeMethod != null)
				{
					nodes.Add(new AnalyzeReflectorNode(prop.InvokeMethod, this));
				}
			}
			return nodes.ToArray();
		}

		public static BaseNode GetParent(BaseNode node)
		{
			return (BaseNode)node.Annotations["Analyzer.Parent"];
		}

		private AnalyzeReflectorNode GetParentNode()
		{
			BaseNode node = this.Parent;
			while (!(node is AnalyzeReflectorNode) && node != null)
			{
				node = AnalyzeReflectorNode.GetParent(node);
			}
			return node as AnalyzeReflectorNode;
		}

		public static void SetParent(BaseNode node, BaseNode parent)
		{
			node.Annotations["Analyzer.Parent"] = parent;
		}
	}
}