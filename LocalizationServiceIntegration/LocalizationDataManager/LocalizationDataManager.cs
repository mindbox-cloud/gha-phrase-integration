﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace LocalizationServiceIntegration;

public class LocalizationDataManager
{
	private static readonly Regex FileNameRegex = new(@"^(?<namespace>[^\.]+)\.(?<locale>[^\.]+)\.i18n$", RegexOptions.Compiled);

	private readonly List<string> forbiddenPaths = new()
	{
		"Tests",
		Path.Join("Administration.Web", "Contents"),
		$"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
		$"TestResults{Path.DirectorySeparatorChar}",
		$"IntegrationTestSources{Path.DirectorySeparatorChar}",
		$"{Path.DirectorySeparatorChar}node_modules{Path.DirectorySeparatorChar}"
	};

	private readonly IDictionary<string, string> namespaceNameToNamespaceDirectoryMap;

	public LocalizationDataManager(string referenceLocaleName, string workingDirectory)
	{
		namespaceNameToNamespaceDirectoryMap = new Dictionary<string, string>(
			GetNamespaceWithDirectory(referenceLocaleName, workingDirectory), StringComparer.InvariantCultureIgnoreCase
		);
	}

	public IEnumerable<LocalizationNamespace> GetNamespaces(string localeName) =>
		namespaceNameToNamespaceDirectoryMap.Select(
			nameAndDirectory =>
			{
				var namespaceName = nameAndDirectory.Key;
				var namespaceDirectory = nameAndDirectory.Value;
				var namespaceDataFileName = $"{namespaceName}.{localeName}.i18n.json";
				var namespaceDataFileFullPath = Path.Combine(namespaceDirectory, namespaceDataFileName);

				return new LocalizationNamespace(namespaceDataFileFullPath, namespaceName, localeName);
			}
		);

	private IEnumerable<KeyValuePair<string, string>> GetNamespaceWithDirectory(string localeName, string workingDirectory) =>
		Directory.EnumerateFiles(workingDirectory, $"*.{localeName}.i18n.json", SearchOption.AllDirectories)
			.Where(filePath => forbiddenPaths.Select(filePath.Contains).All(result => !result))
			.Select(
				filePath =>
				{
					var fileName = Path.GetFileNameWithoutExtension(filePath);
					var namespaceName = FileNameRegex.Match(fileName).Groups["namespace"].Value;

					var directory = Path.GetDirectoryName(filePath);

					Console.WriteLine($"Located namespace {namespaceName} in directory {directory}");

					return new KeyValuePair<string, string>(namespaceName, directory);
				}
			);
}