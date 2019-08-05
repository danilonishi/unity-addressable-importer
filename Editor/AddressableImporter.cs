using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using System;
using System.IO;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using System.Linq;

public class AddressableImporter : AssetPostprocessor
{
	public enum GroupSearchResult
	{
		Default = 0,
		Found = 1,
		NotFound = 2
	}

	static GroupSearchResult TryGetGroup(AddressableAssetSettings settings, string groupName, out AddressableAssetGroup group)
	{
		if (string.IsNullOrWhiteSpace(groupName))
		{
			group = settings.DefaultGroup;
			return GroupSearchResult.Default;
		}

		return ((group = settings.groups.Find(g => string.Equals(g.Name, groupName.Trim()))) == null) ? GroupSearchResult.NotFound : GroupSearchResult.Found;
	}

	static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
	{
		var settings = AddressableAssetSettingsDefaultObject.Settings;

		AddressableImportSettings scriptable = ScriptableObject.CreateInstance<AddressableImportSettings>();

		AddressableImportSettings importSettings;
		scriptable.GetOrCreate(out importSettings);

		if (importSettings.rules == null || importSettings.rules.Count == 0)
			return;

		// Handling imported assets
		var entriesAdded = new List<AddressableAssetEntry>();
		foreach (string path in importedAssets)
		{
			foreach (var rule in importSettings.rules)
			{
				if (rule.Match(path))
				{
					var entry = CreateOrUpdateAddressableAssetEntry(settings, path, rule, importSettings);
					if (entry != null)
					{
						entriesAdded.Add(entry);
					}
				}
			}
		}

		if (entriesAdded.Count > 0)
		{
			settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entriesAdded, true);
			Debug.Log($"AddressablesImporter: {entriesAdded.Count} addressable assets processed.");
			AssetDatabase.SaveAssets();
		}

		if (importSettings.removeEmtpyGroups)
		{
			settings.groups.RemoveAll(_ => _.entries.Count == 0);
		}
	}

	static AddressableAssetEntry CreateAddressableAsset(AddressableAssetSettings settings, string assetPath, AddressableAssetGroup group)
	{
		return settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(assetPath), group);
	}

	static void SimplifyAddresByPath(AddressableAssetEntry entry, string path)
	{
		entry.address = Path.GetFileNameWithoutExtension(path);
	}

	static AddressableAssetGroup CreateAssetGroup<SchemaType>(AddressableAssetSettings settings, string groupName)
	{
		return settings.CreateGroup(groupName, false, false, false, new List<AddressableAssetGroupSchema> { settings.DefaultGroup.Schemas[0] }, typeof(SchemaType));
	}

	static AddressableAssetEntry CreateOrUpdateAddressableAssetEntry(
		AddressableAssetSettings settings,
		string path,
		AddressableImportRule rule,
		AddressableImportSettings importSettings)
	{
		AddressableAssetGroup group;
		string groupName = importSettings.ParseGroupName(rule, path);

		if (TryGetGroup(settings, groupName, out group) == GroupSearchResult.NotFound)
		{
			if (importSettings.allowGroupCreation)
			{
				group = CreateAssetGroup<BundledAssetGroupSchema>(settings, groupName);
			}
			else
			{
				Debug.LogErrorFormat("[AddressableImporter] Failed to find group {0} when importing {1}. Please check the group exists, then reimport the asset.", groupName, path);
				return null;
			}
		}

		

		var assetEntry = CreateAddressableAsset(settings, path, group);

		if (rule.Simplify && (string.IsNullOrEmpty(assetEntry.address) || assetEntry.address.StartsWith("Assets/")))
		{
			SimplifyAddresByPath(assetEntry, path);
		}

		// Remove Labels
		assetEntry.labels.Clear();

		// Add labels
		foreach (var label in rule.Labels)
		{
			if (!assetEntry.labels.Contains(label))
				assetEntry.labels.Add(label);
		}

		return assetEntry;

	}

	/// <summary>
	/// Allows assets within the selected folder to be checked agains the Addressable Importer rules.
	/// </summary>
	public class FolderImporter
	{
		[MenuItem("Assets/AddressablesImporter: Check sub folders")]
		private static void DoSomethingWithVariable()
		{
			HashSet<string> filesToImport = new HashSet<string>();
			// Folders comes up as Object.
			foreach (UnityEngine.Object obj in Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.Assets))
			{
				var assetPath = AssetDatabase.GetAssetPath(obj);
				// Other assets may appear as Object, so a Directory Check filters directories from folders.
				if (Directory.Exists(assetPath))
				{
					var filesToAdd = Directory.GetFiles(assetPath, "*", SearchOption.AllDirectories);
					foreach (var file in filesToAdd)
					{
						// If Directory.GetFiles accepted Regular Expressions, we could filter the metas before iterating.
						if (!file.EndsWith(".meta"))
						{
							filesToImport.Add(file.Replace('\\', '/'));
						}
					}
				}
			}

			if (filesToImport.Count > 0)
			{
				Debug.Log($"AddressablesImporter: Found {filesToImport.Count} assets...");
				OnPostprocessAllAssets(filesToImport.ToArray(), null, null, null);
			}
			else
			{
				Debug.Log($"AddressablesImporter: No files to reimport");
			}
		}

		// Note that we pass the same path, and also pass "true" to the second argument.
		[MenuItem("Assets/AddressablesImporter: Check sub folders", true)]
		private static bool NewMenuOptionValidation()
		{
			foreach (UnityEngine.Object obj in Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.Assets))
			{
				if (Directory.Exists(AssetDatabase.GetAssetPath(obj)))
				{
					return true;
				}
			}
			return false;
		}
	}

}

