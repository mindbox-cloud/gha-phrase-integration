using System;
using System.Diagnostics;
using Newtonsoft.Json;
using RestSharp;

namespace LocalizationServiceIntegration
{
	public class GitClient
	{
		private readonly string _gitHubToken;
		private readonly string _repositoryName;
		private readonly RestClient _client;

		private const string RepositoryBaseUrl = "https://api.github.com/repos/";
		private const string DefaultRepositoryName = "DirectCRM";

		public GitClient(string gitHubToken, string repositoryName = null)
		{
			_gitHubToken = gitHubToken;
			_repositoryName = repositoryName ?? DefaultRepositoryName;

			_client = new RestClient(RepositoryBaseUrl + _repositoryName);
		}

		private IRestResponse ConfigureGitHubRequestAndExecute(Action<RestRequest> requestConfigurator)
		{
			var request = new RestRequest();
			request.AddHeader("Authorization", $"token {_gitHubToken}");
			request.AddHeader("Accept", "application/json");
			requestConfigurator(request);

			var response = _client.Execute(request);

			if (!response.IsSuccessful)
				throw new InvalidOperationException(
					$"Github response is not OK! Response code: {response.StatusCode}, message: {response.ErrorMessage}");

			return response;
		}
		
		private string ExecuteGitExeAndGetOutput(params string[] parameters)
		{
			var parametersString = string.Join(" ", parameters);
			Console.WriteLine($"git {parametersString}");

			var p = new Process();

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

		public bool HasChanges()
		{
			var result = ExecuteGitExeAndGetOutput("status");

			if (result.Contains("nothing to commit, working tree clean"))
				return false;

			if (result.Contains("Changes not staged for commit") || result.Contains("Untracked files"))
				return true;

			throw new InvalidOperationException($"git status resulted with some not expected output");
		}

		public void CommitAllChangesToBranchAndPush(string branchName, string message)
		{
			ExecuteGitExeAndGetOutput("checkout", "-b", branchName);
			ExecuteGitExeAndGetOutput("add", "-A");
			ExecuteGitExeAndGetOutput("commit", "-am", message);
			ExecuteGitExeAndGetOutput("push", GetOAuthGitHubRepositoryLink(), branchName);
		}

		private string GetOAuthGitHubRepositoryLink()
		{
			return $"https://{_gitHubToken}:x-oauth-basic@github.com/{_repositoryName}.git";
		}

		public CreatePullRequestResponse CreatePullRequest(string branchName)
		{
			var result = ConfigureGitHubRequestAndExecute(request =>
			{
				request.Resource = $"pulls";
				request.Method = Method.POST;

				var requestBody = new CreatePullRequestRequest
				{
					Title = $"Automatic pull request for branch {branchName}",
					Base = "master",
					Head = branchName
				};

				request.Parameters.Add(new Parameter() { 
					ContentType = "application/json", 
					Type = ParameterType.RequestBody, 
					Value = JsonConvert.SerializeObject(requestBody)
				});
			});

			var response = JsonConvert.DeserializeObject<CreatePullRequestResponse>(result.Content);

			return response;
		}

		public PullRequestStatus GetPullRequestStatus(CreatePullRequestResponse createPullRequestResponse)
		{
			var result = ConfigureGitHubRequestAndExecute(request =>
			{
				request.Resource = $"pulls/{createPullRequestResponse.Number}";
				request.Method = Method.GET;
			});

			var response = JsonConvert.DeserializeObject<GetPullRequestResponse>(result.Content);

			Console.WriteLine(
				$"PullRequest " +
				$"mergable = {response.Mergeable}, " +
				$"state = {response.State}, " +
				$"mergableState = {response.MergeableState}");

			if (response.Mergeable == false)
				return PullRequestStatus.Failed;

			if (response.State != PullRequestState.Open)
				return PullRequestStatus.Failed;

			if (response.MergeableState == MergeableState.Dirty)
				return PullRequestStatus.Failed;

			if (response.MergeableState == MergeableState.Clean || response.MergeableState == MergeableState.Unstable)
				return PullRequestStatus.CanBeMerged;

			return PullRequestStatus.InProcess;
		}

		public void Merge(string pullRequestNumber, string sha)
		{
			ConfigureGitHubRequestAndExecute(request =>
			{
				request.Resource = $"pulls/{pullRequestNumber}/merge";
				request.Method = Method.PUT;

				var requestBody = new MergeRequest
				{
					CommitMessage = $"Automatic merge PR {pullRequestNumber}",
					Sha = sha
				};

				request.Parameters.Add(new Parameter() { 
					ContentType = "application/json", 
					Type = ParameterType.RequestBody, 
					Value = JsonConvert.SerializeObject(requestBody)
				});
			});
		}

		public void ClosePullRequest(string pullRequestNumber)
		{
			ConfigureGitHubRequestAndExecute(request =>
			{
				request.Resource = $"pulls/{pullRequestNumber}";
				request.Method = Method.PATCH;

				var requestBody = new EditPullRequestRequest
				{
					State = PullRequestState.Closed
				};

				request.Parameters.Add(new Parameter() { 
					ContentType = "application/json", 
					Type = ParameterType.RequestBody, 
					Value = JsonConvert.SerializeObject(requestBody)
				});
			});
		}

		public string GetPullRequestLink(string pullRequestNumber) => 
			$"https://github.com/mindbox-moscow/{_repositoryName}/pull/{pullRequestNumber}";

		public void DeleteBranch(string branchName)
		{
			ExecuteGitExeAndGetOutput("push", GetOAuthGitHubRepositoryLink(), "--delete", branchName);
		}
	}
}