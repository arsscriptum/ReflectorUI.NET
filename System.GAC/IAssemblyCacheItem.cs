using System;
using System.Runtime.InteropServices;

namespace System.GAC
{
	[Guid("9E3AAEB4-D1CD-11D2-BAB9-00C04F8ECEAE")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IAssemblyCacheItem
	{
		void AbortItem();

		void Commit(uint dwFlags, out long pulDisposition);

		void CreateStream(uint dwFlags, string pszStreamName, uint dwFormat, uint dwFormatFlags, out UCOMIStream ppIStream, ref long puliMaxSize);
	}
}