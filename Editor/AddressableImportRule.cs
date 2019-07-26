using UnityEngine;
using UnityEditor;
using UnityEngine.AddressableAssets;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public enum AddressableImportRuleMatchType
{
	/// <summary>
	/// Simple wildcard
	/// *, matches any number of characters
	/// ?, matches a single character
	/// </summary>
	Wildcard = 0,

	/// <summary>
	/// Regex pattern
	/// </summary>
	Regex
}

[System.Serializable]
public class AddressableImportRule
{
	/// <summary>
	/// Path pattern
	/// </summary>
	public string path;

	/// <summary>
	/// Path match type
	/// </summary>
	public AddressableImportRuleMatchType matchType;

	/// <summary>
	/// Label reference list
	/// </summary>
	public List<AssetLabelReference> labelRefs;

	/// <summary>
	/// Simplify address
	/// </summary>
	[Tooltip("Simplify address to filename without extension")]
	public bool simplified;

	public bool HasLabel
	{
		get
		{
			return labelRefs != null && labelRefs.Count > 0;
		}
	}

	public const string cpath = "$PATH$";

	public string[] GetPathArray(string path)
	{
		return path.Split('/');
	}

	public string GetPathAtArray(string path, int idx)
	{
		return GetPathArray(path)[idx];
	}

	public string TargetGroupName = "";

	const string pathregex = @"\$PATH\$\[\d{1,3}\]";
	public string ParsePath(string targetGroupName, string customPath = null)
	{
		var _path = path;
		if (!string.IsNullOrWhiteSpace(customPath))
		{
			_path = customPath;
		}

		var slashSplit = _path.Split('/');
		var splitpath = Regex.Split(targetGroupName, pathregex);
		var matches = Regex.Matches(targetGroupName, pathregex);

		string[] parsedMatches = new string[matches.Count];
		int i = 0;
		foreach (var match in matches)
		{
			string v = match.ToString();
			var sidx = v.IndexOf('[') + 1;
			var eidx = v.IndexOf(']');
			int idx = int.Parse(v.Substring(sidx, eidx - sidx));
			idx = Mathf.Clamp(idx, 0, slashSplit.Length - 1);
			parsedMatches[i++] = GetPathAtArray(_path, idx);
		}

		i = 0;
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
		if (matchType == AddressableImportRuleMatchType.Wildcard)
		{
			if (path.Contains("*") || path.Contains("?"))
			{
				var regex = "^" + Regex.Escape(path).Replace(@"\*", ".*").Replace(@"\?", ".");
				return Regex.IsMatch(assetPath, regex);
			}
			else
				return assetPath.StartsWith(path);
		}
		else if (matchType == AddressableImportRuleMatchType.Regex)
			return Regex.IsMatch(assetPath, path);
		return false;
	}

	public IEnumerable<string> labels
	{
		get
		{
			if (labelRefs == null)
				yield break;
			else
			{
				foreach (var labelRef in labelRefs)
				{
					yield return labelRef.labelString;
				}
			}
		}
	}
}