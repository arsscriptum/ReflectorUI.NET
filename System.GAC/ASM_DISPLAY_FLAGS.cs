using System;

namespace System.GAC
{
	[Flags]
	public enum ASM_DISPLAY_FLAGS
	{
		VERSION = 1,
		CULTURE = 2,
		PUBLIC_KEY_TOKEN = 4,
		PUBLIC_KEY = 8,
		CUSTOM = 16,
		PROCESSORARCHITECTURE = 32,
		LANGUAGEID = 64
	}
}