using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace NiposChecker.Services;

public static class SelfIntegrity
{
	private static readonly Dictionary<string, string> _known = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
	{
		["browserdownloadsview.exe"] = "7034EBFB236C1CDF85CDAC041BC80F6143A55680AF32CC4AF22A3379C9A71B4A",
		["browsinghistoryview.exe"] = "9F65528998B39F2E7239F89A56DED47EA865EEA2D6B82B300CD5DE7E62072CF0",
		["devmanview.exe"] = "269F9C9A117508EB62B1E5C4F0AA5AB75307A36FADEFCA3DACD39C1C9BB56343",
		["everything.exe"] = "D3A6ED07BD3B52C62411132D060560F9C0C88CE183851F16B632A99B4D4E7581",
		["executedprogramslist.exe"] = "E3AF98717BF1CDA7DC4AACB5B34D111AC237604161CD96F7929EC33F2FF260B6",
		["jumplistsview.exe"] = "E000C782AF989E016EFCEF1664B9D652B0FEE59B011E28154072F7B6001B124D",
		["lastactivityview.exe"] = "B1B7A772C927B4D3E2E4D59BA69E3FE955506FF80CEE0947D54C6B3FABEF6860",
		["muicacheview.exe"] = "02E28FA849121A1FFCE2CCCDFAED4974636253C3A8D5F16207D0FD13C0EA72D5",
		["openedfilesview.exe"] = "BFB1A374772CCCC06440EE3DEF14D6556D3B51C9E6DE95B69917798B235E733B",
		["registryfinder.exe"] = "A6128839289730E45E1AB3412CE546D32E3C1F97D950EF8FABAECD4C362A3096",
		["shellbaganalyzer.exe"] = "C93A0F4C6B5F24EE31CDDB92B0EA3337021B5FB91FAAE8A381D3BD2C9B6ADD54",
		["shellbagsview.exe"] = "624A01ED209F21E55AECD2C81E818401CFAA5CB82AAAF62DE5142FC8F1DEF6F4",
		["systeminformer/peview.exe"] = "6A1E34AE9C3D1F2B8DB3DE8B3B6429CFACFAF971354C97D3662F1FD9F5511BA6",
		["systeminformer/systeminformer.exe"] = "99EF6F5A131400282AB53910262A33D5CC08A3948E39700F344D64D34C092524",
		["systeminformer/x86/systeminformer.exe"] = "CC999E12FF59E18A56329C8BDD4A695C37AFE7FD1F5D8B20FB2F66CA06B472D9",
		["usbdeview.exe"] = "D9C7C59BBCAEA076172F87C4E6FD042E891306BA08A55A007BB58657818F7902",
		["userassistview.exe"] = "BC11C4150BBC6F8B2CF7BC96BEDBB183C61D53AB8E4052B15D58BAD6B6D1BEFA"
	};

	public static (List<string> tampered, List<string> missing) Check()
	{
		List<string> list = new List<string>();
		List<string> list2 = new List<string>();
		string path;
		try
		{
			path = Path.Combine(AppContext.BaseDirectory, "Apps");
		}
		catch
		{
			return (tampered: list, missing: list2);
		}
		foreach (KeyValuePair<string, string> item in _known)
		{
			string path2 = Path.Combine(path, item.Key.Replace('/', '\\'));
			if (!File.Exists(path2))
			{
				list2.Add(Path.GetFileName(item.Key));
				continue;
			}
			try
			{
				using FileStream inputStream = File.OpenRead(path2);
				using SHA256 sHA = SHA256.Create();
				if (!Convert.ToHexString(sHA.ComputeHash(inputStream)).Equals(item.Value, StringComparison.OrdinalIgnoreCase))
				{
					list.Add(Path.GetFileName(item.Key));
				}
			}
			catch
			{
			}
		}
		return (tampered: list, missing: list2);
	}
}
