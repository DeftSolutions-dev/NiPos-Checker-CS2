using System;
using System.IO;
using System.Runtime.InteropServices;

namespace NiposChecker.Services;

public static class Authenticode
{
	private struct WINTRUST_FILE_INFO
	{
		public uint cbStruct;

		[MarshalAs(UnmanagedType.LPWStr)]
		public string pcwszFilePath;

		public nint hFile;

		public nint pgKnownSubject;
	}

	private struct WINTRUST_DATA
	{
		public uint cbStruct;

		public nint pPolicyCallbackData;

		public nint pSIPClientData;

		public uint dwUIChoice;

		public uint fdwRevocationChecks;

		public uint dwUnionChoice;

		public nint pFile;

		public uint dwStateAction;

		public nint hWVTStateData;

		public nint pwszURLReference;

		public uint dwProvFlags;

		public uint dwUIContext;
	}

	private static readonly Guid GENERIC_VERIFY_V2 = new Guid("00AAC56B-CD44-11d0-8CC2-00C04FC295EE");

	private const uint WTD_UI_NONE = 2u;

	private const uint WTD_REVOKE_NONE = 0u;

	private const uint WTD_CHOICE_FILE = 1u;

	private const uint WTD_STATEACTION_VERIFY = 1u;

	private const uint WTD_STATEACTION_CLOSE = 2u;

	private const uint WTD_SAFER_FLAG = 256u;

	private const uint WTD_REVOCATION_CHECK_NONE = 16u;

	[DllImport("wintrust.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
	private static extern int WinVerifyTrust(nint hwnd, [MarshalAs(UnmanagedType.LPStruct)] Guid pgActionID, ref WINTRUST_DATA pWVTData);

	public static bool IsSigned(string path)
	{
		if (string.IsNullOrEmpty(path) || !File.Exists(path))
		{
			return false;
		}
		WINTRUST_FILE_INFO structure = new WINTRUST_FILE_INFO
		{
			cbStruct = (uint)Marshal.SizeOf<WINTRUST_FILE_INFO>(),
			pcwszFilePath = path
		};
		nint num = Marshal.AllocHGlobal(Marshal.SizeOf<WINTRUST_FILE_INFO>());
		Marshal.StructureToPtr(structure, num, fDeleteOld: false);
		WINTRUST_DATA pWVTData = new WINTRUST_DATA
		{
			cbStruct = (uint)Marshal.SizeOf<WINTRUST_DATA>(),
			dwUIChoice = 2u,
			fdwRevocationChecks = 0u,
			dwUnionChoice = 1u,
			pFile = num,
			dwStateAction = 1u,
			dwProvFlags = 272u
		};
		try
		{
			int num2 = WinVerifyTrust(new IntPtr(-1), GENERIC_VERIFY_V2, ref pWVTData);
			pWVTData.dwStateAction = 2u;
			WinVerifyTrust(new IntPtr(-1), GENERIC_VERIFY_V2, ref pWVTData);
			return num2 == 0;
		}
		catch
		{
			return false;
		}
		finally
		{
			Marshal.FreeHGlobal(num);
		}
	}
}
