using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Threading;
using NiposChecker.Localization;

namespace NiposChecker.Views;

public partial class UserBannedWindow : Window
{
	private int _countdown = 4;

	private readonly DispatcherTimer _timer;









	public UserBannedWindow(string banInfo)
	{
		InitializeComponent();
		base.Title = Strings.Get("Banned_WindowTitle");
		BannedHeader.Text = Strings.Get("Banned_Header");
		string[] array = banInfo.Split('|');
		BanReason.Text = ((array.Length != 0 && !string.IsNullOrEmpty(array[0])) ? Strings.Get("Banned_Reason", array[0]) : Strings.Get("Banned_ReasonUnknown"));
		BanDate.Text = ((array.Length > 1) ? Strings.Get("Banned_Date", array[1]) : "");
		BanEndDate.Text = ((array.Length > 2) ? Strings.Get("Banned_EndDate", array[2]) : "");
		BanIssuedBy.Text = ((array.Length > 3) ? Strings.Get("Banned_IssuedBy", array[3]) : "");
		_timer = new DispatcherTimer
		{
			Interval = TimeSpan.FromSeconds(1.0)
		};
		_timer.Tick += OnTick;
		_timer.Start();
	}

	private void OnTick(object sender, EventArgs e)
	{
		_countdown--;
		if (_countdown <= 0)
		{
			_timer.Stop();
			TimerText.Text = Strings.Get("Banned_CanClose");
		}
		else
		{
			TimerText.Text = Strings.Get("Banned_Countdown", _countdown);
		}
	}

}
