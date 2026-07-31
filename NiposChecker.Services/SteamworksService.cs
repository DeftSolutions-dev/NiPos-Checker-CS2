using System;
using NiposChecker.Localization;
using NiposChecker.Views;
using Steamworks;

namespace NiposChecker.Services;

public static class SteamworksService
{
	public static bool Init()
	{
		if (!SteamAPI.IsSteamRunning())
		{
			AppDialog.Alert(null, "NIPOS CHECKER", Strings.Get("Msg_SteamNotRunning"), null, DialogKind.Warning);
			return false;
		}
		return SteamAPI.Init();
	}

	public static string GetCurrentSteamID()
	{
		try
		{
			return SteamUser.GetSteamID().ToString();
		}
		catch
		{
			return null;
		}
	}

	public static uint GetEarliestPurchaseUnixTime()
	{
		try
		{
			return SteamApps.GetEarliestPurchaseUnixTime(new AppId_t(730u));
		}
		catch
		{
			return 0u;
		}
	}

	public static DateTime? GetPurchaseDate()
	{
		uint earliestPurchaseUnixTime = GetEarliestPurchaseUnixTime();
		if (earliestPurchaseUnixTime == 0)
		{
			return null;
		}
		return DateTimeOffset.FromUnixTimeSeconds(earliestPurchaseUnixTime).LocalDateTime;
	}

	public static string GetAppInstallDir()
	{
		try
		{
			SteamApps.GetAppInstallDir(new AppId_t(730u), out var pchFolder, 260u);
			return pchFolder;
		}
		catch
		{
			return null;
		}
	}

	public static void Shutdown()
	{
		try
		{
			SteamAPI.Shutdown();
		}
		catch
		{
		}
	}

	public static string GetLevelColorHex(int lvl)
	{
		switch (lvl)
		{
		case 0:
		case 1:
		case 2:
		case 3:
		case 4:
		case 5:
		case 6:
		case 7:
		case 8:
		case 9:
			return "#9c9e9c";
		case 10:
		case 11:
		case 12:
		case 13:
		case 14:
		case 15:
		case 16:
		case 17:
		case 18:
		case 19:
			return "#c02942";
		case 20:
		case 21:
		case 22:
		case 23:
		case 24:
		case 25:
		case 26:
		case 27:
		case 28:
		case 29:
			return "#d95b43";
		case 30:
		case 31:
		case 32:
		case 33:
		case 34:
		case 35:
		case 36:
		case 37:
		case 38:
		case 39:
			return "#fecc23";
		case 40:
		case 41:
		case 42:
		case 43:
		case 44:
		case 45:
		case 46:
		case 47:
		case 48:
		case 49:
			return "#467a3c";
		case 50:
		case 51:
		case 52:
		case 53:
		case 54:
		case 55:
		case 56:
		case 57:
		case 58:
		case 59:
			return "#4e8ddb";
		case 60:
		case 61:
		case 62:
		case 63:
		case 64:
		case 65:
		case 66:
		case 67:
		case 68:
		case 69:
			return "#7652c9";
		case 70:
		case 71:
		case 72:
		case 73:
		case 74:
		case 75:
		case 76:
		case 77:
		case 78:
		case 79:
			return "#c252c9";
		case 80:
		case 81:
		case 82:
		case 83:
		case 84:
		case 85:
		case 86:
		case 87:
		case 88:
		case 89:
			return "#542437";
		default:
			return "#997c52";
		}
	}
}
