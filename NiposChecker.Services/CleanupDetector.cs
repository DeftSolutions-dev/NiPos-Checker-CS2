using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using NiposChecker.Models;

namespace NiposChecker.Services;

public static class CleanupDetector
{
	private struct BamEntry
	{
		public string ExeName;

		public string FullPath;

		public DateTime Last;
	}

	private static readonly string[] CleanerKeys = new string[16]
	{
		"ccleaner", "bleachbit", "privazer", "kcleaner", "wise disk", "wisecare", "wisecleaner", "glary", "systemcare", "eraser",
		"r-wipe", "bcuninstaller", "privacyeraser", "cleanmy", "ashampoo", "wipe.exe"
	};

	public static List<TraceSignal> Run(CheatDatabase db = null)
	{
		List<TraceSignal> list = new List<TraceSignal>();
		if (!IsElevated())
		{
			list.Add(new TraceSignal
			{
				Level = "info",
				Title = "Запущено без прав администратора",
				Detail = "Часть системных проверок недоступна без прав администратора. Нажмите «Запустить от админа» для полной картины."
			});
		}
		List<BamEntry> bam = ReadBam();
		bool accessible;
		int count;
		DateTime newest;
		HashSet<string> pfPrefixes = ReadPrefetchPrefixes(out accessible, out count, out newest);
		try
		{
			CheckSrumReset(list);
		}
		catch
		{
		}
		try
		{
			CheckTargetedPrefetch(list, bam, pfPrefixes, accessible, count, newest);
		}
		catch
		{
		}
		try
		{
			CheckCleaners(list, bam, pfPrefixes);
		}
		catch
		{
		}
		try
		{
			CheckDefenderExclusions(list);
		}
		catch
		{
		}
		try
		{
			CheckDriverIntegrity(list);
		}
		catch
		{
		}
		try
		{
			CheckCs2LaunchOptions(list);
		}
		catch
		{
		}
		try
		{
			CheckCs2Configs(list);
		}
		catch
		{
		}
		try
		{
			CheckRegistryInjection(list);
		}
		catch
		{
		}
		try
		{
			CheckPowerShellHistory(list);
		}
		catch
		{
		}
		try
		{
			CheckWerCrashes(list, db);
		}
		catch
		{
		}
		try
		{
			CheckExecutionHistory(list, db, ReadRecycleDeletedMap());
		}
		catch
		{
		}
		try
		{
			CheckFreshExecutables(list);
		}
		catch
		{
		}
		try
		{
			CheckPersistence(list);
		}
		catch
		{
		}
		try
		{
			CheckHosts(list);
		}
		catch
		{
		}
		try
		{
			CheckEventLogCleared(list);
		}
		catch
		{
		}
		try
		{
			CheckRecycleBin(list, db);
		}
		catch
		{
		}
		try
		{
			CheckDebugger(list);
		}
		catch
		{
		}
		try
		{
			CheckSelfIntegrity(list);
		}
		catch
		{
		}
		return list.OrderBy((TraceSignal s) => s.Rank).ToList();
	}

