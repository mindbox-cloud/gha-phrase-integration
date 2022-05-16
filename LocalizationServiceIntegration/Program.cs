using System;
using System.CommandLine;
using System.Threading.Tasks;

namespace LocalizationServiceIntegration;

public static class Program
{
	private static async Task Main(string[] args)
	{
		throw new InvalidCastException("Blah-blah");
		/*
		var config = IntegrationConfiguration.Load();

		var rootCommand = new RootCommand("Localization Service Integration");
		var commands = new ExecutableCommand[]
		{
			new PullCommand(config), new PushCommand(config), new WipeCommand(config)
		};

		foreach (var command in commands)
		{
			command.SetHandler(command.Execute);
			rootCommand.AddCommand(command);
		}

		await rootCommand.InvokeAsync(args);
		*/
	}
}