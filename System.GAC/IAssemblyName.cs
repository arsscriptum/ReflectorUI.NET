using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace System.GAC
{
	[Guid("CD193BC0-B4BC-11d2-9833-00C04FC31D2E")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IAssemblyName
	{
		[MethodImpl(MethodImplOptions.PreserveSig)]
		int BindToObject(ref Guid refIID, object pUnkSink, object pUnkContext, string szCodeBase, long llFlags, IntPtr pvReserved, uint cbReserved, out IntPtr ppv);

		[MethodImpl(MethodImplOptions.PreserveSig)]
		int Clone(out IAssemblyName pName);

		[MethodImpl(MethodImplOptions.PreserveSig)]
		int Finalize();

		[MethodImpl(MethodImplOptions.PreserveSig)]
		int GetDisplayName([Out] StringBuilder szDisplayName, ref uint pccDisplayName, ASM_DISPLAY_FLAGS dwDisplayFlags);

		[MethodImpl(MethodImplOptions.PreserveSig)]
		int GetName(ref uint lpcwBuffer, [Out] StringBuilder pwzName);

		[MethodImpl(MethodImplOptions.PreserveSig)]
		int GetProperty(ASM_NAME PropertyId, IntPtr pvProperty, ref uint pcbProperty);

		[MethodImpl(MethodImplOptions.PreserveSig)]
		int GetVersion(out uint pdwVersionHi, out uint pdwVersionLow);

		[MethodImpl(MethodImplOptions.PreserveSig)]
		int IsEqual(IAssemblyName pName, ASM_CMP_FLAGS dwCmpFlags);

		[MethodImpl(MethodImplOptions.PreserveSig)]
		int SetProperty(ASM_NAME PropertyId, IntPtr pvProperty, uint cbProperty);
	}
}