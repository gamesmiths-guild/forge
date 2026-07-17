// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Attributes;

namespace Gamesmiths.Forge.Statescript.Nodes.State;

/// <summary>
/// The context for an <see cref="AttributeListenerNode"/>. Tracks the subscribed attribute and handler so the
/// subscription can be removed on deactivation.
/// </summary>
public class AttributeListenerNodeContext : StateNodeContext
{
	/// <summary>
	/// Gets or sets the attribute this node is currently subscribed to.
	/// </summary>
	public EntityAttribute? SubscribedAttribute { get; set; }

	internal Action<EntityAttribute, int>? Handler { get; set; }
}
