using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.GAC
{
	[Guid("e707dcde-d1cd-11d2-bab9-00c04f8eceae")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IAssemblyCache
	{
		[MethodImpl(MethodImplOptions.PreserveSig)]
		int CreateAssemblyCacheItem(uint dwFlags, IntPtr pvReserved, out IAssemblyCacheItem ppAsmItem, string pszAssemblyName);

		[MethodImpl(MethodImplOptions.PreserveSig)]
		int CreateAssemblyScavenger(out object ppAsmScavenger);

		[MethodImpl(MethodImplOptions.PreserveSig)]
		int InstallAssembly(uint dwFlags, string pszManifestFilePath, FUSION_INSTALL_REFERENCE[] pRefData);

		[MethodImpl(MethodImplOptions.PreserveSig)]
		int QueryAssemblyInfo(uint dwFlags, string pszAssemblyName, ref ASSEMBLY_INFO pAsmInfo);

		[MethodImpl(MethodImplOptions.PreserveSig)]
		int UninstallAssembly(uint dwFlags, string pszAssemblyName, FUSION_INSTALL_REFERENCE[] pRefData, out uint pulDisposition);
	}
}