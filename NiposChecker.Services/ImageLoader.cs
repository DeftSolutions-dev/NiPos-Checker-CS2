using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace NiposChecker.Services;

public static class ImageLoader
{
	private static readonly HttpClient _http = new HttpClient
	{
		Timeout = TimeSpan.FromSeconds(15.0)
	};

	private static readonly ConcurrentDictionary<string, BitmapImage> _cache = new ConcurrentDictionary<string, BitmapImage>();

	public static async Task<BitmapImage> DownloadAsync(string url, int attempts = 3)
	{
		if (string.IsNullOrWhiteSpace(url))
		{
			return null;
		}
		if (_cache.TryGetValue(url, out var value))
		{
			return value;
		}
		string[] candidates = CandidateUrls(url);
		for (int i = 0; i < attempts; i++)
		{
			string[] array = candidates;
			foreach (string requestUri in array)
			{
				try
				{
					byte[] array2 = await _http.GetByteArrayAsync(requestUri);
					if (array2 == null || array2.Length == 0)
					{
						throw new Exception("empty");
					}
					BitmapImage bitmapImage = new BitmapImage();
					bitmapImage.BeginInit();
					bitmapImage.StreamSource = new MemoryStream(array2);
					bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
					bitmapImage.EndInit();
					bitmapImage.Freeze();
					_cache[url] = bitmapImage;
					return bitmapImage;
				}
				catch (Exception)
				{
				}
			}
			if (i < attempts - 1)
			{
				await Task.Delay(400 * (i + 1));
			}
		}
		return null;
	}

	private static string[] CandidateUrls(string url)
	{
		try
		{
			Uri uri = new Uri(url);
			List<string> list = new List<string>();
			list.Add(new Uri(Config.ApiUrl).Host);
			string[] apiUrls = Config.ApiUrls;
			for (int i = 0; i < apiUrls.Length; i++)
			{
				string host = new Uri(apiUrls[i]).Host;
				if (!list.Contains(host))
				{
					list.Add(host);
				}
			}
			if (!list.Contains(uri.Host))
			{
				return new string[1] { url };
			}
			List<string> list2 = new List<string>();
			foreach (string item in list)
			{
				UriBuilder uriBuilder = new UriBuilder(uri)
				{
					Host = item,
					Port = -1
				};
				list2.Add(uriBuilder.Uri.ToString());
			}
			return list2.ToArray();
		}
		catch
		{
			return new string[1] { url };
		}
	}
}
