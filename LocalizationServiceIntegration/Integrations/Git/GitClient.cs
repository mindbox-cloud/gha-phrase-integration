using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Octokit;

namespace LocalizationServiceIntegration;

public class GitClient
{
	public const string BranchPrefix = "LocalizationPull";
	private readonly GitHubClient client;
	private readonly string gitHubToken;
	private readonly string repositoryName;
	private readonly string repositoryOwner;

	public GitClient(string gitHubToken, string repositoryOwner, string repositoryName)
	{
		this.gitHubToken = gitHubToken;
		this.repositoryOwner = repositoryOwner;
		this.repositoryName = repositoryName;

		client = new GitHubClient(new ProductHeaderValue("LocalizationServiceIntegration"))
		{
			Credentials = new Credentials(gitHubToken)
		};
	}

	public void CommitAllChangesToBranchAndPush(string branchName, string message)
	{
		ExecuteGitExeAndGetOutput("checkout", "-b", branchName);
		ExecuteGitExeAndGetOutput("add", "-A");
		ExecuteGitExeAndGetOutput("config", "--global", "user.email", "action-ci@mindbox.ru");
		ExecuteGitExeAndGetOutput("commit", "-am", message);
		ExecuteGitExeAndGetOutput("push", GetOAuthGitHubRepositoryLink(), branchName);
	}

	public async Task CreatePullRequestAndAddAutoMergeLabel(string branchName, string baseBranch)
	{
		var pullRequest = await client.PullRequest.Create(
			repositoryOwner,
			repositoryName,
			new NewPullRequest($"Automatic pull request for branch {branchName}", branchName, baseBranch)
		);

		await client.Issue.Labels
			.AddToIssue(repositoryOwner, repositoryName, pullRequest.Number, new[] {"Merge when ready"});
	}

	private static string ExecuteGitExeAndGetOutput(params string[] parameters)
	{
		var parametersString = string.Join(" ", parameters);
		Console.WriteLine($"git {parametersString}");

		using var p = new Process();

		p.StartInfo.UseShellExecute = false;
		p.StartInfo.RedirectStandardOutput = true;
		p.StartInfo.RedirectStandardError = true;
		p.StartInfo.FileName = "git";
		foreach (var parameter in parameters)
		{
			p.StartInfo.ArgumentList.Add(parameter);
		}

		p.Start();

		var output = p.StandardOutput.ReadToEnd();
		var errors = p.StandardError.ReadToEnd();

		Console.Error.WriteLine(errors);
		Console.WriteLine(output);

		p.WaitForExit();

		return output;
	}

	public async Task CleanStalePullRequestsAndBranches()
	{
		var pullRequestsCloseTasks = (await client.PullRequest.GetAllForRepository(repositoryOwner, repositoryName))
			.Where(pullRequest => pullRequest.State == ItemState.Open && pullRequest.Head.Ref.StartsWith(BranchPrefix))
			.Select(
				pullRequest => client.PullRequest.Update(
					repositoryOwner, repositoryOwner, pullRequest.Number, new PullRequestUpdate {State = ItemState.Closed}
				)
			);

		await Task.WhenAll(pullRequestsCloseTasks);

		var branchesRemovalTasks = (await client.Repository.Branch.GetAll(repositoryOwner, repositoryName))
			.Where(branch => branch.Name.StartsWith(BranchPrefix))
			.Select(branch => client.Git.Reference.Delete(repositoryOwner, repositoryName, "heads/" + branch.Name));

		await Task.WhenAll(branchesRemovalTasks);
	}

	private string GetOAuthGitHubRepositoryLink() =>
		$"https://{gitHubToken}:x-oauth-basic@github.com/{repositoryOwner}/{repositoryName}.git";

	public static bool HasChanges()
	{
		var result = ExecuteGitExeAndGetOutput("status");

		if (result.Contains("nothing to commit, working tree clean"))
			return false;

		if (result.Contains("Changes not staged for commit") || result.Contains("Untracked files"))
			return true;

		throw new InvalidOperationException("git status resulted with some not expected output");
	}
}