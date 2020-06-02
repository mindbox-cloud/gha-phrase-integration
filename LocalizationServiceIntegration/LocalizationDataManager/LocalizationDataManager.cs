﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace LocalizationServiceIntegration
{
	public class LocalizationDataManager
	{
		private readonly string _referenceLocaleName;
		private readonly IDictionary<string, string> namespaceNameToNamespaceDirectoryMap = 
			new Dictionary<string, string>();

		public LocalizationDataManager(string referenceLocaleName)
		{
			_referenceLocaleName = referenceLocaleName;

			namespaceNameToNamespaceDirectoryMap = new Dictionary<string, string>(
				GetNamespaceWithDirectory(_referenceLocaleName), StringComparer.InvariantCultureIgnoreCase);
		}

		public IEnumerable<LocalizationNamespace> GetNamespaces(string localeName)
		{
			return namespaceNameToNamespaceDirectoryMap.Select(nameAndDirectory =>
			{
				var namespaceName = nameAndDirectory.Key;
				var namespaceDirectory = nameAndDirectory.Value;
				var namespaceDataFileName = $"{namespaceName}.{localeName}.i18n.json";
				var namespaceDataFileFullPath = Path.Combine(namespaceDirectory, namespaceDataFileName);

				return new LocalizationNamespace(namespaceDataFileFullPath, namespaceName, localeName);
			});
		}

		private static readonly Regex FileNameRegex = new Regex(@"^(?<namespace>[^\.]+)\.(?<locale>[^\.]+)\.i18n$");

		private IEnumerable<KeyValuePair<string, string>> GetNamespaceWithDirectory(string localeName)
		{
			return Directory.EnumerateFiles("./", $"*.{localeName}.i18n.json", SearchOption.AllDirectories)
				.Where(filePath => !filePath.Contains("Tests"))
				.Where(filePath => !filePath.Contains(@"Administration.Web\Content"))
				.Where(filePath => !filePath.Contains(@"\bin\"))
				.Where(filePath => !filePath.Contains(@"TestResults\"))
				.Where(filePath => !filePath.Contains(@"IntegrationTestSources\"))
				.Select(filePath =>
				{
					var fileName = Path.GetFileNameWithoutExtension(filePath);
					var namespaceName = FileNameRegex.Match(fileName).Groups["namespace"].Value;

					var directory = Path.GetDirectoryName(filePath);

					return new KeyValuePair<string, string>(namespaceName, directory);
				});
		}

	}
}