using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace LocalizationServiceIntegration
{
	public class LocalizationDataManager
	{
		private readonly IDictionary<string, string> namespaceNameToNamespaceDirectoryMap;

		public LocalizationDataManager(string referenceLocaleName, string workingDirectory)
		{
			namespaceNameToNamespaceDirectoryMap = new Dictionary<string, string>(
				GetNamespaceWithDirectory(referenceLocaleName, workingDirectory), StringComparer.InvariantCultureIgnoreCase);
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

		private IEnumerable<KeyValuePair<string, string>> GetNamespaceWithDirectory(string localeName, string workingDirectory)
		{
			return Directory.EnumerateFiles(workingDirectory, $"*.{localeName}.i18n.json", SearchOption.AllDirectories)
				.Select(filePath => filePath.Replace("\\", "/"))
				.Where(filePath => !filePath.Contains("Tests"))
				.Where(filePath => !filePath.Contains(@"Administration.Web/Content"))
				.Where(filePath => !filePath.Contains(@"/bin/"))
				.Where(filePath => !filePath.Contains(@"TestResults/"))
				.Where(filePath => !filePath.Contains(@"IntegrationTestSources/"))
				.Select(filePath =>
				{
					var fileName = Path.GetFileNameWithoutExtension(filePath);
					var namespaceName = FileNameRegex.Match(fileName).Groups["namespace"].Value;

					var directory = Path.GetDirectoryName(filePath);

					Console.WriteLine($"Located namespace {namespaceName} in directory {directory}");
					return new KeyValuePair<string, string>(namespaceName, directory);
				});
		}

	}
}