// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Core;

/// <summary>
/// Extension methods for <see cref="EntityAttributes"/> class.
/// </summary>
public static class AttributesExtensions
{
	/// <summary>
	/// Returns an enumerable collection of <see cref="AttributeInfo"/> objects, each containing an attribute along with
	/// its key metadata.
	/// </summary>
	/// <remarks>
	/// This is useful when you need to access attribute names and associated data at runtime.
	/// </remarks>
	/// <param name="attributes">The attributes container to iterate over.</param>
	/// <returns>An enumerable of <see cref="AttributeInfo"/>, each holding the full key, attribute name, and set name.
	/// </returns>
	public static IEnumerable<AttributeInfo> WithKeys(this EntityAttributes attributes)
	{
		foreach (KeyValuePair<StringKey, Attribute> entry in attributes.AttributesMap)
		{
			// Assume full key is stored in entry.Key (e.g. "vitalattributeset.health")
			// Split the key into set name and attribute name based on a delimiter, here using '.'.
			var parts = entry.Key.ToString().Split('.');
			var setName = parts.Length > 1 ? parts[0] : string.Empty;
			var attributeName = parts.Length > 1 ? parts[1] : parts[0];

			yield return new AttributeInfo(entry.Key, attributeName, setName, entry.Value);
		}
	}
}
