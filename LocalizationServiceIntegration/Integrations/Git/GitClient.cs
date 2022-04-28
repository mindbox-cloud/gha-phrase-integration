using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Octokit;
using Octokit.GraphQL;
using Octokit.GraphQL.Model;

using Connection = Octokit.GraphQL.Connection;
using ProductHeaderValue = Octokit.ProductHeaderValue;

namespace LocalizationServiceIntegration;

public class GitClient
{
	public const string BranchPrefix = "LocalizationPull";
	private const string commitEmail = "action-ci@mindbox.ru";
	private const string pullRequestVariableName = "pullRequest";
	private readonly GitHubClient client;
	private readonly Connection githubConnection;
	private readonly string gitHubToken;
	private readonly string repositoryName;
	private readonly string repositoryOwner;
	private readonly ICompiledQuery<string> pullRequestEnablingMutation;

	public GitClient(string gitHubToken, string repositoryOwner, string repositoryName)
	{
		this.gitHubToken = gitHubToken;
		this.repositoryOwner = repositoryOwner;
		this.repositoryName = repositoryName;

		client = new GitHubClient(new ProductHeaderValue("LocalizationServiceIntegration"))
		{
			Credentials = new Credentials(gitHubToken)
		};

		githubConnection = new Connection(new Octokit.GraphQL.ProductHeaderValue("productHeaderValue"), gitHubToken);
		pullRequestEnablingMutation = new Mutation().EnablePullRequestAutoMerge(Variable.Var(pullRequestVariableName))
			.Select(x => x.ClientMutationId)
			.Compile();
	}

	public void CommitAllChangesToBranchAndPush(string branchName, string message)
	{
		ExecuteGitExeAndGetOutput("checkout", "-b", branchName);
		ExecuteGitExeAndGetOutput("add", "-A");
		ExecuteGitExeAndGetOutput("config", "--global", "user.email", commitEmail);
		ExecuteGitExeAndGetOutput("commit", "-am", message);
		ExecuteGitExeAndGetOutput("push", GetOAuthGitHubRepositoryLink(), branchName);
	}

	public async Task CreatePullRequestWithAutoMerge(string branchName, string baseBranch)
	{
		var pullRequest = await client.PullRequest.Create(
			repositoryOwner,
			repositoryName,
			new NewPullRequest($"Automatic pull request for branch {branchName}", branchName, baseBranch)
		);

		try
		{
			await client.Issue.Labels
				.AddToIssue(repositoryOwner, repositoryName, pullRequest.Number, new[] {"Merge when ready"});
		}
		catch (Exception e)
		{
			Console.WriteLine("Failed to add label to pull request");
			Console.WriteLine(e);
		}

		var variables = new Dictionary<string, object>
		{
			[pullRequestVariableName] = new EnablePullRequestAutoMergeInput
			{
				AuthorEmail = commitEmail, PullRequestId = new ID(pullRequest.NodeId)
			}
		};

		try
		{
			await githubConnection.Run(pullRequestEnablingMutation, variables);
		}
		catch (Exception e)
		{
			Console.WriteLine($"Failed to enable auto merge for pull request {pullRequest.Number}");
			Console.WriteLine(e);
		}
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
		var pullRequests = await client.PullRequest.GetAllForRepository(repositoryOwner, repositoryName);
		var pullRequestsCloseTasks = pullRequests
			.Where(pullRequest => pullRequest.State == ItemState.Open && pullRequest.Head.Ref.StartsWith(BranchPrefix))
			.Select(
				pullRequest => client.PullRequest.Update(
					repositoryOwner, repositoryName, pullRequest.Number, new PullRequestUpdate {State = ItemState.Closed}
				)
			);

		await Task.WhenAll(pullRequestsCloseTasks);

		var branches = await client.Repository.Branch.GetAll(repositoryOwner, repositoryName);
		var branchesRemovalTasks = branches
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