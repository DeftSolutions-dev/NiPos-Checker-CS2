using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using NiposChecker.Models;

namespace NiposChecker.Services;

public static class UsnJournal
{
	private struct USN_JOURNAL_DATA_V0
	{
		public ulong UsnJournalID;

		public long FirstUsn;

		public long NextUsn;

		public long LowestValidUsn;

		public long MaxUsn;

		public ulong MaximumSize;

		public ulong AllocationDelta;
	}

	private struct READ_USN_JOURNAL_DATA_V0
	{
		public long StartUsn;

		public uint ReasonMask;

		public uint ReturnOnlyOnClose;

		public ulong Timeout;

		public ulong BytesToWaitFor;

		public ulong UsnJournalID;
	}

	private struct FILE_ID_DESCRIPTOR
	{
		public uint dwSize;

		public int Type;

		public long FileId;

		public long FileIdHi;
	}

	private const uint FSCTL_QUERY_USN_JOURNAL = 590068u;

	private const uint FSCTL_READ_USN_JOURNAL = 590011u;

	private const uint USN_REASON_RENAME_OLD_NAME = 4096u;

	private const uint USN_REASON_RENAME_NEW_NAME = 8192u;

	private const uint GENERIC_READ = 2147483648u;

	private const uint FILE_SHARE_ALL = 7u;

	private const uint OPEN_EXISTING = 3u;

	private const uint FILE_FLAG_BACKUP_SEMANTICS = 33554432u;

	private static readonly string[] _noiseExt = new string[17]
	{
		".tmp", ".temp", ".log", ".old", ".bak", ".etl", ".dmp", ".part", ".partial", ".crdownload",
		".download", ".swp", ".swx", ".lock", ".ldb", ".pma", ".~tmp"
	};

