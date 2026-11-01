// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Cues;

namespace Gamesmiths.Forge.Statescript.Nodes.State;

/// <summary>
/// Runtime context for <see cref="CueNode"/>.
/// </summary>
public class CueNodeContext : StateNodeContext
{
	/// <summary>
	/// Gets the cue tag/target pairs applied by this node while it is active, removed on deactivation.
	/// </summary>
	public List<AppliedCue> AppliedCues { get; private set; } = [];

	// What a deferred removal cancels the matching apply by, and a restore matches the cues of an activation by: new
	// for each activation, retriggers included. A copy keeps it, so a restored context still matches what it applied.
	internal object Application { get; set; } = new();

	// What the cues were applied with, for a restore that brings them back.
	internal CueParameters? Parameters { get; set; }

	/// <inheritdoc/>
	protected override void OnCopied()
	{
		base.OnCopied();
		AppliedCues = [.. AppliedCues];
	}
}
