using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace NiposChecker.Services;

public static class SteamCleaningDetector
{
	public static List<string> CollectHiddenSteamIds(IEnumerable<string> knownSteamIds)
	{
		HashSet<string> other = new HashSet<string>(knownSteamIds ?? Enumerable.Empty<string>());
		HashSet<string> hashSet = new HashSet<string>();
		try
		{
			string text = SteamLocal.FindSteamInstallPath();
			if (string.IsNullOrEmpty(text) || !Directory.Exists(text))
			{
				return new List<string>();
			}
			string text2 = Path.Combine(text, "config");
			try
			{
				if (Directory.Exists(text2))
				{
					string[] files = Directory.GetFiles(text2, "coplay_*.vdf");
					for (int i = 0; i < files.Length; i++)
					{
						AddIfSteam64(Path.GetFileNameWithoutExtension(files[i]).Replace("coplay_", ""), hashSet);
					}
				}
			}
			catch
			{
			}
			try
			{
				string path = Path.Combine(text2, "avatarcache");
				if (Directory.Exists(path))
				{
					string[] files = Directory.GetFiles(path, "*.png");
					for (int i = 0; i < files.Length; i++)
					{
						AddIfSteam64(Path.GetFileNameWithoutExtension(files[i]), hashSet);
					}
				}
			}
			catch
			{
			}
			try
			{
				string path2 = Path.Combine(text2, "config.vdf");
				if (File.Exists(path2))
				{
					foreach (Match item in Regex.Matches(File.ReadAllText(path2), "7656119\\d{10}"))
					{
						AddIfSteam64(item.Value, hashSet);
					}
				}
			}
			catch
			{
			}
		}
		catch
		{
		}
		hashSet.ExceptWith(other);
		return hashSet.ToList();
	}

	private static void AddIfSteam64(string id, HashSet<string> set)
	{
		if (!string.IsNullOrWhiteSpace(id) && id.Length == 17 && id.StartsWith("7656119") && long.TryParse(id, out var _))
		{
			set.Add(id);
		}
	}
}
