using Newtonsoft.Json;

namespace NiposChecker.Models;

public class SteamProfileJson
{
	[JsonProperty("steamid")]
	public string SteamId { get; set; }

	[JsonProperty("personaname")]
	public string PersonaName { get; set; }

	[JsonProperty("profileurl")]
	public string ProfileUrl { get; set; }

	[JsonProperty("realName")]
	public string RealName { get; set; }

	[JsonProperty("communityvisibilitystate")]
	public int CommunityVisibilityState { get; set; }

	[JsonProperty("profilestate")]
	public int ProfileState { get; set; }

	[JsonProperty("commentpermission")]
	public int CommentPermission { get; set; }

	[JsonProperty("avatar")]
	public string Avatar { get; set; }

	[JsonProperty("avatar_string")]
	public string AvatarString { get; set; }

	[JsonProperty("avatarmedium")]
	public string AvatarMedium { get; set; }

	[JsonProperty("avatarfull")]
	public string AvatarFull { get; set; }

	[JsonProperty("avatarhash")]
	public string AvatarHash { get; set; }

	[JsonProperty("personastate")]
	public int PersonaState { get; set; }

	[JsonProperty("primaryclanid")]
	public string PrimaryClanId { get; set; }

	[JsonProperty("timecreated")]
	public long TimeCreated { get; set; }

	[JsonProperty("personastateflags")]
	public int PersonaStateFlags { get; set; }

	[JsonProperty("loccountrycode")]
	public string LocCountryCode { get; set; }

	[JsonProperty("locstatecode")]
	public string LocStateCode { get; set; }

	[JsonProperty("loccityid")]
	public int LocCityId { get; set; }

	[JsonProperty("level")]
	public int? Level { get; set; }

	[JsonProperty("daysSinceBan")]
	public int? DaysSinceBan { get; set; }

	[JsonProperty("vacBanned")]
	public bool VacBanned { get; set; }
}
