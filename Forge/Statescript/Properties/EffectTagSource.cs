// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Selects which set of an effect's tags an <see cref="ActiveEffectTagQueryResolver"/> should evaluate.
/// </summary>
public enum EffectTagSource : byte
{
	/// <summary>
	/// Evaluates against the effect's own tags together with the tags it grants to its target.
	/// </summary>
	OwningTags = 0,

	/// <summary>
	/// Evaluates against the effect's own identity tags only.
	/// </summary>
	EffectTags = 1,

	/// <summary>
	/// Evaluates against the tags the effect grants to its target only.
	/// </summary>
	GrantedTags = 2,
}
