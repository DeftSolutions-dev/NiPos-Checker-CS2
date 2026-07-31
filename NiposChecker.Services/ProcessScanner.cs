using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using NiposChecker.Models;

namespace NiposChecker.Services;

public static class ProcessScanner
{
	private struct RECT
	{
		public int Left;

		public int Top;

		public int Right;

		public int Bottom;
	}

	private delegate bool EnumWindowsProc(nint hWnd, nint p);

	private const int GWL_EXSTYLE = -20;

	private const uint WS_EX_TOPMOST = 8u;

	private const uint WS_EX_TRANSPARENT = 32u;

	private const uint WS_EX_LAYERED = 524288u;

	public static (List<ProcessItem> items, int total) Scan(CheatDatabase db)
	{
		List<ProcessItem> list = new List<ProcessItem>();
		List<(int, string, string)> list2 = new List<(int, string, string)>();
		try
		{
			using ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher("SELECT ProcessId,Name,ExecutablePath FROM Win32_Process");
			foreach (ManagementObject item4 in managementObjectSearcher.Get())
			{
				int item = ToInt(item4["ProcessId"]);
				string item2 = item4["Name"]?.ToString() ?? "";
				string item3 = item4["ExecutablePath"]?.ToString();
				list2.Add((item, item2, item3));
			}
		}
		catch
		{
		}
		Dictionary<int, List<string>> dictionary = EstablishedByPid();
		bool exact;
		foreach (var (num, text, text2) in list2)
		{
			if (num <= 4 || db == null)
			{
				continue;
			}
			string text3 = null;
			if (db.NameLooksLikeCheat(text, out exact))
			{
				text3 = "Имя процесса совпадает с базой читов";
			}
			else if (!string.IsNullOrEmpty(text2) && IsUserPath(text2) && File.Exists(text2) && db.Evaluate(text2) != null)
			{
				text3 = "Процесс опознан по содержимому (правило/подпись/хеш)";
			}
			if (text3 != null)
			{
				if (dictionary.TryGetValue(num, out var value) && value.Count > 0)
				{
					text3 = text3 + " · соединения: " + string.Join(", ", value.Take(3));
				}
				list.Add(new ProcessItem
				{
					Name = text,
					Pid = num.ToString(),
					Path = (text2 ?? "—"),
					Note = text3,
					Level = "alert"
				});
			}
		}
		(int, string, string) tuple2 = list2.FirstOrDefault<(int, string, string)>(((int pid, string name, string path) p) => p.name.Equals("cs2.exe", StringComparison.OrdinalIgnoreCase));
		if (tuple2.Item1 > 0)
		{
			foreach (string item5 in ModulesOf(tuple2.Item1))
			{
				if (string.IsNullOrEmpty(item5))
				{
					continue;
				}
				string fileName = Path.GetFileName(item5);
				string text4 = null;
				string note = null;
				if (db != null && db.NameLooksLikeCheat(fileName, out exact))
				{
					text4 = "alert";
					note = "Модуль в cs2 совпадает с базой читов";
				}
				else if (!InTrustedLocation(item5))
				{
					if (!File.Exists(item5))
					{
						text4 = "alert";
						note = "Модуль загружен в cs2, но файла на диске нет — инъекция/подчистка";
					}
					else if (db != null && db.Evaluate(item5) != null)
					{
						text4 = "alert";
						note = "Модуль в cs2 опознан по содержимому (правило/подпись/хеш)";
					}
					else if (!Authenticode.IsSigned(item5))
					{
						if (IsHotPath(item5))
						{
							text4 = "alert";
							note = "Неподписанный модуль в cs2 из Temp/Downloads — инъекция";
						}
						else
						{
							text4 = "warn";
							note = "Неподписанный модуль в cs2 вне системных папок";
						}
					}
				}
				if (text4 != null)
				{
					list.Add(new ProcessItem
					{
						Name = fileName,
						Pid = tuple2.Item1.ToString(),
						Path = item5,
						Note = note,
						Level = text4
					});
				}
			}
		}
		if (tuple2.Item1 > 0)
		{
			try
			{
				ScanOverlays(tuple2.Item1, list2, list);
			}
			catch
			{
			}
		}
		if (db != null)
		{
			try
			{
				foreach (string item6 in Directory.EnumerateFiles("\\\\.\\pipe\\"))
				{
					string fileName2 = Path.GetFileName(item6);
					if (fileName2.Length >= 4 && db.NameLooksLikeCheat(fileName2, out exact))
					{
						list.Add(new ProcessItem
						{
							Name = fileName2,
							Pid = "—",
							Path = "\\\\.\\pipe\\" + fileName2,
							Note = "Именованный канал совпадает с базой читов",
							Level = "alert"
						});
					}
				}
			}
			catch
			{
			}
		}
		return (items: (from i in list
			orderby i.Rank, i.Name
			select i).ToList(), total: list2.Count);
	}

	[DllImport("user32.dll")]
	private static extern bool EnumWindows(EnumWindowsProc cb, nint p);

	[DllImport("user32.dll")]
	private static extern bool IsWindowVisible(nint h);

	[DllImport("user32.dll")]
	private static extern int GetWindowLong(nint h, int idx);

	[DllImport("user32.dll")]
	private static extern bool GetWindowRect(nint h, out RECT r);

	[DllImport("user32.dll")]
	private static extern uint GetWindowThreadProcessId(nint h, out uint pid);

