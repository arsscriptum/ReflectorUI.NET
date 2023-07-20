using System;

namespace System.GAC
{
	public struct ASSEMBLY_INFO
	{
		public uint cbAssemblyInfo;

		public uint dwAssemblyFlags;

		public ulong uliAssemblySizeInKB;

		public string pszCurrentAssemblyPathBuf;

		public uint cchBuf;
	}
}