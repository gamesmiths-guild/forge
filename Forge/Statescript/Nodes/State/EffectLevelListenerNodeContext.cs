// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Effects;

namespace Gamesmiths.Forge.Statescript.Nodes.State;

/// <summary>
/// The context for an <see cref="EffectLevelListenerNode"/>. Tracks the subscribed effect and handler so the
/// subscription can be removed on deactivation.
/// </summary>
public class EffectLevelListenerNodeContext : StateNodeContext
{
	/// <summary>
	/// Gets or sets the effect this node is currently subscribed to.
	/// </summary>
	public Effect? SubscribedEffect { get; set; }

	internal Action<int>? Handler { get; set; }
}
