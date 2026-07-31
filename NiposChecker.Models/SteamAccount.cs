using System;
using NiposChecker.Localization;

namespace NiposChecker.Models;

public class SteamAccount
{
	public string SteamID64 { get; set; }

	public string Nickname { get; set; }

	public bool Vac_Ban { get; set; }

	public string Avatar { get; set; }

	public string RegistrationDate { get; set; }

	public string ProfileType { get; set; }

	public string RealName { get; set; }

	public string LastActivity { get; set; }

	public int Lvl { get; set; }

	public int VAC_SinceBan { get; set; }

	public bool isBannedOnProject { get; set; }

	public string LevelColorHex { get; set; } = "#9c9e9c";

	public bool IsCurrent { get; set; }

	public string ProfileTypeFormatted => ProfileType switch
	{
		"1" => Strings.Get("Profile_Hidden"), 
		"2" => Strings.Get("Profile_Open"), 
		"3" => Strings.Get("Profile_Friends"), 
		_ => Strings.Get("Word_Unknown"), 
	};

	public string VacFormatted
	{
		get
		{
			if (!Vac_Ban)
			{
				return "";
			}
			if (VAC_SinceBan <= 0)
			{
				return Strings.Get("Vac_Yes");
			}
			return Strings.Get("Vac_YesDays", VAC_SinceBan);
		}
	}

	public string ProjectBanBadge
	{
		get
		{
			if (!isBannedOnProject)
			{
				return "";
			}
			return Strings.Get("Steam_ProjectBanBadge");
		}
	}

	public string LvlFormatted => $"Lvl {Lvl}";

	public string LastActivityFormatted
	{
		get
		{
			if (!long.TryParse(LastActivity, out var result) || result == 0L)
			{
				return "";
			}
			try
			{
				return DateTimeOffset.FromUnixTimeSeconds(result).LocalDateTime.ToString("dd.MM.yyyy HH:mm");
			}
			catch
			{
				return "";
			}
		}
	}
}
