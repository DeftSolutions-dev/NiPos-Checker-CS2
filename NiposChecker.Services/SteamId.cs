using System.Text.RegularExpressions;

namespace NiposChecker.Services;

public static class SteamId
{
	private const long Steam64Offset = 76561197960265728L;

	private static readonly Regex Steam2Regex = new Regex("^STEAM_0:[0-1]:([0-9]{1,10})$");

	private static readonly Regex Steam32Regex = new Regex("^U:1:([0-9]{1,10})$");

	private static readonly Regex Steam64Regex = new Regex("^7656119([0-9]{10})$");

	public static string FromSteam32ToSteam64(long id32)
	{
		return (id32 + 76561197960265728L).ToString();
	}

	public static long FromSteam64ToSteam32(string id64)
	{
		if (long.TryParse(id64, out var result))
		{
			return result - 76561197960265728L;
		}
		return 0L;
	}

	public static string FromSteam2ToSteam64(string steam2)
	{
		Match match = Steam2Regex.Match(steam2);
		if (!match.Success)
		{
			return null;
		}
		return (long.Parse(match.Groups[1].Value) * 2 + 76561197960265728L).ToString();
	}

	public static string FromSteam64ToSteam2(string id64)
	{
		long num = FromSteam64ToSteam32(id64);
		if (num <= 0)
		{
			return null;
		}
		long value = num % 2;
		long value2 = num / 2;
		return $"STEAM_0:{value}:{value2}";
	}
}
