// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Events;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Statescript.Nodes.State;

/// <summary>
/// Runtime context for <see cref="EventListenerNode"/>.
/// </summary>
public class EventListenerNodeContext : StateNodeContext
{
	/// <summary>
	/// Gets the subscription tokens to release on deactivation.
	/// </summary>
	public List<EventSubscriptionToken> Tokens { get; } = [];

	/// <summary>
	/// Gets the event bus this node is subscribed to while active.
	/// </summary>
	public EventManager? SubscribedManager { get; internal set; }

	/// <summary>
	/// Gets the payload writer used to decompose received payloads into output variables, or <see langword="null"/>
	/// when no provider is bound.
	/// </summary>
	public EventPayloadWriter? PayloadWriter { get; internal set; }
}
