// Copyright © Gamesmiths Guild.

// Copyright © Gamesmiths Guild.
namespace Gamesmiths.Forge.Attributes;

/// <summary>
/// Initializes a new instance of the <see cref="AttributeInfo"/> struct.
/// </summary>
/// <param name="FullKey">The full key of the attribute, in the format "setname.attributename".</param>
/// <param name="Name">The simple name of the attribute.</param>
/// <param name="SetName">The name of the attribute set.</param>
/// <param name="Value">The attribute instance.</param>
public readonly record struct AttributeInfo(string FullKey, string Name, string SetName, EntityAttribute Value);