	private static void CheckCs2LaunchOptions(List<TraceSignal> list)
	{
		string text = SteamPath();
		if (text == null)
		{
			return;
		}
		string path = Path.Combine(text, "userdata");
		if (!Directory.Exists(path))
		{
			return;
		}
		string[] source = new string[4] { "-allow_third_party_software", "-untrusted", "-insecure", "-tools" };
		foreach (string item in Directory.EnumerateDirectories(path))
		{
			string path2 = Path.Combine(item, "config", "localconfig.vdf");
			if (!File.Exists(path2))
			{
				continue;
			}
			string text2;
			try
			{
				text2 = File.ReadAllText(path2);
			}
			catch
			{
				continue;
			}
			int num = text2.IndexOf("\"730\"", StringComparison.Ordinal);
			if (num < 0)
			{
				continue;
			}
			int num2 = text2.IndexOf("LaunchOptions", num, StringComparison.OrdinalIgnoreCase);
			if (num2 < 0 || num2 - num > 4000)
			{
				continue;
			}
			int num3 = text2.IndexOf('"', num2 + "LaunchOptions".Length);
			if (num3 < 0)
			{
				continue;
			}
			int num4 = text2.IndexOf('"', num3 + 1);
			int num5 = text2.IndexOf('"', num4 + 1);
			int num6 = ((num5 >= 0) ? text2.IndexOf('"', num5 + 1) : (-1));
			if (num5 >= 0 && num6 >= 0)
			{
				string opts = text2.Substring(num5 + 1, num6 - num5 - 1);
				List<string> list2 = source.Where((string b) => opts.IndexOf(b, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
				if (list2.Count > 0)
				{
					list.Add(new TraceSignal
					{
						Level = "alert",
						Title = "CS2: обход Trusted Mode в параметрах запуска",
						Detail = "В параметрах запуска CS2 указано: " + string.Join(" ", list2) + ". Это разрешает сторонний софт с игрой — типичная подготовка к читам."
					});
					break;
				}
			}
		}
	}

	private static void CheckCs2Configs(List<TraceSignal> list)
	{
		string text = SteamPath();
		if (text == null)
		{
			return;
		}
		List<string> list2 = new List<string>();
		List<string> list3 = new List<string>();
		foreach (string item in SteamLibraries(text))
		{
			string text2 = Path.Combine(item, "steamapps", "common", "Counter-Strike Global Offensive", "game", "csgo");
			if (Directory.Exists(text2))
			{
				list3.Add(text2);
				string text3 = Path.Combine(text2, "cfg");
				if (Directory.Exists(text3))
				{
					list2.Add(text3);
				}
			}
		}
		string path = Path.Combine(text, "userdata");
		if (Directory.Exists(path))
		{
			foreach (string item2 in Directory.EnumerateDirectories(path))
			{
				string text4 = Path.Combine(item2, "730", "local", "cfg");
				if (Directory.Exists(text4))
				{
					list2.Add(text4);
				}
			}
		}
		Regex regex = new Regex("alias\\s+\\S+\\s+\"?[+-](jump|left|right|forward|back|attack|duck)", RegexOptions.IgnoreCase);
		List<MissingRunItem> list4 = new List<MissingRunItem>();
		foreach (string item3 in list2.Distinct<string>(StringComparer.OrdinalIgnoreCase))
		{
			try
			{
				foreach (string item4 in Directory.EnumerateFiles(item3, "*.cfg"))
				{
					string text5;
					try
					{
						text5 = File.ReadAllText(item4);
					}
					catch
					{
						continue;
					}
					string text6 = text5.ToLowerInvariant();
					if (regex.IsMatch(text6) || (text6.Contains("alias") && text6.Contains("+jump") && text6.Contains("-jump")))
					{
						list4.Add(new MissingRunItem
						{
							Name = Path.GetFileName(item4),
							Path = item4
						});
					}
				}
			}
			catch
			{
			}
		}
		List<MissingRunItem> list5 = new List<MissingRunItem>();
		foreach (string item5 in list3.Distinct<string>(StringComparer.OrdinalIgnoreCase))
		{
			try
			{
				foreach (string item6 in Directory.EnumerateFiles(item5, "*.*", SearchOption.TopDirectoryOnly))
				{
					switch (Path.GetExtension(item6).ToLowerInvariant())
					{
					case ".lua":
					case ".asi":
					case ".js":
						list5.Add(new MissingRunItem
						{
							Name = Path.GetFileName(item6),
							Path = item6
						});
						break;
					}
				}
			}
			catch
			{
			}
		}
		if (list4.Count > 0)
		{
			list.Add(new TraceSignal
			{
				Level = "warn",
				Title = "CS2: скрипт-конфиги (bhop/авто-движение)",
				Items = list4,
				DetailCols = new string[3] { "Файл", "Путь", null },
				Detail = "Найдены .cfg со скрипт-алиасами (bhop/авто-стрейф/авто-тап) — запрещены на большинстве серверов: " + string.Join(", ", from s in list4.Take(6)
					select s.Name) + "."
			});
		}
		if (list5.Count > 0)
		{
			list.Add(new TraceSignal
			{
				Level = "alert",
				Title = "CS2: чужие скрипт-файлы в папке игры",
				Items = list5,
				DetailCols = new string[3] { "Файл", "Путь", null },
				Detail = "В папке игры лежат нехарактерные файлы (.lua/.asi/.js) — плагины/инъекция: " + string.Join(", ", from s in list5.Take(6)
					select s.Name) + "."
			});
		}
	}

	private static IEnumerable<string> SteamLibraries(string steam)
	{
		List<string> list = new List<string> { steam };
		try
		{
			string path = Path.Combine(steam, "steamapps", "libraryfolders.vdf");
			if (File.Exists(path))
			{
				foreach (Match item in Regex.Matches(File.ReadAllText(path), "\"path\"\\s+\"([^\"]+)\""))
				{
					string text = item.Groups[1].Value.Replace("\\\\", "\\");
					if (Directory.Exists(text))
					{
						list.Add(text);
					}
				}
			}
		}
		catch
		{
		}
		return list.Distinct<string>(StringComparer.OrdinalIgnoreCase);
	}

	private static void CheckRegistryInjection(List<TraceSignal> list)
	{
		List<string> list2 = new List<string>();
		RegistryView[] array = new RegistryView[2]
		{
			RegistryView.Registry64,
			RegistryView.Registry32
		};
		foreach (RegistryView view in array)
		{
			try
			{
				using RegistryKey registryKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view).OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Windows");
				string text = registryKey?.GetValue("AppInit_DLLs")?.ToString();
				if (!string.IsNullOrWhiteSpace(text))
				{
					list2.Add("AppInit_DLLs = " + text.Trim());
				}
			}
			catch
			{
			}
		}
		try
		{
			using RegistryKey registryKey2 = Hklm64().OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\Session Manager\\AppCertDlls");
			if (registryKey2 != null)
			{
				string[] valueNames = registryKey2.GetValueNames();
				foreach (string text2 in valueNames)
				{
					list2.Add("AppCertDLL: " + (registryKey2.GetValue(text2)?.ToString() ?? text2));
				}
			}
		}
		catch
		{
		}
		if (list2.Count > 0)
		{
			list.Add(new TraceSignal
			{
				Level = "alert",
				Title = "Глобальная инъекция DLL (реестр)",
				Detail = "Настроена автоподгрузка сторонних DLL: " + string.Join("; ", list2.Take(4)) + ". Классический способ инжекта."
			});
		}
		try
		{
			using RegistryKey registryKey3 = Hklm64().OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Image File Execution Options");
			if (registryKey3 == null)
			{
				return;
			}
			List<string> list3 = new List<string>();
			string[] valueNames = registryKey3.GetSubKeyNames();
			foreach (string text3 in valueNames)
			{
				try
				{
					using RegistryKey registryKey4 = registryKey3.OpenSubKey(text3);
					string text4 = registryKey4?.GetValue("Debugger")?.ToString();
					if (!string.IsNullOrWhiteSpace(text4))
					{
						list3.Add(text3 + " → " + text4);
					}
				}
				catch
				{
				}
			}
			if (list3.Count > 0)
			{
				list.Add(new TraceSignal
				{
					Level = "warn",
					Title = "Подмена запуска программ (IFEO Debugger)",
					Detail = "У программ задан Debugger в Image File Execution Options: " + string.Join("; ", list3.Take(4)) + "."
				});
			}
		}
		catch
		{
		}
	}

	private static void CheckExecutionHistory(List<TraceSignal> list, CheatDatabase db, Dictionary<string, string> recycleDeleted)
	{
		if (db == null)
		{
			return;
		}
		List<MissingRunItem> found = new List<MissingRunItem>();
		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		bool exact;
		string[] subKeyNames;
		try
		{
			using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\UserAssist");
			if (registryKey != null)
			{
				subKeyNames = registryKey.GetSubKeyNames();
				foreach (string text in subKeyNames)
				{
					try
					{
						using RegistryKey registryKey2 = registryKey.OpenSubKey(text + "\\Count");
						if (registryKey2 == null)
						{
							continue;
						}
						string[] valueNames = registryKey2.GetValueNames();
						foreach (string text2 in valueNames)
						{
							string text3 = Rot13(text2).Replace('/', '\\');
							int num = text3.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
							if (num >= 0)
							{
								string text4 = text3.Substring(0, num + 4);
								string fileName = Path.GetFileName(text4);
								if (!string.IsNullOrEmpty(fileName) && (db.NameLooksLikeCheat(fileName, out exact) || HitByContent(db, text4)))
								{
									string regKey = "HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\UserAssist\\" + text + "\\Count";
									AddHit(fileName, "История запусков", text4, ParseUserAssistRun(registryKey2.GetValue(text2) as byte[]), regKey);
								}
							}
						}
					}
					catch
					{
					}
				}
			}
		}
		catch
		{
		}
		subKeyNames = new string[2] { "Software\\Classes\\Local Settings\\Software\\Microsoft\\Windows\\Shell\\MuiCache", "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\MuiCache" };
		foreach (string text5 in subKeyNames)
		{
			try
			{
				using RegistryKey registryKey3 = Registry.CurrentUser.OpenSubKey(text5);
				if (registryKey3 == null)
				{
					continue;
				}
				string[] valueNames = registryKey3.GetValueNames();
				foreach (string text6 in valueNames)
				{
					int num2 = text6.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
					if (num2 >= 0)
					{
						string text7 = text6.Substring(0, num2 + 4);
						string fileName2 = Path.GetFileName(text7);
						if (!string.IsNullOrEmpty(fileName2) && (db.NameLooksLikeCheat(fileName2, out exact) || HitByContent(db, text7)))
						{
							AddHit(fileName2, "Реестр запусков", text7, "", "HKEY_CURRENT_USER\\" + text5);
						}
					}
				}
			}
			catch
			{
			}
		}
		if (found.Count > 0)
		{
			list.Add(new TraceSignal
			{
				Level = "alert",
				Title = "Запускались программы из базы читов (история)",
				Items = found,
				DetailCols = new string[5] { "Программа", "Путь / ключ", "Источник", "Последний запуск", "Статус файла" },
				Detail = "По истории запусков видно запуск: " + string.Join(", ", from f in found.Take(8)
					select f.Name) + ". Файл мог быть уже закрыт или удалён."
			});
		}
		void AddHit(string fn, string source, string full, string lastRun, string regKey2)
		{
			if (!string.IsNullOrEmpty(fn) && seen.Add(fn))
			{
				found.Add(new MissingRunItem
				{
					Name = fn,
					Path = full,
					LastRun = source,
					Extra1 = (lastRun ?? ""),
					Extra2 = StatusOf(fn, full),
					RegKey = regKey2
				});
			}
		}
		string StatusOf(string fn, string full)
		{
			if (recycleDeleted != null && recycleDeleted.TryGetValue(fn, out var value) && !string.IsNullOrEmpty(value))
			{
				return "удалён (корзина) " + value;
			}
			if (!string.IsNullOrEmpty(full) && full.Length > 3 && full[1] == ':' && full[2] == '\\')
			{
				try
				{
					return File.Exists(full) ? "на диске" : "нет на диске";
				}
				catch
				{
				}
			}
			return "";
		}
	}

	private static bool HitByContent(CheatDatabase db, string full)
	{
		try
		{
			if (string.IsNullOrEmpty(full) || full.Length < 4 || full[1] != ':' || full[2] != '\\')
			{
				return false;
			}
			if (!IsUserPath(full) || !File.Exists(full))
			{
				return false;
			}
			return db.Evaluate(full) != null;
		}
		catch
		{
			return false;
		}
	}

	private static string ParseUserAssistRun(byte[] data)
	{
		try
		{
			if (data == null || data.Length < 68)
			{
				return "";
			}
			uint num = BitConverter.ToUInt32(data, 4);
			long num2 = BitConverter.ToInt64(data, 60);
			string text = "";
			if (num2 > 0)
			{
				try
				{
					text = DateTime.FromFileTimeUtc(num2).ToLocalTime().ToString("dd.MM.yyyy HH:mm");
				}
				catch
				{
				}
			}
			if (text == "" && num == 0)
			{
				return "";
			}
			if (text == "")
			{
				return $"×{num}";
			}
			return (num != 0) ? $"{text} · ×{num}" : text;
		}
		catch
		{
			return "";
		}
	}

	private static string Rot13(string s)
	{
		char[] array = s.ToCharArray();
		for (int i = 0; i < array.Length; i++)
		{
			char c = array[i];
			if (c >= 'a' && c <= 'z')
			{
				array[i] = (char)(97 + (c - 97 + 13) % 26);
			}
			else if (c >= 'A' && c <= 'Z')
			{
				array[i] = (char)(65 + (c - 65 + 13) % 26);
			}
		}
		return new string(array);
	}

	private static void CheckWerCrashes(List<TraceSignal> list, CheatDatabase db)
	{
		if (db == null)
		{
			return;
		}
		string[] obj = new string[3]
		{
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Microsoft\\Windows\\WER\\ReportArchive"),
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Microsoft\\Windows\\WER\\ReportQueue"),
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft\\Windows\\WER\\ReportArchive")
		};
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		string[] array = obj;
		foreach (string path in array)
		{
			try
			{
				if (!Directory.Exists(path))
				{
					continue;
				}
				foreach (string item in Directory.EnumerateDirectories(path))
				{
					Match match = Regex.Match(Path.GetFileName(item), "AppCrash_([^_]+)_", RegexOptions.IgnoreCase);
					if (match.Success)
					{
						string value = match.Groups[1].Value;
						if (db.NameLooksLikeCheat(value + ".exe", out var _))
						{
							hashSet.Add(value);
						}
					}
				}
			}
			catch
			{
			}
		}
		if (hashSet.Count > 0)
		{
			list.Add(new TraceSignal
			{
				Level = "alert",
				Title = "Падал процесс из базы читов (WER)",
				Detail = "В отчётах о сбоях Windows есть падавшие программы, похожие на читы: " + string.Join(", ", hashSet.Take(6)) + "."
			});
		}
	}

	private static void CheckSrumReset(List<TraceSignal> list)
	{
		DateTime dateTime = OsInstallDate();
		DateTime now = DateTime.Now;
		(int start, bool running, string dispName) tuple = DusmState();
		int item = tuple.start;
		bool item2 = tuple.running;
		string item3 = tuple.dispName;
		bool flag = item == 4;
		DateTime? dateTime2 = NewestDusmStop(item3);
		SrumInspector.SrumSpan span = default(SrumInspector.SrumSpan);
		if (IsElevated())
		{
			try
			{
				span = SrumInspector.Inspect();
			}
			catch
			{
			}
		}
		TraceSignal traceSignal;
		if (flag)
		{
			traceSignal = new TraceSignal
			{
				Level = "alert",
				Title = "Использование данных: учёт ОТКЛЮЧЁН",
				Detail = "Служба учёта сетевой активности (DusmSvc) отключена — новые данные не пишутся. Так делают «твикеры»/деблоатеры; при этом проверить сеть невозможно. Нажмите «Восстановить учёт», чтобы включить и запустить запись."
			};
		}
		else if (!item2)
		{
			traceSignal = new TraceSignal
			{
				Level = "warn",
				Title = "Использование данных: учёт остановлен",
				Detail = "Служба учёта сетевой активности (DusmSvc) не запущена — новая активность не пишется. Нажмите «Восстановить учёт», чтобы запустить."
			};
		}
		else if (!span.Ok || span.Earliest.Year <= 1990)
		{
			traceSignal = ((!IsElevated()) ? FileDateFallback(dateTime) : new TraceSignal
			{
				Level = "info",
				Title = "Использование данных: не удалось прочитать",
				Detail = "Служба работает, но офлайн-чтение файла не удалось (нет теневой копии или база повреждена). Можно попробовать «Восстановить учёт»."
			});
		}
		else
		{
			double totalDays = (now - span.Earliest).TotalDays;
			traceSignal = ((!(dateTime != DateTime.MinValue) || !((now - dateTime).TotalDays > 14.0) || !(totalDays < 5.0)) ? new TraceSignal
			{
				Level = "ok",
				Title = "Использование данных: история цела",
				Detail = $"Сетевая активность приложений сохранена с {span.Earliest:dd.MM.yyyy} по {span.Latest:dd.MM.yyyy} (~{totalDays:F0} дн., {span.Rows} записей). Нажмите «Показать список», чтобы посмотреть, " + "какие приложения выходили в сеть."
			} : new TraceSignal
			{
				Level = "warn",
				Title = "Использование данных: историю обрезали",
				Detail = $"Самая ранняя запись сетевой активности — {span.Earliest:dd.MM.yyyy HH:mm} (история всего ~{totalDays:F0} дн.), хотя Windows работает с {dateTime:dd.MM.yyyy}. " + "Похоже, базу «Использования данных» жёстко чистили."
			});
			if (span.GapDays >= 3)
			{
				traceSignal.Detail += $" Внутри истории {span.GapDays} дн. без записей — в эти дни ПК был выключен или учёт отключали.";
			}
			AttachApps(traceSignal, span);
		}
		if (traceSignal == null)
		{
			return;
		}
		if (dateTime2.HasValue)
		{
			DateTime valueOrDefault = dateTime2.GetValueOrDefault();
			if ((now - valueOrDefault).TotalDays <= 14.0)
			{
				traceSignal.Detail += $" Внимание: службу учёта останавливали {valueOrDefault:dd.MM.yyyy HH:mm} — её могли трогать перед проверкой.";
			}
		}
		traceSignal.RepairKind = "datausage";
		if (traceSignal.Items == null)
		{
			AttachApps(traceSignal, span);
		}
		list.Add(traceSignal);
	}

	private static void AttachApps(TraceSignal sig, SrumInspector.SrumSpan span)
	{
		if (sig != null && span.Apps != null && span.Apps.Count != 0)
		{
			sig.DetailCols = new string[5] { "Приложение", "Путь", "Последняя активность", "Отправлено", "Получено" };
			sig.SortPaths = new string[5] { null, null, "LastRunSort", "Extra1Sort", "Extra2Sort" };
			sig.Items = span.Apps.Select((SrumInspector.AppUsage a) => new MissingRunItem
			{
				Name = a.Name,
				Path = a.Path,
				LastRun = ((a.Last.Year > 1990) ? a.Last.ToString("dd.MM.yyyy HH:mm") : ""),
				LastRunSort = a.Last,
				Extra1 = FormatBytes(a.Sent),
				Extra1Sort = a.Sent,
				Extra2 = FormatBytes(a.Recv),
				Extra2Sort = a.Recv
			}).ToList();
		}
	}

	private static TraceSignal FileDateFallback(DateTime install)
	{
		string text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "sru", "SRUDB.dat");
		string text2 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Sysnative", "sru", "SRUDB.dat");
		string path = (File.Exists(text) ? text : text2);
		bool flag;
		DateTime value;
		try
		{
			flag = File.Exists(path);
			value = (flag ? File.GetCreationTime(path) : DateTime.MinValue);
		}
		catch (UnauthorizedAccessException)
		{
			return new TraceSignal
			{
				Level = "info",
				Title = "Нет доступа к «Использованию данных»",
				Detail = "Запустите от администратора, чтобы проверить историю сетевой активности приложений."
			};
		}
		catch
		{
			return null;
		}
		if (!flag || value.Year < 1980)
		{
			return new TraceSignal
			{
				Level = "info",
				Title = "Нет доступа к «Использованию данных»",
				Detail = "Не удалось прочитать файл «Использования данных» (нужны права администратора)."
			};
		}
		if (install != DateTime.MinValue && value.Date > install.Date)
		{
			return new TraceSignal
			{
				Level = "warn",
				Title = "Использование данных: файл пересоздан",
				Detail = $"Файл «Использования данных» создан {value:dd.MM.yyyy HH:mm}, а Windows установлена {install:dd.MM.yyyy}. " + "Похоже, историю сетевой активности приложений жёстко чистили."
			};
		}
		return new TraceSignal
		{
			Level = "info",
			Title = "Использование данных: файл не пересоздавался",
			Detail = $"Файл «Использования данных» создан {value:dd.MM.yyyy HH:mm} (≈ дата установки Windows). " + "Для полной картины (диапазон дат и разбивка по приложениям) запустите проверку от администратора."
		};
	}

