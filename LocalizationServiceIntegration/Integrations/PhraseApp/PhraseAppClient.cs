﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

using Flurl;
using Flurl.Http;

namespace LocalizationServiceIntegration;

public class PhraseAppClient : IDisposable
{
	private readonly IFlurlClient client;
	private readonly string projectId;

	public PhraseAppClient(string apiKey, string projectId)
	{
		this.projectId = projectId;

		client = new FlurlClient(
#pragma warning disable CA2000 // Dispose objects before losing scope
			new HttpClient
			{
				BaseAddress = new Uri("https://api.phrase.com/v2/")
			}
#pragma warning restore CA2000 // Dispose objects before losing scope
		);

		client.Headers.Add("Authorization", $"token {apiKey}");
	}

	public void Dispose() => client.Dispose();

	public static string GetKeyLink(string key) => "https://phraseapp.com"
		.AppendPathSegments("accounts", "mindbox-ltd", "projects", "mindbox", "keys")
		.SetQueryParams(new Dictionary<string, string> {["utf8"] = "✓", ["translation_key_search[query]"] = key});

	public async Task<IReadOnlyDictionary<string, string>> Pull(string localeId)
	{
		if (localeId == null)
			throw new ArgumentNullException(nameof(localeId));

		return await client.Request("projects", projectId, "locales", localeId, "download")
			.SetQueryParam("file_format", "i18next")
			.GetJsonAsync<Dictionary<string, string>>();
	}

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

		await client.Request("projects", projectId, "uploads")
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

	public async Task RemoveKey(string key) =>
		await client.Request("projects", projectId, "keys")
			.SendJsonAsync(
				HttpMethod.Delete, new {q = $"name:{key}"}
			);

	public async Task Wipe() =>
		await client.Request("projects", projectId, "keys")
			.WithHeader("Content-Type", "application/json")
			.DeleteAsync();
}