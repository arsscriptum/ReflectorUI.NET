using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.GAC
{
	[Guid("21b8916c-f28e-11d2-a473-00c04f8ef448")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IAssemblyEnum
	{
		[MethodImpl(MethodImplOptions.PreserveSig)]
		int Clone(out IAssemblyEnum ppEnum);

		[MethodImpl(MethodImplOptions.PreserveSig)]
		int GetNextAssembly(IntPtr pvReserved, out IAssemblyName ppName, uint dwFlags);

		[MethodImpl(MethodImplOptions.PreserveSig)]
		int Reset();
	}
}