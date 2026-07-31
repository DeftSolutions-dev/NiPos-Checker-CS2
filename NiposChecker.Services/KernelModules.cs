using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace NiposChecker.Services;

public static class KernelModules
{
	private const int SystemModuleInformation = 11;

	private const uint STATUS_INFO_LENGTH_MISMATCH = 3221225476u;

	private const int ENTRY_SIZE = 284;

	private const int OFF_TONAME = 26;

	private const int OFF_PATH = 28;

	[DllImport("ntdll.dll")]
	private static extern uint NtQuerySystemInformation(int cls, nint buf, int len, out int retLen);

	public static List<(string name, string path)> List()
	{
		List<(string, string)> list = new List<(string, string)>();
		int num = 1048576;
		nint num2 = IntPtr.Zero;
		try
		{
			for (int i = 0; i < 6; i++)
			{
				num2 = Marshal.AllocHGlobal(num);
				int retLen;
				switch (NtQuerySystemInformation(11, num2, num, out retLen))
				{
				case 3221225476u:
					break;
				default:
					return list;
				case 0u:
				{
					int num3 = Marshal.ReadInt32(num2, 0);
					for (int j = 0; j < num3; j++)
					{
						int num4 = 4 + j * 284;
						if (num4 + 284 > num)
						{
							break;
						}
						string text = Marshal.PtrToStringAnsi(num2 + num4 + 28);
						if (!string.IsNullOrEmpty(text))
						{
							string text2 = Normalize(text);
							list.Add((Path.GetFileName(text2), text2));
						}
					}
					return list;
				}
				}
				Marshal.FreeHGlobal(num2);
				num2 = IntPtr.Zero;
				num = Math.Max(retLen, num * 2);
			}
		}
		catch
		{
		}
		finally
		{
			if (num2 != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(num2);
			}
		}
		return list;
	}

	private static string Normalize(string p)
	{
		try
		{
			if (p.StartsWith("\\SystemRoot\\", StringComparison.OrdinalIgnoreCase))
			{
				return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), p.Substring(12));
			}
			if (p.StartsWith("\\??\\"))
			{
				return p.Substring(4);
			}
			if (p.StartsWith("\\Windows\\", StringComparison.OrdinalIgnoreCase))
			{
				return (Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.Windows)) ?? "C:\\").TrimEnd('\\') + p;
			}
		}
		catch
		{
		}
		return p;
	}
}
