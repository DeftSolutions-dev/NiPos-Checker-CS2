using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Runtime.Intrinsics.X86;
using System.Text;
using NiposChecker.Localization;
using NiposChecker.Models;

namespace NiposChecker.Services;

public static class SystemInfo
{
	public static WindowsInfo Gather()
	{
		WindowsInfo windowsInfo = new WindowsInfo();
		try
		{
			using (ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher("SELECT Caption,InstallDate FROM Win32_OperatingSystem"))
			{
				foreach (ManagementBaseObject item in managementObjectSearcher.Get())
				{
					windowsInfo.WindowsVersion = item["Caption"]?.ToString() ?? Strings.Get("Word_Unknown");
					if (item["InstallDate"] is string { Length: >=8 } text)
					{
						windowsInfo.WindowsInstallDate = $"{text.Substring(0, 4)}-{text.Substring(4, 2)}-{text.Substring(6, 2)}";
					}
				}
			}
			using (ManagementObjectSearcher managementObjectSearcher2 = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize FROM Win32_OperatingSystem"))
			{
				foreach (ManagementBaseObject item2 in managementObjectSearcher2.Get())
				{
					if (item2["TotalVisibleMemorySize"] is ulong num)
					{
						windowsInfo.PcRAM = $"{(double)(num / 1024) / 1024.0:F1} GB";
					}
				}
			}
			using (ManagementObjectSearcher managementObjectSearcher3 = new ManagementObjectSearcher("SELECT Name,NumberOfCores FROM Win32_Processor"))
			{
				using ManagementObjectCollection.ManagementObjectEnumerator managementObjectEnumerator = managementObjectSearcher3.Get().GetEnumerator();
				if (managementObjectEnumerator.MoveNext())
				{
					ManagementBaseObject current2 = managementObjectEnumerator.Current;
					windowsInfo.Processor = current2["Name"]?.ToString()?.Trim() ?? "N/A";
				}
			}
			using (ManagementObjectSearcher managementObjectSearcher4 = new ManagementObjectSearcher("SELECT Name FROM Win32_VideoController"))
			{
				List<string> list = new List<string>();
				foreach (ManagementBaseObject item3 in managementObjectSearcher4.Get())
				{
					string text2 = item3["Name"]?.ToString()?.Trim();
					if (!string.IsNullOrEmpty(text2))
					{
						list.Add(text2);
					}
				}
				string[] virtualMarks = new string[18]
				{
					"parsec", "virtual", "spacedesk", "displaylink", "idd", "sunshine", "microsoft basic", "microsoft remote", "rdp", "meta",
					"usb display", "citrix", "vmware", "virtualbox", "hyper-v", "mirror", "duetdisplay", "amyuni"
				};
				string[] realVendors = new string[8] { "nvidia", "geforce", "rtx", "gtx", "radeon", "amd", "intel", "arc" };
				string text3 = list.FirstOrDefault((string n) => realVendors.Any((string v) => n.ToLowerInvariant().Contains(v))) ?? list.FirstOrDefault((string n) => !IsVirtual(n)) ?? list.FirstOrDefault();
				windowsInfo.GPU = (string.IsNullOrEmpty(text3) ? "N/A" : text3);
				bool IsVirtual(string n)
				{
					return virtualMarks.Any((string m) => n.ToLowerInvariant().Contains(m));
				}
			}
			using (ManagementObjectSearcher managementObjectSearcher5 = new ManagementObjectSearcher("SELECT Manufacturer,Product FROM Win32_BaseBoard"))
			{
				using ManagementObjectCollection.ManagementObjectEnumerator managementObjectEnumerator = managementObjectSearcher5.Get().GetEnumerator();
				if (managementObjectEnumerator.MoveNext())
				{
					ManagementBaseObject current3 = managementObjectEnumerator.Current;
					string text4 = current3["Manufacturer"]?.ToString() ?? "";
					string text5 = current3["Product"]?.ToString() ?? "";
					windowsInfo.Motherboard = (text4 + " " + text5).Trim();
				}
			}
			try
			{
				using ManagementObjectSearcher managementObjectSearcher6 = new ManagementObjectSearcher("SELECT * FROM Win32_DesktopMonitor");
				int num2 = 0;
				foreach (ManagementBaseObject item4 in managementObjectSearcher6.Get())
				{
					_ = item4;
					num2++;
				}
				windowsInfo.ScreensCount = num2;
			}
			catch
			{
				windowsInfo.ScreensCount = 1;
			}
			windowsInfo.DetectVM = DetectVirtualMachine(out var name);
			windowsInfo.VMName = name;
		}
		catch
		{
		}
		return windowsInfo;
	}

	private static bool DetectVirtualMachine(out string name)
	{
		name = "";
		try
		{
			using ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher("SELECT Manufacturer,Model FROM Win32_ComputerSystem");
			foreach (ManagementBaseObject item in managementObjectSearcher.Get())
			{
				string text = (item["Manufacturer"]?.ToString() ?? "").ToLowerInvariant();
				string text2 = (item["Model"]?.ToString() ?? "").ToLowerInvariant();
				if (text.Contains("vmware") || text.Contains("virtualbox") || (text.Contains("microsoft") && text.Contains("virtual")) || text.Contains("xen"))
				{
					name = text;
					return true;
				}
				if (text2.Contains("virtual") || text2.Contains("vmware") || text2.Contains("virtualbox"))
				{
					name = text2;
					return true;
				}
			}
		}
		catch
		{
		}
		if (DetectHypervisorVendor(out var vendor))
		{
			name = vendor;
			return true;
		}
		return false;
	}

	private static bool DetectHypervisorVendor(out string vendor)
	{
		vendor = "";
		try
		{
			if (!X86Base.IsSupported)
			{
				return false;
			}
			if ((X86Base.CpuId(1, 0).Ecx & int.MinValue) == 0)
			{
				return false;
			}
			(int Eax, int Ebx, int Ecx, int Edx) tuple = X86Base.CpuId(1073741824, 0);
			StringBuilder sb = new StringBuilder();
			Append(tuple.Ebx);
			Append(tuple.Ecx);
			Append(tuple.Edx);
			string text = sb.ToString().Trim('\0', ' ').ToLowerInvariant();
			if (text.Contains("vmware"))
			{
				vendor = "VMware";
				return true;
			}
			if (text.Contains("vbox"))
			{
				vendor = "VirtualBox";
				return true;
			}
			if (text.Contains("kvm"))
			{
				vendor = "KVM";
				return true;
			}
			if (text.Contains("xen"))
			{
				vendor = "Xen";
				return true;
			}
			if (text.Contains("prl") || text.Contains("parallels"))
			{
				vendor = "Parallels";
				return true;
			}
			if (text.Contains("qemu") || text.Contains("tcg"))
			{
				vendor = "QEMU";
				return true;
			}
			void Append(int v)
			{
				for (int i = 0; i < 4; i++)
				{
					sb.Append((char)((v >> i * 8) & 0xFF));
				}
			}
		}
		catch
		{
		}
		return false;
	}
}
