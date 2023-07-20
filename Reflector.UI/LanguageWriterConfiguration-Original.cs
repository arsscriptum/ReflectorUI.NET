using Reflector.CodeModel;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Reflector.UI
{
	internal class LanguageWriterConfiguration : ILanguageWriterConfiguration
	{
		private Dictionary<string, string> cfg = new Dictionary<string, string>();

		public readonly static LanguageWriterConfiguration Instance;

		public string Item(string name)
		{
			get
			{
				return JustDecompileGenerated_get_Item(name);
			}
			set
			{
				JustDecompileGenerated_set_Item(name, value);
			}
		}

		public string JustDecompileGenerated_get_Item(string name)
		{
			return this.cfg[name];
		}

		public void JustDecompileGenerated_set_Item(string name, string value)
		{
			this.cfg[name] = value;
		}

		public IVisibilityConfiguration Visibility
		{
			get
			{
				return LanguageWriterConfiguration.VisibilityConfiguration.Instance;
			}
		}

		static LanguageWriterConfiguration()
		{
			LanguageWriterConfiguration.Instance = new LanguageWriterConfiguration();
		}

		private LanguageWriterConfiguration()
		{
			this.cfg["NumberFormat"] = "Auto";
			this.cfg["ShowCustomAttributes"] = "true";
			this.cfg["ShowNamespaceImports"] = "false";
			this.cfg["ShowNamespaceBody"] = "true";
			this.cfg["ShowTypeDefinitionBody"] = "false";
			this.cfg["ShowMethodDefinitionBody"] = "false";
			this.cfg["ShowDocumentation"] = "false";
			this.cfg["Optimization"] = "3.5";
		}

		private class VisibilityConfiguration : IVisibilityConfiguration
		{
			public static LanguageWriterConfiguration.VisibilityConfiguration Instance;

			public bool Assembly
			{
				get
				{
					return true;
				}
			}

			public bool Family
			{
				get
				{
					return true;
				}
			}

			public bool FamilyAndAssembly
			{
				get
				{
					return true;
				}
			}

			public bool FamilyOrAssembly
			{
				get
				{
					return true;
				}
			}

			public bool Private
			{
				get
				{
					return true;
				}
			}

			public bool Public
			{
				get
				{
					return true;
				}
			}

			static VisibilityConfiguration()
			{
				LanguageWriterConfiguration.VisibilityConfiguration.Instance = new LanguageWriterConfiguration.VisibilityConfiguration();
			}

			public VisibilityConfiguration()
			{
			}
		}
	}
}