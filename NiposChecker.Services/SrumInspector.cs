using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Isam.Esent.Interop;

namespace NiposChecker.Services;

internal static class SrumInspector
{
	public struct AppUsage
	{
		public string Name;

		public string Path;

		public long Sent;

		public long Recv;

		public DateTime Last;
	}

	public struct SrumSpan
	{
		public bool Ok;

		public DateTime Earliest;

		public DateTime Latest;

		public long Rows;

		public int GapDays;

		public List<AppUsage> Apps;
	}

	private const string NetTable = "{973F5D5C-1D90-4944-BE8E-24B94231A174}";

	public static SrumSpan Inspect()
	{
		SrumSpan result = default(SrumSpan);
		string text = Path.Combine(Path.GetTempPath(), "nipos_sru_" + Guid.NewGuid().ToString("N").Substring(0, 8));
		string id = null;
		try
		{
			Directory.CreateDirectory(text);
			string text2 = CreateShadow("C:\\", out id);
			if (text2 == null)
			{
				return result;
			}
			string srcFolder = text2 + "\\Windows\\System32\\sru";
			string text3 = Path.Combine(text, "sru");
			Directory.CreateDirectory(text3);
			if (!CopyFolder(srcFolder, text3))
			{
				return result;
			}
			string text4 = Path.Combine(text3, "SRUDB.dat");
			if (!File.Exists(text4))
			{
				return result;
			}
			string[] files = Directory.GetFiles(text3);
			foreach (string fileName in files)
			{
				try
				{
					new FileInfo(fileName).IsReadOnly = false;
				}
				catch
				{
				}
			}
			Recover(text3);
			if (!ReadDb(text4, out result.Earliest, out result.Latest, out result.Rows, out result.GapDays, out result.Apps))
			{
				return result;
			}
			result.Ok = true;
			return result;
		}
		catch
		{
			return result;
		}
		finally
		{
			if (id != null)
			{
				DeleteShadow(id);
			}
			try
			{
				Directory.Delete(text, recursive: true);
			}
			catch
			{
			}
		}
	}

	private static string CreateShadow(string volume, out string id)
	{
		id = null;
		try
		{
			using ManagementClass managementClass = new ManagementClass("Win32_ShadowCopy");
			ManagementBaseObject methodParameters = managementClass.GetMethodParameters("Create");
			methodParameters["Volume"] = volume;
			methodParameters["Context"] = "ClientAccessible";
			ManagementBaseObject managementBaseObject = managementClass.InvokeMethod("Create", methodParameters, null);
			if (managementBaseObject == null || Convert.ToInt32(managementBaseObject["ReturnValue"]) != 0)
			{
				return null;
			}
			id = (string)managementBaseObject["ShadowID"];
			using ManagementObject managementObject = new ManagementObject("Win32_ShadowCopy.ID=\"" + id + "\"");
			managementObject.Get();
			return (string)managementObject["DeviceObject"];
		}
		catch
		{
			id = null;
			return null;
		}
	}

	private static void DeleteShadow(string id)
	{
		try
		{
			using ManagementObject managementObject = new ManagementObject("Win32_ShadowCopy.ID=\"" + id + "\"");
			managementObject.Delete();
		}
		catch
		{
			try
			{
				Run("vssadmin.exe", "delete shadows /shadow=" + id + " /quiet", null);
			}
			catch
			{
			}
		}
	}

	private static bool CopyFolder(string srcFolder, string dstFolder)
	{
		Run("cmd.exe", $"/c copy \"{srcFolder}\\*\" \"{dstFolder}\\\"", null);
		return Directory.GetFiles(dstFolder).Any((string f) => f.EndsWith("SRUDB.dat", StringComparison.OrdinalIgnoreCase));
	}

	private static void Recover(string folder)
	{
		Run("esentutl.exe", "/r SRU /i /d", folder);
	}

	private static void Run(string exe, string args, string workDir)
	{
		try
		{
			ProcessStartInfo processStartInfo = new ProcessStartInfo(exe, args)
			{
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true
			};
			if (!string.IsNullOrEmpty(workDir))
			{
				processStartInfo.WorkingDirectory = workDir;
			}
			using Process process = Process.Start(processStartInfo);
			if (process != null)
			{
				process.StandardOutput.ReadToEnd();
				process.StandardError.ReadToEnd();
				process.WaitForExit(60000);
			}
		}
		catch
		{
		}
	}

