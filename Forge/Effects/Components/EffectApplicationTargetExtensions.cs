// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Effects.Components;

/// <summary>
/// Resolution for <see cref="EffectApplicationTarget"/>, shared by every component that can aim what it does at an
/// entity other than the one carrying it.
/// </summary>
internal static class EffectApplicationTargetExtensions
{
	/// <summary>
	/// Resolves which entity an <see cref="EffectApplicationTarget"/> names for a given application.
	/// </summary>
	/// <remarks>
	/// Returns <see langword="null"/> when the effect has no such entity — thorns on an effect with no source. Callers
	/// skip that entry rather than falling back to the target, since silently redirecting it would apply something to
	/// the wrong entity.
	/// </remarks>
	/// <param name="applicationTarget">The entity to resolve.</param>
	/// <param name="target">The entity carrying the effect.</param>
	/// <param name="ownership">The effect's ownership.</param>
	/// <returns>The named entity, or <see langword="null"/> when the effect does not have one.</returns>
	internal static IForgeEntity? Resolve(
		this EffectApplicationTarget applicationTarget,
		IForgeEntity target,
		in EffectOwnership ownership)
	{
		return applicationTarget switch
		{
			EffectApplicationTarget.Source => ownership.Source,
			EffectApplicationTarget.Owner => ownership.Owner,
			_ => target,
		};
	}
}
