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
	public List<AppliedCue> AppliedCues { get; } = [];
}
