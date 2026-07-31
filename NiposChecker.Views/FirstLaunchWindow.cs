using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using NiposChecker.Localization;
using NiposChecker.Services;

namespace NiposChecker.Views;

public partial class FirstLaunchWindow : Window
{
	private int _step = 1;















	private static string MarkerPath => Path.Combine(AppContext.BaseDirectory, "firstlaunch.cfg");

	public static bool IsFirstLaunch => !File.Exists(MarkerPath);

	public FirstLaunchWindow()
	{
		InitializeComponent();
		SoundCheck.IsChecked = SoundService.Enabled;
		DiscordCheck.IsChecked = DiscordService.Enabled;
		RenderStep();
	}

	private void RenderStep()
	{
		RuText.Text = Strings.Get("Lang_Russian");
		EnText.Text = Strings.Get("Lang_English");
		RuBtn.Tag = ((Strings.Current == "RU") ? "active" : null);
		EnBtn.Tag = ((Strings.Current == "EN") ? "active" : null);
		SoundCheck.Content = Strings.Get("Settings_Sound");
		DiscordCheck.Content = Strings.Get("FL_Discord");
		BackText.Text = Strings.Get("Btn_Back");
		if (_step == 1)
		{
			StepTitle.Text = Strings.Get("FL_ChooseLanguage");
			LangStep.Visibility = Visibility.Visible;
			SettingsStep.Visibility = Visibility.Collapsed;
			BackBtn.Visibility = Visibility.Collapsed;
			NextText.Text = Strings.Get("Btn_Next");
		}
		else
		{
			StepTitle.Text = Strings.Get("FL_ChooseSettings");
			LangStep.Visibility = Visibility.Collapsed;
			SettingsStep.Visibility = Visibility.Visible;
			BackBtn.Visibility = Visibility.Visible;
			NextText.Text = Strings.Get("FL_SaveAndStart");
		}
	}

	private void Ru_Click(object s, RoutedEventArgs e)
	{
		SoundService.Click();
		Strings.SetLanguage("RU");
		RenderStep();
	}

	private void En_Click(object s, RoutedEventArgs e)
	{
		SoundService.Click();
		Strings.SetLanguage("EN");
		RenderStep();
	}

	private void Next_Click(object s, RoutedEventArgs e)
	{
		SoundService.Click();
		if (_step == 1)
		{
			_step = 2;
			RenderStep();
		}
		else
		{
			SaveAndClose();
		}
	}

	private void Back_Click(object s, RoutedEventArgs e)
	{
		SoundService.Click();
		_step = 1;
		RenderStep();
	}

	private void SaveAndClose()
	{
		SoundService.Enabled = SoundCheck.IsChecked == true;
		SoundService.SaveConfig();
		DiscordService.Enabled = DiscordCheck.IsChecked == true;
		try
		{
			File.WriteAllText(MarkerPath, "1");
		}
		catch
		{
		}
		base.DialogResult = true;
		Close();
	}

	private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (e.ButtonState == MouseButtonState.Pressed)
		{
			try
			{
				DragMove();
			}
			catch
			{
			}
		}
	}

}
