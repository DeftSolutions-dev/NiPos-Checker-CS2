using System;
using System.IO;
using System.Linq;
using System.Management;

namespace NiposChecker.Services;

public static class MouseAppFinder
{
	public class MouseSoftware
	{
		public string Name { get; set; }

		public string Path { get; set; }
	}

	private static readonly (string Name, string[] Procs, string[] Paths)[] Catalog = new(string, string[], string[])[11]
	{
		("Logitech G HUB", new string[2] { "lghub", "lghub_agent" }, new string[2] { "%ProgramFiles%\\LGHUB\\lghub.exe", "%LocalAppData%\\LGHUB\\lghub.exe" }),
		("Logitech Gaming Software", new string[1] { "LCore" }, new string[2] { "%ProgramFiles%\\Logitech Gaming Software\\LCore.exe", "%ProgramFiles(x86)%\\Logitech Gaming Software\\LCore.exe" }),
		("Razer Synapse", new string[3] { "Razer Synapse 3 Host", "RazerAppEngine", "RazerCentralService" }, new string[1] { "%ProgramFiles(x86)%\\Razer\\Synapse3\\WPFUI\\Framework\\Razer Synapse 3 Host\\Razer Synapse 3.exe" }),
		("SteelSeries GG", new string[3] { "SteelSeriesGG", "SteelSeries GG", "SteelSeriesEngine3" }, new string[2] { "%ProgramFiles%\\SteelSeries\\GG\\SteelSeriesGG.exe", "%ProgramFiles(x86)%\\SteelSeries\\SteelSeries Engine 3\\SteelSeriesEngine3.exe" }),
		("Corsair iCUE", new string[1] { "iCUE" }, new string[3] { "%ProgramFiles%\\Corsair\\CORSAIR iCUE5 Software\\iCUE.exe", "%ProgramFiles%\\Corsair\\CORSAIR iCUE4 Software\\iCUE.exe", "%ProgramFiles(x86)%\\Corsair\\CORSAIR iCUE Software\\iCUE.exe" }),
		("ASUS Armoury Crate", new string[3] { "ArmouryCrate.UserSessionHelper", "ArmouryCrate", "ArmourySwAgent" }, new string[1] { "%ProgramFiles%\\ASUS\\ARMOURY CRATE Service\\ArmouryCrate.exe" }),
		("Glorious CORE", new string[2] { "glorious core", "GloriousCoreHelper" }, new string[1] { "%LocalAppData%\\Programs\\glorious-core\\Glorious CORE.exe" }),
		("Cooler Master MasterPlus+", new string[1] { "MasterPlus" }, new string[1] { "%ProgramFiles%\\Cooler Master\\MasterPlus+\\MasterPlus.exe" }),
		("HyperX NGENUITY", new string[2] { "ngenuity", "HP.Omen.OmenCommandCenter" }, Array.Empty<string>()),
		("A4Tech Bloody", new string[3] { "Bloody7", "Bloody6", "Bloody" }, new string[1] { "%ProgramFiles(x86)%\\A4Tech\\Bloody7\\Bloody7.exe" }),
		("Endgame Gear / Pulsar / VAXEE", new string[3] { "Pulsar", "VAXEE", "Endgame Gear" }, Array.Empty<string>())
	};

	public static MouseSoftware Find()
	{
		(string, string[], string[])[] catalog = Catalog;
		for (int i = 0; i < catalog.Length; i++)
		{
			(string, string[], string[]) tuple = catalog[i];
			try
			{
				string[] item = tuple.Item3;
				for (int j = 0; j < item.Length; j++)
				{
					string path = Environment.ExpandEnvironmentVariables(item[j]);
					if (File.Exists(path))
					{
						return new MouseSoftware
						{
							Name = tuple.Item1,
							Path = path
						};
					}
				}
				item = tuple.Item2;
				for (int j = 0; j < item.Length; j++)
				{
					string executablePath = GetExecutablePath(item[j]);
					if (!string.IsNullOrEmpty(executablePath))
					{
						string path2 = SiblingGuiExe(executablePath, tuple.Item3) ?? executablePath;
						return new MouseSoftware
						{
							Name = tuple.Item1,
							Path = path2
						};
					}
				}
			}
			catch
			{
			}
		}
		return null;
	}

	private static string SiblingGuiExe(string procExe, string[] paths)
	{
		try
		{
			string directoryName = Path.GetDirectoryName(procExe);
			if (string.IsNullOrEmpty(directoryName))
			{
				return null;
			}
			for (int i = 0; i < paths.Length; i++)
			{
				string fileName = Path.GetFileName(Environment.ExpandEnvironmentVariables(paths[i]));
				string text = Path.Combine(directoryName, fileName);
				if (File.Exists(text))
				{
					return text;
				}
			}
		}
		catch
		{
		}
		return null;
	}

	private static string GetExecutablePath(string processName)
	{
		try
		{
			string text = processName.Replace("'", "''");
			using ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher("SELECT ExecutablePath FROM Win32_Process WHERE Name = '" + text + ".exe'");
			using ManagementObjectCollection source = managementObjectSearcher.Get();
			return source.Cast<ManagementObject>().FirstOrDefault()?["ExecutablePath"]?.ToString() ?? "";
		}
		catch
		{
			return "";
		}
	}
}
