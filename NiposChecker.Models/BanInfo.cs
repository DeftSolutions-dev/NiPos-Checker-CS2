using System;
using System.Windows;
using System.Windows.Media;
using Newtonsoft.Json;
using NiposChecker.Localization;

namespace NiposChecker.Models;

public class BanInfo
{
	[JsonProperty("id")]
	public int Id { get; set; }

	[JsonProperty("discord_id")]
	public long DiscordId { get; set; }

	[JsonProperty("steamid64")]
	public string SteamId64 { get; set; }

	[JsonProperty("user_ip")]
	public string UserIp { get; set; }

	[JsonProperty("reason")]
	public string Reason { get; set; }

	[JsonProperty("project_name")]
	public string ProjectName { get; set; }

	[JsonProperty("duration")]
	public int Duration { get; set; }

	[JsonProperty("created_at")]
	public long CreatedAt { get; set; }

	[JsonProperty("is_active")]
	public bool? IsActiveJson { get; set; }

	public bool IsPermanent => Duration == 0;

	public bool IsActive
	{
		get
		{
			if (!IsActiveJson.HasValue)
			{
				if (!IsPermanent)
				{
					return DateTimeOffset.FromUnixTimeSeconds(CreatedAt).AddMinutes(Duration) > DateTimeOffset.UtcNow;
				}
				return true;
			}
			return IsActiveJson.Value;
		}
	}

	public string CreatedAtFormatted
	{
		get
		{
			if (CreatedAt <= 0)
			{
				return "—";
			}
			return DateTimeOffset.FromUnixTimeSeconds(CreatedAt).LocalDateTime.ToString("dd.MM.yyyy HH:mm");
		}
	}

	public string StatusFormatted
	{
		get
		{
			if (IsPermanent)
			{
				if (!IsActive)
				{
					return Strings.Get("Ban_LiftedPerm");
				}
				return Strings.Get("Ban_Permanent");
			}
			DateTime localDateTime = DateTimeOffset.FromUnixTimeSeconds(CreatedAt).AddMinutes(Duration).LocalDateTime;
			if (IsActive)
			{
				return Strings.Get("Ban_ActiveUntil", localDateTime);
			}
			return Strings.Get("Ban_Lifted", localDateTime);
		}
	}

	public string DurationFormatted
	{
		get
		{
			if (!IsPermanent)
			{
				return Strings.Get("Ban_Minutes", Duration);
			}
			return Strings.Get("Ban_Forever");
		}
	}

	private DateTime Expiry => DateTimeOffset.FromUnixTimeSeconds(CreatedAt).AddMinutes(Duration).LocalDateTime;

	public string BanState
	{
		get
		{
			if (IsActive)
			{
				if (!IsPermanent)
				{
					return "temp";
				}
				return "active";
			}
			if (IsPermanent)
			{
				return "lifted";
			}
			if (!(Expiry > DateTime.Now))
			{
				return "expired";
			}
			return "lifted";
		}
	}

	public string BanTagText => BanState switch
	{
		"active" => "Активен · перманент", 
		"temp" => $"Активен · до {Expiry:dd.MM.yyyy}", 
		"expired" => $"Истёк {Expiry:dd.MM.yyyy}", 
		_ => "Снят", 
	};

	public Brush BanTagBrush
	{
		get
		{
			string banState = BanState;
			if (!(banState == "active"))
			{
				if (banState == "temp")
				{
					return Brush(245, 190, 107);
				}
				return Brush(154, 142, 150);
			}
			return Brush(byte.MaxValue, 128, 134);
		}
	}

	public Brush BanTagFill
	{
		get
		{
			string banState = BanState;
			if (!(banState == "active"))
			{
				if (banState == "temp")
				{
					return BrushA(36, 240, 169, 60);
				}
				return Brush(28, 24, 28);
			}
			return BrushA(38, byte.MaxValue, 40, 63);
		}
	}

	public Brush BanLeftBrush
	{
		get
		{
			string banState = BanState;
			if (!(banState == "active"))
			{
				if (banState == "temp")
				{
					return Brush(240, 169, 60);
				}
				return Brush(55, 43, 55);
			}
			return Brush(byte.MaxValue, 40, 63);
		}
	}

	public double CardOpacity
	{
		get
		{
			if (!(BanState == "expired") && !(BanState == "lifted"))
			{
				return 1.0;
			}
			return 0.82;
		}
	}

	public string IssuedText => "выдан " + CreatedAtFormatted;

	public Visibility NoteVisibility
	{
		get
		{
			if (!(BanState == "active"))
			{
				return Visibility.Visible;
			}
			return Visibility.Collapsed;
		}
	}

	public string NoteText => BanState switch
	{
		"lifted" => "✓  Бан снят", 
		"expired" => $"⏱  Срок истёк {Expiry:dd.MM.yyyy HH:mm}", 
		"temp" => $"⏱  Истекает {Expiry:dd.MM.yyyy HH:mm}", 
		_ => "", 
	};

	public Brush NoteBrush
	{
		get
		{
			if (BanState == "temp")
			{
				return Brush(240, 169, 60);
			}
			return Brush(154, 142, 150);
		}
	}

	private static SolidColorBrush Brush(byte r, byte g, byte b)
	{
		return new SolidColorBrush(Color.FromRgb(r, g, b));
	}

	private static SolidColorBrush BrushA(byte a, byte r, byte g, byte b)
	{
		return new SolidColorBrush(Color.FromArgb(a, r, g, b));
	}
}
