using Newtonsoft.Json;

namespace NiposChecker.Models;

public class ProjectInfo
{
	[JsonProperty("checkid")]
	public string CheckId { get; set; } = "";

	[JsonProperty("Name")]
	public string Name { get; set; } = "NIPOS CHECKER";

	[JsonProperty("CheckerName")]
	public string CheckerName { get; set; } = "NIPOS CHECKER";

	[JsonProperty("project_name")]
	public string ProjectName { get; set; } = "";

	[JsonProperty("LastVersion")]
	public string LastVersion { get; set; } = "1.1";

	[JsonProperty("LinkToWebsite")]
	public string LinkToWebsite { get; set; } = "";

	[JsonProperty("update_url")]
	public string UpdateUrl { get; set; } = "";

	[JsonProperty("LinkToDiscord")]
	public string LinkToDiscord { get; set; } = "";

	[JsonProperty("LinkToVK")]
	public string LinkToVK { get; set; } = "";

	[JsonProperty("DiscordRTC_ID")]
	public string DiscordRTC_ID { get; set; } = "";

	[JsonProperty("ip")]
	public string IP { get; set; } = "";

	[JsonProperty("country_code")]
	public string CountryCode { get; set; } = "";

	[JsonProperty("flag_url")]
	public string FlagURL { get; set; } = "";

	[JsonProperty("main_logourl")]
	public string MainLogoUrl { get; set; } = "";

	[JsonProperty("white_logourl")]
	public string WhiteLogoUrl { get; set; } = "";

	[JsonProperty("software_icon")]
	public string SoftwareIcon { get; set; } = "";

	[JsonProperty("brand_color")]
	public string BrandColor { get; set; } = "";

	[JsonProperty("logoMargin")]
	public string LogoMargin { get; set; } = "0,0,0,0";

	[JsonProperty("logowidth")]
	public int LogoWidth { get; set; }

	[JsonProperty("logoheight")]
	public int LogoHeight { get; set; }

	[JsonProperty("banner1_url")]
	public string Banner1Url { get; set; } = "";

	[JsonProperty("banner2_url")]
	public string Banner2Url { get; set; } = "";

	[JsonProperty("banner1_linkurl")]
	public string Banner1LinkUrl { get; set; } = "";

	[JsonProperty("banner2_linkurl")]
	public string Banner2LinkUrl { get; set; } = "";

	[JsonProperty("is_show_banner1")]
	public string IsShowBanner1Raw { get; set; } = "0";

	[JsonProperty("is_show_banner2")]
	public string IsShowBanner2Raw { get; set; } = "0";

	[JsonProperty("banner1_texttop")]
	public string Banner1TextTop { get; set; } = "";

	[JsonProperty("banner1_textlink")]
	public string Banner1TextLink { get; set; } = "";

	[JsonProperty("banner2_texttop")]
	public string Banner2TextTop { get; set; } = "";

	[JsonProperty("banner2_textdown")]
	public string Banner2TextDown { get; set; } = "";

	[JsonProperty("banner2_textlink")]
	public string Banner2TextLink { get; set; } = "";

	[JsonIgnore]
	public bool IsShowBanner1
	{
		get
		{
			if (!(IsShowBanner1Raw == "1"))
			{
				return IsShowBanner1Raw == "true";
			}
			return true;
		}
	}

	[JsonIgnore]
	public bool IsShowBanner2
	{
		get
		{
			if (!(IsShowBanner2Raw == "1"))
			{
				return IsShowBanner2Raw == "true";
			}
			return true;
		}
	}

	[JsonProperty("is_soft_banned")]
	public string IsSoftBanned { get; set; } = "0";

	[JsonProperty("ban_reason")]
	public string SoftwareBanReason { get; set; } = "";

	[JsonIgnore]
	public bool IsSoftwareBanned
	{
		get
		{
			if (!(IsSoftBanned == "1"))
			{
				return IsSoftBanned == "true";
			}
			return true;
		}
	}
}
