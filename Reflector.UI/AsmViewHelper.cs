using Mono.Cecil;
using Mono.Collections.Generic;
using Reflector.CodeModel;
using Reflector.CodeModel.Memory;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Reflector.UI
{
	public static class AsmViewHelper
	{
		private static Dictionary<WeakReference, WeakReference> cache;

		static AsmViewHelper()
		{
			AsmViewHelper.cache = new Dictionary<WeakReference, WeakReference>();
		}

		private static void Clean()
		{
			WeakReference[] array = AsmViewHelper.cache.Keys.ToArray<WeakReference>();
			for (int num = 0; num < (int)array.Length; num++)
			{
				WeakReference i = array[num];
				if (!i.IsAlive || !AsmViewHelper.cache[i].IsAlive)
				{
					AsmViewHelper.cache.Remove(i);
				}
			}
		}

		private static object CreateNode(Type type, Dispatcher dis, params object[] param)
		{
			Dispatcher dispatcher = dis;
			Func<Type, object[], object> func = (Type t, object[] par) => Activator.CreateInstance(t, par);
			object[] objArray = new object[] { type, param };
			return dispatcher.Invoke(func, DispatcherPriority.Background, objArray);
		}

		public static string Escape(string str)
		{
			if (Options.Instance.ShortensName)
			{
				return AsmViewHelper.EscapeShort(str);
			}
			StringBuilder ret = new StringBuilder();
			string str1 = str;
			for (int i = 0; i < str1.Length; i++)
			{
				char chr = str1[i];
				char chr1 = chr;
				switch (chr1)
				{
					case '\0':
					{
						ret.Append("\\0");
						break;
					}
					case '\u0001':
					case '\u0002':
					case '\u0003':
					case '\u0004':
					case '\u0005':
					case '\u0006':
					{
						ushort chrVal = chr;
						if (chrVal < 32 || chrVal >= 127)
						{
							ret.Append("\\u");
							ret.Append(chrVal.ToString("X4"));
							break;
						}
						else
						{
							ret.Append(chr);
							break;
						}
					}
					case '\a':
					{
						ret.Append("\\a");
						break;
					}
					case '\b':
					{
						ret.Append("\\b");
						break;
					}
					case '\t':
					{
						ret.Append("\\t");
						break;
					}
					case '\n':
					{
						ret.Append("\\n");
						break;
					}
					case '\v':
					{
						ret.Append("\\v");
						break;
					}
					case '\f':
					{
						ret.Append("\\f");
						break;
					}
					case '\r':
					{
						ret.Append("\\r");
						break;
					}
					default:
					{
						if (chr1 == '\'')
						{
							ret.Append("\\'");
							break;
						}
						else
						{
							if (chr1 != '\\')
							{
								goto case '\u0006';
							}
							ret.Append("\\\\");
							break;
						}
					}
				}
			}
			return ret.ToString();
		}

		private static string EscapeShort(string str)
		{
			if (string.IsNullOrEmpty(str))
			{
				return str;
			}
			StringBuilder ret = new StringBuilder();
			int hash = 0;
			int factor = 1;
			string str1 = str;
			for (int i = 0; i < str1.Length; i++)
			{
				char chr = str1[i];
				if (!AsmViewHelper.IsUnreadable(chr))
				{
					if (hash != 0)
					{
						//ret.Append("2346789ABCDEFGHJKLMNOPQRSTUVWXYZ"[(int)((long)hash & (ulong)-134217728) >> 27]);
                        ret.Append("2346789ABCDEFGHJKLMNOPQRSTUVWXYZ"[(int)((long)hash & (long)-134217728) >> 27]);
                        ret.Append("BCDFGHJKMPQRTVXY"[(hash & 125829120) >> 23]);
						ret.Append("2346789ABCDEFGHJKLMNOPQRSTUVWXYZ"[(hash & 8126464) >> 18]);
						ret.Append("BCDFGHJKMPQRTVXY"[(hash & 245760) >> 14]);
						ret.Append("2346789ABCDEFGHJKLMNOPQRSTUVWXYZ"[(hash & 15872) >> 9]);
						ret.Append("BCDFGHJKMPQRTVXY"[(hash & 480) >> 5]);
						ret.Append("2346789ABCDEFGHJKLMNOPQRSTUVWXYZ"[hash & 31]);
						hash = 0;
						factor = 1;
					}
					ret.Append(chr);
				}
				else
				{
					hash = hash + chr * (char)factor;
					factor++;
				}
			}
			if (hash != 0)
			{
				//ret.Append("2346789ABCDEFGHJKLMNOPQRSTUVWXYZ"[(int)((long)hash & (ulong)-134217728) >> 27]);
                ret.Append("2346789ABCDEFGHJKLMNOPQRSTUVWXYZ"[(int)((long)hash & (long)-134217728) >> 27]);
                ret.Append("BCDFGHJKMPQRTVXY"[(hash & 125829120) >> 23]);
				ret.Append("2346789ABCDEFGHJKLMNOPQRSTUVWXYZ"[(hash & 8126464) >> 18]);
				ret.Append("BCDFGHJKMPQRTVXY"[(hash & 245760) >> 14]);
				ret.Append("2346789ABCDEFGHJKLMNOPQRSTUVWXYZ"[(hash & 15872) >> 9]);
				ret.Append("BCDFGHJKMPQRTVXY"[(hash & 480) >> 5]);
				ret.Append("2346789ABCDEFGHJKLMNOPQRSTUVWXYZ"[hash & 31]);
			}
			return ret.ToString();
		}

		public static List<object> Get(ModuleDefinition mod)
		{
			List<object> target;
			AsmViewHelper.Clean();
			Dictionary<WeakReference, WeakReference>.KeyCollection.Enumerator enumerator = AsmViewHelper.cache.Keys.GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					WeakReference m = enumerator.Current;
					if ((ModuleDefinition)m.Target != mod)
					{
						continue;
					}
					target = (List<object>)AsmViewHelper.cache[m].Target;
					return target;
				}
				return null;
			}
			finally
			{
				((IDisposable)enumerator).Dispose();
			}
			return target;
		}

		public static object[] GetChildren(Dispatcher dispatcher, object obj)
		{
			Namespace ns;
			List<object> ret = new List<object>();
			if (obj is AssemblyDefinition)
			{
				AssemblyDefinition asm = (AssemblyDefinition)obj;
				List<Resource> res = new List<Resource>();
				foreach (ModuleDefinition mod in asm.Modules)
				{
					ret.Add(mod);
					res.AddRange(mod.Resources);
				}
				res.Sort((Resource a, Resource b) => Comparer<string>.Default.Compare(a.Name, b.Name));
				if (res.Count != 0)
				{
					Type type1 = typeof(FolderNode);
					object[] array = new object[] { "Resources", res.ToArray() };
					ret.Add(AsmViewHelper.CreateNode(type1, dispatcher, array));
				}
			}
			else if (obj is AssemblyNameReference)
			{
				AssemblyNameReference asmRef = (AssemblyNameReference)obj;
				AssemblyDefinition asm = asmRef.Resolve();
				if (asm != null)
				{
					foreach (ModuleDefinition mod in asm.Modules)
					{
						foreach (AssemblyNameReference refer in mod.AssemblyReferences)
						{
							ret.Add(refer);
						}
					}
					ret.Sort((object a, object b) => Comparer<string>.Default.Compare(((AssemblyNameReference)a).Name, ((AssemblyNameReference)b).Name));
				}
				else
				{
					Type type2 = typeof(ErrorNode);
					object[] objArray = new object[] { string.Concat("Cannot resolve assembly '", asmRef.ToString(), "'.") };
					ret.Add(AsmViewHelper.CreateNode(type2, dispatcher, objArray));
				}
			}
			else if (!(obj is ModuleDefinition))
			{
				if (obj is INamespace)
				{
					return ((INamespace)obj).Types.OfType<object>().ToArray<object>();
				}
				if (obj is TypeDefinition)
				{
					TypeDefinition typeDecl = (TypeDefinition)obj;
					if (typeDecl.BaseType != null || typeDecl.HasInterfaces)
					{
						Type type3 = typeof(BaseTypeNode);
						object[] objArray1 = new object[] { typeDecl };
						ret.Add(AsmViewHelper.CreateNode(type3, dispatcher, objArray1));
					}
					if (!typeDecl.IsSealed && !AsmViewHelper.IsEnum(typeDecl) && !AsmViewHelper.IsDelegate(typeDecl) && !AsmViewHelper.IsValueType(typeDecl))
					{
						Type type4 = typeof(DerivedTypeNode);
						object[] objArray2 = new object[] { typeDecl };
						ret.Add(AsmViewHelper.CreateNode(type4, dispatcher, objArray2));
					}
					foreach (TypeDefinition nested in 
						from TypeDefinition x in typeDecl.NestedTypes
						orderby x.Name
						select x)
					{
						ret.Add(nested);
					}
					foreach (MethodDefinition mtd in (
						from MethodDefinition x in typeDecl.Methods
						orderby x.Name
						select x).OrderBy<MethodDefinition, int>((MethodDefinition x) => {
						if (x.Name == ".cctor")
						{
							return 0;
						}
						if (x.Name != ".ctor")
						{
							return 2;
						}
						return 1;
					}))
					{
						ret.Add(mtd);
					}
					foreach (PropertyDefinition prop in 
						from PropertyDefinition x in typeDecl.Properties
						orderby x.Name
						select x)
					{
						ret.Add(prop);
						if (prop.GetMethod != null)
						{
							ret.Remove(prop.GetMethod);
						}
						if (prop.SetMethod == null)
						{
							continue;
						}
						ret.Remove(prop.SetMethod);
					}
					foreach (EventDefinition evt in 
						from EventDefinition x in typeDecl.Events
						orderby x.Name
						select x)
					{
						ret.Add(evt);
						if (evt.AddMethod != null)
						{
							ret.Remove(evt.AddMethod);
						}
						if (evt.RemoveMethod != null)
						{
							ret.Remove(evt.RemoveMethod);
						}
						if (evt.InvokeMethod == null)
						{
							continue;
						}
						ret.Remove(evt.InvokeMethod);
					}
					foreach (FieldDefinition fld in 
						from FieldDefinition x in typeDecl.Fields
						orderby x.Name
						select x)
					{
						ret.Add(fld);
					}
				}
				else if (obj is EventDefinition)
				{
					EventDefinition evtDecl = (EventDefinition)obj;
					if (evtDecl.AddMethod != null)
					{
						ret.Add(evtDecl.AddMethod);
					}
					if (evtDecl.RemoveMethod != null)
					{
						ret.Add(evtDecl.RemoveMethod);
					}
					if (evtDecl.InvokeMethod != null)
					{
						ret.Add(evtDecl.InvokeMethod);
					}
				}
				else if (obj is PropertyDefinition)
				{
					PropertyDefinition propDecl = (PropertyDefinition)obj;
					if (propDecl.GetMethod != null)
					{
						ret.Add(propDecl.GetMethod);
					}
					if (propDecl.SetMethod != null)
					{
						ret.Add(propDecl.SetMethod);
					}
				}
			}
			else
			{
				ModuleDefinition mod = (ModuleDefinition)obj;
				ret = AsmViewHelper.Get(mod);
				if (ret == null)
				{
					ret = new List<object>();
					Collection<TypeDefinition> types = mod.Types;
					SortedDictionary<string, Namespace> nss = new SortedDictionary<string, Namespace>();
					foreach (TypeDefinition i in types)
					{
						if (!nss.TryGetValue(i.Namespace, out ns))
						{
							string @namespace = i.Namespace;
							Namespace namespace1 = new Namespace()
							{
								Name = i.Namespace
							};
							ns = namespace1;
							nss.Add(@namespace, namespace1);
						}
						ns.Types.Add(i);
					}
					foreach (Namespace ns1 in nss.Values)
					{
						TypeDefinition[] typeDecls = (
							from type in ns1.Types
							orderby type.Name
							select type).ToArray<TypeDefinition>();
						ns1.Types.Clear();
						TypeDefinition[] typeDefinitionArray = typeDecls;
						for (int num = 0; num < (int)typeDefinitionArray.Length; num++)
						{
							TypeDefinition i = typeDefinitionArray[num];
							ns1.Types.Add(i);
						}
					}
					List<object> refList = new List<object>();
					foreach (AssemblyNameReference r in mod.AssemblyReferences)
					{
						refList.Add(r);
					}
					foreach (ModuleReference r in mod.ModuleReferences)
					{
						refList.Add(r);
					}
					refList.Sort((object a, object b) => Comparer<string>.Default.Compare(((IMetadataScope)a).Name, ((IMetadataScope)b).Name));
					Type type5 = typeof(FolderNode);
					object[] array1 = new object[] { "References", refList.ToArray() };
					ret.Add(AsmViewHelper.CreateNode(type5, dispatcher, array1));
					foreach (Namespace ns2 in nss.Values)
					{
						ret.Add(ns2);
					}
					AsmViewHelper.cache.Add(new WeakReference(mod), new WeakReference(ret));
				}
			}
			return ret.ToArray();
		}

		private static MethodAttributes GetEvtVisibility(EventReference evt)
		{
			MethodDefinition methodDefinition;
			MethodDefinition methodDefinition1;
			MethodDefinition methodDefinition2;
			MethodAttributes ret = MethodAttributes.Public;
			EventDefinition Definition = evt.Resolve();
			if (Definition != null)
			{
				MethodReference addMethod = Definition.AddMethod;
				if (addMethod == null)
				{
					methodDefinition = null;
				}
				else
				{
					methodDefinition = addMethod.Resolve();
				}
				MethodDefinition addDecl = methodDefinition;
				MethodReference removeMethod = Definition.RemoveMethod;
				if (removeMethod == null)
				{
					methodDefinition1 = null;
				}
				else
				{
					methodDefinition1 = removeMethod.Resolve();
				}
				MethodDefinition removeDecl = methodDefinition1;
				MethodReference invokeMethod = Definition.InvokeMethod;
				if (invokeMethod == null)
				{
					methodDefinition2 = null;
				}
				else
				{
					methodDefinition2 = invokeMethod.Resolve();
				}
				MethodDefinition invokeDecl = methodDefinition2;
				if (addDecl != null && removeDecl != null && invokeDecl != null)
				{
					if ((ushort)(addDecl.Attributes & MethodAttributes.MemberAccessMask) == (ushort)(removeDecl.Attributes & MethodAttributes.MemberAccessMask) && (ushort)(addDecl.Attributes & MethodAttributes.MemberAccessMask) == (ushort)(invokeDecl.Attributes & MethodAttributes.MemberAccessMask))
					{
						return (MethodAttributes)((ushort)(addDecl.Attributes & MethodAttributes.MemberAccessMask));
					}
				}
				else if (addDecl != null && removeDecl != null)
				{
					if ((ushort)(addDecl.Attributes & MethodAttributes.MemberAccessMask) == (ushort)(removeDecl.Attributes & MethodAttributes.MemberAccessMask))
					{
						return (MethodAttributes)((ushort)(addDecl.Attributes & MethodAttributes.MemberAccessMask));
					}
				}
				else if (addDecl != null && invokeDecl != null)
				{
					if ((ushort)(addDecl.Attributes & MethodAttributes.MemberAccessMask) == (ushort)(invokeDecl.Attributes & MethodAttributes.MemberAccessMask))
					{
						return (MethodAttributes)((ushort)(addDecl.Attributes & MethodAttributes.MemberAccessMask));
					}
				}
				else if (removeDecl == null || invokeDecl == null)
				{
					if (addDecl != null)
					{
						return (MethodAttributes)((ushort)(addDecl.Attributes & MethodAttributes.MemberAccessMask));
					}
					if (removeDecl != null)
					{
						return (MethodAttributes)((ushort)(removeDecl.Attributes & MethodAttributes.MemberAccessMask));
					}
					if (invokeDecl != null)
					{
						return (MethodAttributes)((ushort)(invokeDecl.Attributes & MethodAttributes.MemberAccessMask));
					}
				}
				else if ((ushort)(removeDecl.Attributes & MethodAttributes.MemberAccessMask) == (ushort)(invokeDecl.Attributes & MethodAttributes.MemberAccessMask))
				{
					return (MethodAttributes)((ushort)(removeDecl.Attributes & MethodAttributes.MemberAccessMask));
				}
			}
			return ret;
		}

		public static string GetFullText(object obj)
		{
			string str;
			if (obj is AssemblyNameReference)
			{
				return ((AssemblyNameReference)obj).FullName;
			}
			if (obj is AssemblyDefinition)
			{
				return ((AssemblyDefinition)obj).Name.FullName;
			}
			if (obj is ModuleReference)
			{
				return ((ModuleReference)obj).Name;
			}
			if (obj is Resource)
			{
				return ((Resource)obj).Name;
			}
			if (obj is Namespace)
			{
				if (string.IsNullOrEmpty(((INamespace)obj).Name))
				{
					return "-";
				}
				return ((INamespace)obj).Name;
			}
			if (!(obj is TypeReference))
			{
				if (obj is FieldReference)
				{
					string name = string.Concat(AsmViewHelper.GetFullText(((FieldReference)obj).DeclaringType), ".", ((FieldReference)obj).Name);
					TypeReference fieldType = ((FieldReference)obj).FieldType;
					if (fieldType.Equals(((FieldReference)obj).DeclaringType))
					{
						TypeReference reference = fieldType;
						if (reference != null && AsmViewHelper.IsEnum(reference))
						{
							return name;
						}
					}
					return string.Concat(name, " : ", AsmViewHelper.GetFullText(fieldType));
				}
				if (!(obj is MethodReference))
				{
					if (!(obj is PropertyReference))
					{
						if (!(obj is EventReference))
						{
							throw new NotSupportedException();
						}
						return string.Concat(AsmViewHelper.GetFullText(((EventReference)obj).DeclaringType), ((EventReference)obj).Name);
					}
					string name = string.Concat(AsmViewHelper.GetFullText(((PropertyReference)obj).DeclaringType), ((PropertyReference)obj).Name);
					Collection<ParameterDefinition> parameters = ((PropertyReference)obj).Parameters;
					TypeReference propType = ((PropertyReference)obj).PropertyType;
					if (parameters.Count <= 0)
					{
						return string.Concat(name, " : ", AsmViewHelper.GetFullText(propType));
					}
					using (StringWriter writer = new StringWriter())
					{
						for (int i = 0; i < parameters.Count; i++)
						{
							if (i != 0)
							{
								writer.Write(", ");
							}
							writer.Write(AsmViewHelper.GetFullText(parameters[i].ParameterType));
						}
						string[] strArrays = new string[] { name, "[", writer.ToString(), "] : ", AsmViewHelper.GetFullText(propType) };
						str = string.Concat(strArrays);
					}
				}
				else
				{
					MethodReference mtdRef = (MethodReference)obj;
					using (StringWriter writer = new StringWriter())
					{
						writer.Write(AsmViewHelper.GetFullText(mtdRef.DeclaringType));
						writer.Write(".");
						writer.Write(AsmViewHelper.GetUnmangledName(mtdRef));
						List<TypeReference> refers = new List<TypeReference>();
						if (!(obj is GenericInstanceType))
						{
							foreach (GenericParameter i in mtdRef.GenericParameters)
							{
								refers.Add(i);
							}
						}
						else
						{
							foreach (GenericArgument i in ((GenericInstanceMethod)mtdRef).GenericArguments)
							{
								refers.Add(i.ElementType);
							}
						}
						if (refers.Count > 0)
						{
							writer.Write("<");
							for (int i = 0; i < refers.Count; i++)
							{
								if (i != 0)
								{
									writer.Write(", ");
								}
								TypeReference type = refers[i];
								if (type == null)
								{
									writer.Write("???");
								}
								else
								{
									writer.Write((type is GenericParameter ? type.Name : AsmViewHelper.GetFullText(type)));
								}
							}
							writer.Write(">");
						}
						writer.Write("(");
						Collection<ParameterDefinition> parameters = mtdRef.Parameters;
						for (int i = 0; i < parameters.Count; i++)
						{
							if (i != 0)
							{
								writer.Write(", ");
							}
							writer.Write(AsmViewHelper.GetFullText(parameters[i].ParameterType));
						}
						if (mtdRef.CallingConvention == MethodCallingConvention.VarArg)
						{
							if (mtdRef.Parameters.Count > 0)
							{
								writer.Write(", ");
							}
							writer.Write("...");
						}
						writer.Write(")");
						if (mtdRef.Name != ".ctor" && mtdRef.Name != ".cctor")
						{
							writer.Write(" : ");
							writer.Write(AsmViewHelper.GetFullText(mtdRef.ReturnType));
						}
						str = writer.ToString();
					}
				}
			}
			else
			{
				using (StringWriter writer = new StringWriter())
				{
					TypeReference typeRefer = (TypeReference)obj;
					if (typeRefer.DeclaringType == null)
					{
						writer.Write((string.IsNullOrEmpty(typeRefer.Namespace) ? "" : string.Concat(typeRefer.Namespace, ".")));
					}
					else
					{
						writer.Write(string.Concat(AsmViewHelper.GetFullText(typeRefer.DeclaringType), "+"));
					}
					writer.Write(AsmViewHelper.GetUnmangledName(typeRefer));
					List<TypeReference> refers = new List<TypeReference>();
					if (!(obj is GenericInstanceType))
					{
						foreach (GenericParameter i in ((TypeReference)obj).GenericParameters)
						{
							refers.Add(i);
						}
					}
					else
					{
						foreach (GenericArgument i in ((GenericInstanceType)obj).GenericArguments)
						{
							refers.Add(i.ElementType);
						}
					}
					if (refers.Count > 0)
					{
						writer.Write("<");
						for (int i = 0; i < refers.Count; i++)
						{
							if (i != 0)
							{
								writer.Write(",");
							}
							TypeReference type = refers[i];
							if (type != null)
							{
								writer.Write((type is GenericParameter ? type.Name : AsmViewHelper.GetFullText(type)));
							}
						}
						writer.Write(">");
					}
					str = writer.ToString();
				}
			}
			return str;
		}

		public static BitmapSource GetIcon(object obj)
		{
			MethodDefinition methodDefinition;
			MethodDefinition methodDefinition1;
			ImageSource ico = null;
			ImageSource sta = null;
			ImageSource vis = null;
			if (obj is AssemblyDefinition || obj is AssemblyNameReference)
			{
				ico = (ImageSource)Application.Current.Resources["asm"];
			}
			else if (obj is ModuleReference)
			{
				ico = (ImageSource)Application.Current.Resources["mod"];
			}
			else if (obj is Resource)
			{
				ico = (ImageSource)Application.Current.Resources["file"];
			}
			else if (obj is INamespace)
			{
				ico = (ImageSource)Application.Current.Resources["ns"];
			}
			else if (obj is TypeDefinition)
			{
				TypeDefinition Definition = (TypeDefinition)obj;
				if (Definition != null)
				{
					ico = (ImageSource)Application.Current.Resources["type"];
					if (Definition.IsInterface)
					{
						ico = (ImageSource)Application.Current.Resources["iface"];
					}
					else if (Definition.BaseType != null)
					{
						if (AsmViewHelper.IsEnum(Definition))
						{
							ico = (ImageSource)Application.Current.Resources["enum"];
						}
						else if (AsmViewHelper.IsValueType(Definition) && !Definition.IsAbstract)
						{
							ico = (ImageSource)Application.Current.Resources["vt"];
						}
						else if (AsmViewHelper.IsDelegate(Definition))
						{
							ico = (ImageSource)Application.Current.Resources["dele"];
						}
					}
					switch (Definition.Attributes & TypeAttributes.VisibilityMask)
					{
						case TypeAttributes.NotPublic:
						case TypeAttributes.NestedAssembly:
						case TypeAttributes.NestedFamANDAssem:
						{
							vis = (ImageSource)Application.Current.Resources["itn"];
							break;
						}
						case TypeAttributes.Public:
						case TypeAttributes.NestedPublic:
						{
							vis = null;
							break;
						}
						case TypeAttributes.NestedPrivate:
						{
							vis = (ImageSource)Application.Current.Resources["priv"];
							break;
						}
						case TypeAttributes.NestedFamily:
						case TypeAttributes.VisibilityMask:
						{
							vis = (ImageSource)Application.Current.Resources["prot"];
							break;
						}
					}
				}
				else
				{
					ico = (ImageSource)Application.Current.Resources["err"];
				}
			}
			else if (obj is TypeReference)
			{
				ico = (ImageSource)Application.Current.Resources["type"];
			}
			else if (obj is FieldDefinition)
			{
				FieldDefinition Definition = (FieldDefinition)obj;
				ico = (ImageSource)Application.Current.Resources["fld"];
				if (Definition.IsStatic)
				{
					if (!AsmViewHelper.IsEnum(Definition.DeclaringType))
					{
						sta = (ImageSource)Application.Current.Resources["stat"];
					}
					else
					{
						ico = (ImageSource)Application.Current.Resources["cst"];
					}
				}
				switch ((ushort)(Definition.Attributes & FieldAttributes.FieldAccessMask))
				{
					case 0:
					case 1:
					{
						vis = (ImageSource)Application.Current.Resources["priv"];
						break;
					}
					case 2:
					case 3:
					{
						vis = (ImageSource)Application.Current.Resources["itn"];
						break;
					}
					case 4:
					case 5:
					{
						vis = (ImageSource)Application.Current.Resources["prot"];
						break;
					}
					case 6:
					{
						vis = null;
						break;
					}
				}
			}
			else if (obj is FieldReference)
			{
				ico = (ImageSource)Application.Current.Resources["fld"];
			}
			else if (obj is MethodDefinition)
			{
				ico = (ImageSource)Application.Current.Resources["mtd"];
				if (!((obj as MethodReference).DeclaringType is ArrayType))
				{
					MethodDefinition Definition = (MethodDefinition)obj;
					string name = Definition.Name;
					if (name == ".ctor" || name == ".cctor")
					{
						ico = (ImageSource)Application.Current.Resources["ctor"];
					}
					else if (Definition.IsVirtual && !Definition.IsAbstract)
					{
						ico = (ImageSource)Application.Current.Resources["omtd"];
					}
					if (Definition.IsStatic)
					{
						sta = (ImageSource)Application.Current.Resources["stat"];
					}
					switch ((ushort)(Definition.Attributes & MethodAttributes.MemberAccessMask))
					{
						case 0:
						case 1:
						{
							vis = (ImageSource)Application.Current.Resources["priv"];
							break;
						}
						case 2:
						case 3:
						{
							vis = (ImageSource)Application.Current.Resources["itn"];
							break;
						}
						case 4:
						case 5:
						{
							vis = (ImageSource)Application.Current.Resources["prot"];
							break;
						}
						case 6:
						{
							vis = null;
							break;
						}
					}
				}
			}
			else if (obj is MethodReference)
			{
				ico = (ImageSource)Application.Current.Resources["mtd"];
			}
			else if (obj is PropertyDefinition)
			{
				PropertyDefinition Definition = (PropertyDefinition)obj;
				MethodReference getMethod = Definition.GetMethod;
				if (getMethod == null)
				{
					methodDefinition = null;
				}
				else
				{
					methodDefinition = getMethod.Resolve();
				}
				MethodDefinition getDecl = methodDefinition;
				MethodReference setMethod = Definition.SetMethod;
				if (setMethod == null)
				{
					methodDefinition1 = null;
				}
				else
				{
					methodDefinition1 = setMethod.Resolve();
				}
				MethodDefinition setDecl = methodDefinition1;
				ico = (ImageSource)Application.Current.Resources["prop"];
				if (getDecl != null && setDecl != null)
				{
					ico = (ImageSource)Application.Current.Resources["prop"];
				}
				else if (getDecl != null)
				{
					ico = (ImageSource)Application.Current.Resources["prop"];
				}
				else if (setDecl != null)
				{
					ico = (ImageSource)Application.Current.Resources["prop"];
				}
				if (AsmViewHelper.IsStatic(Definition))
				{
					sta = (ImageSource)Application.Current.Resources["stat"];
				}
				switch (AsmViewHelper.GetPropVisibility(Definition))
				{
					case MethodAttributes.CompilerControlled:
					case MethodAttributes.Private:
					{
						vis = (ImageSource)Application.Current.Resources["priv"];
						break;
					}
					case MethodAttributes.FamANDAssem:
					case MethodAttributes.Assem:
					{
						vis = (ImageSource)Application.Current.Resources["itn"];
						break;
					}
					case MethodAttributes.Family:
					case MethodAttributes.FamORAssem:
					{
						vis = (ImageSource)Application.Current.Resources["prot"];
						break;
					}
					case MethodAttributes.Public:
					{
						vis = null;
						break;
					}
				}
			}
			else if (obj is PropertyReference)
			{
				ico = (ImageSource)Application.Current.Resources["prop"];
			}
			else if (!(obj is EventDefinition))
			{
				if (!(obj is EventReference))
				{
					throw new NotSupportedException();
				}
				ico = (ImageSource)Application.Current.Resources["evt"];
			}
			else
			{
				ico = (ImageSource)Application.Current.Resources["evt"];
				if (AsmViewHelper.IsStatic(obj as EventReference))
				{
					sta = (ImageSource)Application.Current.Resources["stat"];
				}
				switch (AsmViewHelper.GetEvtVisibility(obj as EventReference))
				{
					case MethodAttributes.CompilerControlled:
					case MethodAttributes.Private:
					{
						vis = (ImageSource)Application.Current.Resources["priv"];
						break;
					}
					case MethodAttributes.FamANDAssem:
					case MethodAttributes.Assem:
					{
						vis = (ImageSource)Application.Current.Resources["itn"];
						break;
					}
					case MethodAttributes.Family:
					case MethodAttributes.FamORAssem:
					{
						vis = (ImageSource)Application.Current.Resources["prot"];
						break;
					}
					case MethodAttributes.Public:
					{
						vis = null;
						break;
					}
				}
			}
			RenderTargetBitmap ret = new RenderTargetBitmap(16, 16, 96, 96, PixelFormats.Pbgra32);
			DrawingVisual visual = new DrawingVisual();
			using (DrawingContext txt = visual.RenderOpen())
			{
				txt.DrawImage(ico, new Rect(0, 0, 16, 16));
				if (vis != null)
				{
					txt.DrawImage(vis, new Rect(0, 0, 16, 16));
				}
				if (sta != null)
				{
					txt.DrawImage(sta, new Rect(0, 0, 16, 16));
				}
			}
			ret.Render(visual);
			ret.Freeze();
			return ret;
		}

		public static string GetMenu(object obj)
		{
			if (obj is AssemblyDefinition)
			{
				return "AsmMgr.Assembly";
			}
			if (obj is AssemblyNameReference)
			{
				return "AsmMgr.AssemblyRef";
			}
			if (obj is ModuleDefinition)
			{
				return "AsmMgr.Module";
			}
			if (obj is ModuleReference)
			{
				return "AsmMgr.ModuleRef";
			}
			if (obj is Resource)
			{
				return "AsmMgr.Resource";
			}
			if (obj is TypeDefinition)
			{
				return "AsmMgr.TypeDecl";
			}
			if (obj is TypeReference)
			{
				return "AsmMgr.TypeRef";
			}
			if (obj is MethodDefinition)
			{
				return "AsmMgr.MethodDecl";
			}
			if (obj is FieldDefinition)
			{
				return "AsmMgr.FieldDecl";
			}
			if (obj is PropertyDefinition)
			{
				return "AsmMgr.PropertyDecl";
			}
			if (obj is EventDefinition)
			{
				return "AsmMgr.EventDecl";
			}
			if (obj is MemberReference)
			{
				return "AsmMgr.MemberRef";
			}
			return null;
		}

		public static string GetName(object obj)
		{
			string str;
			if (obj is AssemblyNameReference)
			{
				return ((AssemblyNameReference)obj).Name;
			}
			if (obj is AssemblyDefinition)
			{
				return ((AssemblyDefinition)obj).Name.Name;
			}
			if (obj is ModuleReference)
			{
				return ((ModuleReference)obj).Name;
			}
			if (obj is Resource)
			{
				return ((Resource)obj).Name;
			}
			if (obj is INamespace)
			{
				if (string.IsNullOrEmpty(((INamespace)obj).Name))
				{
					return "-";
				}
				return ((INamespace)obj).Name;
			}
			if (!(obj is TypeReference))
			{
				if (obj is FieldReference)
				{
					return ((FieldReference)obj).Name;
				}
				if (!(obj is MethodReference))
				{
					if (obj is PropertyReference)
					{
						return ((PropertyReference)obj).Name;
					}
					if (!(obj is EventReference))
					{
						throw new NotSupportedException();
					}
					return ((EventReference)obj).Name;
				}
				MethodReference mtdRef = (MethodReference)obj;
				using (StringWriter writer = new StringWriter())
				{
					writer.Write(AsmViewHelper.GetUnmangledName(mtdRef.DeclaringType));
					writer.Write(".");
					writer.Write(AsmViewHelper.GetUnmangledName(mtdRef));
					List<TypeReference> refers = new List<TypeReference>();
					if (!(mtdRef is GenericInstanceMethod))
					{
						foreach (GenericParameter i in mtdRef.GenericParameters)
						{
							refers.Add(i);
						}
					}
					else
					{
						foreach (GenericArgument i in ((GenericInstanceMethod)mtdRef).GenericArguments)
						{
							refers.Add(i.ElementType);
						}
					}
					if (refers.Count > 0)
					{
						writer.Write("<");
						for (int i = 0; i < refers.Count; i++)
						{
							if (i != 0)
							{
								writer.Write(", ");
							}
							TypeReference type = refers[i];
							if (type == null)
							{
								writer.Write("???");
							}
							else
							{
								writer.Write(AsmViewHelper.GetName(type));
							}
						}
						writer.Write(">");
					}
					str = writer.ToString();
				}
			}
			else
			{
				string name = AsmViewHelper.GetUnmangledName((TypeReference)obj);
				List<TypeReference> refers = new List<TypeReference>();
				if (!(obj is GenericInstanceType))
				{
					foreach (GenericParameter i in ((TypeReference)obj).GenericParameters)
					{
						refers.Add(i);
					}
				}
				else
				{
					foreach (GenericArgument i in ((GenericInstanceType)obj).GenericArguments)
					{
						refers.Add(i.ElementType);
					}
				}
				if (refers.Count <= 0)
				{
					return name;
				}
				using (StringWriter writer = new StringWriter())
				{
					for (int i = 0; i < refers.Count; i++)
					{
						if (i != 0)
						{
							writer.Write(",");
						}
						TypeReference type = refers[i];
						if (type != null)
						{
							writer.Write(AsmViewHelper.GetName(type));
						}
					}
					str = string.Concat(name, "<", writer.ToString(), ">");
				}
			}
			return str;
		}

		private static MethodAttributes GetPropVisibility(PropertyReference prop)
		{
			MethodDefinition methodDefinition;
			MethodDefinition methodDefinition1;
			MethodAttributes ret = MethodAttributes.Public;
			PropertyDefinition Definition = prop.Resolve();
			if (Definition != null)
			{
				MethodReference setMethod = Definition.SetMethod;
				if (setMethod == null)
				{
					methodDefinition = null;
				}
				else
				{
					methodDefinition = setMethod.Resolve();
				}
				MethodDefinition setDecl = methodDefinition;
				MethodReference getMethod = Definition.GetMethod;
				if (getMethod == null)
				{
					methodDefinition1 = null;
				}
				else
				{
					methodDefinition1 = getMethod.Resolve();
				}
				MethodDefinition getDecl = methodDefinition1;
				if (setDecl != null && getDecl != null)
				{
					if ((ushort)(getDecl.Attributes & MethodAttributes.MemberAccessMask) == (ushort)(setDecl.Attributes & MethodAttributes.MemberAccessMask))
					{
						ret = (MethodAttributes)((ushort)(getDecl.Attributes & MethodAttributes.MemberAccessMask));
					}
					return ret;
				}
				if (setDecl != null)
				{
					return (MethodAttributes)((ushort)(setDecl.Attributes & MethodAttributes.MemberAccessMask));
				}
				if (getDecl != null)
				{
					ret = (MethodAttributes)((ushort)(getDecl.Attributes & MethodAttributes.MemberAccessMask));
				}
			}
			return ret;
		}

		public static string GetText(object obj)
		{
			string str;
			if (obj is AssemblyNameReference)
			{
				return ((AssemblyNameReference)obj).Name;
			}
			if (obj is AssemblyDefinition)
			{
				return ((AssemblyDefinition)obj).Name.Name;
			}
			if (obj is ModuleReference)
			{
				return ((ModuleReference)obj).Name;
			}
			if (obj is Resource)
			{
				return ((Resource)obj).Name;
			}
			if (obj is INamespace)
			{
				if (string.IsNullOrEmpty(((INamespace)obj).Name))
				{
					return "-";
				}
				return ((INamespace)obj).Name;
			}
			if (!(obj is TypeReference))
			{
				if (obj is FieldReference)
				{
					string name = ((FieldReference)obj).Name;
					TypeReference fieldType = ((FieldReference)obj).FieldType;
					if (fieldType.Equals(((FieldReference)obj).DeclaringType))
					{
						TypeReference reference = fieldType;
						if (reference != null && AsmViewHelper.IsEnum(reference))
						{
							return name;
						}
					}
					return string.Concat(name, " : ", AsmViewHelper.GetText(fieldType));
				}
				if (!(obj is MethodReference))
				{
					if (!(obj is PropertyReference))
					{
						if (!(obj is EventReference))
						{
							throw new NotSupportedException();
						}
						return ((EventReference)obj).Name;
					}
					string name = ((PropertyReference)obj).Name;
					Collection<ParameterDefinition> parameters = ((PropertyReference)obj).Parameters;
					TypeReference propType = ((PropertyReference)obj).PropertyType;
					if (parameters.Count <= 0)
					{
						return string.Concat(name, " : ", AsmViewHelper.GetText(propType));
					}
					using (StringWriter writer = new StringWriter())
					{
						for (int i = 0; i < parameters.Count; i++)
						{
							if (i != 0)
							{
								writer.Write(", ");
							}
							writer.Write(AsmViewHelper.GetText(parameters[i].ParameterType));
						}
						string[] strArrays = new string[] { name, "[", writer.ToString(), "] : ", AsmViewHelper.GetText(propType) };
						str = string.Concat(strArrays);
					}
				}
				else
				{
					MethodReference mtdRef = (MethodReference)obj;
					using (StringWriter writer = new StringWriter())
					{
						writer.Write(AsmViewHelper.GetUnmangledName(mtdRef));
						List<TypeReference> refers = new List<TypeReference>();
						if (!(mtdRef is GenericInstanceMethod))
						{
							foreach (GenericParameter i in mtdRef.GenericParameters)
							{
								refers.Add(i);
							}
						}
						else
						{
							foreach (GenericArgument i in ((GenericInstanceMethod)mtdRef).GenericArguments)
							{
								refers.Add(i.ElementType);
							}
						}
						if (refers.Count > 0)
						{
							writer.Write("<");
							for (int i = 0; i < refers.Count; i++)
							{
								if (i != 0)
								{
									writer.Write(", ");
								}
								TypeReference type = refers[i];
								if (type == null)
								{
									writer.Write("???");
								}
								else
								{
									writer.Write(AsmViewHelper.GetText(type));
								}
							}
							writer.Write(">");
						}
						writer.Write("(");
						Collection<ParameterDefinition> parameters = mtdRef.Parameters;
						for (int i = 0; i < parameters.Count; i++)
						{
							if (i != 0)
							{
								writer.Write(", ");
							}
							writer.Write(AsmViewHelper.GetText(parameters[i].ParameterType));
						}
						if (mtdRef.CallingConvention == MethodCallingConvention.VarArg)
						{
							if (mtdRef.Parameters.Count > 0)
							{
								writer.Write(", ");
							}
							writer.Write("...");
						}
						writer.Write(")");
						if (mtdRef.Name != ".ctor" && mtdRef.Name != ".cctor")
						{
							writer.Write(" : ");
							writer.Write(AsmViewHelper.GetText(mtdRef.ReturnType));
						}
						str = writer.ToString();
					}
				}
			}
			else
			{
				string name = AsmViewHelper.GetUnmangledName((TypeReference)obj);
				List<TypeReference> refers = new List<TypeReference>();
				if (!(obj is GenericInstanceType))
				{
					foreach (GenericParameter i in ((TypeReference)obj).GenericParameters)
					{
						refers.Add(i);
					}
				}
				else
				{
					foreach (GenericArgument i in ((GenericInstanceType)obj).GenericArguments)
					{
						refers.Add(i.ElementType);
					}
				}
				if (refers.Count <= 0)
				{
					return name;
				}
				using (StringWriter writer = new StringWriter())
				{
					for (int i = 0; i < refers.Count; i++)
					{
						if (i != 0)
						{
							writer.Write(",");
						}
						TypeReference type = refers[i];
						if (type != null)
						{
							writer.Write(AsmViewHelper.GetText(type));
						}
					}
					str = string.Concat(name, "<", writer.ToString(), ">");
				}
			}
			return str;
		}

		public static ImageSource GetThreadStaticIcon(ImageSource ico)
		{
			RenderTargetBitmap ret = new RenderTargetBitmap(16, 16, 96, 96, PixelFormats.Pbgra32);
			DrawingVisual visual = new DrawingVisual();
			using (DrawingContext txt = visual.RenderOpen())
			{
				txt.DrawImage(ico, new Rect(0, 0, 16, 16));
			}
			ret.Render(visual);
			ret.Freeze();
			return ret;
		}

		public static string GetUnmangledName(TypeReference typeRef)
		{
			if (typeRef is TypeSpecification)
			{
				if (typeRef is FunctionPointerType)
				{
					return typeRef.ToString();
				}
				string ret = AsmViewHelper.GetUnmangledName(((TypeSpecification)typeRef).ElementType);
				string spec = "";
				if (typeRef is ArrayType)
				{
					spec = string.Format("[{0}]", new string(',', ((ArrayType)typeRef).Dimensions.Count - 1));
				}
				else if (typeRef is ByReferenceType)
				{
					spec = "&";
				}
				else if (typeRef is PointerType || typeRef is PinnedType)
				{
					spec = "*";
				}
				return string.Concat(ret, spec);
			}
			typeRef = typeRef.GetElementType();
			if (typeRef.HasGenericParameters)
			{
				int count = typeRef.GenericParameters.Count - (typeRef.DeclaringType == null ? 0 : typeRef.DeclaringType.GenericParameters.Count);
				string str = string.Concat("`", count.ToString(CultureInfo.InvariantCulture));
				if (typeRef.Name.EndsWith(str))
				{
					return typeRef.Name.Substring(0, typeRef.Name.Length - str.Length);
				}
			}
			return typeRef.Name;
		}

		public static string GetUnmangledName(MethodReference methodRef)
		{
			methodRef = methodRef.GetElementMethod();
			if (methodRef.HasGenericParameters)
			{
				int count = methodRef.GenericParameters.Count;
				string str = string.Concat("``", count.ToString(CultureInfo.InvariantCulture));
				if (methodRef.Name.EndsWith(str))
				{
					return methodRef.Name.Substring(0, methodRef.Name.Length - str.Length);
				}
			}
			return methodRef.Name;
		}

		public static bool GetVisibility(object obj)
		{
			if (obj is MemberReference && !AsmViewHelper.IsPublicVisible((MemberReference)obj) || obj is Resource && ((Resource)obj).Attributes != ManifestResourceAttributes.Public)
			{
				return false;
			}
			return true;
		}

		public static bool HasChildren(object obj)
		{
			List<object> objs = new List<object>();
			if (obj is AssemblyDefinition)
			{
				return true;
			}
			if (obj is AssemblyNameReference)
			{
				return true;
			}
			if (obj is ModuleDefinition)
			{
				return true;
			}
			if (obj is INamespace)
			{
				return true;
			}
			if (obj is TypeDefinition)
			{
				TypeDefinition typeDecl = (TypeDefinition)obj;
				if (typeDecl.BaseType != null || typeDecl.HasInterfaces || !typeDecl.IsSealed && !AsmViewHelper.IsEnum(typeDecl) && !AsmViewHelper.IsDelegate(typeDecl) && !AsmViewHelper.IsValueType(typeDecl) || typeDecl.HasNestedTypes || typeDecl.HasMethods || typeDecl.HasFields || typeDecl.HasProperties)
				{
					return true;
				}
				return typeDecl.HasEvents;
			}
			if (obj is EventDefinition)
			{
				EventDefinition evtDecl = (EventDefinition)obj;
				if (evtDecl.AddMethod != null || evtDecl.RemoveMethod != null)
				{
					return true;
				}
				return evtDecl.InvokeMethod != null;
			}
			if (!(obj is PropertyDefinition))
			{
				return false;
			}
			PropertyDefinition propDecl = (PropertyDefinition)obj;
			if (propDecl.GetMethod != null)
			{
				return true;
			}
			return propDecl.SetMethod != null;
		}

		private static bool IsDelegate(TypeReference typeRef)
		{
			if (typeRef == null)
			{
				return false;
			}
			TypeDefinition typeDecl = typeRef.Resolve();
			if (typeDecl == null || typeDecl.BaseType == null)
			{
				return false;
			}
			if (typeDecl.BaseType.Name != "MulticastDelegate")
			{
				return false;
			}
			return typeDecl.BaseType.Namespace == "System";
		}

		private static bool IsEnum(TypeReference typeRef)
		{
			if (typeRef == null)
			{
				return false;
			}
			TypeDefinition typeDecl = typeRef.Resolve();
			if (typeDecl == null || typeDecl.BaseType == null)
			{
				return false;
			}
			if (typeDecl.BaseType.Name != "Enum")
			{
				return false;
			}
			return typeDecl.BaseType.Namespace == "System";
		}

		public static bool IsMutable(object obj)
		{
			if (obj is AssemblyNameReference && !(obj is AssemblyDefinition))
			{
				return true;
			}
			return false;
		}

		private static bool IsPublicVisible(MemberReference vis)
		{
			FieldReference reference = vis as FieldReference;
			if (reference != null)
			{
				FieldDefinition Definition = reference.Resolve();
				if (Definition == null)
				{
					return true;
				}
				switch ((ushort)(Definition.Attributes & FieldAttributes.FieldAccessMask))
				{
					case 0:
					case 1:
					case 2:
					case 3:
					{
						return false;
					}
					case 4:
					case 5:
					case 6:
					{
						return true;
					}
				}
			}
			MethodReference reference2 = vis as MethodReference;
			if (reference2 != null)
			{
				ArrayType declaringType = reference2.DeclaringType as ArrayType;
				if (declaringType != null)
				{
					return AsmViewHelper.IsPublicVisible(declaringType.ElementType);
				}
				MethodDefinition Definition2 = reference2.Resolve();
				if (Definition2 == null)
				{
					return true;
				}
				switch ((ushort)(Definition2.Attributes & MethodAttributes.MemberAccessMask))
				{
					case 0:
					case 1:
					case 2:
					case 3:
					{
						return false;
					}
					case 4:
					case 5:
					case 6:
					{
						return true;
					}
				}
			}
			PropertyReference reference3 = vis as PropertyReference;
			if (reference3 != null)
			{
				switch (AsmViewHelper.GetPropVisibility(reference3))
				{
					case MethodAttributes.CompilerControlled:
					case MethodAttributes.Private:
					case MethodAttributes.FamANDAssem:
					case MethodAttributes.Assem:
					{
						return false;
					}
					case MethodAttributes.Family:
					case MethodAttributes.FamORAssem:
					case MethodAttributes.Public:
					{
						return true;
					}
				}
			}
			EventReference reference4 = vis as EventReference;
			if (reference4 != null)
			{
				switch (AsmViewHelper.GetEvtVisibility(reference4))
				{
					case MethodAttributes.CompilerControlled:
					case MethodAttributes.Private:
					case MethodAttributes.FamANDAssem:
					case MethodAttributes.Assem:
					{
						return false;
					}
					case MethodAttributes.Family:
					case MethodAttributes.FamORAssem:
					case MethodAttributes.Public:
					{
						return true;
					}
				}
			}
			TypeReference reference5 = vis as TypeReference;
			if (reference5 != null)
			{
				switch (reference5.Resolve().Attributes & TypeAttributes.VisibilityMask)
				{
					case TypeAttributes.NotPublic:
					case TypeAttributes.NestedPrivate:
					case TypeAttributes.NestedAssembly:
					case TypeAttributes.NestedFamANDAssem:
					{
						return false;
					}
					case TypeAttributes.Public:
					case TypeAttributes.NestedPublic:
					case TypeAttributes.NestedFamily:
					case TypeAttributes.VisibilityMask:
					{
						return true;
					}
				}
			}
			throw new NotSupportedException();
		}

		private static bool IsStatic(EventReference prop)
		{
			MethodDefinition methodDefinition;
			MethodDefinition methodDefinition1;
			MethodDefinition methodDefinition2;
			bool flag = false;
			EventDefinition Definition = prop.Resolve();
			if (Definition != null)
			{
				MethodReference addMethod = Definition.AddMethod;
				if (addMethod == null)
				{
					methodDefinition = null;
				}
				else
				{
					methodDefinition = addMethod.Resolve();
				}
				MethodDefinition addDecl = methodDefinition;
				MethodReference removeMethod = Definition.RemoveMethod;
				if (removeMethod == null)
				{
					methodDefinition1 = null;
				}
				else
				{
					methodDefinition1 = removeMethod.Resolve();
				}
				MethodDefinition removeDecl = methodDefinition1;
				MethodReference invokeMethod = Definition.InvokeMethod;
				if (invokeMethod == null)
				{
					methodDefinition2 = null;
				}
				else
				{
					methodDefinition2 = invokeMethod.Resolve();
				}
				MethodDefinition invokeDecl = methodDefinition2;
				flag = flag | (addDecl == null ? false : addDecl.IsStatic);
				flag = flag | (removeDecl == null ? false : removeDecl.IsStatic);
				flag = flag | (invokeDecl == null ? false : invokeDecl.IsStatic);
			}
			return flag;
		}

		private static bool IsStatic(PropertyReference prop)
		{
			MethodDefinition methodDefinition;
			MethodDefinition methodDefinition1;
			bool flag = false;
			PropertyDefinition Definition = prop.Resolve();
			if (Definition != null)
			{
				MethodReference setMethod = Definition.SetMethod;
				if (setMethod == null)
				{
					methodDefinition = null;
				}
				else
				{
					methodDefinition = setMethod.Resolve();
				}
				MethodDefinition addDecl = methodDefinition;
				MethodReference getMethod = Definition.GetMethod;
				if (getMethod == null)
				{
					methodDefinition1 = null;
				}
				else
				{
					methodDefinition1 = getMethod.Resolve();
				}
				MethodDefinition getDecl = methodDefinition1;
				flag = flag | (addDecl == null ? false : addDecl.IsStatic);
				flag = flag | (getDecl == null ? false : getDecl.IsStatic);
			}
			return flag;
		}

		private static bool IsUnreadable(char chr)
		{
			ushort chrVal;
			char chr1 = chr;
			if (chr1 <= '\r')
			{
				if (chr1 != '\0')
				{
					switch (chr1)
					{
						case '\b':
						case '\t':
						case '\n':
						case '\r':
						{
							break;
						}
						case '\v':
						case '\f':
						{
							chrVal = chr;
							if (chrVal >= 32 && chrVal < 127)
							{
								return false;
							}
							return true;
						}
						default:
						{
							chrVal = chr;
							if (chrVal >= 32 && chrVal < 127)
							{
								return false;
							}
							return true;
						}
					}
				}
			}
			else if (chr1 != '\'' && chr1 != '\\')
			{
				chrVal = chr;
				if (chrVal >= 32 && chrVal < 127)
				{
					return false;
				}
				return true;
			}
			return true;
		}

		private static bool IsValueType(TypeReference typeRef)
		{
			if (typeRef == null)
			{
				return false;
			}
			TypeDefinition typeDecl = typeRef.Resolve();
			if (typeDecl == null || typeDecl.BaseType == null)
			{
				return false;
			}
			if (typeDecl.BaseType.Name != "ValueType")
			{
				return false;
			}
			return typeDecl.BaseType.Namespace == "System";
		}
	}
}