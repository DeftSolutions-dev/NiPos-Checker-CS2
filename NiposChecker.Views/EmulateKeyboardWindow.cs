using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using InputSimulatorStandard;
using InputSimulatorStandard.Native;
using NiposChecker.Localization;

namespace NiposChecker.Views;

public partial class EmulateKeyboardWindow : Window
{
	private bool _isChecking;










	[DllImport("user32.dll")]
	private static extern nint GetForegroundWindow();

	[DllImport("user32.dll", CharSet = CharSet.Auto)]
	private static extern bool PostMessage(nint hWnd, int Msg, int wParam, int lParam);

	[DllImport("user32.dll")]
	private static extern int LoadKeyboardLayout(string pwszKLID, uint Flags);

	[DllImport("user32.dll")]
	private static extern bool SetForegroundWindow(nint hWnd);

	[DllImport("user32.dll")]
	private static extern bool ShowWindow(nint hWnd, int nCmdShow);

	public EmulateKeyboardWindow()
	{
		InitializeComponent();
		base.Title = Strings.Get("Emulate_WindowTitle");
		HeaderTitle.Text = Strings.Get("Emulate_Title");
		DescText.Text = Strings.Get("Emulate_Desc");
		StartText.Text = Strings.Get("Emulate_StartCheck");
	}

