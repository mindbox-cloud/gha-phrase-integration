using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using RestSharp;

namespace LocalizationServiceIntegration
{
	public class PhraseAppClient
	{
		private readonly string _apiKey;
		private readonly string _projectId;
		private readonly RestClient _client;

		public PhraseAppClient(string apiKey, string projectId)
		{
			_apiKey = apiKey;
			_projectId = projectId;

			_client = new RestClient("https://api.phraseapp.com/");
		}

		private IRestResponse ConfigureRequestAndExecute(Action<RestRequest> requestConfigurator)
		{
			var request = new RestRequest();
			request.AddHeader("Authorization", $"token {_apiKey}");

			requestConfigurator(request);

			var response = _client.Execute(request);

			if (!response.IsSuccessful)
				throw new InvalidOperationException(
					$"PhraseApp response is not OK! Response code: {response.StatusCode}, message: {response.ErrorMessage}");

			return response;
		}

		public string GetKeyLink(string key) => "https://phraseapp.com/accounts/mindbox-ltd/projects/mindbox/keys?utf8=✓" +
			 $"&translation_key_search%5Bquery%5D={key}";

		public void Push(
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

			ConfigureRequestAndExecute(request =>
			{
				request.Resource = $"api/v2/projects/{_projectId}/uploads";

				request.Method = Method.POST;

				request
					.WithParameter("autotranslate", "true")
					.WithParameter("file_format", "i18next")
					.WithParameter("skip_upload_tags", "true")
					.WithParameter("tags", tag)
					.WithParameter("locale_id", localeId);

				request.AddFile("file", filePath);
			});
		}

		public IReadOnlyDictionary<string, string> Pull(string localeId)
		{
			if (localeId == null)
				throw new ArgumentNullException(nameof(localeId));

			var response = ConfigureRequestAndExecute(request =>
			{
				request.Resource = $"/api/v2/projects/{_projectId}/locales/{localeId}/download";

				request.Method = Method.GET;

				request.WithParameter("file_format", "i18next");
			});

			var json = JsonConvert.DeserializeObject<Dictionary<string, string>>(response.Content);
			return json;
		}

		public void RemoveKey(string key)
		{
			ConfigureRequestAndExecute(request =>
			{
				request.AddHeader("Content-Type", "application/json");

				request.Resource = $"api/v2/projects/{_projectId}/keys";

				var filter = new
				{
					q = $"name:{key}"
				};

				request.AddJsonBody(filter);

				request.Method = Method.DELETE;
			});
		}

		public void Wipe(string localeId)
		{
			ConfigureRequestAndExecute(request =>
			{
				request.AddHeader("Content-Type", "application/json");

				request.Resource = $"api/v2/projects/{_projectId}/keys";

				request.Method = Method.DELETE;
			});
		}
	}
}