	private static bool ReadDb(string dbPath, out DateTime earliest, out DateTime latest, out long rows, out int gapDays, out List<AppUsage> apps)
	{
		earliest = DateTime.MaxValue;
		latest = DateTime.MinValue;
		rows = 0L;
		gapDays = 0;
		apps = new List<AppUsage>();
		HashSet<DateTime> hashSet = new HashSet<DateTime>();
		Api.JetCreateInstance(out var instance, "nipos_srum");
		try
		{
			Api.JetSetSystemParameter(instance, JET_SESID.Nil, JET_param.Recovery, 0, "Off");
			Api.JetSetSystemParameter(instance, JET_SESID.Nil, JET_param.TempPath, 0, Path.GetTempPath());
			Api.JetInit(ref instance);
			Api.JetBeginSession(instance, out var sesid, null, null);
			try
			{
				Api.JetAttachDatabase(sesid, dbPath, AttachDatabaseGrbit.None);
			}
			catch (EsentErrorException)
			{
			}
			Api.JetOpenDatabase(sesid, dbPath, null, out var dbid, OpenDatabaseGrbit.ReadOnly);
			Dictionary<int, string> dictionary = new Dictionary<int, string>();
			try
			{
				Api.JetOpenTable(sesid, dbid, "SruDbIdMapTable", null, 0, OpenTableGrbit.ReadOnly, out var tableid);
				Dictionary<string, JET_COLUMNID> dictionary2 = ColMap(sesid, tableid);
				if (dictionary2.ContainsKey("IdIndex") && dictionary2.ContainsKey("IdBlob") && Api.TryMoveFirst(sesid, tableid))
				{
					do
					{
						int? num = Api.RetrieveColumnAsInt32(sesid, tableid, dictionary2["IdIndex"]);
						byte[] array = Api.RetrieveColumn(sesid, tableid, dictionary2["IdBlob"]);
						if (num.HasValue && array != null && array.Length != 0)
						{
							dictionary[num.Value] = Encoding.Unicode.GetString(array).TrimEnd('\0').Trim();
						}
					}
					while (Api.TryMoveNext(sesid, tableid));
				}
			}
			catch
			{
			}
			Api.JetOpenTable(sesid, dbid, "{973F5D5C-1D90-4944-BE8E-24B94231A174}", null, 0, OpenTableGrbit.ReadOnly, out var tableid2);
			Dictionary<string, JET_COLUMNID> dictionary3 = ColMap(sesid, tableid2);
			if (!dictionary3.ContainsKey("TimeStamp"))
			{
				return false;
			}
			JET_COLUMNID columnid = dictionary3["TimeStamp"];
			JET_COLUMNID value;
			JET_COLUMNID jET_COLUMNID = (dictionary3.TryGetValue("AppId", out value) ? value : JET_COLUMNID.Nil);
			JET_COLUMNID value2;
			JET_COLUMNID jET_COLUMNID2 = (dictionary3.TryGetValue("BytesSent", out value2) ? value2 : JET_COLUMNID.Nil);
			JET_COLUMNID value3;
			JET_COLUMNID jET_COLUMNID3 = (dictionary3.TryGetValue("BytesRecvd", out value3) ? value3 : JET_COLUMNID.Nil);
			Dictionary<int, AppUsage> dictionary4 = new Dictionary<int, AppUsage>();
			if (Api.TryMoveFirst(sesid, tableid2))
			{
				do
				{
					DateTime? dateTime = Api.RetrieveColumnAsDateTime(sesid, tableid2, columnid);
					if (!dateTime.HasValue || dateTime.Value.Year < 1990 || dateTime.Value.Year > 2100)
					{
						continue;
					}
					rows++;
					if (dateTime.Value < earliest)
					{
						earliest = dateTime.Value;
					}
					if (dateTime.Value > latest)
					{
						latest = dateTime.Value;
					}
					hashSet.Add(dateTime.Value.Date);
					if (jET_COLUMNID == JET_COLUMNID.Nil)
					{
						continue;
					}
					int? num2 = Api.RetrieveColumnAsInt32(sesid, tableid2, jET_COLUMNID);
					if (num2.HasValue)
					{
						long num3 = ((jET_COLUMNID2 != JET_COLUMNID.Nil) ? Api.RetrieveColumnAsInt64(sesid, tableid2, jET_COLUMNID2).GetValueOrDefault() : 0);
						long num4 = ((jET_COLUMNID3 != JET_COLUMNID.Nil) ? Api.RetrieveColumnAsInt64(sesid, tableid2, jET_COLUMNID3).GetValueOrDefault() : 0);
						if (!dictionary4.TryGetValue(num2.Value, out var value4))
						{
							value4 = default(AppUsage);
						}
						value4.Sent += num3;
						value4.Recv += num4;
						if (dateTime.Value > value4.Last)
						{
							value4.Last = dateTime.Value;
						}
						dictionary4[num2.Value] = value4;
					}
				}
				while (Api.TryMoveNext(sesid, tableid2));
			}
			if (rows == 0L)
			{
				return false;
			}
			if (latest.Date > earliest.Date)
			{
				DateTime dateTime2 = earliest.Date;
				while (dateTime2 < latest.Date)
				{
					if (!hashSet.Contains(dateTime2))
					{
						gapDays++;
					}
					dateTime2 = dateTime2.AddDays(1.0);
				}
			}
			Dictionary<string, string> vol = BuildVolumeMap();
			foreach (KeyValuePair<int, AppUsage> item in dictionary4.OrderByDescending((KeyValuePair<int, AppUsage> k) => k.Value.Sent + k.Value.Recv).Take(80))
			{
				AppUsage value5 = item.Value;
				string value6;
				string text = (dictionary.TryGetValue(item.Key, out value6) ? value6 : null);
				value5.Name = FriendlyName(value5.Path = PrettyPath(text, vol), text);
				apps.Add(value5);
			}
			return true;
		}
		catch
		{
			return false;
		}
		finally
		{
			try
			{
				Api.JetTerm(instance);
			}
			catch
			{
			}
		}
	}

