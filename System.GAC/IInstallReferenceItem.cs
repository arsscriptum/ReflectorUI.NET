using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.GAC
{
	[Guid("582dac66-e678-449f-aba6-6faaec8a9394")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IInstallReferenceItem
	{
		[MethodImpl(MethodImplOptions.PreserveSig)]
		int GetReference(out FUSION_INSTALL_REFERENCE[] ppRefData, uint dwFlags, IntPtr pvReserved);
	}
}