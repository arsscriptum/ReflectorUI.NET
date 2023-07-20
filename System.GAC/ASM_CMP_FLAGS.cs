using System;

namespace System.GAC
{
	[Flags]
	public enum ASM_CMP_FLAGS
	{
		NAME = 1,
		MAJOR_VERSION = 2,
		MINOR_VERSION = 4,
		BUILD_NUMBER = 8,
		REVISION_NUMBER = 16,
		PUBLIC_KEY_TOKEN = 32,
		CULTURE = 64,
		CUSTOM = 128,
		ALL = 255,
		DEFAULT = 256
	}
}