	private static void ScanOverlays(int cs2Pid, List<(int pid, string name, string path)> procs, List<ProcessItem> items)
	{
		int self = 0;
		try
		{
			self = Process.GetCurrentProcess().Id;
		}
		catch
		{
		}
		HashSet<int> flagged = new HashSet<int>();
		EnumWindows(delegate(nint hWnd, nint _)
		{
			try
			{
				if (!IsWindowVisible(hWnd))
				{
					return true;
				}
				uint windowLong = (uint)GetWindowLong(hWnd, -20);
				if ((windowLong & 0x80000) == 0 || (windowLong & 0x20) == 0 || (windowLong & 8) == 0)
				{
					return true;
				}
				if (!GetWindowRect(hWnd, out var r))
				{
					return true;
				}
				int num = r.Right - r.Left;
				int num2 = r.Bottom - r.Top;
				if (num < 400 || num2 < 300)
				{
					return true;
				}
				GetWindowThreadProcessId(hWnd, out var pid);
				int pid2 = (int)pid;
				if (pid2 <= 4 || pid2 == cs2Pid || pid2 == self || !flagged.Add(pid2))
				{
					return true;
				}
				(int, string, string) tuple = procs.FirstOrDefault(((int pid, string name, string path) p) => p.pid == pid2);
				if (string.IsNullOrEmpty(tuple.Item3))
				{
					return true;
				}
				if (InTrustedLocation(tuple.Item3) && Authenticode.IsSigned(tuple.Item3))
				{
					return true;
				}
				items.Add(new ProcessItem
				{
					Name = tuple.Item2,
					Pid = pid2.ToString(),
					Path = tuple.Item3,
					Note = "Прозрачный оверлей поверх игры (click-through, topmost) от чужого процесса — возможен внешний ESP",
					Level = "alert"
				});
			}
			catch
			{
			}
			return true;
		}, IntPtr.Zero);
	}

	private static Dictionary<int, List<string>> EstablishedByPid()
	{
		Dictionary<int, List<string>> dictionary = new Dictionary<int, List<string>>();
		try
		{
			using Process process = Process.Start(new ProcessStartInfo("netstat", "-ano")
			{
				RedirectStandardOutput = true,
				UseShellExecute = false,
				CreateNoWindow = true
			});
			if (process == null)
			{
				return dictionary;
			}
			string text = process.StandardOutput.ReadToEnd();
			if (!process.WaitForExit(6000))
			{
				try
				{
					process.Kill();
				}
				catch
				{
				}
				return dictionary;
			}
			string[] array = text.Split('\n');
			for (int i = 0; i < array.Length; i++)
			{
				string text2 = array[i].Trim();
				if (!text2.StartsWith("TCP", StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}
				string[] array2 = Regex.Split(text2, "\\s+");
				if (array2.Length < 5 || !array2[3].Equals("ESTABLISHED", StringComparison.OrdinalIgnoreCase) || !int.TryParse(array2[4], out var result))
				{
					continue;
				}
				string text3 = array2[2];
				if (!text3.StartsWith("127.") && !text3.StartsWith("[::1]") && !text3.StartsWith("0.0.0.0") && !text3.StartsWith("[::]"))
				{
					if (!dictionary.TryGetValue(result, out var value))
					{
						value = (dictionary[result] = new List<string>());
					}
					if (!value.Contains(text3))
					{
						value.Add(text3);
					}
				}
			}
		}
		catch
		{
		}
		return dictionary;
	}

	private static IEnumerable<string> ModulesOf(int pid)
	{
		List<string> list = new List<string>();
		try
		{
			using ManagementObject managementObject = new ManagementObject($"Win32_Process.Handle='{pid}'");
			foreach (ManagementObject item in managementObject.GetRelated("CIM_DataFile"))
			{
				string text = item["Name"]?.ToString();
				if (!string.IsNullOrEmpty(text))
				{
					list.Add(text);
				}
			}
		}
		catch
		{
		}
		return list;
	}

	private static bool InTrustedLocation(string p)
	{
		string text = p.ToLowerInvariant();
		if (!text.Contains("\\windows\\") && !text.Contains("\\program files") && !text.Contains("\\programdata\\microsoft\\") && !text.Contains("\\steam\\"))
		{
			return text.Contains("\\steamapps\\");
		}
		return true;
	}

	private static bool IsUserPath(string p)
	{
		if (string.IsNullOrEmpty(p))
		{
			return false;
		}
		string text = p.ToLowerInvariant();
		if (!text.Contains("\\users\\") && !text.Contains("\\downloads\\") && !text.Contains("\\temp\\") && !text.Contains("\\appdata\\"))
		{
			return text.Contains("\\desktop\\");
		}
		return true;
	}

	private static bool IsHotPath(string p)
	{
		string text = p.ToLowerInvariant();
		if (text.Contains("\\temp\\") || text.Contains("\\downloads\\") || text.Contains("\\desktop\\"))
		{
			return true;
		}
		string[] array = text.Split('\\');
		foreach (string text2 in array)
		{
			if (text2.StartsWith(".") || text2.StartsWith("tmp_"))
			{
				return true;
			}
		}
		return false;
	}

	private static int ToInt(object o)
	{
		try
		{
			return Convert.ToInt32(o);
		}
		catch
		{
			return 0;
		}
	}
}
