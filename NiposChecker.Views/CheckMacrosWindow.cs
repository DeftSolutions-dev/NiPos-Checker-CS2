using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using NiposChecker.Localization;

namespace NiposChecker.Views;

public partial class CheckMacrosWindow : Window
{
	private bool _isDrawing;

	private Point _lastPoint;

	private Polyline _currentLine;

	private DispatcherTimer _timer;

	private double _strokeThickness;

	private double _increaseRate;

	private Brush _lineColor = Brushes.White;















	public CheckMacrosWindow()
	{
		InitializeComponent();
		base.Title = Strings.Get("Macros_Title");
		HeaderTitle.Text = Strings.Get("Macros_Title");
		ColorLabel.Text = Strings.Get("Macros_Color");
		TplCs2Btn.Content = Strings.Get("Macros_CrosshairCS2");
		TplModelsBtn.Content = Strings.Get("Macros_CrosshairModel");
		ClearBtn.Content = Strings.Get("Macros_Clear");
		_strokeThickness = 1.0;
		_increaseRate = 0.1;
		_timer = new DispatcherTimer
		{
			Interval = TimeSpan.FromMilliseconds(50.0)
		};
		_timer.Tick += Timer_Tick;
		base.Topmost = true;
		base.Opacity = 0.85;
	}

	private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (!(e.OriginalSource is Canvas))
		{
			DragMove();
		}
	}

	private void Close_Click(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private void Timer_Tick(object sender, EventArgs e)
	{
		_strokeThickness += _increaseRate;
		if (_currentLine != null && _isDrawing)
		{
			Point position = Mouse.GetPosition(DrawingCanvas);
			CreateNewSegment(position);
		}
	}

	private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
	{
		_isDrawing = true;
		_lastPoint = e.GetPosition(DrawingCanvas);
		CreateNewSegment(_lastPoint);
		_timer.Start();
	}

	private void Canvas_MouseMove(object sender, MouseEventArgs e)
	{
		if (_isDrawing)
		{
			Point position = e.GetPosition(DrawingCanvas);
			_currentLine?.Points.Add(position);
			_lastPoint = position;
		}
	}

	private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
	{
		StopDrawing();
	}

	private void Canvas_MouseLeave(object sender, MouseEventArgs e)
	{
		StopDrawing();
	}

	private void StopDrawing()
	{
		_isDrawing = false;
		_timer.Stop();
		_strokeThickness = 1.0;
		AnimateAndRemoveLine(_currentLine);
	}

	private void CreateNewSegment(Point point)
	{
		_currentLine = new Polyline
		{
			Stroke = _lineColor,
			StrokeThickness = _strokeThickness,
			StrokeLineJoin = PenLineJoin.Round
		};
		_currentLine.Points.Add(point);
		DrawingCanvas.Children.Add(_currentLine);
	}

	private void AnimateAndRemoveLine(Polyline line)
	{
		if (line != null)
		{
			DoubleAnimation doubleAnimation = new DoubleAnimation(1.0, 0.0, TimeSpan.FromSeconds(3.0))
			{
				FillBehavior = FillBehavior.HoldEnd
			};
			doubleAnimation.Completed += delegate
			{
				DrawingCanvas.Children.Remove(line);
			};
			line.BeginAnimation(UIElement.OpacityProperty, doubleAnimation);
		}
	}

	private void SetColor(Brush color, Rectangle active)
	{
		_lineColor = color;
		Rectangle[] array = new Rectangle[7] { ClrWhite, ClrRed, ClrYellow, ClrGreen, ClrCyan, ClrBlue, ClrPurple };
		foreach (Rectangle obj in array)
		{
			obj.Stroke = ((obj == active) ? new SolidColorBrush(Color.FromArgb(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue)) : Brushes.Transparent);
		}
	}

	private void Color_White(object s, MouseButtonEventArgs e)
	{
		SetColor(Brushes.White, ClrWhite);
	}

	private void Color_Red(object s, MouseButtonEventArgs e)
	{
		SetColor(Brushes.Red, ClrRed);
	}

	private void Color_Yellow(object s, MouseButtonEventArgs e)
	{
		SetColor(Brushes.Yellow, ClrYellow);
	}

	private void Color_Green(object s, MouseButtonEventArgs e)
	{
		SetColor(Brushes.LightGreen, ClrGreen);
	}

	private void Color_Cyan(object s, MouseButtonEventArgs e)
	{
		SetColor(Brushes.Azure, ClrCyan);
	}

	private void Color_Blue(object s, MouseButtonEventArgs e)
	{
		SetColor(Brushes.Blue, ClrBlue);
	}

	private void Color_Purple(object s, MouseButtonEventArgs e)
	{
		SetColor(Brushes.MediumPurple, ClrPurple);
	}

	private void Template_CS2_Click(object sender, RoutedEventArgs e)
	{
		DrawingCanvas.Children.Clear();
		double num = DrawingCanvas.ActualWidth / 2.0;
		double num2 = DrawingCanvas.ActualHeight / 2.0;
		AddStaticLine(num - 20.0, num2, num - 6.0, num2, Brushes.Green, 2.0);
		AddStaticLine(num + 6.0, num2, num + 20.0, num2, Brushes.Green, 2.0);
		AddStaticLine(num, num2 - 20.0, num, num2 - 6.0, Brushes.Green, 2.0);
		AddStaticLine(num, num2 + 6.0, num, num2 + 20.0, Brushes.Green, 2.0);
		AddStaticLine(num - 1.0, num2, num + 1.0, num2, Brushes.Green, 2.0);
	}

	private void Template_Models_Click(object sender, RoutedEventArgs e)
	{
		DrawingCanvas.Children.Clear();
		double num = DrawingCanvas.ActualWidth / 2.0;
		double num2 = DrawingCanvas.ActualHeight / 2.0;
		Ellipse element = new Ellipse
		{
			Width = 40.0,
			Height = 40.0,
			Stroke = Brushes.Red,
			StrokeThickness = 1.5,
			Fill = Brushes.Transparent
		};
		Canvas.SetLeft(element, num - 20.0);
		Canvas.SetTop(element, num2 - 20.0);
		DrawingCanvas.Children.Add(element);
		AddStaticLine(num - 30.0, num2, num + 30.0, num2, Brushes.Red, 1.0);
		AddStaticLine(num, num2 - 30.0, num, num2 + 30.0, Brushes.Red, 1.0);
	}

	private void AddStaticLine(double x1, double y1, double x2, double y2, Brush color, double thickness)
	{
		Line element = new Line
		{
			X1 = x1,
			Y1 = y1,
			X2 = x2,
			Y2 = y2,
			Stroke = color,
			StrokeThickness = thickness
		};
		DrawingCanvas.Children.Add(element);
	}

	private void ClearCanvas_Click(object sender, RoutedEventArgs e)
	{
		DrawingCanvas.Children.Clear();
	}

}
