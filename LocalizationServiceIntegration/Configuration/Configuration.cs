using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace LocalizationServiceIntegration
{
	[DataContract]
	public class Configuration
	{
		public static Configuration Load()
		{
			var executionDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			var configPath = Path.Combine(executionDirectory, "LocalizationServiceIntegrationConfig.json");
			var result = JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(configPath));

			result.WorkingDirectory = Environment.GetEnvironmentVariable("GITHUB_WORKSPACE ") ?? "./";
			
			Console.WriteLine($"Working directory set to {result.WorkingDirectory}");
			
			var environmentPhraseAppToken = Environment.GetEnvironmentVariable("phraseAppToken");
			if (environmentPhraseAppToken != null)
				result.PhraseAppToken = environmentPhraseAppToken;

			var environmentGitHubToken = Environment.GetEnvironmentVariable("gitHubToken");
			if (environmentGitHubToken != null)
				result.GitHubToken = environmentGitHubToken;

			var environmentSlackWebhookUrl = Environment.GetEnvironmentVariable("slackWebhookUrl");
			if (environmentSlackWebhookUrl != null)
				result.SlackWebhookUrl = environmentSlackWebhookUrl;

			return result;
		}

		[DataMember(Name = "projectId")]
		public string ProjectId { get; set; }

		[DataMember(Name = "phraseAppToken")]
		public string PhraseAppToken { get; set; }

		[DataMember(Name = "locales")]
		public LocaleInfo[] Locales { get; set; }

		[DataMember(Name = "gitHubToken")]
		public string GitHubToken { get; set; }

		[DataMember(Name = "slackWebhookUrl")]
		public string SlackWebhookUrl { get; set; }

		public string WorkingDirectory { get; set; }
		
		public LocaleInfo GetReferenceLocale()
		{
			return Locales.Where(l => l.IsReference).Single();
		}
	}
}