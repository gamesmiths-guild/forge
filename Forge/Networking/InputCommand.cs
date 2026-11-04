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
	/// <remarks>
	/// A client can send any index, so only tags that match one of <paramref name="inputEvents"/> are raised: a client
	/// can't trigger an ability meant for a server event. An index that names no tag, or a tag that doesn't match, is
	/// skipped.
	/// </remarks>
	/// <param name="entity">The entity the input belongs to.</param>
	/// <param name="inputEvents">The tags a client may raise, children included, from the tags manager the indices
	/// come from.</param>
	public void RaiseEvents(IForgeEntity entity, TagContainer inputEvents)
	{
		TagsManager tagsManager = inputEvents.TagsManager;

		foreach (ushort index in Events)
		{
			if (index >= tagsManager.NetIndexCount)
			{
				continue;
			}

			var tag = Tag.RequestTag(tagsManager, tagsManager.GetTagKeyFromNetIndex(index));

			if (!tag.MatchesAny(inputEvents))
			{
				continue;
			}

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
