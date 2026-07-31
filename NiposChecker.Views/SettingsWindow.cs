using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using NiposChecker.Localization;
using NiposChecker.Services;

namespace NiposChecker.Views;

public partial class SettingsWindow : Window
{
	private bool _init;







	public SettingsWindow()
	{
		InitializeComponent();
		base.Title = Strings.Get("Tip_Settings");
		HeaderTitle.Text = Strings.Get("Tip_Settings");
		SoundCheck.Content = Strings.Get("Settings_Sound");
		CloseText.Text = Strings.Get("Btn_Close");
		SoundCheck.IsChecked = SoundService.Enabled;
		VolumeSlider.Value = SoundService.Volume * 100.0;
		UpdateVolumeLabel();
		_init = true;
	}

	private void UpdateVolumeLabel()
	{
		VolumeLabel.Text = Strings.Get("Settings_Volume", (int)VolumeSlider.Value);
	}

	private void Sound_Changed(object sender, RoutedEventArgs e)
	{
		if (_init)
		{
			SoundService.Enabled = SoundCheck.IsChecked == true;
			SoundService.SaveConfig();
			if (SoundService.Enabled)
			{
				SoundService.Soft();
			}
		}
	}

	private void Volume_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
	{
		if (_init)
		{
			SoundService.Volume = VolumeSlider.Value / 100.0;
			SoundService.SaveConfig();
			UpdateVolumeLabel();
			SoundService.Soft();
		}
	}

	private void Close_Click(object sender, RoutedEventArgs e)
	{
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