	private static (int start, bool running, string dispName) DusmState()
	{
		int item = -1;
		bool item2 = false;
		string item3 = null;
		try
		{
			using RegistryKey registryKey = Hklm64().OpenSubKey("SYSTEM\\CurrentControlSet\\Services\\DusmSvc");
			if (registryKey?.GetValue("Start") is int num)
			{
				item = num;
			}
		}
		catch
		{
		}
		try
		{
			using ServiceController serviceController = new ServiceController("DusmSvc");
			item2 = serviceController.Status == ServiceControllerStatus.Running;
			item3 = serviceController.DisplayName;
		}
		catch
		{
		}
		return (start: item, running: item2, dispName: item3);
	}

	private static DateTime? NewestDusmStop(string dispName)
	{
		if (string.IsNullOrWhiteSpace(dispName))
		{
			return null;
		}
		try
		{
			string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
			string text = Path.Combine(folderPath, "Sysnative", "WindowsPowerShell", "v1.0", "powershell.exe");
			string fileName = (File.Exists(text) ? text : "powershell.exe");
			string text2 = dispName.Replace("'", "''");
			string arguments = "-NoProfile -NonInteractive -Command \"$e=Get-WinEvent -FilterHashtable @{LogName='System';ProviderName='Service Control Manager';Id=7036} -MaxEvents 1000 -ErrorAction SilentlyContinue | Where-Object { $_.Message -like '*" + text2 + "*' -and ($_.Message -match 'остановлен' -or $_.Message -match 'stopped') } | Select-Object -First 1; if($e){$e.TimeCreated.ToString('o')}\"";
			using Process process = Process.Start(new ProcessStartInfo(fileName, arguments)
			{
				RedirectStandardOutput = true,
				UseShellExecute = false,
				CreateNoWindow = true
			});
			if (process == null)
			{
				return null;
			}
			string text3 = process.StandardOutput.ReadToEnd();
			if (!process.WaitForExit(7000))
			{
				try
				{
					process.Kill();
				}
				catch
				{
				}
				return null;
			}
			text3 = text3?.Trim();
			if (string.IsNullOrWhiteSpace(text3))
			{
				return null;
			}
			DateTime result;
			return DateTime.TryParse(text3, null, DateTimeStyles.RoundtripKind, out result) ? new DateTime?(result) : ((DateTime?)null);
		}
		catch
		{
			return null;
		}
	}

