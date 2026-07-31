using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using NiposChecker.Models;

namespace NiposChecker.Services;

public static class SteamLocal
{
	public static string FindSteamInstallPath()
	{
		using RegistryKey registryKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey("SOFTWARE\\Wow6432Node\\Valve\\Steam");
		if (registryKey?.GetValue("InstallPath") is string text && Directory.Exists(text))
		{
			return text;
		}
		using RegistryKey registryKey2 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey("Software\\Valve\\Steam");
		return (registryKey2?.GetValue("SourceModInstallPath") as string) ?? (registryKey2?.GetValue("InstallPath") as string);
	}

	public static List<Account> GetAllAccounts()
	{
		HashSet<string> hashSet = new HashSet<string>();
		List<Account> list = new List<Account>();
		string text = FindSteamInstallPath();
		if (string.IsNullOrEmpty(text) || !Directory.Exists(text))
		{
			return list;
		}
		string path = Path.Combine(text, "config", "loginusers.vdf");
		if (File.Exists(path))
		{
			string input = File.ReadAllText(path);
			foreach (Match item in new Regex("\"(\\d+)[\\s\\S]*?\"Timestamp\"\\s+\"(\\d+)\"").Matches(input))
			{
				string value = item.Groups[1].Value;
				string value2 = item.Groups[2].Value;
				if (hashSet.Add(value))
				{
					list.Add(new Account(value, value2));
				}
			}
		}
		using RegistryKey registryKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64).OpenSubKey("SOFTWARE\\Valve\\Steam\\Users");
		if (registryKey != null)
		{
			string[] subKeyNames = registryKey.GetSubKeyNames();
			for (int i = 0; i < subKeyNames.Length; i++)
			{
				if (long.TryParse(subKeyNames[i], out var result))
				{
					string text2 = SteamId.FromSteam32ToSteam64(result);
					if (hashSet.Add(text2))
					{
						list.Add(new Account(text2, "0"));
					}
				}
			}
		}
		string path2 = Path.Combine(text, "userdata");
		if (Directory.Exists(path2))
		{
			foreach (string item2 in Directory.EnumerateDirectories(path2))
			{
				if (long.TryParse(Path.GetFileName(item2), out var result2))
				{
					string text3 = SteamId.FromSteam32ToSteam64(result2);
					if (hashSet.Add(text3))
					{
						list.Add(new Account(text3, "0"));
					}
				}
			}
		}
		return list;
	}
}
