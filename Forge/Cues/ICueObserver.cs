// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Cues;

// Sees every cue a CuesManager is told about, before any handler and whether or not the manager presents cues. The
// application is what a persistent cue is applied for, an active effect or a Cue node's activation, and tells the
// removal or update of one apart from another of the same cue on the same target.
internal interface ICueObserver
{
	void CueApplied(Tag cueTag, IForgeEntity? target, CueParameters? parameters, object? application);

	void CueUpdated(Tag cueTag, IForgeEntity? target, CueParameters? parameters, object? application);

	void CueRemoved(Tag cueTag, IForgeEntity? target, bool interrupted, object? application);

	void CueExecuted(Tag cueTag, IForgeEntity? target, CueParameters? parameters);
}