	private static string FormatBytes(long b)
	{
		if (b <= 0)
		{
			return "0";
		}
		string[] array = new string[5] { "Б", "КБ", "МБ", "ГБ", "ТБ" };
		double num = b;
		int num2 = 0;
		while (num >= 1024.0 && num2 < array.Length - 1)
		{
			num /= 1024.0;
			num2++;
		}
		return ((num2 == 0) ? num.ToString("0") : num.ToString("0.0")) + " " + array[num2];
	}

	private static void CheckTargetedPrefetch(List<TraceSignal> list, List<BamEntry> bam, HashSet<string> pfPrefixes, bool pfAccessible, int pfCount, DateTime pfNewest)
	{
		if (!pfAccessible)
		{
			list.Add(new TraceSignal
			{
				Level = "info",
				Title = "Нет доступа к следам запусков",
				Detail = "Запустите от администратора, чтобы сверить историю запусков с системным кэшем."
			});
			return;
		}
		if (pfCount == 0)
		{
			list.Add(new TraceSignal
			{
				Level = "alert",
				Title = "Кэш запусков очищен",
				Detail = "Системный кэш запусков пуст — его очистили целиком."
			});
			return;
		}
		List<MissingRunItem> list2 = new List<MissingRunItem>();
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (BamEntry item in bam)
		{
			if (!((DateTime.Now - item.Last).TotalDays > 30.0) && IsUserPath(item.FullPath) && !IsCleaner(item.ExeName))
			{
				string up = item.ExeName.ToUpperInvariant();
				if (!pfPrefixes.Any((string p) => p == up || up.StartsWith(p) || p.StartsWith(up)) && hashSet.Add(item.FullPath))
				{
					list2.Add(new MissingRunItem
					{
						Name = item.ExeName,
						Path = PrettyPath(item.FullPath),
						LastRun = item.Last.ToString("dd.MM.yyyy HH:mm")
					});
				}
			}
		}
		if (list2.Count > 0)
		{
			string text = string.Join(", ", from m in list2.Take(6)
				select m.Name);
			if (list2.Count > 6)
			{
				text += $" … (+{list2.Count - 6})";
			}
			list.Add(new TraceSignal
			{
				Level = "warn",
				Title = "Запуски без следа в системном кэше",
				Items = list2,
				Detail = "Программы запускались (по журналу активности), но их следа в системном кэше запусков нет — возможно, кэш подчистили точечно: " + text + "."
			});
		}
		else
		{
			list.Add(new TraceSignal
			{
				Level = "ok",
				Title = "Кэш запусков согласован с историей",
				Detail = $"{pfCount} записей, последняя — {pfNewest:dd.MM.yyyy HH:mm}. Точечных пропусков не видно."
			});
		}
	}

