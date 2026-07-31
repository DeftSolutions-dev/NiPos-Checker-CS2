using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace NiposChecker.Services;

public static class Branding
{
	private const string ReferenceHex = "#FFFF283F";

	private static readonly string[] ColorKeys = new string[14]
	{
		"Clr_Brand", "Clr_BrandSoft", "Clr_BrandHover", "Clr_BrandDeep", "Clr_BrandDeep2", "Clr_BrandGlow", "Clr_BrandTag", "Clr_BrandFill", "Clr_BrandFillSoft", "Clr_BrandFillFaint",
		"Clr_BrandFillDim", "Clr_BrandLine", "Clr_BrandLineSoft", "Clr_BrandLineHard"
	};

	private static readonly string[] BrushKeys = new string[22]
	{
		"Brush_Brand", "Brush_BrandSoft", "Brush_BrandHover", "Brush_BrandFill", "Brush_BrandTag", "Brush_BrandFillSoft", "Brush_BrandFillFaint", "Brush_BrandFillDim", "Brush_BrandLine", "Brush_BrandLineSoft",
		"Brush_BrandLineHard", "Brush_Neon", "Brush_NeonBlue", "Brush_BlueGlow", "Brush_BlueBtn", "Brush_Red", "Brush_RedBright", "Brush_BlueButton", "Brush_GreenButton", "Brush_RedButton",
		"Brush_MenuActive", "Brush_WindowBg"
	};

	private static Color[] _baseColors;

	private static Dictionary<string, Color[]> _baseBrush;

	private static string _appliedHex;

	public static double Delta { get; private set; }

	public static Color Shift(Color c)
	{
		if (!(Math.Abs(Delta) < 0.001))
		{
			return RotateHue(c, Delta);
		}
		return c;
	}

	public static void ApplyBrandColor(string hex)
	{
		if (string.IsNullOrWhiteSpace(hex))
		{
			return;
		}
		Color c;
		try
		{
			c = (Color)ColorConverter.ConvertFromString(Normalize(hex));
		}
		catch
		{
			return;
		}
		ResourceDictionary resourceDictionary = Application.Current?.Resources;
		if (resourceDictionary == null)
		{
			return;
		}
		string text = Normalize(hex);
		if (_appliedHex == text)
		{
			return;
		}
		SnapshotBase(resourceDictionary);
		Color c2 = (Color)ColorConverter.ConvertFromString("#FFFF283F");
		double num = (Delta = Hue(c) - Hue(c2));
		for (int i = 0; i < ColorKeys.Length; i++)
		{
			resourceDictionary[ColorKeys[i]] = RotateHue(_baseColors[i], num);
		}
		string[] brushKeys = BrushKeys;
		foreach (string key in brushKeys)
		{
			if (_baseBrush.TryGetValue(key, out var value))
			{
				RecolorBrush(resourceDictionary, key, value, num);
			}
		}
		Color color = RotateHue(_baseColors[0], num);
		Color color2 = ((0.299 * (double)(int)color.R + 0.587 * (double)(int)color.G + 0.114 * (double)(int)color.B > 150.0) ? Color.FromRgb(10, 10, 12) : Colors.White);
		resourceDictionary["Clr_BrandText"] = color2;
		if (resourceDictionary["Brush_BrandText"] is SolidColorBrush { IsFrozen: false } solidColorBrush)
		{
			solidColorBrush.Color = color2;
		}
		_appliedHex = text;
	}

	private static void SnapshotBase(ResourceDictionary res)
	{
		if (_baseColors != null)
		{
			return;
		}
		_baseColors = new Color[ColorKeys.Length];
		for (int i = 0; i < ColorKeys.Length; i++)
		{
			_baseColors[i] = ((res[ColorKeys[i]] is Color color) ? color : Colors.Transparent);
		}
		_baseBrush = new Dictionary<string, Color[]>();
		string[] brushKeys = BrushKeys;
		foreach (string key in brushKeys)
		{
			object obj = res[key];
			if (obj is SolidColorBrush solidColorBrush)
			{
				_baseBrush[key] = new Color[1] { solidColorBrush.Color };
			}
			else if (obj is GradientBrush gradientBrush)
			{
				Color[] array = new Color[gradientBrush.GradientStops.Count];
				for (int k = 0; k < array.Length; k++)
				{
					array[k] = gradientBrush.GradientStops[k].Color;
				}
				_baseBrush[key] = array;
			}
		}
	}

