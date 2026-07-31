using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NiposChecker.Services;

public static class AppsLauncher
{
	public static string AppsPath => Path.Combine(AppContext.BaseDirectory, "Apps");

	public static void OpenFile(string exeRelative)
	{
		string text = Path.Combine(AppsPath, exeRelative);
		if (!File.Exists(text))
		{
			return;
		}
		try
		{
			Process.Start(new ProcessStartInfo(text)
			{
				UseShellExecute = false,
				WorkingDirectory = Path.GetDirectoryName(text)
			});
		}
		catch (Exception)
		{
		}
	}

	public static async Task<XDocument> RunSxmlAsync(string exeName, string tmpName)
	{
		string exePath = Path.Combine(AppsPath, exeName);
		string tmpPath = Path.Combine(Path.GetTempPath(), tmpName);
		if (!File.Exists(exePath))
		{
			return null;
		}
		try
		{
			await Task.Run(delegate
			{
				if (File.Exists(tmpPath))
				{
					File.Delete(tmpPath);
				}
				using Process process = Process.Start(new ProcessStartInfo(exePath, "/sxml " + tmpPath)
				{
					UseShellExecute = false,
					CreateNoWindow = true,
					WorkingDirectory = AppsPath
				});
				process?.WaitForExit(30000);
			});
			if (!File.Exists(tmpPath))
			{
				return null;
			}
			return XDocument.Parse(CleanInvalidXmlChars(await Task.Run(() => File.ReadAllText(tmpPath, Encoding.GetEncoding("ISO-8859-1")))));
		}
		catch (Exception)
		{
			return null;
		}
	}

	private static string CleanInvalidXmlChars(string text)
	{
		return Regex.Replace(text, "[^\n\r -\ud7ff\ue000-\ufffd]", string.Empty);
	}
}
