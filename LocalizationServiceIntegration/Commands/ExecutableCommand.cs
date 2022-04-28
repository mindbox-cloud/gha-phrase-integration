using System;
using System.CommandLine;
using System.Threading.Tasks;

namespace LocalizationServiceIntegration;

public abstract class ExecutableCommand : Command
{
	protected ExecutableCommand(IntegrationConfiguration configuration, string name, string description) : base(name, description)
	{
		Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
		PhraseAppClient = new PhraseAppClient(configuration.PhraseAppToken, configuration.ProjectId);
		GitClient = new GitClient(configuration.GitHubToken, configuration.RepositoryOwner, configuration.RepositoryName);
	}

	protected IntegrationConfiguration Configuration { get; }
	protected PhraseAppClient PhraseAppClient { get; }
	protected GitClient GitClient { get; }

	public abstract Task Execute();
}