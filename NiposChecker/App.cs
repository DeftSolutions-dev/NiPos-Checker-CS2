using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using NiposChecker.Localization;
using NiposChecker.Models;
using NiposChecker.Services;
using NiposChecker.Views;

namespace NiposChecker;

public partial class App : Application
{
	private static Mutex _mutex;


	public static string CurrentSteamID { get; set; }

	public static uint GameBuyDateUnix { get; set; }

	public static List<Account> LocalAccounts { get; set; } = new List<Account>();

	public static string HWID { get; set; }

	public static CheatDatabase CheatDatabase { get; set; }

	protected override async void OnStartup(StartupEventArgs e)
	{
		_mutex = new Mutex(initiallyOwned: true, "NIPOS_CHECKER_SINGLE_INSTANCE", out var createdNew);
		if (!createdNew)
		{
			AppDialog.Alert(null, "NIPOS CHECKER", Strings.Get("Msg_AlreadyRunning"));
			Shutdown();
			return;
		}
		base.OnStartup(e);
		base.ShutdownMode = ShutdownMode.OnExplicitShutdown;
		try
		{
			await ApiClient.SelectBaseAsync();
		}
		catch
		{
		}
		try
		{
			using ApiClient api = new ApiClient();
			Task<ProjectInfo> fetch = api.GetProjectInfoAsync();
			if (await Task.WhenAny(fetch, Task.Delay(6000)) == fetch)
			{
				ProjectInfo result = fetch.Result;
				if (result != null && !string.IsNullOrWhiteSpace(result.BrandColor))
				{
					Branding.ApplyBrandColor(result.BrandColor);
				}
			}
		}
		catch
		{
		}
		if (FirstLaunchWindow.IsFirstLaunch)
		{
			try
			{
				if (new FirstLaunchWindow().ShowDialog() != true)
				{
					Shutdown();
					return;
				}
			}
			catch
			{
			}
		}
		new LoadingWindow().Show();
		base.ShutdownMode = ShutdownMode.OnLastWindowClose;
	}

	protected override void OnExit(ExitEventArgs e)
	{
		try
		{
			DiscordService.Shutdown();
		}
		catch
		{
		}
		base.OnExit(e);
	}

	private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
	{
		try
		{
			File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "error.log"), $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {e.Exception}\r\n\r\n");
		}
		catch
		{
		}
		e.Handled = true;
	}

}
