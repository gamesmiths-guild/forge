// Copyright © 2024 Gamesmiths Guild.

using Gamesmiths.Forge.GameplayTags;

namespace Gamesmiths.Forge.Core;

/// <summary>
/// Provides comprehensive tag management for an entity, integrating both original and inherited tags.
/// Tracks inherited tags with reference counts and ensures the consistency of the entity's complete tag set.
/// </summary>
public class InheritableTags
{
	private readonly Dictionary<GameplayTag, int> _inheritedTagCounts = [];

	private readonly GameplayTagContainer _baseTags;

	/// <summary>
	/// Gets a container with all tags from the entity.
	/// </summary>
	public GameplayTagContainer CombinedTags { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="InheritableTags"/> class.
	/// </summary>
	/// <param name="baseTags">The original, immutable tags for this entity.</param>
	public InheritableTags(GameplayTagContainer baseTags)
	{
		_baseTags = baseTags;
		CombinedTags = new GameplayTagContainer(baseTags.GameplayTagsManager);

		CombinedTags.AppendTags(_baseTags);
	}

	/// <summary>
	/// Adds or increments a new inherited tag for the entity.
	/// </summary>
	/// <param name="tag">The new tag to be added or incremented.</param>
	public void AddOrIncrementInheritedTag(GameplayTag tag)
	{
		if (_inheritedTagCounts.TryGetValue(tag, out var count))
		{
			_inheritedTagCounts[tag] = count + 1;
		}
		else
		{
			_inheritedTagCounts[tag] = 1;
			CombinedTags.AddTag(tag);
		}
	}

	/// <summary>
	/// Decrements or removes an inherited tag from the entity.
	/// </summary>
	/// <param name="tag">The tag to be decremented or removed.</param>
	public void DecrementOrRemoveInheritedTag(GameplayTag tag)
	{
		if (!_inheritedTagCounts.TryGetValue(tag, out var count))
		{
			return;
		}

		_inheritedTagCounts[tag] = count - 1;

		if (count == 0 && !_baseTags.HasTagExact(tag))
		{
			CombinedTags.RemoveTag(tag);
		}
	}
}
