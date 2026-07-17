// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Nodes.State;

/// <summary>
/// The context for a <see cref="ConditionMonitorNode"/>. Tracks the last evaluated condition value so the node can
/// detect transitions between true and false.
/// </summary>
public class ConditionMonitorNodeContext : StateNodeContext
{
	/// <summary>
	/// Gets or sets the last evaluated condition value, or <see langword="null"/> when the condition has not been
	/// evaluated since activation.
	/// </summary>
	public bool? LastConditionValue { get; set; }
}
