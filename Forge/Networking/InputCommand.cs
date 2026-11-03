// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Events;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Networking;

/// <summary>
/// What a client tells the server about one tick: the input frame its entity reads that tick, and the discrete
/// input events it raised, as tag indices. The only thing a client ever sends about its intent.
/// </summary>
/// <param name="Tick">The tick the command is for.</param>
/// <param name="Frame">The input frame the entity reads that tick.</param>
/// <param name="Events">The net indices of the tags of the discrete input events raised that tick, such as a menu
/// choice, in the order they were raised. Empty for most ticks.</param>
public readonly record struct InputCommand(long Tick, InputFrame Frame, ushort[] Events)
{
	/// <summary>
	/// Initializes a new instance of the <see cref="InputCommand"/> struct.
	/// A command with no events.
	/// </summary>
	/// <param name="tick">The tick the command is for.</param>
	/// <param name="frame">The input frame.</param>
	public InputCommand(long tick, InputFrame frame)
		: this(tick, frame, [])
	{
	}

	/// <summary>
	/// Gets the index a tag travels as in a command's <see cref="Events"/>.
	/// </summary>
	/// <param name="tagsManager">The tags manager the tag is registered in.</param>
	/// <param name="tag">The tag.</param>
	/// <returns>The tag's net index.</returns>
	public static ushort EventIndex(TagsManager tagsManager, Tag tag)
	{
		return tagsManager.GetNetIndexFromTag(tag);
	}

	/// <summary>
	/// Raises the command's events on an entity, as Forge events with the tag the index names and the entity as both
	/// source and target, so abilities triggered by the tag activate from them. The server does this for the owning
	/// entity as it simulates the tick.
	/// </summary>
	/// <param name="entity">The entity the input belongs to.</param>
	/// <param name="tagsManager">The tags manager the indices come from.</param>
	/// <exception cref="InvalidTagNetIndexException">Thrown when an index names no tag.</exception>
	public void RaiseEvents(IForgeEntity entity, TagsManager tagsManager)
	{
		foreach (ushort index in Events)
		{
			var tag = Tag.RequestTag(tagsManager, tagsManager.GetTagKeyFromNetIndex(index));
			var data = new EventData
			{
				EventTags = new TagContainer(tagsManager, [tag]),
				Source = entity,
				Target = entity,
			};

			entity.Events.Raise(in data);
		}
	}
}
