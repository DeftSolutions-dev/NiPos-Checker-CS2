using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using NiposChecker.Models;

namespace NiposChecker.Services;

public static class CandidateBuilder
{
	public static string BuildJson(IEnumerable<FoundedFile> files)
	{
		if (files == null)
		{
			return null;
		}
		List<object> list = new List<object>();
		foreach (FoundedFile file in files)
		{
			if (file == null || !file.IsDetected || (file.Severity != "red" && file.Severity != "amber"))
			{
				continue;
			}
			string text = file.MatchedSignals ?? "";
			if (text == "ext" || text.Contains("hash"))
			{
				continue;
			}
			string signer = "";
			long length;
			string text2;
			try
			{
				FileInfo fileInfo = new FileInfo(file.Path);
				if (!fileInfo.Exists || fileInfo.Length > 62914560)
				{
					continue;
				}
				length = fileInfo.Length;
				using (FileStream inputStream = fileInfo.OpenRead())
				{
					using SHA256 sHA = SHA256.Create();
					text2 = Convert.ToHexString(sHA.ComputeHash(inputStream));
					signer = SignerCn(file.Path) ?? "";
				}
				goto IL_0117;
			}
			catch
			{
			}
			continue;
			IL_0117:
			if (!string.IsNullOrEmpty(text2))
			{
				list.Add(new
				{
					sha256 = text2,
					filename = (file.Name ?? ""),
					signer = signer,
					size = length,
					cheat_guess = (file.CheatName ?? ""),
					severity = file.Severity,
					signals = text,
					path_hint = (file.Path ?? "")
				});
			}
		}
		if (list.Count != 0)
		{
			return JsonConvert.SerializeObject(list);
		}
		return null;
	}

	private static string SignerCn(string path)
	{
		try
		{
			using X509Certificate2 x509Certificate = new X509Certificate2(path);
			Match match = Regex.Match(x509Certificate.Subject, "CN\\s*=\\s*([^,]+)", RegexOptions.IgnoreCase);
			return match.Success ? match.Groups[1].Value.Trim() : null;
		}
		catch
		{
			return null;
		}
	}
}
