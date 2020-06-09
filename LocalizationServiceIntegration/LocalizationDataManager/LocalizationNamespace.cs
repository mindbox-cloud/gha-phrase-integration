using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Newtonsoft.Json;

namespace LocalizationServiceIntegration
{
	[DebuggerDisplay("{Name},{DataFilePath}")]
	public class LocalizationNamespace
	{
		private const string LocalizationDirectoryRelativePath = @"Resources\Localization";

		public LocalizationNamespace(string dataFilePath, string ns, string localeName)
		{
			DataFilePath = dataFilePath;
			Name = ns;
			LocaleName = localeName;
		}

		public string DataFilePath { get; }
		public string Name { get; }
		public string LocaleName { get; }

		public bool DoesHaveData()
		{
			return File.Exists(DataFilePath);
		}

		public void ApplyNewTranslations(IReadOnlyDictionary<string, string> sourceLocalizationData)
		{
			Console.WriteLine($"Applying translations for {Name}: {DataFilePath}");

			var currentNamespaceSourceData = new Dictionary<string, string>(
				sourceLocalizationData.Where(d => IsLocalizationKeyFromCurrentNamespace(d.Key)));

			if (!currentNamespaceSourceData.Any())
			{
				Console.WriteLine($"No keys found for namespace {Name}");
				return;
			}

			var currentNamespaceLocalData = GetCurrentNamespaceLocalData();

			foreach (var (key, value) in currentNamespaceSourceData)
			{
				var newValue = value.ProcessTemplateSyntax();

				if (currentNamespaceLocalData.TryGetValue(key, out var oldValue))
				{
					if (newValue != oldValue)
					{
						Console.WriteLine($"Replaced value for key {key}");
						Console.WriteLine($"Previous: {oldValue}");
						Console.WriteLine($"New: {newValue}");
					}
				}
				else
				{
					Console.WriteLine($"Key {key} did not exist locally, added");
					Console.WriteLine($"Value: {newValue}");
				}


				currentNamespaceLocalData[key] = newValue;
			}

			TryAddFileToProjectFile();

			File.WriteAllText(
				DataFilePath, 
				SerializeObject(currentNamespaceLocalData), 
				Encoding.UTF8);
		}

		public static string SerializeObject<T>(T value)
		{
			StringBuilder sb = new StringBuilder(256);
			StringWriter sw = new StringWriter(sb, CultureInfo.InvariantCulture);

			var jsonSerializer = JsonSerializer.CreateDefault();
			using (JsonTextWriter jsonWriter = new JsonTextWriter(sw))
			{
				jsonWriter.Formatting = Formatting.Indented;
				jsonWriter.IndentChar = '\t';
				jsonWriter.Indentation = 1;

				jsonSerializer.Serialize(jsonWriter, value, typeof(T));
			}

			return sw.ToString();
		}

		private void TryAddFileToProjectFile()
		{
			if (DoesHaveData())
				return;

			if (IsFrontendNamespace())
				return;

			var projectFilePath = GetProjectFilePath();

			var document = XDocument.Load(projectFilePath);

			var xmlns = document.Root.Name.Namespace;

			document.Root.Add(
				new XElement(xmlns + "ItemGroup",
					new XElement(xmlns + "Content", 
						new XAttribute(
							"Include", 
							$@"{LocalizationDirectoryRelativePath}\{Name}.{LocaleName}.i18n.json"),
						new XElement(
							xmlns + "CopyToOutputDirectory", 
							"PreserveNewest"))));

			document.Save(projectFilePath);
		}

		private string GetProjectFilePath()
		{
			var localizationDirectoryPath = Path.GetDirectoryName(DataFilePath);

			if (!localizationDirectoryPath.EndsWith(LocalizationDirectoryRelativePath))
				throw new InvalidOperationException(
					$"Localization directory path is not supported: {localizationDirectoryPath}");

			var projectFilePathDirectoryPath =
				localizationDirectoryPath.Replace(LocalizationDirectoryRelativePath, String.Empty);
			var projectFiles = Directory.EnumerateFiles(projectFilePathDirectoryPath, "*.csproj").ToArray();

			if (projectFiles.Length != 1)
				throw new InvalidOperationException($"Cant find project file in a directory {projectFilePathDirectoryPath}");

			var projectFilePath = projectFiles.Single();
			return projectFilePath;
		}

		private bool IsFrontendNamespace()
		{
			return DataFilePath.Contains("Frontend");
		}

		private Dictionary<string, string> GetCurrentNamespaceLocalData()
		{
			if (!DoesHaveData())
				return new Dictionary<string, string>();

			return JsonConvert.DeserializeObject<Dictionary<string, string>>(
				File.ReadAllText(DataFilePath));
		}

		private bool IsLocalizationKeyFromCurrentNamespace(string localizationKey)
		{
			var keyParts = TryGetKeyParts(localizationKey);
			if (keyParts == null)
				throw new InvalidOperationException($"Key {localizationKey} is invalid. Valid key is <namespace>:<key>.");

			return string.Equals(keyParts.Namespace, Name, StringComparison.InvariantCultureIgnoreCase);
		}

		private static LocalizationKey TryGetKeyParts(string key)
		{
			var splitKey = key.Split(':');
			if (splitKey.Length != 2)
				return null;

			return new LocalizationKey
			{
				Namespace = splitKey[0],
				Key = splitKey[1]
			};
		}

		public void CheckData()
		{
			var localData = GetCurrentNamespaceLocalData();

			CheckKeys(localData);
		}

		private void CheckKeys(Dictionary<string, string> localData)
		{
			foreach (var localDataKey in localData.Keys)
			{
				if (TryGetKeyParts(localDataKey) == null)
					throw new InvalidOperationException($"Key {localDataKey} is invalid. Valid key is <namespace>:<key>.");
			}
		}

		public IEnumerable<string> GetAddedKeys(IReadOnlyDictionary<string, string> referenceLocaleData)
		{
			var currentNamespaceLocalData = GetCurrentNamespaceLocalData();

			var currentNamespaceSourceData = new Dictionary<string, string>(
				referenceLocaleData.Where(d => IsLocalizationKeyFromCurrentNamespace(d.Key)));

			return currentNamespaceLocalData.Keys.Except(currentNamespaceSourceData.Keys);
		}

		public IEnumerable<string> RemovedKeys(IReadOnlyDictionary<string, string> referenceLocaleData)
		{
			var currentNamespaceSourceData = new Dictionary<string, string>(
				referenceLocaleData.Where(d => IsLocalizationKeyFromCurrentNamespace(d.Key)));

			var currentNamespaceLocalData = GetCurrentNamespaceLocalData();

			return currentNamespaceSourceData.Keys.Except(currentNamespaceLocalData.Keys);
		}
	}
}