// Copyright © Gamesmiths Guild.

using System.Diagnostics;
using Gamesmiths.Forge.GameplayTags;

namespace Gamesmiths.Forge.Core;

/// <summary>
/// Provides comprehensive tag management for an entity, integrating both original and inherited tags.
/// Tracks inherited tags with reference counts and ensures the consistency of the entity's complete tag set.
/// </summary>
public class GameplayTags
{
	private readonly Dictionary<GameplayTag, int> _modifierTagCounts = [];

	internal event Action<GameplayTagContainer>? OnTagsChanged;

	/// <summary>
	/// Gets a container with the base tags for this entity.
	/// </summary>
	public GameplayTagContainer BaseTags { get; }

	/// <summary>
	/// Gets a container with the modifier tags for this entity (temporarily added tags).
	/// </summary>
	public GameplayTagContainer ModifierTags { get; }

	/// <summary>
	/// Gets a container with all tags (base + modifier) from the entity.
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

	internal void AddBaseTag(GameplayTag tag)
	{
		if (BaseTags.HasTag(tag))
		{
			return;
		}

		BaseTags.AddTagFast(tag);
		CombinedTags.AddTagFast(tag);

		OnTagsChanged?.Invoke(CombinedTags);
	}

	internal void AddBaseTags(GameplayTagContainer tags)
	{
		if (BaseTags.HasAllExact(tags))
		{
			return;
		}

		BaseTags.AppendTags(tags);
		CombinedTags.AppendTags(tags);

		OnTagsChanged?.Invoke(CombinedTags);
	}

	internal void RemoveBaseTag(GameplayTag tag)
	{
		foreach (GameplayTag removedTag in BaseTags.RemoveTag(tag))
		{
			if (!_modifierTagCounts.TryGetValue(removedTag, out var count) || count == 0)
			{
				CombinedTags.RemoveTag(removedTag);
			}
		}
	}

	internal void RemoveBaseTags(GameplayTagContainer tags)
	{
		foreach (GameplayTag removedTag in BaseTags.RemoveTags(tags))
		{
			if (!_modifierTagCounts.TryGetValue(removedTag, out var count) || count == 0)
			{
				CombinedTags.RemoveTag(removedTag);
			}
		}
	}

	internal void AddModifierTag(GameplayTag tag)
	{
		if (_modifierTagCounts.TryGetValue(tag, out var modifierTagCount))
		{
			_modifierTagCounts[tag] = ++modifierTagCount;

			if (modifierTagCount != 1)
			{
				return;
			}
		}
		else
		{
			_modifierTagCounts[tag] = 1;
		}

		ModifierTags.AddTagFast(tag);
		CombinedTags.AddTagFast(tag);

		OnTagsChanged?.Invoke(CombinedTags);
	}

	internal void AddModifierTags(GameplayTagContainer tags)
	{
		foreach (GameplayTag tag in tags)
		{
			AddModifierTag(tag);
		}
	}

	internal void RemoveModifierTag(GameplayTag tag)
	{
		if (!_modifierTagCounts.TryGetValue(tag, out var modifierTagCount))
		{
			return;
		}

		_modifierTagCounts[tag] = --modifierTagCount;

		Debug.Assert(modifierTagCount >= 0, $"{nameof(modifierTagCount)} count should never reach a negative value.");

		if (modifierTagCount == 0)
		{
			ModifierTags.RemoveTagExact(tag);

			if (!BaseTags.HasTagExact(tag))
			{
				CombinedTags.RemoveTagExact(tag);
			}

			OnTagsChanged?.Invoke(CombinedTags);
		}
	}

	internal void RemoveModifierTags(GameplayTagContainer tags)
	{
		foreach (GameplayTag tag in tags)
		{
			RemoveModifierTag(tag);
		}
	}
}
