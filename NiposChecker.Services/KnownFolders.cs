using System;
using System.IO;
using System.Runtime.InteropServices;

namespace NiposChecker.Services;

public static class KnownFolders
{
	private static readonly Guid Downloads = new Guid("374DE290-123F-4565-9164-39C4925E467B");

	[DllImport("shell32.dll", CharSet = CharSet.Unicode)]
	private static extern int SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint dwFlags, nint hToken, out nint ppszPath);

	public static string GetDownloads()
	{
		try
		{
			if (SHGetKnownFolderPath(Downloads, 0u, IntPtr.Zero, out var ppszPath) == 0)
			{
				string text = Marshal.PtrToStringUni(ppszPath);
				Marshal.FreeCoTaskMem(ppszPath);
				if (!string.IsNullOrEmpty(text))
				{
					return text;
				}
			}
		}
		catch
		{
		}
		return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
	}
}
