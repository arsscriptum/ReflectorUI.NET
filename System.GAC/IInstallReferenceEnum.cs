using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.GAC
{
	[Guid("56b1a988-7c0c-4aa2-8639-c3eb5a90226f")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IInstallReferenceEnum
	{
		[MethodImpl(MethodImplOptions.PreserveSig)]
		int GetNextInstallReferenceItem(out IInstallReferenceItem ppRefItem, uint dwFlags, IntPtr pvReserved);
	}
}