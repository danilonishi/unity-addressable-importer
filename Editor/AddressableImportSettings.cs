using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "AddressableImportSettings", menuName = "Addressable Assets/Import Settings", order = 50)]
public class AddressableImportSettings : ScriptableObject
{
	public enum AddressableResult
	{
		Loaded,
		Created
	}

	[Tooltip("Creates a group if the specified group doesn't exist.")]
	public bool allowGroupCreation = true;

	[Tooltip("Rules for managing imported assets.")]
	public List<AddressableImportRule> rules;

	/// <summary>
	/// Gets a group name based on Rule's target group name and provided path
	/// </summary>
	public string ParseGroupName(AddressableImportRule rule, string path)
	{
		return rule.ParsePath(rule.TargetGroupName, path);
	}

	/// <summary>
	/// Attempts to load the current import settings from EditorBuildSettings. If not found, attempts to load from a fixed file path.
	/// </summary>
	public bool TryGet(out AddressableImportSettings settings)
	{
		AddressableImportSettings settingsFile;

		Debug.Log("Trying to get from EditorBuildSettings");
		if (EditorBuildSettings.TryGetConfigObject("AddressableImportSettings", out settingsFile))
		{
			Debug.Log("...found");
			settings = settingsFile;
			return true;
		}

		Debug.Log("Trying to get from AddressableImportSettings.asset");
		if (settingsFile != null)
		{
			settings = AssetDatabase.LoadAssetAtPath<AddressableImportSettings>("Assets/AddressableAssetsData/AddressableImportSettings.asset");
			Debug.Log("..." + ((settings != null) ? "found" : "not found"));
			return (settings != null);
		}

		Debug.Log("Giving up.");
		settings = default;
		return false;
	}

	/// <summary>
	/// Creates a new AddressableImportSettings and add it to the EditorBuildSettings.
	/// </summary>
	public void Create(AddressableImportSettings settings)
	{
		Debug.Log("Creating Addressable Import Settings");
		settings = CreateInstance<AddressableImportSettings>();//default;
		AssetDatabase.CreateAsset(settings, "Assets/AddressableAssetsData/AddressableImportSettings.asset");
		AssetDatabase.SaveAssets();

		Debug.Log("Saving to EditorBuildSettings");

		EditorBuildSettings.AddConfigObject("AddressableImportSettings", settings, true);
		settings.rules = new List<AddressableImportRule>();
	}

	public AddressableResult GetOrCreate(out AddressableImportSettings settingsFile)
	{
		if (!TryGet(out settingsFile))
		{
			Create(settingsFile);
			return AddressableResult.Created;
		}
		return AddressableResult.Loaded;
	}
}