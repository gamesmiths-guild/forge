// Copyright © Gamesmiths Guild.

using System.Diagnostics.CodeAnalysis;
using Gamesmiths.Forge.Attributes;
using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Effects.Magnitudes;

/// <summary>
/// Set of data that definies how an attribute is going to be captured.
/// </summary>
/// <param name="Attribute">Which attribute to capture.</param>
/// <param name="Source">From what target to capture the attribute from.</param>
/// <param name="Snapshot">Whether the captured value should be a snapshot or not.</param>
public readonly record struct AttributeCaptureDefinition(
	StringKey Attribute,
	AttributeCaptureSource Source,
	bool Snapshot = true)
{
	/// <summary>
	/// Tries to retrieve the captured attribute for a given entity.
	/// </summary>
	/// <param name="source">The source entity to get the attribute from.</param>
	/// <param name="attribute">The captured attribute, if any.</param>
	/// <returns>Whether the attribute was successfully retrieved or not.</returns>
	public readonly bool TryGetAttribute(IForgeEntity? source, [NotNullWhen(true)] out EntityAttribute? attribute)
	{
		attribute = null;

		if (source?.Attributes.ContainsAttribute(Attribute) != true)
		{
			return false;
		}

		attribute = source.Attributes[Attribute];

		return true;
	}
}
