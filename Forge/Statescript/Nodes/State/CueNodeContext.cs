// Copyright © Gamesmiths Guild.

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

	// What a deferred removal cancels the matching apply by. A copy keeps it, so a restored context still matches
	// what the context it replaced applied.
	internal object Application { get; } = new();

	/// <inheritdoc/>
	protected override void OnCopied()
	{
		base.OnCopied();
		AppliedCues = [.. AppliedCues];
	}
}
