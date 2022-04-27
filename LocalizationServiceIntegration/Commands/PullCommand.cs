using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Newtonsoft.Json;

namespace LocalizationServiceIntegration
{
	public class PullCommand : Command
	{
		private const int TotalWaitsLimit = 50;

		public PullCommand(string[] arguments) : base(arguments)
		{
		}

		protected override void ExecuteCore()
		{
			Directory.SetCurrentDirectory(Config.WorkingDirectory);
			
			UpdateLocalizationKeysFiles();
			UpdateCyrillicExceptionsFile();
			
			if (!AnyChangesWasMade())
			{
				Console.WriteLine("There are no changes in translations, exiting");
				return;
			}

			var branchName = $"LocalizationPull{DateTime.Now.Ticks}";

			PushChanges(branchName);
			CreatePullRequestAndMergeIt(branchName);
		}
		
		private void UpdateCyrillicExceptionsFile()
		{
			var cyrillicLinesInFolder = CyrillicCounter.CountLinesWithCyrillicInFolder(Config.WorkingDirectory);
			var exceptionsFilePath = Path.Combine(Config.WorkingDirectory, "build/cyrillic-lines-exceptions.json");

			var directory = Path.GetDirectoryName(exceptionsFilePath);

			if (!Directory.Exists(directory))
				Directory.CreateDirectory(directory);
			
			Console.WriteLine(JsonConvert.SerializeObject(cyrillicLinesInFolder, Formatting.Indented));
			
			File.WriteAllText(exceptionsFilePath, JsonConvert.SerializeObject(cyrillicLinesInFolder, Formatting.Indented));
		}
		
		private void UpdateLocalizationKeysFiles()
		{
			var localizationDataManager = new LocalizationDataManager(Config.GetReferenceLocale().Name, Config.WorkingDirectory);
			
			foreach (var configLocale in Config.Locales)
			{
				PullForLocale(configLocale, localizationDataManager);
			}
		}
		
		private void PullForLocale(LocaleInfo locale, LocalizationDataManager localizationDataManager)
		{
			var client = GetPhraseAppClient();
			
			var sourceLocalizationData = client.Pull(locale.Id);
			
			foreach (var namespaceInfo in localizationDataManager.GetNamespaces(locale.Name))
			{
				namespaceInfo.ApplyNewTranslations(sourceLocalizationData);
			}
		}
		
		private bool AnyChangesWasMade()
		{
			var gitClient = GetGitHubClient();
			
			return gitClient.HasChanges();
		}
		
		private void PushChanges(string branchName)
		{
			var gitHubClient = GetGitHubClient();
			
			gitHubClient.CommitAllChangesToBranchAndPush(branchName, "fix: localization (automatic integration commit)");
		}
		
		private void CreatePullRequestAndMergeIt(string branchName)
		{
			var gitClient = GetGitHubClient();
			
			string pullRequestNumber = null;
			var isPullRequestMerged = false;

			var waitCount = 0;

			try
			{
				var createPullRequestResponse = gitClient.CreatePullRequest(branchName);
				pullRequestNumber = createPullRequestResponse.Number;

				while (true)
				{
					var pullRequestStatus = gitClient.GetPullRequestStatus(createPullRequestResponse);

					var shouldWait = ProcessPullRequestStatusAndGetShouldWait(
						gitClient, 
						pullRequestStatus, 
						createPullRequestResponse.Number,
						createPullRequestResponse.Head.Sha);

					if (!shouldWait)
					{
						isPullRequestMerged = true;
						break;
					}

					if (waitCount > TotalWaitsLimit)
						throw new InvalidOperationException(
							$"Have waited for {gitClient.GetPullRequestLink(pullRequestNumber)} for too long");

					waitCount++;
					Thread.Sleep(TimeSpan.FromMinutes(1));
				}
			}
			finally
			{
				if (pullRequestNumber != null && !isPullRequestMerged)
				{
					gitClient.ClosePullRequest(pullRequestNumber);
				}

				gitClient.DeleteBranch(branchName);
			}
		}

		private bool ProcessPullRequestStatusAndGetShouldWait(
			GitClient client, 
			PullRequestStatus pullRequestStatus,
			string pullRequestNumber,
			string sha)
		{
			var pullRequestLink = client.GetPullRequestLink(pullRequestNumber);

			switch (pullRequestStatus)
			{
				case PullRequestStatus.InProcess:
					Console.WriteLine($"Pull request {pullRequestLink} is in process, waiting.");
					return true;

				case PullRequestStatus.CanBeMerged:
					client.Merge(pullRequestNumber, sha);
					Console.WriteLine($"Pull request {pullRequestLink} merged.");
					return false;

				case PullRequestStatus.Merged:
					// Видимо мы замержили его руками, всё ок
					return false;

				default:
					throw new InvalidOperationException(
						$"Can't merge pull request {pullRequestLink} with status {pullRequestStatus}");
			}
		}
	}
}