using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace System.GAC
{
	public class AssemblyCache
	{
		public AssemblyCache()
		{
		}

		public static IAssemblyCache CreateAssemblyCache()
		{
			return AssemblyCache.Native.CreateAssemblyCache(0);
		}

		public static IAssemblyEnum CreateGACEnum()
		{
			return AssemblyCache.Native.CreateAssemblyEnum(IntPtr.Zero, null, ASM_CACHE_FLAGS.ASM_CACHE_GAC, IntPtr.Zero);
		}

		public static CultureInfo GetCulture(IAssemblyName name)
		{
			uint bufferSize = 255;
			IntPtr buffer = Marshal.AllocHGlobal((int)bufferSize);
			name.GetProperty(ASM_NAME.ASM_NAME_CULTURE, buffer, ref bufferSize);
			string result = Marshal.PtrToStringAuto(buffer);
			Marshal.FreeHGlobal(buffer);
			return new CultureInfo(result);
		}

		public static string GetDisplayName(IAssemblyName name, ASM_DISPLAY_FLAGS which)
		{
			uint bufferSize = 255;
			StringBuilder buffer = new StringBuilder((int)bufferSize);
			name.GetDisplayName(buffer, ref bufferSize, which);
			return buffer.ToString();
		}

		public static string GetName(IAssemblyName name)
		{
			uint bufferSize = 255;
			StringBuilder buffer = new StringBuilder((int)bufferSize);
			name.GetName(ref bufferSize, buffer);
			return buffer.ToString();
		}

		public static int GetNextAssembly(IAssemblyEnum enumerator, out IAssemblyName name)
		{
			return enumerator.GetNextAssembly((IntPtr)0, out name, 0);
		}

		public static byte[] GetPublicKey(IAssemblyName name)
		{
			unsafe // need ubsafe code in compilacion
			{
				uint bufferSize = 512;
				IntPtr buffer = Marshal.AllocHGlobal((int)bufferSize);
				name.GetProperty(ASM_NAME.ASM_NAME_PUBLIC_KEY, buffer, ref bufferSize);
				byte[] result = new byte[bufferSize];
				//for (int i = 0; (long)i < (ulong)bufferSize; i++)
                for (int i = 0; (long)i < (long)bufferSize; i++)
                {
                        result[i] = Marshal.ReadByte(buffer, i);
				}
				Marshal.FreeHGlobal(buffer);
				return result;
			}
		}

		public static byte[] GetPublicKeyToken(IAssemblyName name)
		{
			byte[] result = new byte[8];
			uint bufferSize = 8;
			IntPtr buffer = Marshal.AllocHGlobal((int)bufferSize);
			name.GetProperty(ASM_NAME.ASM_NAME_PUBLIC_KEY_TOKEN, buffer, ref bufferSize);
			for (int i = 0; i < 8; i++)
			{
				result[i] = Marshal.ReadByte(buffer, i);
			}
			Marshal.FreeHGlobal(buffer);
			return result;
		}

		public static Version GetVersion(IAssemblyName name)
		{
			uint major;
			uint minor;
			name.GetVersion(out major, out minor);
			return new Version((int)(major >> 16), (int)(major & 65535), (int)(minor >> 16), (int)(minor & 65535));
		}

		private static class Native
		{
			private static IntPtr fusion;

			static Native()
			{
				string str = Environment.ExpandEnvironmentVariables("%systemroot%\\Microsoft.NET");
				string bit64 = Path.Combine(str, "Framework64");
				string bit32 = Path.Combine(str, "Framework");
				if (Marshal.SizeOf(typeof(IntPtr)) != 8 || !Directory.Exists(bit64))
				{
					AssemblyCache.Native.fusion = AssemblyCache.Native.LoadLibrary(AssemblyCache.Native.GetFusion(bit32));
				}
				else
				{
					string ret = AssemblyCache.Native.GetFusion(bit64);
					if (ret != null)
					{
						AssemblyCache.Native.fusion = AssemblyCache.Native.LoadLibrary(ret);
						return;
					}
				}
			}

			[DllImport("fusion.dll", CharSet=CharSet.None, ExactSpelling=false, PreserveSig=false, SetLastError=true)]
			private static extern void CreateAssemblyCache(out IAssemblyCache ppAsmCache, uint dwReserved);

			public static IAssemblyCache CreateAssemblyCache(uint dwReserved)
			{
				IAssemblyCache ret;
				if (typeof(Marshal).GetMethod("GetDelegateForFunctionPointer") == null)
				{
					AssemblyCache.Native.CreateAssemblyCache(out ret, dwReserved);
				}
				else
				{
					IntPtr proc = AssemblyCache.Native.GetProcAddress(AssemblyCache.Native.fusion, "CreateAssemblyCache");
					AssemblyCache.Native._CreateAssemblyCache dele = (AssemblyCache.Native._CreateAssemblyCache)Marshal.GetDelegateForFunctionPointer(proc, typeof(AssemblyCache.Native._CreateAssemblyCache));
					dele(out ret, dwReserved);
				}
				return ret;
			}

			[DllImport("fusion.dll", CharSet=CharSet.None, ExactSpelling=false, PreserveSig=false, SetLastError=true)]
			private static extern void CreateAssemblyEnum(out IAssemblyEnum pEnum, IntPtr pUnkReserved, IAssemblyName pName, ASM_CACHE_FLAGS dwFlags, IntPtr pvReserved);

			public static IAssemblyEnum CreateAssemblyEnum(IntPtr pUnkReserved, IAssemblyName pName, ASM_CACHE_FLAGS dwFlags, IntPtr pvReserved)
			{
				IAssemblyEnum ret;
				if (typeof(Marshal).GetMethod("GetDelegateForFunctionPointer") == null)
				{
					AssemblyCache.Native.CreateAssemblyEnum(out ret, pUnkReserved, pName, dwFlags, pvReserved);
				}
				else
				{
					IntPtr proc = AssemblyCache.Native.GetProcAddress(AssemblyCache.Native.fusion, "CreateAssemblyEnum");
					AssemblyCache.Native._CreateAssemblyEnum dele = (AssemblyCache.Native._CreateAssemblyEnum)Marshal.GetDelegateForFunctionPointer(proc, typeof(AssemblyCache.Native._CreateAssemblyEnum));
					dele(out ret, pUnkReserved, pName, dwFlags, pvReserved);
				}
				return ret;
			}

			private static string GetFusion(string root)
			{
				Version version = null;
				string ret = null;
				string[] directories = Directory.GetDirectories(root, "v*");
				for (int i = 0; i < (int)directories.Length; i++)
				{
					string frms = directories[i];
					string path = Path.Combine(frms, "fusion.dll");
					if (File.Exists(path))
					{
						int idx = frms.LastIndexOf("v");
						Version ver = new Version(frms.Substring(idx + 1));
						if (version == null || ver > version)
						{
							version = ver;
							ret = path;
						}
					}
				}
				return ret;
			}

			[DllImport("kernel32.dll", CharSet=CharSet.Ansi, ExactSpelling=true, SetLastError=true)]
			private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

			[DllImport("kernel32.dll", CharSet=CharSet.None, ExactSpelling=false, SetLastError=true)]
			private static extern IntPtr LoadLibrary(string lpFileName);

			private delegate void _CreateAssemblyCache(out IAssemblyCache ppAsmCache, uint dwReserved);

			private delegate void _CreateAssemblyEnum(out IAssemblyEnum pEnum, IntPtr pUnkReserved, IAssemblyName pName, ASM_CACHE_FLAGS dwFlags, IntPtr pvReserved);
		}
	}
}