// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Statescript.Nodes;

/// <summary>
/// A cue applied to a target by a cue node, captured so the same cue/target pair can be removed later.
/// </summary>
/// <param name="Tag">The applied cue tag.</param>
/// <param name="Target">The entity the cue was applied to.</param>
public readonly record struct AppliedCue(Tag Tag, IForgeEntity Target);
