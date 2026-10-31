// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Cues;

// A persistent cue that an active effect or a running Cue node holds applied on a target, with the parameters it was
// last applied or updated with. A restore compares the ones held before it with the ones held after it, matched by
// application, tag and target.
internal readonly record struct PersistentCue(
	object Application,
	Tag CueTag,
	IForgeEntity Target,
	CueParameters? Parameters);
