using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NiposChecker.Models;

namespace NiposChecker.Services;

public class BlockDbService
{
	private readonly ApiClient _api;

	public BlockDbService(ApiClient api)
	{
		_api = api;
	}

	public static bool IsForProject(BanInfo ban, string projectName)
	{
		if (ban == null)
		{
			return false;
		}
		if (string.IsNullOrWhiteSpace(projectName))
		{
			return true;
		}
		return string.Equals(ban.ProjectName?.Trim(), projectName.Trim(), StringComparison.OrdinalIgnoreCase);
	}

	public static int ActiveProjectBanCount(IEnumerable<BanInfo> bans, string projectName)
	{
		return bans?.Count((BanInfo b) => b.IsActive && IsForProject(b, projectName)) ?? 0;
	}

	public static bool HasProjectBan(IEnumerable<BanInfo> bans, string projectName)
	{
		return ActiveProjectBanCount(bans, projectName) > 0;
	}

	public async Task<List<BanInfo>> CheckSteamIdAsync(string steamId)
	{
		return Parse(await _api.BlockDbLookupAsync(steamId));
	}

	public async Task<List<BanInfo>> CheckIpAsync(string ip)
	{
		return Parse(await _api.BlockDbLookupAsync(ip));
	}

	private static List<BanInfo> Parse(JToken token)
	{
		List<BanInfo> list = new List<BanInfo>();
		if (token == null)
		{
			return list;
		}
		try
		{
			if (token.Type == JTokenType.Object)
			{
				List<BanInfo> list2 = token["bans"]?.ToObject<List<BanInfo>>();
				if (list2 != null)
				{
					list.AddRange(list2);
				}
				return list;
			}
			if (token.Type == JTokenType.Array)
			{
				List<BlockDbOffender> list3 = token.ToObject<List<BlockDbOffender>>();
				if (list3 == null)
				{
					return list;
				}
				foreach (BlockDbOffender item in list3)
				{
					if (item.Bans == null)
					{
						continue;
					}
					BlockDbSteamId[] steamIds = item.SteamIds;
					string steamId = ((steamIds != null && steamIds.Length != 0) ? item.SteamIds[0].SteamId64 : "");
					BlockDbIp[] ips = item.Ips;
					string userIp = ((ips != null && ips.Length != 0) ? item.Ips[0].Value : "");
					BanInfo[] bans = item.Bans;
					foreach (BanInfo banInfo in bans)
					{
						if (string.IsNullOrEmpty(banInfo.SteamId64))
						{
							banInfo.SteamId64 = steamId;
						}
						if (string.IsNullOrEmpty(banInfo.UserIp))
						{
							banInfo.UserIp = userIp;
						}
						list.Add(banInfo);
					}
				}
			}
		}
		catch (Exception)
		{
		}
		return list;
	}
}
