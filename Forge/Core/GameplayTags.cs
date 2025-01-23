// Copyright © 2024 Gamesmiths Guild.

using Gamesmiths.Forge.GameplayTags;

namespace Gamesmiths.Forge.Core;

/// <summary>
/// Provides comprehensive tag management for an entity, integrating both original and inherited tags.
/// Tracks inherited tags with reference counts and ensures the consistency of the entity's complete tag set.
/// </summary>
public class GameplayTags
{
	private readonly Dictionary<GameplayTag, int> _modifierTagCounts = [];

	/// <summary>
	/// Gets a container with the base tags for this entity.
	/// </summary>
	public GameplayTagContainer BaseTags { get; }

	/// <summary>
	/// Gets a container with the modifier tags for this entity.
	/// </summary>
	public GameplayTagContainer ModifierTags { get; }

	/// <summary>
	/// Gets a container with all tags from the entity.
	/// </summary>
	public GameplayTagContainer CombinedTags { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="GameplayTags"/> class.
	/// </summary>
	/// <param name="baseTags">The original, immutable tags for this entity.</param>
	public GameplayTags(GameplayTagContainer baseTags)
	{
		BaseTags = baseTags;
		CombinedTags = new GameplayTagContainer(baseTags.GameplayTagsManager);
		ModifierTags = new GameplayTagContainer(baseTags.GameplayTagsManager);

		CombinedTags.AppendTags(BaseTags);
	}

	/// <summary>
	/// Adds a new base tag to the entity.
	/// </summary>
	/// <param name="tag">The new tag to be added.</param>
	internal void AddBaseTag(GameplayTag tag)
	{
		BaseTags.AddTag(tag);
		CombinedTags.AddTag(tag);
	}

	/// <summary>
	/// Removes a base tag from the entity.
	/// </summary>
	/// <param name="tag">Tag to be removed.</param>
	internal void RemoveBaseTag(GameplayTag tag)
	{
		BaseTags.RemoveTag(tag);

		if (!_modifierTagCounts.TryGetValue(tag, out var count) || count == 0)
		{
			CombinedTags.RemoveTag(tag);
		}
	}

	/// <summary>
	/// Adds or increments a new modifier tag for the entity.
	/// </summary>
	/// <param name="tag">The new tag to be added or incremented.</param>
	internal void AddModifierTag(GameplayTag tag)
	{
		if (_modifierTagCounts.TryGetValue(tag, out var count))
		{
			_modifierTagCounts[tag] = count++;

			if (count == 1)
			{
				CombinedTags.AddTag(tag);
				ModifierTags.AddTag(tag);
			}
		}
		else
		{
			_modifierTagCounts[tag] = 1;
			CombinedTags.AddTag(tag);
			ModifierTags.AddTag(tag);
		}
	}

	/// <summary>
	/// Removes or decrements a modifier tag from the entity.
	/// </summary>
	/// <param name="tag">The tag to be removed or decremented.</param>
	internal void RemoveModifierTag(GameplayTag tag)
	{
		if (!_modifierTagCounts.TryGetValue(tag, out var count))
		{
			return;
		}

		_modifierTagCounts[tag] = --count;

		if (count == 0)
		{
			ModifierTags.RemoveTag(tag);

			if (!BaseTags.HasTagExact(tag))
			{
				CombinedTags.RemoveTag(tag);
			}
		}
	}
}
