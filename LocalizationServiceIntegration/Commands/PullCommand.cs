using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace LocalizationServiceIntegration;

public class PullCommand : ExecutableCommand
{
	public PullCommand(IntegrationConfiguration configuration) : base(configuration, "pull", "Pulls localization from PhraseApp")
	{
	}

	public override async Task Execute()
	{
		Console.WriteLine("Cleaning stale pull requests and branches...");

		await GitClient.CleanStalePullRequestsAndBranches();

		Directory.SetCurrentDirectory(Configuration.WorkingDirectory);

		var localizationDataManager = new LocalizationDataManager(
			Configuration.GetReferenceLocale().Name, Configuration.WorkingDirectory
		);

		await Task.WhenAll(Configuration.Locales.Select(locale => PullForLocale(locale, localizationDataManager)));

		await UpdateCyrillicExceptionsFile();

		var hasChanges = GitClient.HasChanges();
		if (!hasChanges)
		{
			Console.WriteLine("There are no changes in translations, exiting");

			return;
		}

		var branchName = GitClient.BranchPrefix + DateTime.Now.Ticks;
		GitClient.CommitAllChangesToBranchAndPush(branchName, "fix: localization (automatic integration commit)");

		await GitClient.CreatePullRequestWithAutoMerge(branchName, Configuration.BaseBranch);
	}

	private async Task UpdateCyrillicExceptionsFile()
	{
		var cyrillicLinesInFolder = CyrillicCounter.CountLinesWithCyrillicInFolder(Configuration.WorkingDirectory);
		var exceptionsFilePath = Path.Combine(Configuration.WorkingDirectory, "localization/cyrillic-lines-exceptions.json");

		var directory = Path.GetDirectoryName(exceptionsFilePath);

		if (!Directory.Exists(directory))
			Directory.CreateDirectory(directory);

		await File.WriteAllTextAsync(exceptionsFilePath, JsonConvert.SerializeObject(cyrillicLinesInFolder, Formatting.Indented));
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