// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Effects;

namespace Gamesmiths.Forge.Statescript.Nodes.State;

/// <summary>
/// Runtime context for <see cref="EffectNode"/>.
/// </summary>
public class EffectNodeContext : StateNodeContext
{
	/// <summary>
	/// Gets the active effect handles created by this node while it is active.
	/// </summary>
	public List<ActiveEffectHandle> ActiveEffectHandles { get; } = [];
}
