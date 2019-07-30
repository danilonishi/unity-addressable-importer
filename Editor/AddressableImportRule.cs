using UnityEngine;
using UnityEditor;
using UnityEngine.AddressableAssets;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine.Serialization;

public enum AddressableImportRuleMatchType
{
	/// <summary>
	/// Simple wildcard
	/// *, matches any number of characters
	/// ?, matches a single character
	/// </summary>
	[Tooltip("Simple wildcard.\n\"*\" matches any number of characters.\n\"?\" matches a single character.")]
	Wildcard = 0,

	/// <summary>
	/// Regex pattern
	/// </summary>
	[Tooltip("A regular Expression pattern.")]
	Regex
}

[System.Serializable]
public class AddressableImportRule
{
	/// <summary>
	/// Assets within this path will be processed.
	/// </summary>
	[Tooltip("The asset within this path will be processed. Matching occurs with MatchType method chosen below.")]
	public string Path;


	/// <summary>
	/// Method used to parse the Path.
	/// </summary>
	[Tooltip("The Path parsing method.")]
	public AddressableImportRuleMatchType MatchType;

	/// <summary>
	/// The group the asset will be added.
	/// </summary>
	[Tooltip("The group name in which the Addressable will be added")]
	public string TargetGroupName = string.Empty;

	/// <summary>
	/// Simplify address.
	/// </summary>
	[Tooltip("If enabled, the asset Address will be its filename without extension, otherwise, the full path will be used.")]
	public bool Simplify;

	/// <summary>
	/// Label reference list.
	/// </summary>
	[Tooltip("The list of labels to be added to the Addressable")]
	[FormerlySerializedAs("labelRefs")]
	public List<AssetLabelReference> LabelRefs;

	public bool HasLabel
	{
		get
		{
			return LabelRefs != null && LabelRefs.Count > 0;
		}
	}

	const string pathregex = @"\%PATH\%\[\-{0,1}\d{1,3}\]"; // ie: $PATH$[0]

	public string[] GetPathArray(string path)
	{
		return path.Split('/');
	}

	public string GetPathAtArray(string path, int idx)
	{
		return GetPathArray(path)[idx];
	}

	public string ParsePath(string targetGroupName, string customPath = null)
	{
		var _path = Path;
		if (!string.IsNullOrWhiteSpace(customPath))
		{
			_path = customPath;
		}

		int i = 0;
		var slashSplit = _path.Split('/');
		var len = slashSplit.Length - 1;
		var matches = Regex.Matches(targetGroupName, pathregex);
		string[] parsedMatches = new string[matches.Count];
		foreach (var match in matches)
		{
			string v = match.ToString();
			var sidx = v.IndexOf('[') + 1;
			var eidx = v.IndexOf(']');
			int idx = int.Parse(v.Substring(sidx, eidx - sidx));
			while (idx > len)
			{
				idx -= len;
			}
			while (idx < 0)
			{
				idx += len;
			}
			//idx = Mathf.Clamp(idx, 0, slashSplit.Length - 1);
			parsedMatches[i++] = GetPathAtArray(_path, idx);
		}

		i = 0;
		var splitpath = Regex.Split(targetGroupName, pathregex);
		string finalPath = string.Empty;
		foreach (var split in splitpath)
		{
			finalPath += splitpath[i];
			if (i < parsedMatches.Length)
			{
				finalPath += parsedMatches[i];
			}
			i++;
		}
		return finalPath;
	}

	/// <summary>
	/// Returns True if given assetPath matched with the rule.
	/// </summary>
	public bool Match(string assetPath)
	{
		if (MatchType == AddressableImportRuleMatchType.Wildcard)
		{
			if (Path.Contains("*") || Path.Contains("?"))
			{
				var regex = "^" + Regex.Escape(Path).Replace(@"\*", ".*").Replace(@"\?", ".");
				return Regex.IsMatch(assetPath, regex);
			}
			else
				return assetPath.StartsWith(Path);
		}
		else if (MatchType == AddressableImportRuleMatchType.Regex)
			return Regex.IsMatch(assetPath, Path);
		return false;
	}

	public IEnumerable<string> Labels
	{
		get
		{
			if (LabelRefs == null)
				yield break;
			else
			{
				foreach (var labelRef in LabelRefs)
				{
					yield return labelRef.labelString;
				}
			}
		}
	}
}