	private static readonly Regex _rxTmpHex = new Regex("\\.tmp[-_.][0-9a-f]*$|~[0-9a-f]{4,}$|^[0-9a-f]{8,}\\.tmp$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex _rxLogRot = new Regex("\\.log[.\\-_]?\\d*$|\\.\\d+\\.(log|txt|etl)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex _rxGuid = new Regex("[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex _rxRenderSeg = new Regex("\\._\\d+_$", RegexOptions.Compiled);

	private static readonly Regex _rxNumSeg = new Regex("\\.\\d{3,}\\.\\d{3,}\\.\\w+$", RegexOptions.Compiled);

	private static readonly string[] _noiseNames = new string[17]
	{
		"temp-index", "the-real-index", "index-dir", "index", "data_0", "data_1", "data_2", "data_3", "manifest-000001", "current",
		"lock", "log", "log.old", "crx_install", "heartbeat", "temp", "new"
	};

	private static readonly string[] _noiseDirs = new string[24]
	{
		"\\cache\\", "\\cache2\\", "\\cachestorage\\", "\\service worker\\", "\\code cache\\", "\\gpucache\\", "\\dawncache\\", "\\graphitedawncache\\", "\\indexeddb\\", "\\blob_storage\\",
		"\\shadercache\\", "\\grshadercache\\", "\\component_crx_cache\\", "\\media cache\\", "\\inetcache\\", "\\thumbnails\\", "\\webcache\\", "\\tdata\\", "\\safe browsing\\", "\\extensions\\",
		"\\obj\\", "\\.git\\", "\\node_modules\\", "\\crashpad\\"
	};

	[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern SafeFileHandle CreateFile(string name, uint access, uint share, nint sec, uint creation, uint flags, nint template);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool DeviceIoControl(SafeFileHandle h, uint code, nint inBuf, int inSize, nint outBuf, int outSize, out int bytesReturned, nint overlapped);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern SafeFileHandle OpenFileById(SafeFileHandle volumeHint, ref FILE_ID_DESCRIPTOR id, uint access, uint share, nint sec, uint flags);

	[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern int GetFinalPathNameByHandle(SafeFileHandle h, StringBuilder path, int cch, uint flags);

	private static bool IsNoiseName(string n)
	{
		if (string.IsNullOrEmpty(n))
		{
			return true;
		}
		string text = n.ToLowerInvariant();
		string[] noiseExt = _noiseExt;
		foreach (string value in noiseExt)
		{
			if (text.EndsWith(value))
			{
				return true;
			}
		}
		noiseExt = _noiseNames;
		foreach (string text2 in noiseExt)
		{
			if (text == text2)
			{
				return true;
			}
		}
		if (text.StartsWith("todelete_") || text.StartsWith("scoped_dir"))
		{
			return true;
		}
		if (text.EndsWith("~"))
		{
			return true;
		}
		if (text.EndsWith(".store"))
		{
			return true;
		}
		string text3 = StripLastExt(text);
		if (text3.EndsWith("-new") || text3.EndsWith("_new") || text3.EndsWith("-old") || text3.EndsWith("_old") || text3.EndsWith("-bak"))
		{
			return true;
		}
		if (text.Contains(".tmp-") || text.Contains(".tmp.") || text.Contains("~tmp"))
		{
			return true;
		}
		if (text.Contains(".temp_") || text.Contains(".temp-") || text.Contains(".temp."))
		{
			return true;
		}
		if (text.StartsWith("~$") || text.StartsWith("~wrl"))
		{
			return true;
		}
		if (text.EndsWith("-wal") || text.EndsWith("-shm") || text.EndsWith("-journal"))
		{
			return true;
		}
		if (_rxTmpHex.IsMatch(text) || _rxLogRot.IsMatch(text))
		{
			return true;
		}
		if (_rxGuid.IsMatch(text) || _rxRenderSeg.IsMatch(text) || _rxNumSeg.IsMatch(text))
		{
			return true;
		}
		if (IsHexStem(text))
		{
			return true;
		}
		return false;
	}

	private static string StripLastExt(string n)
	{
		int num = n.LastIndexOf('.');
		if (num <= 0)
		{
			return n;
		}
		return n.Substring(0, num);
	}

	private static bool IsAtomicSave(string a, string b)
	{
		if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
		{
			return false;
		}
		string text = a.ToLowerInvariant();
		string text2 = b.ToLowerInvariant();
		if (!(StripLastExt(text) == text2))
		{
			return StripLastExt(text2) == text;
		}
		return true;
	}

	private static bool IsHexStem(string l)
	{
		int num = l.LastIndexOf('.');
		string text = ((num > 0) ? l.Substring(0, num) : l);
		if (text.Length < 12)
		{
			return false;
		}
		string text2 = text;
		foreach (char c in text2)
		{
			if ((c < '0' || c > '9') && (c < 'a' || c > 'f') && c != '_' && c != '-')
			{
				return false;
			}
		}
		return true;
	}

	private static bool IsNoisePath(string path)
	{
		if (string.IsNullOrEmpty(path))
		{
			return false;
		}
		string text = path.ToLowerInvariant();
		string[] noiseDirs = _noiseDirs;
		foreach (string value in noiseDirs)
		{
			if (text.Contains(value))
			{
				return true;
			}
		}
		return false;
	}

	private static bool IsNoiseRename(string oldN, string newN)
	{
		if (!IsNoiseName(oldN) && !IsNoiseName(newN))
		{
			return IsAtomicSave(oldN, newN);
		}
		return true;
	}

	public static List<RenameEvent> GetRenames(int maxPerVolume = 500, bool includeNoise = false, int sinceDays = 14)
	{
		long cutoff = 0L;
		try
		{
			cutoff = DateTime.UtcNow.AddDays(-Math.Max(1, sinceDays)).ToFileTimeUtc();
		}
		catch
		{
		}
		string b = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.Windows)) ?? "C:\\";
		List<char> list = new List<char>();
		DriveInfo[] drives = DriveInfo.GetDrives();
		foreach (DriveInfo driveInfo in drives)
		{
			try
			{
				if (driveInfo.DriveType != DriveType.Fixed || !driveInfo.IsReady || !string.Equals(driveInfo.DriveFormat, "NTFS", StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}
				bool flag = string.Equals(driveInfo.Name, b, StringComparison.OrdinalIgnoreCase);
				if (!flag)
				{
					try
					{
						flag = Directory.Exists(driveInfo.Name + "Users");
					}
					catch
					{
					}
				}
				if (flag)
				{
					list.Add(driveInfo.Name[0]);
				}
			}
			catch
			{
			}
		}
		ConcurrentBag<RenameEvent> bag = new ConcurrentBag<RenameEvent>();
		ParallelOptions parallelOptions = new ParallelOptions
		{
			MaxDegreeOfParallelism = Math.Min(2, Math.Max(1, Environment.ProcessorCount - 2))
		};
		Parallel.ForEach(list, parallelOptions, delegate(char letter)
		{
			try
			{
				List<RenameEvent> list3 = new List<RenameEvent>();
				ReadVolume(letter, list3, maxPerVolume, includeNoise, cutoff);
				foreach (RenameEvent item in list3)
				{
					bag.Add(item);
				}
			}
			catch
			{
			}
		});
		List<RenameEvent> list2 = new List<RenameEvent>(bag);
		list2.Sort((RenameEvent a, RenameEvent renameEvent) => renameEvent.WhenRaw.CompareTo(a.WhenRaw));
		return list2;
	}

	private static void ReadVolume(char letter, List<RenameEvent> result, int maxEvents, bool includeNoise, long cutoffFileTime)
	{
		using SafeFileHandle safeFileHandle = CreateFile($"\\\\.\\{letter}:", 2147483648u, 7u, IntPtr.Zero, 3u, 0u, IntPtr.Zero);
		if (safeFileHandle.IsInvalid)
		{
			return;
		}
		int num = Marshal.SizeOf<USN_JOURNAL_DATA_V0>();
		nint num2 = Marshal.AllocHGlobal(num);
		USN_JOURNAL_DATA_V0 uSN_JOURNAL_DATA_V;
		try
		{
			if (!DeviceIoControl(safeFileHandle, 590068u, IntPtr.Zero, 0, num2, num, out var _, IntPtr.Zero))
			{
				return;
			}
			uSN_JOURNAL_DATA_V = Marshal.PtrToStructure<USN_JOURNAL_DATA_V0>(num2);
		}
		finally
		{
			Marshal.FreeHGlobal(num2);
		}
		READ_USN_JOURNAL_DATA_V0 structure = new READ_USN_JOURNAL_DATA_V0
		{
			StartUsn = uSN_JOURNAL_DATA_V.FirstUsn,
			ReasonMask = 12288u,
			ReturnOnlyOnClose = 0u,
			Timeout = 0uL,
			BytesToWaitFor = 0uL,
			UsnJournalID = uSN_JOURNAL_DATA_V.UsnJournalID
		};
		int num3 = Marshal.SizeOf<READ_USN_JOURNAL_DATA_V0>();
		nint num4 = Marshal.AllocHGlobal(num3);
		nint num5 = Marshal.AllocHGlobal(262144);
		Dictionary<long, string> dictionary = new Dictionary<long, string>();
		List<(long, string, string, long)> list = new List<(long, string, string, long)>();
		int num6 = 0;
		try
		{
			while (num6++ < 20000)
			{
				Marshal.StructureToPtr(structure, num4, fDeleteOld: false);
				if (!DeviceIoControl(safeFileHandle, 590011u, num4, num3, num5, 262144, out var bytesReturned2, IntPtr.Zero) || bytesReturned2 <= 8)
				{
					break;
				}
				long num7 = Marshal.ReadInt64(num5, 0);
				int num9;
				for (int i = 8; i < bytesReturned2; i += num9)
				{
					nint num8 = num5 + i;
					num9 = Marshal.ReadInt32(num8, 0);
					if (num9 <= 0)
					{
						break;
					}
					long num10 = Marshal.ReadInt64(num8, 8);
					long num11 = Marshal.ReadInt64(num8, 32);
					uint num12 = (uint)Marshal.ReadInt32(num8, 40);
					ushort num13 = (ushort)Marshal.ReadInt16(num8, 56);
					ushort num14 = (ushort)Marshal.ReadInt16(num8, 58);
					string text = ((num13 > 0) ? Marshal.PtrToStringUni(num8 + num14, num13 / 2) : "");
					if ((num12 & 0x2000) != 0)
					{
						if (!dictionary.TryGetValue(num10, out var value))
						{
							continue;
						}
						dictionary.Remove(num10);
						if (num11 >= cutoffFileTime && !string.IsNullOrEmpty(value) && !string.Equals(value, text, StringComparison.OrdinalIgnoreCase) && (includeNoise || !IsNoiseRename(value, text)))
						{
							list.Add((num10, value, text, num11));
							if (list.Count > maxEvents * 4)
							{
								list.RemoveRange(0, list.Count - maxEvents * 2);
							}
						}
					}
					else if ((num12 & 0x1000) != 0)
					{
						dictionary[num10] = text;
					}
				}
				if (num7 <= structure.StartUsn || num7 >= uSN_JOURNAL_DATA_V.NextUsn)
				{
					break;
				}
				structure.StartUsn = num7;
			}
		}
		finally
		{
			Marshal.FreeHGlobal(num4);
			Marshal.FreeHGlobal(num5);
		}
		int num15 = Math.Max(0, list.Count - maxEvents);
		for (int num16 = list.Count - 1; num16 >= num15; num16--)
		{
			(long, string, string, long) tuple = list[num16];
			RenameEvent renameEvent = BuildEvent(safeFileHandle, tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4);
			if (renameEvent != null && (includeNoise || !IsNoisePath(renameEvent.CurrentPath)))
			{
				result.Add(renameEvent);
			}
		}
	}

	private static RenameEvent BuildEvent(SafeFileHandle vol, long frn, string oldName, string newName, long ts)
	{
		RenameEvent renameEvent = new RenameEvent
		{
			OldName = oldName,
			NewName = newName
		};
		try
		{
			renameEvent.WhenRaw = DateTime.FromFileTimeUtc(ts).ToLocalTime();
			renameEvent.When = renameEvent.WhenRaw.ToString("dd.MM.yyyy HH:mm");
		}
		catch
		{
			renameEvent.When = "";
		}
		string text = ResolvePath(vol, frn);
		if (!string.IsNullOrEmpty(text) && string.Equals(Path.GetFileName(text), newName, StringComparison.OrdinalIgnoreCase))
		{
			renameEvent.CurrentPath = text;
			renameEvent.OnDisk = "есть";
		}
		else
		{
			renameEvent.CurrentPath = text ?? "";
			renameEvent.OnDisk = "нет";
		}
		return renameEvent;
	}

	private static string ResolvePath(SafeFileHandle vol, long frn)
	{
		FILE_ID_DESCRIPTOR id = new FILE_ID_DESCRIPTOR
		{
			dwSize = (uint)Marshal.SizeOf<FILE_ID_DESCRIPTOR>(),
			Type = 0,
			FileId = frn
		};
		try
		{
			using SafeFileHandle safeFileHandle = OpenFileById(vol, ref id, 0u, 7u, IntPtr.Zero, 33554432u);
			if (safeFileHandle.IsInvalid)
			{
				return null;
			}
			StringBuilder stringBuilder = new StringBuilder(1024);
			if (GetFinalPathNameByHandle(safeFileHandle, stringBuilder, stringBuilder.Capacity, 0u) <= 0)
			{
				return null;
			}
			string text = stringBuilder.ToString();
			if (text.StartsWith("\\\\?\\"))
			{
				text = text.Substring(4);
			}
			return text;
		}
		catch
		{
			return null;
		}
	}
}
