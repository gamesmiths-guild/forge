// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Selects which tag container a <see cref="TagQueryResolver"/> should evaluate.
/// </summary>
public enum TagQuerySource : byte
{
	/// <summary>
	/// Evaluates against the entity's combined tag view, including base and modifier tags.
	/// </summary>
	CombinedTags = 0,

	/// <summary>
	/// Evaluates against the entity's immutable base tags only.
	/// </summary>
	BaseTags = 1,

	/// <summary>
	/// Evaluates against the entity's temporary modifier tags only.
	/// </summary>
	ModifierTags = 2,
}
