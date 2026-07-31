using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Resources;
using NAudio.Wave;

namespace NiposChecker.Services;

public static class SoundService
{
	private static string _clickFile;

	private static string _softFile;

	private static bool _loaded;

	private static readonly HashSet<IDisposable> _active;

	public static bool Enabled { get; set; }

	public static double Volume { get; set; }

	private static string CfgPath => Path.Combine(AppContext.BaseDirectory, "sound.cfg");

	static SoundService()
	{
		Enabled = true;
		Volume = 0.3;
		_active = new HashSet<IDisposable>();
		try
		{
			if (File.Exists(CfgPath))
			{
				string[] array = File.ReadAllText(CfgPath).Trim().Split('|');
				if (array.Length >= 1)
				{
					Enabled = array[0] == "1";
				}
				if (array.Length >= 2 && double.TryParse(array[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
				{
					Volume = Math.Clamp(result, 0.0, 1.0);
				}
			}
		}
		catch
		{
		}
	}

	public static void SaveConfig()
	{
		try
		{
			File.WriteAllText(CfgPath, (Enabled ? "1" : "0") + "|" + Volume.ToString("0.##", CultureInfo.InvariantCulture));
		}
		catch
		{
		}
	}

	private static void EnsureLoaded()
	{
		if (!_loaded)
		{
			_loaded = true;
			_clickFile = Extract("Assets/sounds/click1.wav", "nipos_click1.wav");
			_softFile = Extract("Assets/sounds/click2.wav", "nipos_click2.wav");
		}
	}

	private static string Extract(string packPath, string tmpName)
	{
		try
		{
			StreamResourceInfo resourceStream = Application.GetResourceStream(new Uri("pack://application:,,,/" + packPath, UriKind.Absolute));
			if (resourceStream == null)
			{
				return null;
			}
			string text = Path.Combine(Path.GetTempPath(), tmpName);
			using (Stream stream = resourceStream.Stream)
			{
				using FileStream destination = File.Create(text);
				stream.CopyTo(destination);
			}
			return text;
		}
		catch
		{
			return null;
		}
	}

	private static void PlayFile(string path)
	{
		if (!Enabled || string.IsNullOrEmpty(path))
		{
			return;
		}
		try
		{
			AudioFileReader reader = new AudioFileReader(path)
			{
				Volume = (float)Math.Clamp(Volume, 0.0, 1.0)
			};
			WaveOutEvent output = new WaveOutEvent();
			output.Init(reader);
			lock (_active)
			{
				_active.Add(output);
				_active.Add(reader);
			}
			output.PlaybackStopped += delegate
			{
				lock (_active)
				{
					_active.Remove(output);
					_active.Remove(reader);
				}
				try
				{
					output.Dispose();
				}
				catch
				{
				}
				try
				{
					reader.Dispose();
				}
				catch
				{
				}
			};
			output.Play();
		}
		catch
		{
		}
	}

	public static void Click()
	{
		if (Enabled)
		{
			EnsureLoaded();
			PlayFile(_clickFile);
		}
	}

	public static void Soft()
	{
		if (Enabled)
		{
			EnsureLoaded();
			PlayFile(_softFile);
		}
	}
}