	private static void CheckCleaners(List<TraceSignal> list, List<BamEntry> bam, HashSet<string> pfPrefixes)
	{
		List<string> list2 = (from e in bam
			where IsCleaner(e.ExeName) && (DateTime.Now - e.Last).TotalDays <= 14.0
			select $"{e.ExeName} ({e.Last:dd.MM.yyyy HH:mm})").Distinct().ToList();
		if (list2.Count > 0)
		{
			list.Add(new TraceSignal
			{
				Level = "alert",
				Title = "Запускался чистильщик следов",
				Detail = "Прямо перед проверкой работала программа очистки: " + string.Join(", ", list2.Take(5)) + "."
			});
			return;
		}
		List<string> list3 = pfPrefixes.Where((string p) => CleanerKeys.Any((string k) => p.ToLowerInvariant().Contains(k))).ToList();
		if (list3.Count > 0)
		{
			list.Add(new TraceSignal
			{
				Level = "warn",
				Title = "Найден след чистильщика",
				Detail = "В системном кэше запусков есть запись программы очистки: " + string.Join(", ", list3.Take(5)) + "."
			});
		}
	}

	private static void CheckEventLogCleared(List<TraceSignal> list)
	{
		DateTime? t = NewestEvent("System", 104);
		DateTime? t2 = NewestEvent("Security", 1102);
		Report(t, "System");
		Report(t2, "Security");
		void Report(DateTime? dateTime, string logName)
		{
			if (dateTime.HasValue)
			{
				DateTime valueOrDefault = dateTime.GetValueOrDefault();
				if ((DateTime.Now - valueOrDefault).TotalDays <= 14.0)
				{
					list.Add(new TraceSignal
					{
						Level = "warn",
						Title = "Журнал «" + logName + "» очищали",
						Detail = $"Событие очистки лога {valueOrDefault:dd.MM.yyyy HH:mm} — недавно чистили историю событий."
					});
				}
			}
		}
	}

