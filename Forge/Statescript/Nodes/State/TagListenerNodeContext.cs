// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Statescript.Nodes.State;

/// <summary>
/// The context for a <see cref="TagListenerNode"/>. Tracks the subscribed entity, handler and last observed presence
/// of every watched tag so transitions can be detected.
/// </summary>
public class TagListenerNodeContext : StateNodeContext
{
	/// <summary>
	/// Gets or sets the entity whose tags are observed.
	/// </summary>
	public IForgeEntity? SubscribedEntity { get; set; }

	/// <summary>
	/// Gets the last observed presence per watched tag.
	/// </summary>
	public Dictionary<Tag, bool> LastPresence { get; } = [];

	internal Action<TagContainer>? Handler { get; set; }
}
