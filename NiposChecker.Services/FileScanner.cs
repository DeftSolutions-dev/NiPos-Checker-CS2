using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NiposChecker.Models;

namespace NiposChecker.Services;

public class FileScanner
{
	private CancellationTokenSource _cts;

	private long _scannedCount;

	private long _totalFiles;

	private const int MultiThreadThreshold = 50;

	private const int MaxWorkers = 4;

	public bool IsRunning
	{
		get
		{
			if (_cts != null)
			{
				return !_cts.IsCancellationRequested;
			}
			return false;
		}
	}

	public event Action<FoundedFile> FileFound;

	public event Action<bool, long> SearchCompleted;

	public event Action<long, long, string> Progress;

	public async Task StartAsync(IEnumerable<string> directories, CheatDatabase cheatDb, bool useIconMatch = false, bool useSignatureMatch = false)
	{
		if (_cts != null)
		{
			return;
		}
		_cts = new CancellationTokenSource();
		CancellationToken token = _cts.Token;
		Stopwatch sw = Stopwatch.StartNew();
		Interlocked.Exchange(ref _scannedCount, 0L);
		_totalFiles = 0L;
		try
		{
			List<string> rootDirs = directories.Where((string d) => !string.IsNullOrEmpty(d) && Directory.Exists(d)).Distinct().ToList();
			if (rootDirs.Count == 0)
			{
				sw.Stop();
				this.SearchCompleted?.Invoke(arg1: false, sw.ElapsedMilliseconds);
				_cts = null;
				return;
			}
			await Task.Run(delegate
			{
				CancellationTokenSource countCts = CancellationTokenSource.CreateLinkedTokenSource(token);
				try
				{
					Task.Run(delegate
					{
						try
						{
							_totalFiles = CountFiles(rootDirs, countCts.Token);
						}
						catch
						{
						}
					}, countCts.Token);
					if (CountDirectories(rootDirs, token) < 50)
					{
						foreach (string item in rootDirs)
						{
							if (token.IsCancellationRequested)
							{
								break;
							}
							ScanDirectorySingle(item, cheatDb, useIconMatch, useSignatureMatch, token);
						}
					}
					else
					{
						ConcurrentQueue<string> queue = new ConcurrentQueue<string>(rootDirs);
						int num = Math.Min(4, Environment.ProcessorCount);
						Task[] array = new Task[num];
						for (int num2 = 0; num2 < num; num2++)
						{
							array[num2] = Task.Run(delegate
							{
								WorkerLoop(queue, cheatDb, useIconMatch, useSignatureMatch, token);
							}, token);
						}
						Task.WaitAll(array);
					}
					countCts.Cancel();
				}
				finally
				{
					if (countCts != null)
					{
						((IDisposable)countCts).Dispose();
					}
				}
			}, token);
		}
		catch (OperationCanceledException)
		{
		}
		catch (Exception)
		{
		}
		finally
		{
			sw.Stop();
			bool isCancellationRequested = token.IsCancellationRequested;
			this.SearchCompleted?.Invoke(isCancellationRequested, sw.ElapsedMilliseconds);
			_cts = null;
		}
	}

	public void Stop()
	{
		_cts?.Cancel();
	}

	private void ScanDirectorySingle(string dir, CheatDatabase cheatDb, bool useIconMatch, bool useSignatureMatch, CancellationToken token)
	{
		if (token.IsCancellationRequested)
		{
			return;
		}
		try
		{
			ScanFiles(dir, cheatDb, useIconMatch, useSignatureMatch, token);
			foreach (string item in Directory.EnumerateDirectories(dir))
			{
				if (token.IsCancellationRequested)
				{
					break;
				}
				ScanDirectorySingle(item, cheatDb, useIconMatch, useSignatureMatch, token);
			}
		}
		catch (UnauthorizedAccessException)
		{
		}
		catch (DirectoryNotFoundException)
		{
		}
	}

	private void WorkerLoop(ConcurrentQueue<string> queue, CheatDatabase cheatDb, bool useIconMatch, bool useSignatureMatch, CancellationToken token)
	{
		string result;
		while (!token.IsCancellationRequested && queue.TryDequeue(out result))
		{
			try
			{
				ScanFiles(result, cheatDb, useIconMatch, useSignatureMatch, token);
				foreach (string item in Directory.EnumerateDirectories(result))
				{
					if (token.IsCancellationRequested)
					{
						return;
					}
					queue.Enqueue(item);
				}
			}
			catch (UnauthorizedAccessException)
			{
			}
			catch (DirectoryNotFoundException)
			{
			}
		}
	}

