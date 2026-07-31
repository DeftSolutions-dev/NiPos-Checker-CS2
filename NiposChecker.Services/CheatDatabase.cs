using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NiposChecker.Models;

namespace NiposChecker.Services;

public class CheatDatabase
{
	private HashSet<string> _names;

	private List<string> _nameList;

	private Dictionary<string, string> _nameSeverity;

	private HashSet<string> _extensions;

	private HashSet<string> _icons;

	private List<FileHash> _signatures;

	private HashSet<string> _hashes;

	private Dictionary<string, (string cheat, string sev)> _hashInfo;

	private bool _hasSigNames;

	private bool _loaded;

	private static readonly string[] _trustedSigners = new string[10] { "esl gaming", "faceit", "valve", "riot games", "vanguard", "easyanticheat", "easy anti-cheat", "epic games", "battleye", "esea" };

	public CheatRuleEngine RuleEngine { get; private set; } = new CheatRuleEngine();

	public bool IsLoaded => _loaded;

	public async Task LoadAsync(ApiClient api)
	{
		try
		{
			DatabaseData databaseData = await api.GetDatabaseDataAsync();
			_names = new HashSet<string>((databaseData.FilesNames ?? new List<FileName>()).Select((FileName n) => n.Name?.ToLowerInvariant()), StringComparer.OrdinalIgnoreCase);
			_nameList = new List<string>();
			_nameSeverity = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			foreach (FileName item in databaseData.FilesNames ?? new List<FileName>())
			{
				string text = item.Name?.Trim();
				if (!string.IsNullOrEmpty(text))
				{
					_nameList.Add(text);
					if (!string.IsNullOrEmpty(item.Severity))
					{
						_nameSeverity[text] = item.Severity;
					}
				}
			}
			_extensions = new HashSet<string>((databaseData.FilesExtensions ?? new List<FileExtension>()).Select(delegate(FileExtension e)
			{
				string text2 = e.Extension?.Trim().ToLowerInvariant();
				if (!string.IsNullOrEmpty(text2) && text2[0] != '.')
				{
					text2 = "." + text2;
				}
				return text2;
			}), StringComparer.OrdinalIgnoreCase);
			_icons = new HashSet<string>((databaseData.FilesIcons ?? new List<FileIcon>()).Select((FileIcon i) => i.Icon), StringComparer.OrdinalIgnoreCase);
			_signatures = databaseData.FilesHashes ?? new List<FileHash>();
			_hashes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			_hashInfo = new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase);
			foreach (FileHash signature in _signatures)
			{
				string fileSha = signature.FileSha256;
				if (!string.IsNullOrWhiteSpace(fileSha))
				{
					fileSha = new string(fileSha.Where(Uri.IsHexDigit).ToArray()).ToUpperInvariant();
					if (fileSha.Length == 64)
					{
						_hashes.Add(fileSha);
						_hashInfo[fileSha] = (signature.CheatName, signature.Severity);
					}
				}
			}
			_hasSigNames = _signatures.Any((FileHash s) => !string.IsNullOrWhiteSpace(s.FileSignatureName));
			_loaded = true;
			if (databaseData.CheatRules != null && databaseData.CheatRules.Count > 0)
			{
				List<(string, string)> list = (from r in databaseData.CheatRules
					where !string.IsNullOrWhiteSpace(r.Rule)
					select (Rule: r.Rule, r.CheatName ?? "Detected")).ToList();
				if (list.Count > 0)
				{
					RuleEngine.Load(list);
				}
			}
		}
		catch
		{
			_names = new HashSet<string>();
			_nameList = new List<string>();
			_nameSeverity = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			_extensions = new HashSet<string>();
			_icons = new HashSet<string>();
			_signatures = new List<FileHash>();
			_hashes = new HashSet<string>();
			_hashInfo = new Dictionary<string, (string, string)>();
			_loaded = true;
		}
	}

	public bool MatchNameOrExtension(string fileName, string extension)
	{
		if (string.IsNullOrEmpty(fileName))
		{
			return false;
		}
		string lower = fileName.ToLowerInvariant();
		if (_names.Any((string n) => lower.Contains(n, StringComparison.OrdinalIgnoreCase)))
		{
			return true;
		}
		if (!string.IsNullOrEmpty(extension) && _extensions.Contains(extension.ToLowerInvariant()))
		{
			return true;
		}
		return false;
	}

	public bool MatchIcon(string filePath)
	{
		if (_icons.Count == 0)
		{
			return false;
		}
		try
		{
			Icon icon = Icon.ExtractAssociatedIcon(filePath);
			if (icon == null)
			{
				return false;
			}
			using Bitmap bitmap = icon.ToBitmap();
			using MemoryStream memoryStream = new MemoryStream();
			bitmap.Save(memoryStream, ImageFormat.Png);
			string item = Convert.ToBase64String(memoryStream.ToArray());
			return _icons.Contains(item);
		}
		catch
		{
			return false;
		}
	}

	public string MatchSignature(string filePath)
	{
		string serverSeverity;
		return MatchSignature(filePath, out serverSeverity);
	}

	public string MatchSignature(string filePath, out string serverSeverity)
	{
		serverSeverity = null;
		if (_signatures.Count == 0)
		{
			return null;
		}
		try
		{
			string text = ExtractCommonName(new X509Certificate2(filePath).Subject);
			if (string.IsNullOrEmpty(text))
			{
				return null;
			}
			foreach (FileHash signature in _signatures)
			{
				if (!string.IsNullOrEmpty(signature.FileSignatureName) && text.IndexOf(signature.FileSignatureName, StringComparison.OrdinalIgnoreCase) >= 0)
				{
					serverSeverity = signature.Severity;
					return signature.CheatName ?? signature.FileSignatureName;
				}
			}
		}
		catch
		{
		}
		return null;
	}

	public bool NameLooksLikeCheat(string fileName, out bool exact)
	{
		exact = false;
		if (!_loaded || string.IsNullOrEmpty(fileName) || _nameList == null)
		{
			return false;
		}
		int num = 0;
		string text = null;
		foreach (string name in _nameList)
		{
			string kind;
			int num2 = CheatScoring.NameMatchScore(fileName, name, out kind);
			if (num2 > num)
			{
				num = num2;
				text = kind;
			}
		}
		if (num <= 0)
		{
			return false;
		}
		exact = text == "exact";
		if (!(text == "exact") && !(text == "token"))
		{
			return text == "prefix";
		}
		return true;
	}

	public string MatchHash(string filePath, out string serverSeverity)
	{
		serverSeverity = null;
		if (_hashes == null || _hashes.Count == 0)
		{
			return null;
		}
		try
		{
			using FileStream inputStream = File.OpenRead(filePath);
			using SHA256 sHA = SHA256.Create();
			string text = Convert.ToHexString(sHA.ComputeHash(inputStream));
			if (_hashes.Contains(text))
			{
				_hashInfo.TryGetValue(text, out (string, string) value);
				serverSeverity = value.Item2;
				object result;
				if (!string.IsNullOrEmpty(value.Item1))
				{
					(result, _) = value;
				}
				else
				{
					result = "Detected (hash)";
				}
				return (string)result;
			}
		}
		catch
		{
		}
		return null;
	}

	private int BestNameMatch(string fileName, out string cheatName, out string serverSeverity, out string kind)
	{
		cheatName = null;
		serverSeverity = null;
		kind = null;
		int num = 0;
		if (_nameList == null)
		{
			return 0;
		}
		foreach (string name in _nameList)
		{
			string kind2;
			int num2 = CheatScoring.NameMatchScore(fileName, name, out kind2);
			if (num2 > num)
			{
				num = num2;
				kind = kind2;
				cheatName = name;
				_nameSeverity.TryGetValue(name, out serverSeverity);
			}
		}
		return num;
	}

	private static string ExtractCommonName(string subject)
	{
		if (string.IsNullOrEmpty(subject))
		{
			return null;
		}
		Match match = Regex.Match(subject, "CN\\s*=\\s*([^,]+)", RegexOptions.IgnoreCase);
		if (!match.Success)
		{
			return subject;
		}
		return match.Groups[1].Value.Trim();
	}

	public string GetCheatName(string filePath, bool useIconMatch = false, bool useSignatureMatch = false)
	{
		return Evaluate(filePath, useIconMatch, useSignatureMatch)?.CheatName;
	}

	public DetectionResult Evaluate(string filePath, bool useIconMatch = false, bool useSignatureMatch = false)
	{
		if (!_loaded || string.IsNullOrEmpty(filePath))
		{
			return null;
		}
		try
		{
			FileInfo fileInfo = new FileInfo(filePath);
			if (!fileInfo.Exists || fileInfo.Length > 62914560)
			{
				return null;
			}
			DetectionResult detectionResult = new DetectionResult();
			string text = null;
			string text2 = RuleEngine.Match(fileInfo);
			bool flag = text2 != null;
			if (flag)
			{
				detectionResult.Add("rule", 45);
				detectionResult.CheatName = text2;
			}
			string name = fileInfo.Name;
			string extension = fileInfo.Extension;
			string cheatName;
			string serverSeverity;
			string kind;
			int num = BestNameMatch(name, out cheatName, out serverSeverity, out kind);
			bool flag2 = false;
			if (num > 0)
			{
				detectionResult.Add("name:" + kind, num);
				if (detectionResult.CheatName == "Detected" && !string.IsNullOrEmpty(cheatName))
				{
					detectionResult.CheatName = cheatName;
				}
				if (text == null)
				{
					text = serverSeverity;
				}
				flag2 = kind == "exact";
			}
			else if (!string.IsNullOrEmpty(extension) && _extensions.Contains(extension.ToLowerInvariant()))
			{
				detectionResult.Add("ext", 5);
			}
			if (useIconMatch && MatchIcon(filePath))
			{
				detectionResult.Add("icon", 25);
			}
			bool flag3 = false;
			if (useSignatureMatch || _hasSigNames)
			{
				string serverSeverity2;
				string text3 = MatchSignature(filePath, out serverSeverity2);
				if (text3 != null)
				{
					detectionResult.Add("signature", 60);
					detectionResult.CheatName = text3;
					if (serverSeverity2 != null)
					{
						text = serverSeverity2;
					}
					flag3 = true;
				}
			}
			bool flag4 = false;
			string serverSeverity3;
			string text4 = MatchHash(filePath, out serverSeverity3);
			if (text4 != null)
			{
				detectionResult.Add("hash", 100);
				detectionResult.CheatName = text4;
				if (serverSeverity3 != null)
				{
					text = serverSeverity3;
				}
				flag4 = true;
			}
			if (detectionResult.Signals.Count == 0)
			{
				return null;
			}
			if (!(flag || flag4 || flag3 || flag2) && IsTrustedPublisher(filePath))
			{
				return null;
			}
			if (IsSuspiciousPath(filePath))
			{
				detectionResult.Add("path", 10);
			}
			if (IsTimestomped(fileInfo))
			{
				detectionResult.Add("timestomp", 12);
			}
			detectionResult.Severity = CheatScoring.FromServer(text) ?? CheatScoring.Bucket(detectionResult.Score);
			if (flag || flag2 || flag4 || flag3)
			{
				detectionResult.Severity = "red";
			}
			return detectionResult;
		}
		catch
		{
			return null;
		}
	}

	private static bool IsTimestomped(FileInfo fi)
	{
		try
		{
			DateTime creationTimeUtc = fi.CreationTimeUtc;
			DateTime lastWriteTimeUtc = fi.LastWriteTimeUtc;
			if (creationTimeUtc == default(DateTime) || lastWriteTimeUtc == default(DateTime))
			{
				return false;
			}
			if (creationTimeUtc == lastWriteTimeUtc && creationTimeUtc.Ticks % 10000000 == 0L)
			{
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private bool IsTrustedPublisher(string filePath)
	{
		try
		{
			if (!Authenticode.IsSigned(filePath))
			{
				return false;
			}
			using X509Certificate2 x509Certificate = new X509Certificate2(filePath);
			string text = ExtractCommonName(x509Certificate.Subject);
			if (string.IsNullOrEmpty(text))
			{
				return false;
			}
			string low = text.ToLowerInvariant();
			return _trustedSigners.Any((string t) => low.Contains(t));
		}
		catch
		{
			return false;
		}
	}

	private static bool IsSuspiciousPath(string path)
	{
		try
		{
			string text = path.ToLowerInvariant();
			if (text.Contains("\\temp\\") || text.Contains("\\appdata\\") || text.Contains("\\downloads\\"))
			{
				return true;
			}
			string[] array = text.Split('\\');
			foreach (string text2 in array)
			{
				if (text2.StartsWith(".") || text2.StartsWith("tmp_") || text2.Contains(".inj"))
				{
					return true;
				}
			}
		}
		catch
		{
		}
		return false;
	}
}
