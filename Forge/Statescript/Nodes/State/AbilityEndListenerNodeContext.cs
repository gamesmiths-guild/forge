// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript.Nodes.State;

/// <summary>
/// The context for an <see cref="AbilityEndListenerNode"/>. Tracks the subscribed entity, handler and optional
/// ability filter so the subscription can be removed on deactivation.
/// </summary>
public class AbilityEndListenerNodeContext : StateNodeContext
{
	/// <summary>
	/// Gets or sets the entity whose abilities are observed.
	/// </summary>
	public IForgeEntity? SubscribedEntity { get; set; }

	/// <summary>
	/// Gets or sets the handle used to filter ended abilities, or <see langword="null"/> when every ended ability is
	/// reported.
	/// </summary>
	public AbilityHandle? FilterHandle { get; set; }

	internal Action<AbilityEndedData>? Handler { get; set; }
}
