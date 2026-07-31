using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NiposChecker.Models;

namespace NiposChecker.Services;

public class ApiClient : IDisposable
{
	private readonly HttpClient _http;

	public ApiClient()
	{
		_http = new HttpClient(CreatePinnedHandler())
		{
			BaseAddress = new Uri(Config.ApiUrl),
			Timeout = TimeSpan.FromSeconds(120.0)
		};
	}

	private static HttpClientHandler CreatePinnedHandler()
	{
		HttpClientHandler httpClientHandler = new HttpClientHandler();
		if (Config.CertPinsSha256 != null && Config.CertPinsSha256.Length != 0)
		{
			httpClientHandler.ServerCertificateCustomValidationCallback = PinCallback;
		}
		return httpClientHandler;
	}

	public static async Task SelectBaseAsync(int timeoutMs = 5000)
	{
		string[] apiUrls = Config.ApiUrls;
		if (apiUrls == null || apiUrls.Length <= 1)
		{
			return;
		}
		CancellationTokenSource cts = new CancellationTokenSource();
		try
		{
			List<Task<string>> tasks = new List<Task<string>>();
			string[] array = apiUrls;
			foreach (string text in array)
			{
				string u = text;
				tasks.Add(Task.Run(async delegate
				{
					try
					{
						using HttpClient hc = new HttpClient(CreatePinnedHandler())
						{
							Timeout = TimeSpan.FromMilliseconds(timeoutMs)
						};
						using (await hc.GetAsync(u, cts.Token))
						{
							return u;
						}
					}
					catch
					{
						return (string)null;
					}
				}));
			}
			while (tasks.Count > 0)
			{
				Task<string> task = await Task.WhenAny(tasks);
				tasks.Remove(task);
				string text2 = await task;
				if (text2 != null)
				{
					Config.SetActiveApiUrl(text2);
					cts.Cancel();
					break;
				}
			}
		}
		finally
		{
			if (cts != null)
			{
				((IDisposable)cts).Dispose();
			}
		}
	}

	private static bool PinCallback(HttpRequestMessage req, X509Certificate2 cert, X509Chain chain, SslPolicyErrors errors)
	{
		if (errors != SslPolicyErrors.None)
		{
			return false;
		}
		try
		{
			if (cert != null && SpkiMatches(cert))
			{
				return true;
			}
			if (chain != null)
			{
				foreach (X509ChainElement chainElement in chain.ChainElements)
				{
					if (SpkiMatches(chainElement.Certificate))
					{
						return true;
					}
				}
			}
		}
		catch
		{
		}
		return false;
	}

	private static bool SpkiMatches(X509Certificate2 cert)
	{
		string b = Convert.ToBase64String(SHA256.HashData(cert.PublicKey.ExportSubjectPublicKeyInfo()));
		string[] certPinsSha = Config.CertPinsSha256;
		for (int i = 0; i < certPinsSha.Length; i++)
		{
			if (string.Equals(certPinsSha[i], b, StringComparison.Ordinal))
			{
				return true;
			}
		}
		return false;
	}

	public async Task<JObject> CallAsync(string function, Dictionary<string, string> extraData = null)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>
		{
			["func"] = function,
			["key"] = Config.ClientKey
		};
		if (extraData != null)
		{
			foreach (KeyValuePair<string, string> extraDatum in extraData)
			{
				dictionary[extraDatum.Key] = extraDatum.Value;
			}
		}
		StringContent content = new StringContent(JsonConvert.SerializeObject(dictionary), Encoding.UTF8, "application/json");
		try
		{
			string text = await (await _http.PostAsync(_http.BaseAddress, content)).Content.ReadAsStringAsync();
			if (string.IsNullOrEmpty(text) || text == "{}")
			{
				return null;
			}
			return JObject.Parse(text);
		}
		catch (Exception)
		{
			return null;
		}
	}

	public async Task<string> SaveReportAsync(string reportJson, string hwid, string steam, string session = "")
	{
		return (await CallAsync("savereport", new Dictionary<string, string>
		{
			["report"] = reportJson ?? "{}",
			["hwid"] = hwid ?? "",
			["steam"] = steam ?? "",
			["session"] = session ?? ""
		}))?["url"]?.ToString();
	}

	public async Task SaveCandidatesAsync(string itemsJson, string hwid, string steam)
	{
		if (!string.IsNullOrEmpty(itemsJson))
		{
			await CallAsync("savecandidate", new Dictionary<string, string>
			{
				["items"] = itemsJson,
				["hwid"] = hwid ?? "",
				["steam"] = steam ?? ""
			});
		}
	}

	public async Task<List<ReportHistoryItem>> GetReportHistoryAsync(string steamid)
	{
		List<ReportHistoryItem> result = new List<ReportHistoryItem>();
		if (string.IsNullOrEmpty(steamid))
		{
			return result;
		}
		if (!((await CallAsync("reporthistory", new Dictionary<string, string> { ["steamid"] = steamid }))?["items"] is JArray jArray))
		{
			return result;
		}
		foreach (JToken item in jArray)
		{
			try
			{
				result.Add(item.ToObject<ReportHistoryItem>());
			}
			catch
			{
			}
		}
		return result;
	}

	public async Task<JToken> BlockDbLookupAsync(string param)
	{
		_ = 1;
		try
		{
			Uri requestUri = new Uri(_http.BaseAddress, "index.php?bdb=" + Uri.EscapeDataString(param) + "&key=" + Uri.EscapeDataString(Config.ClientKey));
			HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri);
			httpRequestMessage.Headers.TryAddWithoutValidation("Accept", "application/json");
			httpRequestMessage.Content = new StringContent("{\"check_votebkm\":false}", Encoding.UTF8, "application/json");
			return JToken.Parse(await (await _http.SendAsync(httpRequestMessage)).Content.ReadAsStringAsync());
		}
		catch (Exception)
		{
			return null;
		}
	}

	public async Task<ProjectInfo> GetProjectInfoAsync()
	{
		JObject jObject = await CallAsync("startcheck");
		if (jObject == null)
		{
			return new ProjectInfo();
		}
		return jObject.ToObject<ProjectInfo>();
	}

	public async Task<DatabaseData> GetDatabaseDataAsync()
	{
		JObject jObject = await CallAsync("searchdatabase");
		if (jObject == null)
		{
			return new DatabaseData();
		}
		return jObject.ToObject<DatabaseData>();
	}

	public async Task<string> CheckHwidBanAsync(string hwid)
	{
		JObject jObject = await CallAsync("check_for_hwidban", new Dictionary<string, string> { ["hwid"] = hwid });
		if (jObject?["ban_info"] != null)
		{
			JToken jToken = jObject["ban_info"];
			return $"{jToken["reason"]}|{jToken["ban_date"]}|{jToken["ban_end_date"]}|{jToken["issued_by"]}";
		}
		return null;
	}

	public void Dispose()
	{
		_http?.Dispose();
	}
}
