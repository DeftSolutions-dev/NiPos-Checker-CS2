using System;
using System.Collections.Generic;

namespace NiposChecker.Services;

public static class VulnerableDrivers
{
	public static readonly Dictionary<string, string> Known = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
	{
		["rtcore64.sys"] = "MSI Afterburner / RivaTuner",
		["rtcore32.sys"] = "MSI Afterburner / RivaTuner",
		["winring0x64.sys"] = "мониторинг железа (LHM/OHM/CPU-Z)",
		["winring0.sys"] = "мониторинг железа",
		["winio64.sys"] = "WinIo — прямой доступ к портам/памяти",
		["winio.sys"] = "WinIo",
		["gdrv.sys"] = "утилиты Gigabyte",
		["gdrv2.sys"] = "утилиты Gigabyte",
		["gpcidrv64.sys"] = "Gigabyte",
		["aswarpot.sys"] = "Avast / AVG",
		["dbutil_2_3.sys"] = "Dell",
		["dbutildrv2.sys"] = "Dell",
		["pcdsrvc.sys"] = "Dell PC-Doctor",
		["iqvw64e.sys"] = "Intel",
		["iqvw32e.sys"] = "Intel",
		["mhyprot2.sys"] = "античит miHoYo (Genshin/HSR)",
		["mhyprot3.sys"] = "античит miHoYo",
		["atillk64.sys"] = "ASUS",
		["asio.sys"] = "ASUS AI Suite",
		["asio2.sys"] = "ASUS AI Suite",
		["asio3.sys"] = "ASUS AI Suite",
		["asupio.sys"] = "ASUS",
		["cpuz141.sys"] = "CPU-Z",
		["cpuz.sys"] = "CPU-Z",
		["procexp152.sys"] = "Sysinternals Process Explorer",
		["procexp.sys"] = "Sysinternals Process Explorer",
		["msio64.sys"] = "MSI",
		["msio32.sys"] = "MSI",
		["ene.sys"] = "ENE (RGB/мат.платы)",
		["eneio64.sys"] = "ENE",
		["enetechio64.sys"] = "ENE",
		["openlibsys.sys"] = "OpenLibSys (мониторинг)",
		["speedfan.sys"] = "SpeedFan",
		["amifldrv64.sys"] = "AMI прошивальщик BIOS",
		["nvflash.sys"] = "NVIDIA nvflash",
		["nvoclock.sys"] = "NVIDIA OC",
		["semav6msr64.sys"] = "MSR-доступ",
		["rtkiow10x64.sys"] = "Realtek",
		["rtkiow8x64.sys"] = "Realtek",
		["hwos2ec10x64.sys"] = "мониторинг",
		["piddrv64.sys"] = "утилита",
		["monitor.sys"] = "монитор",
		["phlashnt.sys"] = "Phoenix BIOS",
		["nchgbios2x64.sys"] = "прошивальщик",
		["elrawdsk.sys"] = "ElRawDisk (шифровальщики абузят)",
		["viragt64.sys"] = "TG Soft",
		["viragt.sys"] = "TG Soft",
		["tmcomm.sys"] = "Trend Micro",
		["kprocesshacker.sys"] = "Process Hacker / System Informer",
		["kprocesshacker2.sys"] = "Process Hacker",
		["gmer64.sys"] = "GMER",
		["gmer.sys"] = "GMER",
		["capcom.sys"] = "Capcom (классика BYOVD — почти всегда чит)",
		["lha.sys"] = "LG",
		["msrhook.sys"] = "MSR-хук",
		["physmem.sys"] = "прямой доступ к физпамяти",
		["superbmc.sys"] = "утилита",
		["asrautochk.sys"] = "ASRock",
		["asrdrv101.sys"] = "ASRock",
		["asrdrv102.sys"] = "ASRock",
		["nvaudio.sys"] = "утилита",
		["wingd.sys"] = "утилита",
		["directio64.sys"] = "прямой I/O",
		["directio.sys"] = "прямой I/O",
		["inpoutx64.sys"] = "InpOut (порты/память)",
		["glckio2.sys"] = "ASUS GPU Tweak",
		["atszio64.sys"] = "ASUS"
	};

	public static bool IsKnown(string fileNameLower, out string hint)
	{
		return Known.TryGetValue(fileNameLower, out hint);
	}
}