	private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		DragMove();
	}

	private void Close_Click(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private void Window_Closing(object sender, CancelEventArgs e)
	{
		_isChecking = false;
	}

	private void StartCheck_Click(object sender, RoutedEventArgs e)
	{
		if (Process.GetProcessesByName("cs2").Length == 0)
		{
			AppDialog.Alert(this, Strings.Get("Emulate_CheckTitle"), Strings.Get("Emulate_GameNotRunning"), null, DialogKind.Warning);
		}
		else if (AppDialog.Confirm(this, Strings.Get("Emulate_WindowTitle"), Strings.Get("Emulate_Instructions"), null, Strings.Get("Btn_Ok"), Strings.Get("Msg_Cancel"), DialogKind.Info))
		{
			base.Topmost = true;
			base.Opacity = 0.7;
			StartBtn.Visibility = Visibility.Collapsed;
			ProgressPanel.Visibility = Visibility.Visible;
			ResultText.Visibility = Visibility.Collapsed;
			_isChecking = true;
			new Thread((ThreadStart)delegate
			{
				RunKeyCheck();
			}).Start();
		}
	}

	private void RunKeyCheck()
	{
		InputSimulator inputSimulator = new InputSimulator();
		List<VirtualKeyCode> list = new List<VirtualKeyCode>
		{
			VirtualKeyCode.F1,
			VirtualKeyCode.F2,
			VirtualKeyCode.F3,
			VirtualKeyCode.F4,
			VirtualKeyCode.F5,
			VirtualKeyCode.F6,
			VirtualKeyCode.F7,
			VirtualKeyCode.F8,
			VirtualKeyCode.F9,
			VirtualKeyCode.F11,
			VirtualKeyCode.TAB,
			VirtualKeyCode.HOME,
			VirtualKeyCode.END,
			VirtualKeyCode.DELETE,
			VirtualKeyCode.NUMPAD0,
			VirtualKeyCode.NUMPAD1,
			VirtualKeyCode.NUMPAD2,
			VirtualKeyCode.NUMPAD3,
			VirtualKeyCode.NUMPAD4,
			VirtualKeyCode.NUMPAD5,
			VirtualKeyCode.NUMPAD6,
			VirtualKeyCode.NUMPAD7,
			VirtualKeyCode.NUMPAD8,
			VirtualKeyCode.NUMPAD9,
			VirtualKeyCode.DECIMAL,
			VirtualKeyCode.DIVIDE,
			VirtualKeyCode.MULTIPLY,
			VirtualKeyCode.EREOF,
			VirtualKeyCode.MBUTTON,
			VirtualKeyCode.NEXT,
			VirtualKeyCode.BACK,
			VirtualKeyCode.PAUSE,
			VirtualKeyCode.XBUTTON1,
			VirtualKeyCode.SPACE,
			VirtualKeyCode.PRIOR,
			VirtualKeyCode.UP,
			VirtualKeyCode.DOWN,
			VirtualKeyCode.LEFT,
			VirtualKeyCode.RIGHT,
			VirtualKeyCode.OEM_PLUS,
			VirtualKeyCode.OEM_MINUS,
			VirtualKeyCode.OEM_PERIOD,
			VirtualKeyCode.OEM_2,
			VirtualKeyCode.OEM_4,
			VirtualKeyCode.OEM_5,
			VirtualKeyCode.OEM_6,
			VirtualKeyCode.VK_M,
			VirtualKeyCode.VK_S,
			VirtualKeyCode.VK_W,
			VirtualKeyCode.VK_D,
			VirtualKeyCode.VK_A,
			VirtualKeyCode.VK_Q,
			VirtualKeyCode.VK_E,
			VirtualKeyCode.VK_R,
			VirtualKeyCode.VK_I,
			VirtualKeyCode.VK_O,
			VirtualKeyCode.VK_P,
			VirtualKeyCode.VK_F,
			VirtualKeyCode.VK_J,
			VirtualKeyCode.VK_K,
			VirtualKeyCode.VK_L,
			VirtualKeyCode.VK_Z,
			VirtualKeyCode.VK_X,
			VirtualKeyCode.VK_C,
			VirtualKeyCode.VK_V,
			VirtualKeyCode.VK_N,
			VirtualKeyCode.OEM_1,
			VirtualKeyCode.OEM_CLEAR,
			VirtualKeyCode.OEM_102,
			VirtualKeyCode.OEM_7,
			VirtualKeyCode.OEM_8,
			VirtualKeyCode.VK_0,
			VirtualKeyCode.VK_1,
			VirtualKeyCode.VK_2,
			VirtualKeyCode.VK_3,
			VirtualKeyCode.VK_4,
			VirtualKeyCode.VK_5,
			VirtualKeyCode.VK_6,
			VirtualKeyCode.VK_7,
			VirtualKeyCode.VK_8,
			VirtualKeyCode.VK_9,
			VirtualKeyCode.F13,
			VirtualKeyCode.F14,
			VirtualKeyCode.F15,
			VirtualKeyCode.F16,
			VirtualKeyCode.F17,
			VirtualKeyCode.F18,
			VirtualKeyCode.F19,
			VirtualKeyCode.F20,
			VirtualKeyCode.F21,
			VirtualKeyCode.F22,
			VirtualKeyCode.F23,
			VirtualKeyCode.F24,
			VirtualKeyCode.LMENU,
			VirtualKeyCode.RMENU,
			VirtualKeyCode.MENU,
			VirtualKeyCode.LSHIFT,
			VirtualKeyCode.RSHIFT,
			VirtualKeyCode.LCONTROL,
			VirtualKeyCode.RCONTROL,
			VirtualKeyCode.XBUTTON2,
			VirtualKeyCode.F12
		};
		inputSimulator.Keyboard.KeyPress(VirtualKeyCode.INSERT);
		Thread.Sleep(2000);
		int total = list.Count;
		for (int i = 0; i < total; i++)
		{
			if (!_isChecking)
			{
				break;
			}
			FocusGame();
			inputSimulator.Keyboard.KeyPress(list[i]);
			inputSimulator.Keyboard.Sleep(100);
			int current = i + 1;
			base.Dispatcher.Invoke(delegate
			{
				ProgressText.Text = $"{current}/{total}";
				ProgressBar.Value = (double)current / (double)total * 100.0;
			});
		}
		int lParam = LoadKeyboardLayout("00000409", 1u);
		PostMessage(GetForegroundWindow(), 80, 1, lParam);
		base.Dispatcher.Invoke(delegate
		{
			_isChecking = false;
			base.Topmost = false;
			base.Opacity = 1.0;
			StartBtn.Visibility = Visibility.Visible;
			ProgressPanel.Visibility = Visibility.Collapsed;
			ResultText.Text = Strings.Get("Emulate_Done");
			ResultText.Visibility = Visibility.Visible;
		});
	}

	private static void FocusGame()
	{
		Process[] processesByName = Process.GetProcessesByName("cs2");
		if (processesByName.Length != 0 && processesByName[0].MainWindowHandle != IntPtr.Zero)
		{
			ShowWindow(processesByName[0].MainWindowHandle, 9);
			SetForegroundWindow(processesByName[0].MainWindowHandle);
		}
	}

}
