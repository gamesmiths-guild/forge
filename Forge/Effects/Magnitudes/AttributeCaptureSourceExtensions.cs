// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Effects.Magnitudes;

/// <summary>
/// Resolution for <see cref="AttributeCaptureSource"/>, shared by every path that reads a captured attribute.
/// </summary>
/// <remarks>
/// Capture resolution is used in four different places — evaluating a magnitude, evaluating a custom calculator,
/// validating a custom execution's captures, and registering non-snapshot attributes for live re-evaluation. They must
/// agree on which entity a capture names, or an effect ends up validating against one entity and reading from another,
/// or subscribing to an attribute whose changes it never consumes.
/// </remarks>
internal static class AttributeCaptureSourceExtensions
{
	/// <summary>
	/// Resolves which entity an <see cref="AttributeCaptureSource"/> names for a given application.
	/// </summary>
	/// <remarks>
	/// Returns <see langword="null"/> when the effect has no such entity — an effect with no source, or one applied
	/// without a target. Callers capture zero in that case rather than falling back to another entity, since silently
	/// reading a different entity's attributes would produce a plausible but wrong magnitude.
	/// </remarks>
	/// <param name="captureSource">The entity to resolve.</param>
	/// <param name="target">The entity receiving the effect.</param>
	/// <param name="ownership">The effect's ownership.</param>
	/// <returns>The named entity, or <see langword="null"/> when the effect does not have one.</returns>
	internal static IForgeEntity? Resolve(
		this AttributeCaptureSource captureSource,
		IForgeEntity? target,
		in EffectOwnership ownership)
	{
		return captureSource switch
		{
			AttributeCaptureSource.Source => ownership.Source,
			AttributeCaptureSource.Owner => ownership.Owner,
			_ => target,
		};
	}
}
