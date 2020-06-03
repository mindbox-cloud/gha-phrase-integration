using System;

namespace LocalizationServiceIntegration
{
	public abstract class Command
	{
		protected string[] Arguments { get; }

		protected Command(string[] arguments)
		{
			Arguments = arguments;
		}

		public void SetConfig(Configuration configuration)
		{
			if (configuration == null) 
				throw new ArgumentNullException(nameof(configuration));
			if (Config != null)
				throw new InvalidOperationException("Configuration is already set");

			Config = configuration;
		}

		protected Configuration Config { get; private set; }

		public void Execute()
		{
			if (Config == null)
				throw new InvalidOperationException("Config == null");

			ExecuteCore();
		}

		protected PhraseAppClient GetPhraseAppClient()
		{
			return new PhraseAppClient(Config.PhraseAppToken, Config.ProjectId);
		}

		protected GitClient GetGitHubClient()
		{
			return new GitClient(Config.GitHubToken, Config.Repository);
		}

		protected abstract void ExecuteCore();
	}
}