	private static Dictionary<string, JET_COLUMNID> ColMap(JET_SESID sesid, JET_TABLEID tbl)
	{
		Dictionary<string, JET_COLUMNID> dictionary = new Dictionary<string, JET_COLUMNID>(StringComparer.OrdinalIgnoreCase);
		foreach (ColumnInfo tableColumn in Api.GetTableColumns(sesid, tbl))
		{
			dictionary[tableColumn.Name] = tableColumn.Columnid;
		}
		return dictionary;
	}

	private static Dictionary<string, string> BuildVolumeMap()
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		try
		{
			DriveInfo[] drives = DriveInfo.GetDrives();
			for (int i = 0; i < drives.Length; i++)
			{
				string text = drives[i].Name.TrimEnd('\\');
				StringBuilder stringBuilder = new StringBuilder(1024);
				if (QueryDosDevice(text, stringBuilder, (uint)stringBuilder.Capacity) != 0)
				{
					string text2 = stringBuilder.ToString();
					if (!string.IsNullOrEmpty(text2))
					{
						dictionary[text2] = text;
					}
				}
			}
		}
		catch
		{
		}
		return dictionary;
	}

	private static string PrettyPath(string ntPath, Dictionary<string, string> vol)
	{
		if (string.IsNullOrEmpty(ntPath))
		{
			return "";
		}
		if (!ntPath.StartsWith("\\", StringComparison.Ordinal))
		{
			return ntPath;
		}
		foreach (KeyValuePair<string, string> item in vol)
		{
			if (ntPath.StartsWith(item.Key + "\\", StringComparison.OrdinalIgnoreCase))
			{
				return item.Value + ntPath.Substring(item.Key.Length);
			}
		}
		return ntPath;
	}

	private static string FriendlyName(string path, string id)
	{
		if (!string.IsNullOrEmpty(path))
		{
			try
			{
				string fileName = Path.GetFileName(path.TrimEnd('\\'));
				if (!string.IsNullOrEmpty(fileName))
				{
					return fileName;
				}
			}
			catch
			{
			}
		}
		if (!string.IsNullOrEmpty(id))
		{
			string[] array = id.Split(new char[2] { '\\', '!' }, StringSplitOptions.RemoveEmptyEntries);
			if (array.Length != 0)
			{
				return array[^1];
			}
		}
		return "(неизвестно)";
	}

	[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern uint QueryDosDevice(string lpDeviceName, StringBuilder lpTargetPath, uint ucchMax);
}
