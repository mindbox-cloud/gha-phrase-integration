using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LocalizationServiceIntegration;

public class PullCommand : ExecutableCommand
{
	public PullCommand(Configuration configuration) : base(configuration, "Pull", "Pulls localization from PhraseApp")
	{ }
		
	public override async Task Execute()
	{
		var localizationDataManager = new LocalizationDataManager(Configuration.GetReferenceLocale().Name, Configuration.WorkingDirectory);
		Directory.SetCurrentDirectory(Configuration.WorkingDirectory);

		await Task.WhenAll(Configuration.Locales.Select(locale => PullForLocale(locale, localizationDataManager)));

		var gitClient = new GitClient(Configuration.GitHubToken, Configuration.RepositoryOwner, Configuration.RepositoryName);

		var hasChanges = gitClient.HasChanges();
		if (!hasChanges)
		{
			Console.WriteLine("There are no changes in translations, exiting");
			return;
		}

		var branchName = $"LocalizationPull{DateTime.Now.Ticks}";
		gitClient.CommitAllChangesToBranchAndPush(branchName, "fix: localization (automatic integration commit)");

		await gitClient.CreatePullRequestAndAddAutoMergeLabel(branchName);
	}

	private async Task PullForLocale(LocaleInfo locale, LocalizationDataManager localizationDataManager)
	{
		var sourceLocalizationData = await PhraseAppClient.Pull(locale.Id);

		await Task.WhenAll(
			localizationDataManager.GetNamespaces(locale.Name)
				.Select(namespaceInfo => namespaceInfo.ApplyNewTranslations(sourceLocalizationData))
		);
	}
}