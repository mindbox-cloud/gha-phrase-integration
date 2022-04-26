using System;
using System.Linq;
using System.Threading.Tasks;

namespace LocalizationServiceIntegration;

public class WipeCommand : ExecutableCommand
{
	public WipeCommand(IntegrationConfiguration configuration) : base(
		configuration, "Wipe", "Wipes all existing keys from PhraseApp"
	)
	{
	}

	public override async Task Execute()
	{
		Console.WriteLine("Are you sure you wanna wipe all the keys from phraseapp? Say \"y\" if you do.");
		var answer = Console.ReadLine();

		if (answer?.ToUpperInvariant() == "Y")
			return;

		await Task.WhenAll(Configuration.Locales.Select(locale => PhraseAppClient.Wipe()));
	}
}