	private static void CheckHosts(List<TraceSignal> list)
	{
		try
		{
			string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "drivers\\etc\\hosts");
			if (!File.Exists(path))
			{
				return;
			}
			string[] source = new string[9] { "steam", "valve", "faceit", "esea", "blockdb", "niposproject", "steampowered", "steamcommunity", "matchmaking" };
			List<string> list2 = new List<string>();
			bool flag = false;
			foreach (string item in File.ReadLines(path))
			{
				string text = item.Trim();
				if (text.Length == 0 || text.StartsWith("#"))
				{
					continue;
				}
				string text2 = text.Split('#')[0].Trim();
				if (text2.Length != 0)
				{
					list2.Add(text2);
					string low = text2.ToLowerInvariant();
					if (source.Any((string w) => low.Contains(w)))
					{
						flag = true;
					}
				}
			}
			if (list2.Count != 0)
			{
				List<MissingRunItem> items = list2.Select((string e) => new MissingRunItem
				{
					Path = e
				}).ToList();
				string[] detailCols = new string[3] { null, "Запись hosts", null };
				if (flag)
				{
					list.Add(new TraceSignal
					{
						Level = "alert",
						Title = "Правки hosts затрагивают игровые/анти-чит домены",
						Items = items,
						DetailCols = detailCols,
						Detail = "В hosts есть записи про steam/valve/faceit/blockdb — возможен обход или блокировка проверок: " + string.Join(" · ", list2.Take(6)) + "."
					});
				}
				else
				{
					list.Add(new TraceSignal
					{
						Level = "warn",
						Title = "Изменён файл hosts",
						Items = items,
						DetailCols = detailCols,
						Detail = $"Нестандартных записей: {list2.Count}. " + string.Join(" · ", list2.Take(6)) + "."
					});
				}
			}
		}
		catch
		{
		}
	}

	private static void CheckRecycleBin(List<TraceSignal> list, CheatDatabase db)
	{
		try
		{
			string path = Path.Combine(Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System)) ?? "C:\\", "$Recycle.Bin");
			if (!Directory.Exists(path))
			{
				return;
			}
			int num = 0;
			List<string> list2 = new List<string>();
			List<MissingRunItem> list3 = new List<MissingRunItem>();
			foreach (string item in Directory.EnumerateDirectories(path))
			{
				string[] files;
				try
				{
					files = Directory.GetFiles(item, "$I*");
				}
				catch
				{
					continue;
				}
				num += files.Length;
				string[] array = files;
				foreach (string iFile in array)
				{
					string text = ReadRecycleOriginalPath(iFile);
					if (!string.IsNullOrEmpty(text))
					{
						string fileName = Path.GetFileName(text);
						list3.Add(new MissingRunItem
						{
							Name = fileName,
							Path = text,
							LastRun = ReadRecycleDeletedTime(iFile)
						});
						if (db != null && db.NameLooksLikeCheat(fileName, out var _) && !list2.Contains(fileName))
						{
							list2.Add(fileName);
						}
					}
				}
			}
			if (list2.Count > 0)
			{
				list.Add(new TraceSignal
				{
					Level = "alert",
					Title = "В корзине — удалённые файлы из базы читов",
					Detail = "Удалённые файлы похожи на читы: " + string.Join(", ", list2.Take(8)) + ". Восстановите из корзины и проверьте."
				});
			}
			TraceSignal traceSignal = new TraceSignal
			{
				Level = "note",
				Title = "Корзина",
				Detail = ((num == 0) ? "Корзина пуста — открывать нет смысла." : $"В корзине {num} объектов — можно просмотреть список.")
			};
			if (list3.Count > 0)
			{
				traceSignal.Items = list3;
				traceSignal.DetailCols = new string[3] { "Файл", "Исходный путь", "Удалён" };
			}
			list.Add(traceSignal);
		}
		catch
		{
		}
	}

	private static string ReadRecycleOriginalPath(string iFile)
	{
		try
		{
			byte[] array = File.ReadAllBytes(iFile);
			if (array.Length < 24)
			{
				return null;
			}
			int num2;
			int num3;
			if (BitConverter.ToInt64(array, 0) == 2)
			{
				if (array.Length < 28)
				{
					return null;
				}
				int num = BitConverter.ToInt32(array, 24);
				num2 = 28;
				num3 = Math.Max(0, (num - 1) * 2);
			}
			else
			{
				num2 = 24;
				num3 = array.Length - 24;
			}
			if (num2 + num3 > array.Length)
			{
				num3 = array.Length - num2;
			}
			if (num3 <= 0)
			{
				return null;
			}
			string text = Encoding.Unicode.GetString(array, num2, num3);
			int num4 = text.IndexOf('\0');
			if (num4 >= 0)
			{
				text = text.Substring(0, num4);
			}
			return text;
		}
		catch
		{
			return null;
		}
	}

	private static Dictionary<string, string> ReadRecycleDeletedMap()
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		try
		{
			string path = Path.Combine(Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System)) ?? "C:\\", "$Recycle.Bin");
			if (!Directory.Exists(path))
			{
				return dictionary;
			}
			foreach (string item in Directory.EnumerateDirectories(path))
			{
				string[] files;
				try
				{
					files = Directory.GetFiles(item, "$I*");
				}
				catch
				{
					continue;
				}
				string[] array = files;
				foreach (string iFile in array)
				{
					string text = ReadRecycleOriginalPath(iFile);
					if (string.IsNullOrEmpty(text))
					{
						continue;
					}
					string fileName = Path.GetFileName(text);
					if (!string.IsNullOrEmpty(fileName))
					{
						string value = ReadRecycleDeletedTime(iFile);
						if (!dictionary.ContainsKey(fileName))
						{
							dictionary[fileName] = value;
						}
					}
				}
			}
		}
		catch
		{
		}
		return dictionary;
	}

	private static string ReadRecycleDeletedTime(string iFile)
	{
		try
		{
			byte[] array = File.ReadAllBytes(iFile);
			if (array.Length < 24)
			{
				return "";
			}
			long num = BitConverter.ToInt64(array, 16);
			if (num <= 0)
			{
				return "";
			}
			return DateTime.FromFileTimeUtc(num).ToLocalTime().ToString("dd.MM.yyyy HH:mm");
		}
		catch
		{
			return "";
		}
	}

	private static string SteamPath()
	{
		try
		{
			using RegistryKey registryKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64).OpenSubKey("SOFTWARE\\Valve\\Steam");
			string text = registryKey?.GetValue("SteamPath")?.ToString();
			if (!string.IsNullOrEmpty(text) && Directory.Exists(text))
			{
				return text;
			}
		}
		catch
		{
		}
		try
		{
			using RegistryKey registryKey2 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey("SOFTWARE\\Valve\\Steam");
			string text2 = registryKey2?.GetValue("InstallPath")?.ToString();
			if (!string.IsNullOrEmpty(text2) && Directory.Exists(text2))
			{
				return text2;
			}
		}
		catch
		{
		}
		return null;
	}

	private static void CheckDefenderExclusions(List<TraceSignal> list)
	{
		List<string> list2 = new List<string>();
		bool flag = false;
		string[] array = new string[2] { "SOFTWARE\\Microsoft\\Windows Defender\\Exclusions", "SOFTWARE\\Policies\\Microsoft\\Windows Defender\\Exclusions" };
		foreach (string text in array)
		{
			string[] array2 = new string[3] { "Paths", "Extensions", "Processes" };
			foreach (string text2 in array2)
			{
				try
				{
					using RegistryKey registryKey = Hklm64().OpenSubKey(text + "\\" + text2);
					if (registryKey == null)
					{
						continue;
					}
					string[] valueNames = registryKey.GetValueNames();
					foreach (string text3 in valueNames)
					{
						if (!string.IsNullOrEmpty(text3))
						{
							list2.Add(text3);
						}
					}
				}
				catch (SecurityException)
				{
					flag = true;
				}
				catch
				{
				}
			}
		}
		list2 = list2.Distinct<string>(StringComparer.OrdinalIgnoreCase).ToList();
		if (list2.Count > 0)
		{
			List<MissingRunItem> items = list2.Select((string f) => new MissingRunItem
			{
				Path = f
			}).ToList();
			string text4 = string.Join(", ", list2.Take(5));
			if (list2.Count > 5)
			{
				text4 += $" … (+{list2.Count - 5})";
			}
			list.Add(new TraceSignal
			{
				Level = "warn",
				Title = "Исключения Windows Defender",
				Items = items,
				DetailCols = new string[3] { null, "Исключение Defender", null },
				Detail = $"Из проверки Defender исключено ({list2.Count}): {text4}. Может быть от кряков/пиратки — но так же прячут и папку чита. Проверьте, что именно исключено."
			});
		}
		else if (flag)
		{
			list.Add(new TraceSignal
			{
				Level = "info",
				Title = "Нет доступа к исключениям Defender",
				Detail = "Запустите от администратора, чтобы проверить белый список Defender."
			});
		}
		else
		{
			list.Add(new TraceSignal
			{
				Level = "ok",
				Title = "Исключений Defender нет",
				Detail = "Ручных исключений из антивирусной проверки не найдено."
			});
		}
	}

	private static void CheckDriverIntegrity(List<TraceSignal> list)
	{
		string input = RunCapture(SysnativeExe("bcdedit.exe"), "/enum {current}", 6000) ?? "";
		bool flag = Regex.IsMatch(input, "testsigning\\s+Yes", RegexOptions.IgnoreCase);
		bool flag2 = Regex.IsMatch(input, "nointegritychecks\\s+Yes", RegexOptions.IgnoreCase);
		if (flag || flag2)
		{
			string text = ((flag && flag2) ? "Test Signing и проверка целостности драйверов" : (flag ? "Test Signing (тестовая подпись)" : "проверка целостности драйверов (nointegritychecks)"));
			list.Add(new TraceSignal
			{
				Level = "alert",
				Title = "Разрешена загрузка неподписанных драйверов",
				Detail = "Включено: " + text + ". Так грузят кернел-читы (неподписанные драйверы)."
			});
		}
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			["windivert64.sys"] = "WinDivert — часто GoodbyeDPI (обход блокировок)/VPN/файрвол",
			["windivert32.sys"] = "WinDivert — часто GoodbyeDPI/VPN/файрвол",
			["windivert.sys"] = "WinDivert — часто GoodbyeDPI/VPN/файрвол",
			["netfilter2.sys"] = "NetFilter SDK — часто VPN/прокси/родительский контроль",
			["npcap.sys"] = "Npcap — захват трафика (Wireshark и т.п.)",
			["npf.sys"] = "WinPcap — захват трафика"
		};
		List<string> list2 = new List<string>();
		List<string> list3 = new List<string>();
		List<string> list4 = new List<string>();
		Dictionary<string, string> dictionary2 = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		try
		{
			using ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher("SELECT Name,PathName,State FROM Win32_SystemDriver WHERE State='Running'");
			foreach (ManagementObject item in managementObjectSearcher.Get())
			{
				string text2 = (item["PathName"]?.ToString() ?? "").Trim();
				if (!string.IsNullOrEmpty(text2))
				{
					string text3 = text2.Replace("\\??\\", "").Replace("\\SystemRoot\\", "C:\\Windows\\").Trim('"');
					string fileName = Path.GetFileName(text3);
					if (!string.IsNullOrEmpty(fileName) && !dictionary2.ContainsKey(fileName))
					{
						dictionary2[fileName] = text3;
					}
				}
			}
		}
		catch
		{
		}
		try
		{
			foreach (var (text4, value) in KernelModules.List())
			{
				if (!string.IsNullOrEmpty(text4) && !dictionary2.ContainsKey(text4))
				{
					dictionary2[text4] = value;
				}
			}
		}
		catch
		{
		}
		foreach (KeyValuePair<string, string> item2 in dictionary2)
		{
			string text5 = item2.Key.ToLowerInvariant();
			string value2 = item2.Value;
			string hint;
			if (dictionary.TryGetValue(text5, out var value3))
			{
				list4.Add(item2.Key + " — " + value3);
			}
			else if (IsUserPath(value2))
			{
				list2.Add(item2.Key);
			}
			else if (VulnerableDrivers.IsKnown(text5, out hint))
			{
				list3.Add(item2.Key + " — обычно " + hint);
			}
		}
		if (list2.Count > 0)
		{
			list.Add(new TraceSignal
			{
				Level = "alert",
				Title = "Драйвер загружен из пользовательской папки",
				Items = (from d in list2.Distinct()
					select new MissingRunItem
					{
						Path = d
					}).ToList(),
				DetailCols = new string[3] { null, "Драйвер", null },
				Detail = "Драйверы вне System32 — сильный признак кернел-чита: " + string.Join(", ", list2.Distinct().Take(6)) + "."
			});
		}
		if (list4.Count > 0)
		{
			list.Add(new TraceSignal
			{
				Level = "warn",
				Title = "Сетевой драйвер-перехватчик трафика",
				Items = (from d in list4.Distinct()
					select new MissingRunItem
					{
						Path = d
					}).ToList(),
				DetailCols = new string[3] { null, "Драйвер", null },
				Detail = "Загружен драйвер перехвата сетевых пакетов: " + string.Join("; ", list4.Distinct().Take(6)) + ". Чаще это легитимное (GoodbyeDPI для обхода блокировок, VPN, файрвол, снифферы), но такие драйверы применяют и лаг-свитч/сетевые читы — уточните, какая программа его держит."
			});
		}
		if (list3.Count > 0)
		{
			list.Add(new TraceSignal
			{
				Level = "info",
				Title = "Известные «дырявые» драйверы",
				Items = (from d in list3.Distinct()
					select new MissingRunItem
					{
						Path = d
					}).ToList(),
				DetailCols = new string[3] { null, "Драйвер", null },
				Detail = "Присутствуют драйверы из списка потенциально уязвимых (часто это легитимный софт): " + string.Join("; ", list3.Distinct().Take(6)) + ". Само по себе не улика — но их могут использовать кернел-читы."
			});
		}
	}

	[DllImport("kernel32.dll")]
	private static extern bool IsDebuggerPresent();

	[DllImport("kernel32.dll")]
	private static extern bool CheckRemoteDebuggerPresent(nint hProcess, ref bool isPresent);

	private static void CheckSelfIntegrity(List<TraceSignal> list)
	{
		var (list2, list3) = SelfIntegrity.Check();
		if (list2.Count > 0)
		{
			list.Add(new TraceSignal
			{
				Level = "alert",
				Title = "Утилиты чекера подменены",
				Items = (from t in list2.Distinct()
					select new MissingRunItem
					{
						Path = t
					}).ToList(),
				DetailCols = new string[3] { null, "Утилита", null },
				Detail = "Встроенные утилиты не совпадают с эталоном по хешу: " + string.Join(", ", list2.Take(6)) + ". Их выводу нельзя доверять — возможно, подменены, чтобы скрыть следы. Переустановите чекер."
			});
		}
		else if (list3.Count > 0)
		{
			list.Add(new TraceSignal
			{
				Level = "warn",
				Title = "Утилиты чекера отсутствуют",
				Items = (from t in list3.Distinct()
					select new MissingRunItem
					{
						Path = t
					}).ToList(),
				DetailCols = new string[3] { null, "Утилита", null },
				Detail = "Не найдены встроенные утилиты: " + string.Join(", ", list3.Take(6)) + ". Часть проверок может не работать — переустановите чекер."
			});
		}
	}

	private static void CheckDebugger(List<TraceSignal> list)
	{
		bool flag = false;
		bool flag2 = false;
		try
		{
			flag = IsDebuggerPresent();
		}
		catch
		{
		}
		try
		{
			bool isPresent = false;
			if (CheckRemoteDebuggerPresent(Process.GetCurrentProcess().Handle, ref isPresent))
			{
				flag2 = isPresent;
			}
		}
		catch
		{
		}
		if (flag || flag2)
		{
			list.Add(new TraceSignal
			{
				Level = "warn",
				Title = "К чекеру подключён отладчик",
				Detail = "К процессу проверки подключён отладчик — вывод чекера могли подменять на лету. Доверие к результатам снижено: перезапустите проверку без отладчика."
			});
		}
	}

	private static void CheckPowerShellHistory(List<TraceSignal> list)
	{
		string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft\\Windows\\PowerShell\\PSReadLine\\ConsoleHost_history.txt");
		if (!File.Exists(path))
		{
			return;
		}
		string[] source = new string[11]
		{
			"remove-item.*prefetch", "del\\s+.*prefetch", "wevtutil\\s+cl", "clear-eventlog", "add-mppreference.*exclusion", "set-mppreference.*disable", "bcdedit.*testsigning", "fsutil\\s+usn\\s+deletejournal", "cipher\\s+/w", "sdelete",
			"vssadmin\\s+delete"
		};
		List<string> list2 = new List<string>();
		try
		{
			foreach (string item in File.ReadLines(path))
			{
				string l = item.Trim();
				if (l.Length != 0 && source.Any((string p) => Regex.IsMatch(l, p, RegexOptions.IgnoreCase)) && !list2.Contains(l))
				{
					list2.Add(l);
				}
			}
		}
		catch
		{
		}
		if (list2.Count > 0)
		{
			list.Add(new TraceSignal
			{
				Level = "alert",
				Title = "В истории PowerShell — команды уборки",
				Detail = "Найдены команды очистки следов: " + string.Join("  |  ", list2.Take(4)) + ((list2.Count > 4) ? " …" : "") + "."
			});
		}
	}

	private static void CheckFreshExecutables(List<TraceSignal> list)
	{
		string[] source = new string[5]
		{
			Path.GetTempPath(),
			Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			KnownFolders.GetDownloads(),
			Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)
		};
		DateTime dateTime = DateTime.Now.AddHours(-48.0);
		List<MissingRunItem> list2 = new List<MissingRunItem>();
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (string item in source.Distinct())
		{
			try
			{
				if (!Directory.Exists(item))
				{
					continue;
				}
				foreach (string item2 in Directory.EnumerateFiles(item, "*.exe", SearchOption.TopDirectoryOnly))
				{
					try
					{
						DateTime creationTime = File.GetCreationTime(item2);
						if (!(creationTime < dateTime) && hashSet.Add(item2))
						{
							list2.Add(new MissingRunItem
							{
								Name = Path.GetFileName(item2),
								Path = item2,
								LastRun = creationTime.ToString("dd.MM.yyyy HH:mm")
							});
						}
					}
					catch
					{
					}
				}
			}
			catch
			{
			}
		}
		if (list2.Count > 0)
		{
			list2 = list2.OrderByDescending((MissingRunItem i) => i.LastRun).ToList();
			string text = string.Join(", ", from i in list2.Take(6)
				select i.Name);
			if (list2.Count > 6)
			{
				text += $" … (+{list2.Count - 6})";
			}
			list.Add(new TraceSignal
			{
				Level = "info",
				Title = "Свежие .exe (за 48 часов)",
				Items = list2,
				Detail = "Недавно появились исполняемые файлы в пользовательских папках: " + text + "."
			});
		}
	}

	private static void CheckPersistence(List<TraceSignal> list)
	{
		List<string> list2 = new List<string>();
		List<MissingRunItem> list3 = new List<MissingRunItem>();
		(RegistryKey, string)[] array = new(RegistryKey, string)[4]
		{
			(Registry.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Run"),
			(Registry.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\RunOnce"),
			(Hklm64(), "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run"),
			(Hklm64(), "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce")
		};
		for (int i = 0; i < array.Length; i++)
		{
			var (registryKey, name) = array[i];
			try
			{
				using RegistryKey registryKey2 = registryKey.OpenSubKey(name);
				if (registryKey2 == null)
				{
					continue;
				}
				string[] valueNames = registryKey2.GetValueNames();
				foreach (string text in valueNames)
				{
					string text2 = registryKey2.GetValue(text)?.ToString() ?? "";
					if (IsUserPath(text2))
					{
						list2.Add(text + " → " + Path.GetFileName(text2.Trim('"')));
						list3.Add(new MissingRunItem
						{
							Name = text,
							Path = text2.Trim('"')
						});
					}
				}
			}
			catch
			{
			}
		}
		try
		{
			string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
			if (Directory.Exists(folderPath))
			{
				foreach (string item in Directory.EnumerateFiles(folderPath))
				{
					switch (Path.GetExtension(item).ToLowerInvariant())
					{
					case ".exe":
					case ".bat":
					case ".cmd":
					case ".vbs":
					case ".ps1":
						list2.Add("Автозагрузка: " + Path.GetFileName(item));
						list3.Add(new MissingRunItem
						{
							Name = "Автозагрузка",
							Path = item
						});
						break;
					}
				}
			}
		}
		catch
		{
		}
		if (list2.Count > 0)
		{
			list.Add(new TraceSignal
			{
				Level = "warn",
				Title = "Автозапуск из пользовательских папок",
				Items = list3,
				DetailCols = new string[3] { "Элемент", "Путь", null },
				Detail = "Закреплено в автозапуске (нетипично для системных программ): " + string.Join(", ", list2.Distinct().Take(6)) + "."
			});
		}
	}

	private static string RunCapture(string exe, string args, int timeoutMs)
	{
		try
		{
			using Process process = Process.Start(new ProcessStartInfo(exe, args)
			{
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true
			});
			if (process == null)
			{
				return null;
			}
			string result = process.StandardOutput.ReadToEnd();
			if (!process.WaitForExit(timeoutMs))
			{
				try
				{
					process.Kill();
				}
				catch
				{
				}
				return null;
			}
			return result;
		}
		catch
		{
			return null;
		}
	}

	private static string SysnativeExe(string exeName)
	{
		string text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Sysnative", exeName);
		if (!File.Exists(text))
		{
			return exeName;
		}
		return text;
	}

	private static List<BamEntry> ReadBam()
	{
		List<BamEntry> list = new List<BamEntry>();
		try
		{
			string text = WindowsIdentity.GetCurrent().User?.Value;
			if (string.IsNullOrEmpty(text))
			{
				return list;
			}
			string[] array = new string[2]
			{
				"SYSTEM\\CurrentControlSet\\Services\\bam\\State\\UserSettings\\" + text,
				"SYSTEM\\CurrentControlSet\\Services\\bam\\UserSettings\\" + text
			};
			foreach (string name in array)
			{
				using RegistryKey registryKey = Hklm64().OpenSubKey(name);
				if (registryKey == null)
				{
					continue;
				}
				string[] valueNames = registryKey.GetValueNames();
				foreach (string text2 in valueNames)
				{
					if (string.IsNullOrEmpty(text2) || text2 == "Version" || text2 == "SequenceNumber" || text2.IndexOf(".exe", StringComparison.OrdinalIgnoreCase) < 0)
					{
						continue;
					}
					try
					{
						if (registryKey.GetValue(text2) is byte[] array2 && array2.Length >= 8)
						{
							DateTime last = DateTime.FromFileTimeUtc(BitConverter.ToInt64(array2, 0)).ToLocalTime();
							list.Add(new BamEntry
							{
								ExeName = FileNameOf(text2),
								FullPath = text2,
								Last = last
							});
						}
					}
					catch
					{
					}
				}
				break;
			}
		}
		catch
		{
		}
		return list;
	}

	private static HashSet<string> ReadPrefetchPrefixes(out bool accessible, out int count, out DateTime newest)
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		accessible = false;
		count = 0;
		newest = DateTime.MinValue;
		string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch");
		try
		{
			string[] files = Directory.GetFiles(path, "*.pf");
			accessible = true;
			count = files.Length;
			string[] array = files;
			foreach (string path2 in array)
			{
				try
				{
					DateTime lastWriteTime = File.GetLastWriteTime(path2);
					if (lastWriteTime > newest)
					{
						newest = lastWriteTime;
					}
				}
				catch
				{
				}
				string text = Path.GetFileName(path2).ToUpperInvariant();
				int num = text.LastIndexOf('-');
				if (num > 0)
				{
					hashSet.Add(text.Substring(0, num));
				}
			}
		}
		catch (UnauthorizedAccessException)
		{
			accessible = false;
		}
		catch
		{
			accessible = false;
		}
		return hashSet;
	}

	private static DateTime? NewestEvent(string logName, int id)
	{
		try
		{
			string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
			string text = Path.Combine(folderPath, "Sysnative", "WindowsPowerShell", "v1.0", "powershell.exe");
			string fileName = (File.Exists(text) ? text : "powershell.exe");
			string arguments = $"-NoProfile -NonInteractive -Command \"$e=Get-WinEvent -FilterHashtable @{{LogName='{logName}';Id={id}}} -MaxEvents 1 -ErrorAction SilentlyContinue; if($e){{$e.TimeCreated.ToString('o')}}\"";
			using Process process = Process.Start(new ProcessStartInfo(fileName, arguments)
			{
				RedirectStandardOutput = true,
				UseShellExecute = false,
				CreateNoWindow = true
			});
			if (process == null)
			{
				return null;
			}
			string text2 = process.StandardOutput.ReadToEnd();
			if (!process.WaitForExit(6000))
			{
				try
				{
					process.Kill();
				}
				catch
				{
				}
				return null;
			}
			text2 = text2?.Trim();
			if (string.IsNullOrWhiteSpace(text2))
			{
				return null;
			}
			DateTime result;
			return DateTime.TryParse(text2, null, DateTimeStyles.RoundtripKind, out result) ? new DateTime?(result) : ((DateTime?)null);
		}
		catch
		{
			return null;
		}
	}

	private static DateTime OsInstallDate()
	{
		try
		{
			using RegistryKey registryKey = Hklm64().OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion");
			if (registryKey?.GetValue("InstallDate") is int num && num > 0)
			{
				return DateTimeOffset.FromUnixTimeSeconds(num).LocalDateTime;
			}
		}
		catch
		{
		}
		return DateTime.MinValue;
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

	private static bool IsCleaner(string exeName)
	{
		string l = (exeName ?? "").ToLowerInvariant();
		return CleanerKeys.Any((string k) => l.Contains(k));
	}

	private static string FileNameOf(string devicePath)
	{
		if (string.IsNullOrEmpty(devicePath))
		{
			return devicePath;
		}
		int num = devicePath.LastIndexOf('\\');
		if (num < 0)
		{
			return devicePath;
		}
		return devicePath.Substring(num + 1);
	}

	private static string PrettyPath(string devicePath)
	{
		if (string.IsNullOrEmpty(devicePath))
		{
			return devicePath;
		}
		Match match = Regex.Match(devicePath, "^\\\\Device\\\\HarddiskVolume\\d+(?<rest>\\\\.*)$", RegexOptions.IgnoreCase);
		if (!match.Success)
		{
			return devicePath;
		}
		return match.Groups["rest"].Value;
	}

	private static RegistryKey Hklm64()
	{
		return RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
	}

	private static bool IsElevated()
	{
		try
		{
			using WindowsIdentity ntIdentity = WindowsIdentity.GetCurrent();
			return new WindowsPrincipal(ntIdentity).IsInRole(WindowsBuiltInRole.Administrator);
		}
		catch
		{
			return false;
		}
	}
}
