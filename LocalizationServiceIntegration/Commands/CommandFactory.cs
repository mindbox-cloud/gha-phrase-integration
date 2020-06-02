using System;
using System.Collections.Generic;

namespace LocalizationServiceIntegration
{
	public class CommandFactory
	{
		private static readonly Dictionary<string, Func<string[], Command>> CommandMapping = new Dictionary<string, Func<string[], Command>>
		{
			["push"] = arguments => new PushCommand(arguments),
			["wipe"] = arguments => new WipeCommand(arguments),
			["pull"] = arguments => new PullCommand(arguments),
		};

		public static IEnumerable<string> AllowedCommandNames => CommandMapping.Keys;

		public static Command Create(string commandName, Configuration configuration, string[] arguments)
		{
			var command = CreateCommand(commandName, arguments);

			command.SetConfig(configuration);

			return command;
		}

		private static Command CreateCommand(string commandName, string[] arguments)
		{
			if (!CommandMapping.ContainsKey(commandName))
			{
				var allowedCommandsString = string.Join(", ", CommandMapping.Keys);

				throw new InvalidOperationException(
					$"Unknown command {commandName}. Allowed commands are: {allowedCommandsString}");
			}

			return CommandMapping[commandName](arguments);
		}
	}
}