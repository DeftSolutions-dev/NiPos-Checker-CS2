using System;
using System.Diagnostics;
using System.Security.Principal;
using System.ServiceProcess;
using Microsoft.Win32;

namespace NiposChecker.Services;

public static class SrumRepair
{
	public static (bool ok, string msg) RestoreDataUsage()
	{
		if (!IsElevated())
		{
			return (ok: false, msg: "Нужны права администратора. Перезапустите проверку «от имени администратора» и повторите.");
		}
		try
		{
			SetConsentAllow("appDiagnostics");
			EnableAndStart("DusmSvc", restart: true);
			EnableAndStart("DPS");
			EnableAndStart("iphlpsvc");
			RunSc("config WdiServiceHost start= demand");
			RunExe("schtasks.exe", "/Change /TN \"Microsoft\\Windows\\DUSM\\dusmtask\" /Enable");
			RunExe("schtasks.exe", "/Run /TN \"Microsoft\\Windows\\DUSM\\dusmtask\"");
			bool num = ServiceRunning("DusmSvc");
			bool flag = ServiceRunning("DPS");
			return (num && flag) ? (ok: true, msg: "Готово. Учёт «Использования данных» и диагностика приложений включены и запущены. Откройте страницу «Использование данных» заново — статистика отобразится. Примечание: уже стёртую историю восстановить нельзя, дальше она копится с этого момента.") : (ok: false, msg: "Не все службы запустились (DusmSvc/DPS). Перезагрузите ПК и повторите — возможно, база «Использования данных» повреждена.");
		}
		catch (Exception ex)
		{
			return (ok: false, msg: "Не удалось восстановить учёт: " + ex.Message);
		}
	}

	private static void EnableAndStart(string name, bool restart = false)
	{
		RunSc("config " + name + " start= auto");
		try
		{
			using ServiceController serviceController = new ServiceController(name);
			serviceController.Refresh();
			if (restart && serviceController.Status == ServiceControllerStatus.Running)
			{
				try
				{
					serviceController.Stop();
					serviceController.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(12.0));
				}
				catch
				{
				}
				serviceController.Refresh();
			}
			if (serviceController.Status != ServiceControllerStatus.Running && serviceController.Status != ServiceControllerStatus.StartPending)
			{
				try
				{
					serviceController.Start();
				}
				catch
				{
				}
			}
			try
			{
				serviceController.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(12.0));
			}
			catch
			{
			}
		}
		catch
		{
			RunSc("start " + name);
		}
	}

	private static bool ServiceRunning(string name)
	{
		try
		{
			using ServiceController serviceController = new ServiceController(name);
			return serviceController.Status == ServiceControllerStatus.Running;
		}
		catch
		{
			return false;
		}
	}

	private static void SetConsentAllow(string capability)
	{
		try
		{
			using RegistryKey registryKey = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\" + capability);
			registryKey?.SetValue("Value", "Allow", RegistryValueKind.String);
		}
		catch
		{
		}
	}

	private static void RunSc(string args)
	{
		RunExe("sc.exe", args);
	}

	private static void RunExe(string exe, string args)
	{
		try
		{
			using Process process = Process.Start(new ProcessStartInfo(exe, args)
			{
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true
			});
			if (process != null)
			{
				process.StandardOutput.ReadToEnd();
				process.StandardError.ReadToEnd();
				process.WaitForExit(15000);
			}
		}
		catch
		{
		}
	}

	private static bool IsElevated()
	{
		try
		{
			using WindowsIdentity ntIdentity = WindowsIdentity.GetCurrent();
			return new WindowsPrincipal(ntIdentity).IsInRole(WindowsBuiltInRole.Administrator);
		}
		catch
		{
			return false;
		}
	}
}
