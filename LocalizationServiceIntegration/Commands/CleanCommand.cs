using System.Threading.Tasks;

namespace LocalizationServiceIntegration;

public class CleanCommand : ExecutableCommand
{
	public CleanCommand(IntegrationConfiguration configuration) : base(
		configuration, "clean", "Removes all stale pull requests and branches"
	)
	{
	}

	public override async Task Execute()
	{
		await GitClient.CleanStalePullRequestsAndBranches();
	}
}