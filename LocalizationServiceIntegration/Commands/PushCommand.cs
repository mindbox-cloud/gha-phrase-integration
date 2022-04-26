using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mindbox.Integrations.Slack;

namespace LocalizationServiceIntegration;

public class PushCommand : ExecutableCommand
{
	public PushCommand(IntegrationConfiguration configuration) : base(
		configuration, "Push",
		"Pushes localization from GitHub repository to PhraseApp"
	)
	{ }

	private const int maxAddedKeysCountToNotify = 10;
	private const int maxRemovedKeysCountToNotify = 10;

	public override async Task Execute()
	{
		var referenceLocale = Configuration.GetReferenceLocale();
		var localizationDataManager = new LocalizationDataManager(referenceLocale.Name, Configuration.WorkingDirectory);

		Console.WriteLine("Performing pull from phraseapp for diff check");
		var referenceLocaleData = await PhraseAppClient.Pull(referenceLocale.Id);

		var diffResult = CheckKeysDiff(localizationDataManager, referenceLocale, referenceLocaleData);

		await RemoveKeys(diffResult.RemovedKeys);
		NotifyAboutRemovedKeys(diffResult.RemovedKeys);
		await PushKeys(localizationDataManager);
		NotifyAboutNewKeys(diffResult.AddedKeys);
	}

	private static (IList<string> AddedKeys, IList<string> RemovedKeys) CheckKeysDiff(
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

	private async Task PushKeys(LocalizationDataManager localizationDataManager)
	{
		await Task.WhenAll(Configuration.Locales.Select(locale => PushForLocale(locale, localizationDataManager)));
	}

	private async Task RemoveKeys(IList<string> removedKeys)
	{
		Console.WriteLine("Removing keys: " + string.Join(", ", removedKeys));

		await Task.WhenAll(removedKeys.Select(PhraseAppClient.RemoveKey));
	}

	private void NotifyAboutRemovedKeys(IEnumerable<string> removedKeys)
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
						var keyLink = PhraseAppClient.GetKeyLink(key);

						return $"<{keyLink}|{key}>";
					})
					.Take(maxRemovedKeysCountToNotify)));

		if (removedKeysCollection.Count > maxRemovedKeysCountToNotify)
		{
			notificationMessageBuilder.AppendLine(
				$"И ещё {removedKeysCollection.Count - maxRemovedKeysCountToNotify} ключей");
		}

		var slackClient = new SlackClient(Configuration.SlackWebhookUrl);

		new SlackMessageBuilder(slackClient)
			.AsUser("некачественныйперевод.рф")
			.WithIcon(":flag-gb:")
			.ToChannel("#new-translations")
			.WithText(notificationMessageBuilder.ToString())
			.Send();
	}

	private void NotifyAboutNewKeys(IEnumerable<string> addedKeys)
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
						var keyLink = PhraseAppClient.GetKeyLink(key);

						return $"<{keyLink}|{key}>";
					})
					.Take(maxAddedKeysCountToNotify)));

		if (addedKeysCollection.Count > maxAddedKeysCountToNotify)
		{
			notificationMessageBuilder.AppendLine($"И ещё {addedKeysCollection.Count - maxAddedKeysCountToNotify} ключей");
		}

		var slackClient = new SlackClient(Configuration.SlackWebhookUrl);

		new SlackMessageBuilder(slackClient)
			.AsUser("качественныйперевод.рф")
			.WithIcon(":flag-gb:")
			.ToChannel("#new-translations")
			.WithText(notificationMessageBuilder.ToString())
			.Send();
	}

	private async Task PushForLocale(LocaleInfo locale, LocalizationDataManager localizationDataManager)
	{
		var localizationNamespacesToPush = localizationDataManager.GetNamespaces(locale.Name).Where(ns => ns.DoesHaveData());

		var localeData = await PhraseAppClient.Pull(locale.Id);

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

			await PhraseAppClient.Push(
				locale.Id,
				namespaceToPush.Name,
				namespaceToPush.DataFilePath);
		}
	}
}