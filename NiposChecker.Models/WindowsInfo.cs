using Newtonsoft.Json;

namespace NiposChecker.Models;

public class WindowsInfo
{
	[JsonProperty("screens_count")]
	public int ScreensCount { get; set; }

	[JsonProperty("windows_startup_time")]
	public string WindowsStartupTime { get; set; }

	[JsonProperty("windows_version")]
	public string WindowsVersion { get; set; }

	[JsonProperty("windows_install_date")]
	public string WindowsInstallDate { get; set; }

	[JsonProperty("pc_ram")]
	public string PcRAM { get; set; }

	[JsonProperty("processor")]
	public string Processor { get; set; }

	[JsonProperty("gpu")]
	public string GPU { get; set; }

	[JsonProperty("motherboard")]
	public string Motherboard { get; set; }

	[JsonProperty("detect_vm")]
	public bool DetectVM { get; set; }

	[JsonProperty("vm_name")]
	public string VMName { get; set; }
}
