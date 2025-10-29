// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Effects.Magnitudes;

/// <summary>
/// A set by caller float is a magnitude used for allowing the caller to set a specific value before applying an effect.
/// </summary>
/// <param name="Tag">The tag used to identify this custom magnitude.</param>
/// <param name="Snapshot">Whether this magnitude should be snapshotted when the effect is applied.</param>
/// <remarks>
/// A <see cref="Tags.Tag"/> is used for mapping different possible custom values.
/// </remarks>
public readonly record struct SetByCallerFloat(Tag Tag, bool Snapshot = true);
