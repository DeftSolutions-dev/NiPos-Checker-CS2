using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NiposChecker.Models;

namespace NiposChecker.Services;

public static class ReportBuilder
{
	public static async Task<string> BuildAsync(ApiClient api, CheatDatabase db, ProjectInfo pi, List<SteamAccount> accounts, IEnumerable<FoundedFile> searchResults)
	{
		List<TraceSignal> traces = await Task.Run(() => CleanupDetector.Run(db));
		List<ProcessItem> procItems = (await Task.Run(() => ProcessScanner.Scan(db))).Item1;
		BlockDbService svc = new BlockDbService(api);
		List<object> accList = new List<object>();
		List<object> bans = new List<object>();
		int bannedAccounts = 0;
		foreach (SteamAccount a in accounts ?? new List<SteamAccount>())
		{
			bool projBan = false;
			try
			{
				List<BanInfo> list = await svc.CheckSteamIdAsync(a.SteamID64);
				projBan = BlockDbService.HasProjectBan(list, pi?.ProjectName);
				foreach (BanInfo item in list ?? new List<BanInfo>())
				{
					bans.Add(new
					{
						steamid = item.SteamId64,
						reason = item.Reason,
						project = item.ProjectName,
						active = item.IsActive,
						created = item.CreatedAtFormatted
					});
				}
			}
			catch
			{
			}
			if (projBan)
			{
				bannedAccounts++;
			}
			accList.Add(new
			{
				nick = a.Nickname,
				steamid = a.SteamID64,
				vac = a.Vac_Ban,
				projectBan = projBan
			});
		}
		var list2 = (searchResults ?? Enumerable.Empty<FoundedFile>()).Select((FoundedFile f) => new
		{
			name = f.Name,
			cheat = f.CheatName,
			type = f.Type,
			size = f.Weight,
			modified = f.LastChange,
			path = f.Path,
			severity = f.Severity,
			severityLabel = f.SeverityLabel,
			source = f.Source
		}).ToList();
		return JsonConvert.SerializeObject(new Dictionary<string, object>
		{
			["checkid"] = pi?.CheckId ?? "",
			["project"] = pi?.ProjectName ?? "",
			["created"] = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"),
			["hwid"] = App.HWID ?? "",
			["steam"] = App.CurrentSteamID ?? accounts?.FirstOrDefault((SteamAccount x) => x.IsCurrent)?.SteamID64 ?? "",
			["windows"] = SystemInfo(),
			["vm"] = DetectVm(),
			["summary"] = new
			{
				filesCritical = list2.Count(f => f.severity == "red"),
				filesTotal = list2.Count,
				tracesAlerts = traces.Count((TraceSignal t) => t.Level == "alert"),
				tracesWarns = traces.Count((TraceSignal t) => t.Level == "warn"),
				procAlerts = procItems.Count((ProcessItem p) => p.Level == "alert"),
				procWarns = procItems.Count((ProcessItem p) => p.Level == "warn"),
				bannedAccounts = bannedAccounts
			},
			["sections"] = new
			{
				files = list2,
				processes = procItems.Select((ProcessItem p) => new
				{
					level = p.Level,
					name = p.Name,
					pid = p.Pid,
					path = p.Path,
					note = p.Note,
					tag = p.TagText
				}).ToList(),
				traces = traces.Select((TraceSignal t) => new
				{
					level = t.Level,
					title = t.Title,
					detail = t.Detail,
					tag = t.TagText
				}).ToList(),
				accounts = accList,
				bans = bans
			}
		});
	}

	private static string SystemInfo()
	{
		try
		{
			return $"{Environment.OSVersion.VersionString} · {Environment.MachineName}\\{Environment.UserName} · CPU x{Environment.ProcessorCount}";
		}
		catch
		{
			return "";
		}
	}

	private static string DetectVm()
	{
		try
		{
			WindowsInfo windowsInfo = NiposChecker.Services.SystemInfo.Gather();
			return (windowsInfo == null || !windowsInfo.DetectVM) ? "" : (string.IsNullOrEmpty(windowsInfo.VMName) ? "да" : windowsInfo.VMName);
		}
		catch
		{
			return "";
		}
	}
}
