using System;
using System.Linq;

namespace LocalizationServiceIntegration
{
	public class Program
	{
		private static void Main(string[] args)
		{
			if (args.Length < 1)
			{
				var commandsString = string.Join(", ", CommandFactory.AllowedCommandNames);
				Console.WriteLine($"Usage: <command>. Allowed commands: {commandsString}");

				return;
			};

			var commandName = args[0];
			var arguments = args.Skip(1).ToArray();
			var config = Configuration.Load();

			var command = CommandFactory.Create(commandName, config, arguments);
			try
			{
				command.Execute();
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Exception of type {ex.GetType()} occured: {ex.Message}");
				throw;
			}
		}
	}
}
