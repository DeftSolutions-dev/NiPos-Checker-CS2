using System;
using System.ServiceProcess;

namespace NiposChecker.Services;

public static class WindowsServices
{
	public static bool IsStopped(string serviceName)
	{
		try
		{
			using ServiceController serviceController = new ServiceController(serviceName);
			return serviceController.Status != ServiceControllerStatus.Running;
		}
		catch
		{
			return false;
		}
	}

	public static bool Start(string serviceName)
	{
		try
		{
			using ServiceController serviceController = new ServiceController(serviceName);
			if (serviceController.Status == ServiceControllerStatus.Running)
			{
				return true;
			}
			try
			{
				serviceController.Start();
			}
			catch
			{
			}
			serviceController.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10.0));
			serviceController.Refresh();
			return serviceController.Status == ServiceControllerStatus.Running;
		}
		catch
		{
			return false;
		}
	}
}
