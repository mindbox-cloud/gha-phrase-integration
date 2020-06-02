using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mindbox.Integrations.Slack;

namespace LocalizationServiceIntegration
{
	public class PushCommand : Command
	{
		private const int MaxAddedKeysCountToNotify = 10;
		private const int MaxRemovedKeysCountToNotify = 10;

		public PushCommand(string[] arguments) : base(arguments)
		{
		}

		protected override void ExecuteCore()
		{
			var referenceLocale = Config.GetReferenceLocale();
			var localizationDataManager = new LocalizationDataManager(referenceLocale.Name, Config.WorkingDirectory);

			var client = GetPhraseAppClient();

			Console.WriteLine("Performing pull from phraseapp for diff check");
			var referenceLocaleData =  client.Pull(referenceLocale.Id);

			var diffResult = CheckKeysDiff(localizationDataManager, referenceLocale, referenceLocaleData);

			RemoveKeys(client, diffResult.RemovedKeys);
			PushKeys(localizationDataManager);
			NotifyAboutNewKeys(client, diffResult.AddedKeys);
		}

		private static (IEnumerable<string> AddedKeys, IEnumerable<string> RemovedKeys) CheckKeysDiff(
			LocalizationDataManager localizationDataManager, 
			LocaleInfo locale,
			IReadOnlyDictionary<string, string> localeData)
		{
			var addedKeys = new List<string>();
			var removedKeys = new List<string>();

			Console.WriteLine("Performing diff check");
			foreach (var localizationNamespace in localizationDataManager.GetNamespaces(locale.Name))
			{
				var addedKeysToCurrentNamespace = localizationNamespace.GetAddedKeys(localeData);
				var removedKeysFromCurrentNamespace = localizationNamespace.RemovedKeys(localeData);

				addedKeys.AddRange(addedKeysToCurrentNamespace);
				removedKeys.AddRange(removedKeysFromCurrentNamespace);
			}

			return (addedKeys, removedKeys);
		}

		private void PushKeys(LocalizationDataManager localizationDataManager)
		{
			foreach (var configLocale in Config.Locales)
			{
				PushForLocale(configLocale, localizationDataManager);
			}
		}

		private void RemoveKeys(PhraseAppClient client, IEnumerable<string> removedKeys)
		{
			Console.WriteLine("Removig keys");

			foreach (var removedKey in removedKeys)
			{
				Console.WriteLine($"Removig {removedKey}");
				client.RemoveKey(removedKey);
			}

			NotifyAboutRemovedKeys(client, removedKeys);
		}

		private void NotifyAboutRemovedKeys(PhraseAppClient client, IEnumerable<string> removedKeys)
		{
			Console.WriteLine("Sending notification about removed keys");

			var removedKeysCollection = removedKeys.ToList();
			if (!removedKeysCollection.Any()) 
				return;

			var notificationMessageBuilder = new StringBuilder();

			notificationMessageBuilder.AppendLine("*Из системы перевода удалены следующие ключи:*");

			notificationMessageBuilder.AppendLine(
				string.Join(
					"\n", 
					removedKeysCollection
						.Select(key =>
						{
							var keyLink = client.GetKeyLink(key);
								
							return $"<{keyLink}|{key}>";
						})
						.Take(MaxRemovedKeysCountToNotify)));

			if (removedKeysCollection.Count > MaxRemovedKeysCountToNotify)
			{
				notificationMessageBuilder.AppendLine(
					$"И ещё {removedKeysCollection.Count - MaxRemovedKeysCountToNotify} ключей");
			}

			var slackClient = new SlackClient(Config.SlackWebhookUrl);

			new SlackMessageBuilder(slackClient)
				.AsUser("некачественныйперевод.рф")
				.WithIcon(":flag-gb:")
				.ToChannel("#new-translations")
				.WithText(notificationMessageBuilder.ToString())
				.Send();
		}

		private void NotifyAboutNewKeys(PhraseAppClient client, IEnumerable<string> addedKeys)
		{
			Console.WriteLine("Sending notification about new keys");

			var addedKeysCollection = addedKeys.ToList();
			if (!addedKeysCollection.Any()) 
				return;

			var notificationMessageBuilder = new StringBuilder();

			notificationMessageBuilder.AppendLine("*В системе перевода появились новые ключи:*");

			notificationMessageBuilder.AppendLine(
				string.Join(
					"\n", 
					addedKeysCollection
						.Select(key =>
						{
							var keyLink = client.GetKeyLink(key);
								
							return $"<{keyLink}|{key}>";
						})
						.Take(MaxAddedKeysCountToNotify)));

			if (addedKeysCollection.Count > MaxAddedKeysCountToNotify)
			{
				notificationMessageBuilder.AppendLine($"И ещё {addedKeysCollection.Count - MaxAddedKeysCountToNotify} ключей");
			}

			var slackClient = new SlackClient(Config.SlackWebhookUrl);

			new SlackMessageBuilder(slackClient)
				.AsUser("качественныйперевод.рф")
				.WithIcon(":flag-gb:")
				.ToChannel("#new-translations")
				.WithText(notificationMessageBuilder.ToString())
				.Send();
		}

		private void PushForLocale(LocaleInfo locale, LocalizationDataManager localizationDataManager)
		{
			var client = GetPhraseAppClient();

			var localizationNamespacesToPush = localizationDataManager.GetNamespaces(locale.Name).Where(ns => ns.DoesHaveData());

			var localeData = client.Pull(locale.Id);

			foreach (var namespaceToPush in localizationNamespacesToPush)
			{
				var addedKeys = namespaceToPush.GetAddedKeys(localeData);

				if (!addedKeys.Any())
				{
					Console.WriteLine($"Nothing to push in namespace {namespaceToPush.Name}: {namespaceToPush.DataFilePath}");
					continue;
				}

				Console.WriteLine($"Pushing {namespaceToPush.Name}: {namespaceToPush.DataFilePath}");

				namespaceToPush.CheckData();

				client.Push(
					locale.Id,
					namespaceToPush.Name,
					namespaceToPush.DataFilePath);
			}
		}
	}
}