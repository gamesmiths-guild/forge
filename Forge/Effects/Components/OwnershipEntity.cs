// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Effects.Components;

/// <summary>
/// Which of an effect's ownership entities a source-side requirements component evaluates against.
/// </summary>
/// <remarks>
/// Used by <see cref="SourceTagRequirementsEffectComponent"/> and
/// <see cref="SourceAttributeRequirementsEffectComponent"/>.
/// </remarks>
public enum OwnershipEntity : byte
{
	/// <summary>
	/// Use <see cref="EffectOwnership.Source"/> — what actually caused the effect.
	/// </summary>
	Source = 0,

	/// <summary>
	/// Use <see cref="EffectOwnership.Owner"/> — who triggered the action that caused the effect.
	/// </summary>
	Owner = 1,
}
