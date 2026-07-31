using System;
using System.Management;
using System.Security.Cryptography;
using System.Text;

namespace NiposChecker.Services;

public static class HwId
{
	public static string Generate()
	{
		string cpuId = GetCpuId();
		string motherboard = GetMotherboard();
		string s = cpuId + "|" + motherboard;
		using SHA1 sHA = SHA1.Create();
		return BitConverter.ToString(sHA.ComputeHash(Encoding.UTF8.GetBytes(s))).Replace("-", "").ToLowerInvariant();
	}

	private static string GetCpuId()
	{
		try
		{
			using ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor");
			foreach (ManagementBaseObject item in managementObjectSearcher.Get())
			{
				string text = item["ProcessorId"]?.ToString();
				if (!string.IsNullOrEmpty(text))
				{
					return text;
				}
			}
		}
		catch
		{
		}
		return "UNKNOWN_CPU";
	}

	private static string GetMotherboard()
	{
		try
		{
			using ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher("SELECT Manufacturer,Product FROM Win32_BaseBoard");
			using ManagementObjectCollection.ManagementObjectEnumerator managementObjectEnumerator = managementObjectSearcher.Get().GetEnumerator();
			if (managementObjectEnumerator.MoveNext())
			{
				ManagementBaseObject current = managementObjectEnumerator.Current;
				string text = current["Manufacturer"]?.ToString() ?? "";
				string text2 = current["Product"]?.ToString() ?? "";
				return text + "|" + text2;
			}
		}
		catch
		{
		}
		return "UNKNOWN_MB";
	}
}
