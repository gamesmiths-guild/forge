// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Attributes;
using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript.Nodes.State;

/// <summary>
/// The context for an <see cref="AttributeListenerNode"/>. Tracks the subscribed attribute and handler so the
/// subscription can be removed on deactivation, and the watched entity so the node can follow the attribute across
/// changes to that entity's attribute sets.
/// </summary>
public class AttributeListenerNodeContext : StateNodeContext
{
	/// <summary>
	/// Gets or sets the attribute this node is currently subscribed to.
	/// </summary>
	public EntityAttribute? SubscribedAttribute { get; set; }

	/// <summary>
	/// Gets or sets the entity whose attribute this node is watching.
	/// </summary>
	public IForgeEntity? WatchedEntity { get; set; }

	internal Action<EntityAttribute, int>? Handler { get; set; }

	internal Action<AttributeSet>? MembershipHandler { get; set; }
}
