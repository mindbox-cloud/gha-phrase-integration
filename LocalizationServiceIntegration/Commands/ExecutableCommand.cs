using System.CommandLine;
using System.Threading.Tasks;

namespace LocalizationServiceIntegration;

public abstract class ExecutableCommand : Command
{
	protected Configuration Configuration { get; }
	protected PhraseAppClient PhraseAppClient { get; }

	protected ExecutableCommand(Configuration configuration, string name, string description) : base(name, description)
	{
		Configuration = configuration;
		PhraseAppClient = new PhraseAppClient(configuration.PhraseAppToken, configuration.ProjectId);
	}

	public abstract Task Execute();
}