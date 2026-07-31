namespace NiposChecker.Models;

public class Account
{
	public string SteamId { get; set; }

	public string Timestamp { get; set; }

	public Account()
	{
	}

	public Account(string steamId, string timestamp)
	{
		SteamId = steamId;
		Timestamp = timestamp;
	}
}
