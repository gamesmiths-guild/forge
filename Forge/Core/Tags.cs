// Copyright © Gamesmiths Guild.

using System.Diagnostics;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Core;

/// <summary>
/// Provides comprehensive tag management for an entity, integrating both original and inherited tags.
/// Tracks inherited tags with reference counts and ensures the consistency of the entity's complete tag set.
/// </summary>
public class Tags
{
	private readonly Dictionary<Tag, int> _modifierTagCounts = [];

	internal event Action<TagContainer>? OnTagsChanged;

	/// <summary>
	/// Gets a container with the base tags for this entity.
	/// </summary>
	public TagContainer BaseTags { get; }

	/// <summary>
	/// Gets a container with the modifier tags for this entity (temporarily added tags).
	/// </summary>
	public TagContainer ModifierTags { get; }

	/// <summary>
	/// Gets a container with all tags (base + modifier) from the entity.
	/// </summary>
	public TagContainer CombinedTags { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="Tags"/> class.
	/// </summary>
	/// <param name="baseTags">The original, immutable tags for this entity.</param>
	public Tags(TagContainer baseTags)
	{
		BaseTags = baseTags;
		CombinedTags = new TagContainer(baseTags.TagsManager);
		ModifierTags = new TagContainer(baseTags.TagsManager);

		CombinedTags.AppendTags(BaseTags);
	}

	internal void AddBaseTag(Tag tag)
	{
		if (BaseTags.HasTag(tag))
		{
			return;
		}

		BaseTags.AddTagFast(tag);
		CombinedTags.AddTagFast(tag);

		OnTagsChanged?.Invoke(CombinedTags);
	}

	internal void AddBaseTags(TagContainer tags)
	{
		if (BaseTags.HasAllExact(tags))
		{
			return;
		}

		BaseTags.AppendTags(tags);
		CombinedTags.AppendTags(tags);

		OnTagsChanged?.Invoke(CombinedTags);
	}

	internal void RemoveBaseTag(Tag tag)
	{
		foreach (Tag removedTag in BaseTags.RemoveTag(tag))
		{
			if (!_modifierTagCounts.TryGetValue(removedTag, out var count) || count == 0)
			{
				CombinedTags.RemoveTag(removedTag);
			}
		}
	}

	internal void RemoveBaseTags(TagContainer tags)
	{
		foreach (Tag removedTag in BaseTags.RemoveTags(tags))
		{
			if (!_modifierTagCounts.TryGetValue(removedTag, out var count) || count == 0)
			{
				CombinedTags.RemoveTag(removedTag);
			}
		}
	}

	internal void AddModifierTag(Tag tag)
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

	internal void AddModifierTags(TagContainer tags)
	{
		foreach (Tag tag in tags)
		{
			AddModifierTag(tag);
		}
	}

	internal void RemoveModifierTag(Tag tag)
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

	internal void RemoveModifierTags(TagContainer tags)
	{
		foreach (Tag tag in tags)
		{
			RemoveModifierTag(tag);
		}
	}
}
