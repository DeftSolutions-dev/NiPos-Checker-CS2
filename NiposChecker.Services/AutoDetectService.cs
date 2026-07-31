using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using NiposChecker.Localization;

namespace NiposChecker.Services;

public static class AutoDetectService
{
	public static async Task<List<AutoDetectResult>> DetectAllAsync()
	{
		List<AutoDetectResult> results = new List<AutoDetectResult>();
		await Task.Run(delegate
		{
			try
			{
				string text = Path.Combine(Path.GetTempPath(), "strings.txt");
				if (File.Exists(text))
				{
					FileInfo fileInfo = new FileInfo(text);
					if (fileInfo.Length / 1048576 > 10)
					{
						TimeSpan timeSpan = DateTime.Now - fileInfo.LastWriteTime;
						string text2 = ((timeSpan.TotalDays >= 1.0) ? Strings.Get("AD_Days", (int)timeSpan.TotalDays) : ((timeSpan.TotalHours >= 1.0) ? Strings.Get("AD_Hours", (int)timeSpan.TotalHours) : Strings.Get("AD_Minutes", (int)timeSpan.TotalMinutes)));
						results.Add(new AutoDetectResult
						{
							Id = 1,
							Title = Strings.Get("AD_CleanDetected"),
							Text = Strings.Get("AD_ExLoader", text2)
						});
					}
				}
			}
			catch
			{
			}
			try
			{
				using Process process = Process.Start(new ProcessStartInfo("sc", "query DusmSvc")
				{
					RedirectStandardOutput = true,
					UseShellExecute = false,
					CreateNoWindow = true
				});
				string obj2 = process?.StandardOutput.ReadToEnd() ?? "";
				process?.WaitForExit(5000);
				if (obj2.IndexOf("STOPPED", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					results.Add(new AutoDetectResult
					{
						Id = 2,
						Title = Strings.Get("AD_ServiceOff"),
						Text = Strings.Get("AD_DusmStopped"),
						ActionLabel = Strings.Get("AD_StartService")
					});
				}
			}
			catch
			{
			}
			try
			{
				Process[] processes = Process.GetProcesses();
				foreach (Process process2 in processes)
				{
					try
					{
						string text3 = process2.MainModule?.FileName;
						if (!string.IsNullOrEmpty(text3))
						{
							using X509Certificate2 x509Certificate = new X509Certificate2(text3);
							string thumbprint = x509Certificate.Thumbprint;
							if (thumbprint != null && thumbprint.Equals("5DAD1B5F962AD41ED9B45F5B00201D11AE2E5F17", StringComparison.OrdinalIgnoreCase))
							{
								results.Add(new AutoDetectResult
								{
									Id = 3,
									Title = Strings.Get("AD_XoneRunning"),
									Text = Strings.Get("AD_Process", process2.ProcessName)
								});
								break;
							}
						}
					}
					catch
					{
					}
				}
			}
			catch
			{
			}
			try
			{
				using Process process3 = Process.Start(new ProcessStartInfo("powershell", "-NoProfile -Command \"(Get-NetFirewallRule | Where-Object {$_.Direction -eq 'Inbound' -and $_.Name -like '*.exe' -and $_.Name -notlike '*system*'}).Count\"")
				{
					RedirectStandardOutput = true,
					UseShellExecute = false,
					CreateNoWindow = true
				});
				string s = process3?.StandardOutput.ReadToEnd()?.Trim() ?? "0";
				process3?.WaitForExit(5000);
				if (int.TryParse(s, out var result) && result < 5)
				{
					results.Add(new AutoDetectResult
					{
						Id = 4,
						Title = Strings.Get("AD_FirewallSusp"),
						Text = Strings.Get("AD_FirewallFew", result)
					});
				}
			}
			catch
			{
			}
		});
		return results;
	}
}
