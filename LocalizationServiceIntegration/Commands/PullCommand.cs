using System;
using System.Threading;

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
			var localizationDataManager = new LocalizationDataManager(Config.GetReferenceLocale().Name);

			foreach (var configLocale in Config.Locales)
			{
				PullForLocale(configLocale, localizationDataManager);
			}

			var gitClient = GetGitHubClient();

			var hasChanges = gitClient.HasChanges();
			if (!hasChanges)
			{
				Console.WriteLine("There are no changes in translations, exiting");
				return;
			}

			var branchName = $"LocalizationPull{DateTime.Now.Ticks}";
			gitClient.CommitAllChangesToBranchAndPush(branchName, "Automatic commit for localization integration");

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

		private void PullForLocale(LocaleInfo locale, LocalizationDataManager localizationDataManager)
		{
			var client = GetPhraseAppClient();
			
			var sourceLocalizationData = client.Pull(locale.Id);
			
			foreach (var namespaceInfo in localizationDataManager.GetNamespaces(locale.Name))
			{
				namespaceInfo.ApplyNewTranslations(sourceLocalizationData);
			}
		}
	}
}