using System;
using System.Diagnostics;
using System.Threading.Tasks;

using Octokit;

namespace LocalizationServiceIntegration;

public class GitClient
{
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

	public async Task CreatePullRequestAndAddAutoMergeLabel(string branchName)
	{
		var pullRequest = await client.PullRequest.Create(
			repositoryOwner,
			repositoryName,
			new NewPullRequest($"Automatic pull request for branch {branchName}", branchName, "master")
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

	private string GetOAuthGitHubRepositoryLink() => $"https://{gitHubToken}:x-oauth-basic@github.com/{repositoryName}.git";

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