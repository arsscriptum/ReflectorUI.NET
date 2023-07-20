using System;

namespace System.GAC
{
	public struct FUSION_INSTALL_REFERENCE
	{
		public uint cbSize;

		public uint dwFlags;

		public Guid guidScheme;

		public string szIdentifier;

		public string szNonCannonicalData;
	}
}