	private static void RecolorBrush(ResourceDictionary res, string key, Color[] baseStops, double delta)
	{
		object obj = res[key];
		if (obj is SolidColorBrush solidColorBrush)
		{
			Color color = RotateHue(baseStops[0], delta);
			if (solidColorBrush.IsFrozen)
			{
				SolidColorBrush solidColorBrush2 = new SolidColorBrush(color);
				solidColorBrush2.Opacity = solidColorBrush.Opacity;
				res[key] = solidColorBrush2;
			}
			else
			{
				solidColorBrush.Color = color;
			}
		}
		else
		{
			if (!(obj is GradientBrush gradientBrush))
			{
				return;
			}
			if (gradientBrush.IsFrozen)
			{
				GradientBrush gradientBrush2 = gradientBrush.Clone();
				for (int i = 0; i < gradientBrush2.GradientStops.Count && i < baseStops.Length; i++)
				{
					gradientBrush2.GradientStops[i].Color = RotateHue(baseStops[i], delta);
				}
				res[key] = gradientBrush2;
			}
			else
			{
				for (int j = 0; j < gradientBrush.GradientStops.Count && j < baseStops.Length; j++)
				{
					gradientBrush.GradientStops[j].Color = RotateHue(baseStops[j], delta);
				}
			}
		}
	}

	private static Color RotateHue(Color c, double deltaDeg)
	{
		RgbToHsl(c.R, c.G, c.B, out var h, out var s, out var l);
		if (s < 0.02)
		{
			return c;
		}
		h = NormAngle(h + deltaDeg);
		HslToRgb(h, s, l, out var R, out var G, out var B);
		return Color.FromArgb(c.A, R, G, B);
	}

	private static double Hue(Color c)
	{
		RgbToHsl(c.R, c.G, c.B, out var h, out var _, out var _);
		return h;
	}

	private static void RgbToHsl(byte R, byte G, byte B, out double h, out double s, out double l)
	{
		double num = (double)(int)R / 255.0;
		double num2 = (double)(int)G / 255.0;
		double num3 = (double)(int)B / 255.0;
		double num4 = Math.Max(num, Math.Max(num2, num3));
		double num5 = Math.Min(num, Math.Min(num2, num3));
		l = (num4 + num5) / 2.0;
		double num6 = num4 - num5;
		if (num6 < 1E-09)
		{
			h = 0.0;
			s = 0.0;
			return;
		}
		s = ((l > 0.5) ? (num6 / (2.0 - num4 - num5)) : (num6 / (num4 + num5)));
		if (num4 == num)
		{
			h = (num2 - num3) / num6 + (double)((num2 < num3) ? 6 : 0);
		}
		else if (num4 == num2)
		{
			h = (num3 - num) / num6 + 2.0;
		}
		else
		{
			h = (num - num2) / num6 + 4.0;
		}
		h *= 60.0;
	}

	private static void HslToRgb(double h, double s, double l, out byte R, out byte G, out byte B)
	{
		h = NormAngle(h) / 360.0;
		double val;
		double val2;
		double val3;
		if (s < 1E-09)
		{
			val = (val2 = (val3 = l));
		}
		else
		{
			double num = ((l < 0.5) ? (l * (1.0 + s)) : (l + s - l * s));
			double p = 2.0 * l - num;
			val = HueToRgb(p, num, h + 1.0 / 3.0);
			val2 = HueToRgb(p, num, h);
			val3 = HueToRgb(p, num, h - 1.0 / 3.0);
		}
		R = (byte)Math.Round(Math.Max(0.0, Math.Min(1.0, val)) * 255.0);
		G = (byte)Math.Round(Math.Max(0.0, Math.Min(1.0, val2)) * 255.0);
		B = (byte)Math.Round(Math.Max(0.0, Math.Min(1.0, val3)) * 255.0);
	}

	private static double HueToRgb(double p, double q, double t)
	{
		if (t < 0.0)
		{
			t += 1.0;
		}
		if (t > 1.0)
		{
			t -= 1.0;
		}
		if (t < 1.0 / 6.0)
		{
			return p + (q - p) * 6.0 * t;
		}
		if (t < 0.5)
		{
			return q;
		}
		if (t < 2.0 / 3.0)
		{
			return p + (q - p) * (2.0 / 3.0 - t) * 6.0;
		}
		return p;
	}

	private static double NormAngle(double a)
	{
		a %= 360.0;
		if (a < 0.0)
		{
			a += 360.0;
		}
		return a;
	}

	private static string Normalize(string hex)
	{
		hex = hex.Trim();
		if (!hex.StartsWith("#"))
		{
			hex = "#" + hex;
		}
		if (hex.Length == 7)
		{
			hex = "#FF" + hex.Substring(1);
		}
		return hex;
	}
}