	private void ScanFiles(string dir, CheatDatabase cheatDb, bool useIconMatch, bool useSignatureMatch, CancellationToken token)
	{
		if (token.IsCancellationRequested)
		{
			return;
		}
		bool flag = IsWindowsSystemDir(dir);
		foreach (string item in Directory.EnumerateFiles(dir))
		{
			if (token.IsCancellationRequested)
			{
				return;
			}
			Interlocked.Increment(ref _scannedCount);
			try
			{
				FileInfo fileInfo = new FileInfo(item);
				if (fileInfo.Length <= 62914560 && (!flag || !IsMicrosoftSigned(fileInfo.FullName)))
				{
					DetectionResult detectionResult = cheatDb.Evaluate(fileInfo.FullName, useIconMatch, useSignatureMatch);
					if (detectionResult != null)
					{
						this.FileFound?.Invoke(new FoundedFile
						{
							Name = fileInfo.Name,
							CheatName = detectionResult.CheatName,
							Type = fileInfo.Extension,
							Path = fileInfo.FullName,
							Weight = (fileInfo.Length / 1024).ToString("N0") + " KB",
							LastChange = fileInfo.LastWriteTime.ToString("dd.MM.yyyy HH:mm"),
							LastAccess = fileInfo.LastAccessTime.ToString("dd.MM.yyyy HH:mm"),
							IsDetected = true,
							Score = detectionResult.Score,
							Severity = detectionResult.Severity,
							MatchedSignals = string.Join(", ", detectionResult.Signals),
							Source = ReadZoneSource(fileInfo.FullName),
							FileIcon = GetFileIcon(fileInfo.FullName)
						});
					}
				}
			}
			catch
			{
			}
		}
		this.Progress?.Invoke(Interlocked.Read(in _scannedCount), _totalFiles, dir);
	}

	private static ImageSource GetFileIcon(string path)
	{
		try
		{
			using Icon icon = Icon.ExtractAssociatedIcon(path);
			if (icon == null)
			{
				return null;
			}
			BitmapSource bitmapSource = Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
			bitmapSource.Freeze();
			return bitmapSource;
		}
		catch
		{
			return null;
		}
	}

	private static string ReadZoneSource(string filePath)
	{
		try
		{
			string text = File.ReadAllText(filePath + ":Zone.Identifier");
			if (string.IsNullOrEmpty(text))
			{
				return null;
			}
			Match match = Regex.Match(text, "HostUrl=(.+)");
			if (match.Success)
			{
				return match.Groups[1].Value.Trim();
			}
			match = Regex.Match(text, "ReferrerUrl=(.+)");
			if (match.Success)
			{
				return match.Groups[1].Value.Trim();
			}
			if (text.Contains("ZoneId=3"))
			{
				return "интернет (источник не записан)";
			}
		}
		catch
		{
		}
		return null;
	}

	private static bool IsWindowsSystemDir(string dir)
	{
		try
		{
			string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
			if (string.IsNullOrEmpty(folderPath))
			{
				return false;
			}
			string text = Path.GetFullPath(dir).TrimEnd('\\') + "\\";
			string value = Path.GetFullPath(folderPath).TrimEnd('\\') + "\\";
			return text.StartsWith(value, StringComparison.OrdinalIgnoreCase);
		}
		catch
		{
			return false;
		}
	}

	private static bool IsMicrosoftSigned(string filePath)
	{
		try
		{
			using X509Certificate2 x509Certificate = new X509Certificate2(filePath);
			if (x509Certificate == null)
			{
				return false;
			}
			string subject = x509Certificate.Subject;
			string issuer = x509Certificate.Issuer;
			return subject.IndexOf("Microsoft", StringComparison.OrdinalIgnoreCase) >= 0 || issuer.IndexOf("Microsoft", StringComparison.OrdinalIgnoreCase) >= 0;
		}
		catch
		{
			return false;
		}
	}

	private static long CountFiles(List<string> roots, CancellationToken token)
	{
		long num = 0L;
		Stack<string> stack = new Stack<string>(roots);
		while (stack.Count > 0)
		{
			if (token.IsCancellationRequested)
			{
				return num;
			}
			string path = stack.Pop();
			try
			{
				foreach (string item in Directory.EnumerateFiles(path))
				{
					_ = item;
					num++;
				}
				foreach (string item2 in Directory.EnumerateDirectories(path))
				{
					stack.Push(item2);
				}
			}
			catch (UnauthorizedAccessException)
			{
			}
			catch (DirectoryNotFoundException)
			{
			}
			catch
			{
			}
		}
		return num;
	}

	private static int CountDirectories(List<string> roots, CancellationToken token)
	{
		int num = 0;
		foreach (string root in roots)
		{
			if (token.IsCancellationRequested)
			{
				return num;
			}
			try
			{
				foreach (string item in Directory.EnumerateDirectories(root))
				{
					_ = item;
					num++;
					if (num > 50)
					{
						return num;
					}
				}
			}
			catch
			{
			}
		}
		return num;
	}
}
