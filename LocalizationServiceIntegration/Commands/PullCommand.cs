using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LocalizationServiceIntegration;

public class PullCommand : ExecutableCommand
{
	public PullCommand(IntegrationConfiguration configuration) : base(configuration, "pull", "Pulls localization from PhraseApp")
	{
	}

	public override async Task Execute()
	{
		var localizationDataManager = new LocalizationDataManager(
			Configuration.GetReferenceLocale().Name, Configuration.WorkingDirectory
		);

		Directory.SetCurrentDirectory(Configuration.WorkingDirectory);

		await Task.WhenAll(Configuration.Locales.Select(locale => PullForLocale(locale, localizationDataManager)));

		var hasChanges = GitClient.HasChanges();
		if (!hasChanges)
		{
			Console.WriteLine("There are no changes in translations, exiting");

			return;
		}

		var branchName = GitClient.BranchPrefix + DateTime.Now.Ticks;
		GitClient.CommitAllChangesToBranchAndPush(branchName, "fix: localization (automatic integration commit)");

		await GitClient.CreatePullRequestAndAddAutoMergeLabel(branchName, Configuration.BaseBranch);
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