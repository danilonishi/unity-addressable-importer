using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using System;
using System.IO;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;

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
						if (rule.HasLabel)
							Debug.LogFormat("[AddressableImporter] Entry created for {0} with labels {1}", path, string.Join(", ", entry.labels));
						else
							Debug.LogFormat("[AddressableImporter] Entry created for {0}", path);
					}
				}
			}
		}
		if (entriesAdded.Count > 0)
		{
			settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entriesAdded, true);
			AssetDatabase.SaveAssets();
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

		Debug.Log("Target Group Name: " + groupName);

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

		if (string.IsNullOrEmpty(assetEntry.address) && rule.simplified)
		{
			SimplifyAddresByPath(assetEntry, path);
		}

		// Remove Labels
		assetEntry.labels.Clear();

		// Add labels
		foreach (var label in rule.labels)
		{
			if (!assetEntry.labels.Contains(label))
				assetEntry.labels.Add(label);
		}

		return assetEntry;
	}

}

