using System;
using System.Collections.Generic;
using DiscordRPC;
using NiposChecker.Localization;
using NiposChecker.Models;

namespace NiposChecker.Services;

public static class DiscordService
{
	private static DiscordRpcClient _client;

	public static bool Enabled { get; set; } = true;

	public static void Initialize(ProjectInfo info)
	{
		if (!Enabled || info == null || string.IsNullOrWhiteSpace(info.DiscordRTC_ID))
		{
			return;
		}
		try
		{
			_client = new DiscordRpcClient(info.DiscordRTC_ID);
			_client.Initialize();
			Button[] array = BuildButtons(info);
			_client.SetPresence(new RichPresence
			{
				Details = "\ud83d\udcd1 " + Strings.Get("Discord_Checking"),
				Timestamps = Timestamps.Now,
				Buttons = ((array.Length != 0) ? array : null),
				Assets = new Assets
				{
					LargeImageKey = "image_large",
					LargeImageText = (info.CheckerName ?? "NIPOS CHECKER"),
					SmallImageKey = "smallicon"
				}
			});
		}
		catch (Exception)
		{
		}
	}

	private static Button[] BuildButtons(ProjectInfo info)
	{
		List<Button> list = new List<Button>();
		if (!string.IsNullOrWhiteSpace(info.LinkToWebsite))
		{
			list.Add(new Button
			{
				Label = "\ud83c\udf10 " + Strings.Get("Discord_OpenSite"),
				Url = info.LinkToWebsite
			});
		}
		if (!string.IsNullOrWhiteSpace(info.LinkToDiscord))
		{
			list.Add(new Button
			{
				Label = "\ud83d\udd17 Discord",
				Url = info.LinkToDiscord
			});
		}
		if (list.Count <= 2)
		{
			return list.ToArray();
		}
		return list.GetRange(0, 2).ToArray();
	}

	public static void Shutdown()
	{
		try
		{
			_client?.Dispose();
		}
		catch
		{
		}
		_client = null;
	}
}
