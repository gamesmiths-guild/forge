// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Core;

/// <summary>
/// Initializes a new instance of the <see cref="AttributeInfo"/> struct.
/// </summary>
/// <param name="fullKey">The full key of the attribute, in the format "setname.attributename".</param>
/// <param name="name">The simple name of the attribute.</param>
/// <param name="setName">The name of the attribute set.</param>
/// <param name="value">The attribute instance.</param>
public readonly struct AttributeInfo(string fullKey, string name, string setName, Attribute value)
{
	/// <summary>
	/// Gets the full key of the attribute (e.g. "vitalattributes.health").
	/// </summary>
	public string FullKey { get; } = fullKey;

	/// <summary>
	/// Gets the simple attribute name (e.g. "health").
	/// </summary>
	public string Name { get; } = name;

	/// <summary>
	/// Gets the name of the attribute set (e.g. "vitalattributes").
	/// </summary>
	public string SetName { get; } = setName;

	/// <summary>
	/// Gets the attribute instance.
	/// </summary>
	public Attribute Value { get; } = value;
}
