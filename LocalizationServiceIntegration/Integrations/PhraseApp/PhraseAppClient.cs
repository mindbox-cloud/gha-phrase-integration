using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;

namespace LocalizationServiceIntegration;

public class PhraseAppClient
{
	private readonly string _apiKey;
	private readonly string _projectId;
	private readonly IFlurlClient _client;

	public PhraseAppClient(string apiKey, string projectId)
	{
		_apiKey = apiKey;
		_projectId = projectId;

		_client = new FlurlClient(
			new HttpClient
			{
				BaseAddress = new Uri("https://api.phraseapp.com/api/"),
				DefaultRequestHeaders = {{"Authorization", $"token {_apiKey}"}}
			}
		);
	}

	public string GetKeyLink(string key) => "https://phraseapp.com"
		.AppendPathSegments("accounts", "mindbox-ltd", "projects", "mindbox", "keys")
		.SetQueryParams(new Dictionary<string, string>()
		{
			["utf8"] = "✓",
			["translation_key_search[query]"] = key,
		});

	public async Task Push(
		string localeId,
		string tag,
		string filePath)
	{
		if (localeId == null)
			throw new ArgumentNullException(nameof(localeId));
		if (tag == null) 
			throw new ArgumentNullException(nameof(tag));
		if (filePath == null) 
			throw new ArgumentNullException(nameof(filePath));

		await _client.Request("projects", _projectId, "uploads")
			.SetQueryParams(
				new
				{
					autotranslate = true,
					file_format = "i18next",
					skip_upload_tags = true,
					tags = tag,
					locale_id = localeId
				}
			).PostMultipartAsync(content => content.AddFile("file", filePath));
	}

	public async Task<IReadOnlyDictionary<string, string>> Pull(string localeId)
	{
		if (localeId == null)
			throw new ArgumentNullException(nameof(localeId));

		return await _client.Request("v2", "projects", _projectId, "locales", localeId, "download")
			.SetQueryParam("file_format", "i18next")
			.GetJsonAsync<Dictionary<string, string>>();
	}

	public async Task RemoveKey(string key)
	{
		await _client.Request("v2", "projects", _projectId, "keys")
			.SendJsonAsync(
				HttpMethod.Delete, new
				{
					q = $"name:{key}"
				}
			);
	}

	public async Task Wipe(string localeId)
	{
		await _client.Request("v2", "projects", _projectId, "keys")
			.WithHeader("Content-Type", "application/json")
			.DeleteAsync();
	}
}