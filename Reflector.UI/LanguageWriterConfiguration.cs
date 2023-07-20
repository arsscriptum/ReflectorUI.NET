using System;
using System.Collections.Generic;
using Reflector.CodeModel;

namespace Reflector.UI
{
	internal class LanguageWriterConfiguration : ILanguageWriterConfiguration
	{
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

		public IVisibilityConfiguration Visibility
		{
			get
			{
				return LanguageWriterConfiguration.VisibilityConfiguration.Instance;
			}
		}

		public string this[string name]
		{
			get
			{
				return this.cfg[name];
			}
			set
			{
				this.cfg[name] = value;
			}
		}

		private Dictionary<string, string> cfg = new Dictionary<string, string>();
		public static readonly LanguageWriterConfiguration Instance = new LanguageWriterConfiguration();

		private class VisibilityConfiguration : IVisibilityConfiguration
		{
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

			public static LanguageWriterConfiguration.VisibilityConfiguration Instance = new LanguageWriterConfiguration.VisibilityConfiguration();
		}
	}
}
