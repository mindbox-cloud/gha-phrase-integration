using System;

namespace LocalizationServiceIntegration
{
	public class WipeCommand : Command
	{
		public WipeCommand(string[] arguments) : base(arguments)
		{
		}

		protected override void ExecuteCore()
		{
			Console.WriteLine("Are you sure you wanna wipe all the keys from phraseapp? Say \"y\" if you do.");
			var answer = Console.ReadLine();

			if (answer != "y")
				return;

			var client = GetPhraseAppClient();

			foreach (var configLocale in Config.Locales)
			{
				client.Wipe(configLocale.Id);
			}
		}
	}
}