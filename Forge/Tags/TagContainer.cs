// Copyright © Gamesmiths Guild.

using System.Collections;
using System.Globalization;
using System.Runtime.Serialization;
using System.Text;
#if NETSTANDARD2_1
using Gamesmiths.Forge.Compatibility;
#endif
using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Tags;

/// <summary>
/// A <see cref="TagContainer"/> represets a collection of <see cref="Tag"/>s, containing tags added
/// explicitly and implicitly through their parent-child tag hierarchy.
/// </summary>
public sealed class TagContainer : IEnumerable<Tag>, IEquatable<TagContainer>
{
	/// <summary>
	/// Gets the set of <see cref="Tag"/>s in this container.
	/// </summary>
#if NETSTANDARD2_1
	public IReadOnlySet<Tag> Tags => new ReadOnlySetWrapper<Tag>(InternalTags);
#else
	public IReadOnlySet<Tag> Tags => InternalTags;
#endif

	/// <summary>
	/// Gets a set of all parent tags, along with the current <see cref="Tag"/>.
	/// </summary>
	/// <remarks>
	/// Used to optimize parent tag lookups.
	/// </remarks>
#if NETSTANDARD2_1
	public IReadOnlySet<Tag> ParentTags => new ReadOnlySetWrapper<Tag>(InternalParentTags);
#else
	public IReadOnlySet<Tag> ParentTags => InternalParentTags;
#endif

	/// <summary>
	/// Gets the number of explicitly added tags.
	/// </summary>
	public int Count => Tags.Count;

	/// <summary>
	/// Gets a value indicating whether the container is empty or not.
	/// </summary>
	public bool IsEmpty => Tags.Count == 0;

	internal TagsManager TagsManager { get; }

	internal HashSet<Tag> InternalTags { get; } = [];

	internal HashSet<Tag> InternalParentTags { get; } = [];

	/// <summary>
	/// Initializes a new instance of the <see cref="TagContainer"/> class.
	/// </summary>
	/// <param name="tagsManager">The manager for handling tag indexing and associations for this container.
	/// </param>
	public TagContainer(TagsManager tagsManager)
	{
		TagsManager = tagsManager;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="TagContainer"/> class.
	/// </summary>
	/// <param name="tag">A <see cref="Tag"/> to be added in the container.</param>
	/// <exception cref="ArgumentException">Throws if the tag used to initialize this container is not registered with a
	/// <see cref="TagsManager"/>.</exception>
	public TagContainer(Tag tag)
	{
		if (tag.TagsManager is null)
		{
			throw new ArgumentException("Tag must be registered with a manager.", nameof(tag));
		}

		TagsManager = tag.TagsManager;

		AddTagFast(tag);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="TagContainer"/> class copying the values from another
	/// <see cref="TagContainer"/>.
	/// </summary>
	/// <param name="other">The other <see cref="TagContainer"/> to copy the values from.</param>
	public TagContainer(TagContainer other)
		: this(other.TagsManager)
	{
		InternalTags.UnionWith(other.InternalTags);
		InternalParentTags.UnionWith(other.InternalParentTags);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="TagContainer"/> class based on a list of
	/// <see cref="Tag"/>s.
	/// </summary>
	/// <param name="tagsManager">The manager for handling tag indexing and associations for this container.
	/// </param>
	/// <param name="sourceTags">The set of <see cref="Tag"/>s to initialize this container with.</param>
	public TagContainer(TagsManager tagsManager, HashSet<Tag> sourceTags)
		: this(tagsManager)
	{
		Validation.Assert(
			sourceTags.All(x => x.TagsManager == tagsManager),
			"All tags must have the same TagsManager.");

		InternalTags.UnionWith(sourceTags);
		FillParentTags();
	}

	/// <summary>
	/// Efficient network serialization, leveraging the dictionary for optimized performance.
	/// </summary>
	/// <param name="tagsManager">The manager responsible for tag lookup and net index handling.</param>
	/// <param name="container">The <see cref="TagContainer"/> to be serialized.</param>
	/// <param name="serializedContainerStream">The serialized stream for this caontainer.</param>
	/// <returns><see langword="true"/> if successfully serialized; <see langword="false"/> otherwise.</returns>
	/// <exception cref="SerializationException">Throws if there are more tags than the configured max size.</exception>
	public static bool NetSerialize(
		TagsManager tagsManager,
		TagContainer container,
		out byte[] serializedContainerStream)
	{
		var containerStream = new List<byte>();

		// The first bit indicates an empty container, which is often replicated.
		var isEmpty = (container.InternalTags.Count == 0) ? (byte)1 : (byte)0;
		containerStream.Add(isEmpty);

		// Early return if it's empty.
		if (isEmpty == 1)
		{
			if (container.InternalTags.Count > 0)
			{
				container.Reset(container.InternalTags.Count);
			}

			serializedContainerStream = [.. containerStream];

			return true;
		}

		// Containers at this point should always have a designated manager.
		Validation.Assert(
			container.TagsManager is not null,
			$"Container isn't properly registred in a {typeof(TagsManager)}.");

		if (tagsManager != container.TagsManager)
		{
			serializedContainerStream = [];
			return false;
		}

		var numTags = (byte)container.InternalTags.Count;
		var maxSize = (1 << tagsManager.NumBitsForContainerSize) - 1;

		if (numTags > maxSize)
		{
			throw new SerializationException($"Container has {numTags} elements when max is {maxSize}!\n\nTags: " +
				$"{container}");
		}

		containerStream.Add(numTags);

		foreach (Tag tag in container.InternalTags)
		{
			Tag.NetSerialize(tagsManager, tag, out var index);

			// Read net index from buffer. This is just a practical example, use a BitStream reader here isntead.
			var netIndex = new ushort[] { index };
			var netIndexStream = new byte[2];
			Buffer.BlockCopy(netIndex, 0, netIndexStream, 0, 2);

			containerStream.AddRange(netIndexStream);

			// TODO: Make tag replication statistics for replication optimization.
		}

		serializedContainerStream = [.. containerStream];

		return true;
	}

	/// <summary>
	/// Efficient network deserialization, leveraging the dictionary for optimized performance.
	/// </summary>
	/// <param name="tagsManager">The manager responsible for tag lookup and net index handling.</param>
	/// <param name="stream">The data stream to be deserialized.</param>
	/// <param name="deserializedContainer">The resulting <see cref="TagContainer"/> from deserialization.
	/// </param>
	/// <returns><see langword="true"/> if successfully deserialized; <see langword="false"/> otherwise.</returns>
	public static bool NetDeserialize(
		TagsManager tagsManager,
		byte[] stream,
		out TagContainer deserializedContainer)
	{
		deserializedContainer = new TagContainer(tagsManager);

		// Empty container.
		if (stream[0] == 1)
		{
			return true;
		}

		var numTags = stream[1];
		deserializedContainer.InternalTags.EnsureCapacity(numTags);

		for (var i = 0; i < numTags; i++)
		{
			var tagStream = new byte[2];
			Array.Copy(stream, 2 + (2 * i), tagStream, 0, 2);

			if (Tag.NetDeserialize(tagsManager, tagStream, out Tag deserializedTag))
			{
				deserializedContainer.AddTag(deserializedTag);
			}
		}

		deserializedContainer.FillParentTags();
		return true;
	}

	/// <summary>
	/// Checks if <paramref name="tag"/> is explicitly present in this container, including any parent tags.
	/// </summary>
	/// <remarks>
	/// {"enemy.undead"}.HasTag("enemy") will return <see langword="true"/>, while {"enemy"}.HasTag("enemy.undead")
	/// will return <see langword="false"/>.
	/// </remarks>
	/// <param name="tag"><see cref="Tag"/> to search for within this container.</param>
	/// <returns><see langword="true"/> if <paramref name="tag"/> is found in this container, <see langword="false"/>
	/// otherwise.
	/// <para>If <paramref name="tag"/> is not valid, it will always return <see langword="false"/>.</para></returns>
	public bool HasTag(Tag tag)
	{
		if (!tag.IsValid)
		{
			return false;
		}

		return InternalTags.Contains(tag) || InternalParentTags.Contains(tag);
	}

	/// <summary>
	/// Checks if <paramref name="tag"/> is explicitly present in this container, allowing only exact matches.
	/// </summary>
	/// <remarks>
	/// {"enemy.undead"}.HasTagExact("enemy") will return <see langword="false"/>.
	/// </remarks>
	/// <param name="tag"><see cref="Tag"/> to verify in this container.</param>
	/// <returns><see langword="true"/> if <paramref name="tag"/> is exactly in this container; otherwise,
	/// <see langword="false"/>.
	/// <para>Returns <see langword="false"/> if <paramref name="tag"/> is invalid.</para></returns>
	public bool HasTagExact(Tag tag)
	{
		if (!tag.IsValid)
		{
			return false;
		}

		return InternalTags.Contains(tag);
	}

	/// <summary>
	/// Checks if this container contains any tag from the given container, considering parent tags as well.
	/// </summary>
	/// <remarks>
	/// {"enemy.undead"}.HasAny({"enemy", "item"}) will return <see langword="true"/>, while
	/// {"enemy"}.HasAny({"enemy.undead", "item"}) will return <see langword="false"/>.
	/// </remarks>
	/// <param name="otherContainer"><see cref="TagContainer"/> to compare with this container.</param>
	/// <returns>
	/// <see langword="true"/> if any tag from <paramref name="otherContainer"/> is found in this container,
	/// <see langword="false"/> otherwise.
	/// <para>Returns <see langword="false"/> if <paramref name="otherContainer"/> is empty or invalid.</para></returns>
	public bool HasAny(TagContainer otherContainer)
	{
		if (otherContainer.IsEmpty)
		{
			return false;
		}

		return otherContainer.Any(x => InternalTags.Contains(x) || InternalParentTags.Contains(x));
	}

	/// <summary>
	/// Checks if this container contains any tags from the given container, only matching tags exactly.
	/// </summary>
	/// <remarks>
	/// {"enemy.undead"}.HasAnyExact({"enemy", "item"}) will return <see langword="false"/>.
	/// </remarks>
	/// <param name="otherContainer"><see cref="TagContainer"/> to compare with this container.</param>
	/// <returns>
	/// <see langword="true"/> if any tag from <paramref name="otherContainer"/> is an exact match in this container;
	/// otherwise, <see langword="false"/>.
	/// <para>Returns <see langword="false"/> if <paramref name="otherContainer"/> is empty or invalid.</para></returns>
	public bool HasAnyExact(TagContainer otherContainer)
	{
		if (otherContainer.IsEmpty)
		{
			return false;
		}

		return otherContainer.Any(InternalTags.Contains);
	}

	/// <summary>
	/// Checks if this container contains all tags from the given container, considering parent tags as well.
	/// </summary>
	/// <remarks>
	/// {"enemy.undead", "item.consumable"}.HasAll({"enemy", "item"}) returns <see langword="true"/>, but
	/// {"enemy", "item"}.HasAll({"enemy.undead", "item.consumable"}) will return <see langword="false"/>.
	/// </remarks>
	/// <param name="otherContainer"><see cref="TagContainer"/> to compare with this container.</param>
	/// <returns><see langword="true"/> if this container includes all tags from <paramref name="otherContainer"/>, or
	/// if <paramref name="otherContainer"/> is empty.
	/// <para>If <paramref name="otherContainer"/> is empty or invalid, it will return <see langword="true"/> since
	/// there are no tags to check.</para></returns>
	public bool HasAll(TagContainer otherContainer)
	{
		if (otherContainer.IsEmpty)
		{
			return true;
		}

		return otherContainer.All(x => InternalTags.Contains(x) || InternalParentTags.Contains(x));
	}

	/// <summary>
	/// Checks if this container contains all tags from the given container, only matching tags exactly.
	/// </summary>
	/// <remarks>
	/// {"enemy.undead", "item.consumable"}.HasAllExact({"enemy", "item"}) will return <see langword="false"/>.
	/// </remarks>
	/// <param name="otherContainer"><see cref="TagContainer"/> containing the tags to compare exactly.</param>
	/// <returns><see langword="true"/> if all tags from <paramref name="otherContainer"/> are exactly present in this
	/// container, or if it is empty.
	/// <para>If <paramref name="otherContainer"/> is empty or invalid, it will return <see langword="true"/> as there
	/// are no tags to compare.</para></returns>
	public bool HasAllExact(TagContainer otherContainer)
	{
		if (otherContainer.IsEmpty)
		{
			return true;
		}

		return otherContainer.All(InternalTags.Contains);
	}

	/// <summary>
	/// Returns a subset of this container that contains all tags that match any tag in
	/// <paramref name="otherContainer"/>, considering parent tags as well.
	/// </summary>
	/// <param name="otherContainer">The <see cref="TagContainer"/> used to filter this container.</param>
	/// <returns>A <see cref="TagContainer"/> with the tags that match any tags any from
	/// <paramref name="otherContainer"/>.</returns>
	public TagContainer Filter(TagContainer otherContainer)
	{
		TagContainer resultContainer = new(TagsManager);

		foreach (Tag tag in InternalTags.Where(x => x.MatchesAny(otherContainer)))
		{
			resultContainer.AddTagFast(tag);
		}

		return resultContainer;
	}

	/// <summary>
	/// Returns a subset of this container that contains only the tags that exactly match any tag from
	/// <paramref name="otherContainer"/>.
	/// </summary>
	/// <param name="otherContainer">The <see cref="TagContainer"/> to filter against.</param>
	/// <returns>A <see cref="TagContainer"/> with tags that exactly match those in
	/// <paramref name="otherContainer"/>.</returns>
	public TagContainer FilterExact(TagContainer otherContainer)
	{
		TagContainer resultContainer = new(TagsManager);

		foreach (Tag tag in InternalTags.Where(x => x.MatchesAnyExact(otherContainer)))
		{
			resultContainer.AddTagFast(tag);
		}

		return resultContainer;
	}

	/// <summary>
	/// Evaluates whether this container satisfies the specified query.
	/// </summary>
	/// <param name="query"><see cref="TagQuery"/> to compare against.</param>
	/// <returns><see langword="true"/> if this container meets the criteria of the query, <see langword="false"/>
	/// otherwise.</returns>
	public bool MatchesQuery(TagQuery query)
	{
		return query.Matches(this);
	}

	/// <inheritdoc />
	public override string ToString()
	{
		if (IsEmpty)
		{
			return string.Empty;
		}

		var stringBuilder = new StringBuilder();

		foreach (Tag tag in InternalTags)
		{
			stringBuilder.AppendFormat(CultureInfo.InvariantCulture, "'{0}', ", tag);
		}

		stringBuilder.Remove(stringBuilder.Length - 2, 2);

		return stringBuilder.ToString();
	}

	/// <inheritdoc />
	public override bool Equals(object? obj)
	{
		if (obj is TagContainer other)
		{
			return Equals(other);
		}

		return false;
	}

	/// <inheritdoc />
	public bool Equals(TagContainer? other)
	{
		if (other is null)
		{
			return false;
		}

		if (InternalTags.Count != other.InternalTags.Count)
		{
			return false;
		}

		return HasAllExact(other);
	}

	/// <inheritdoc />
	public override int GetHashCode()
	{
		return InternalTags?.GetHashCode() ?? 0;
	}

	/// <inheritdoc />
	public IEnumerator<Tag> GetEnumerator()
	{
		return InternalTags.GetEnumerator();
	}

	/// <inheritdoc />
	IEnumerator IEnumerable.GetEnumerator()
	{
		return InternalTags.GetEnumerator();
	}

#pragma warning disable T0009 // Internal Styling Rule T0009

	internal void AddTag(Tag tag)
	{
		if (tag.IsValid && InternalTags.Add(tag))
		{
			InternalParentTags.UnionWith(TagsManager.ExtractParentTags(tag));
		}
	}

	internal void AddTagFast(Tag tag)
	{
		if (InternalTags.Add(tag))
		{
			InternalParentTags.UnionWith(TagsManager.ExtractParentTags(tag));
		}
	}

	internal HashSet<Tag> RemoveTag(Tag tag, bool deferParentTags = false)
	{
		var tagsToRemove = InternalTags
			.Where(x => x.MatchesTag(tag))
			.ToHashSet();

		if (tagsToRemove.Count == 0)
		{
			return tagsToRemove;
		}

		foreach (Tag tagToRemove in tagsToRemove)
		{
			InternalTags.Remove(tagToRemove);
		}

		if (!deferParentTags)
		{
			// Rebuild the parent table from scratch, as tags may provide the same parent tag.
			FillParentTags();
		}

		return tagsToRemove;
	}

	internal bool RemoveTagExact(Tag tag, bool deferParentTags = false)
	{
		if (InternalTags.Remove(tag))
		{
			if (!deferParentTags)
			{
				// Rebuild the parent table from scratch, as tags may provide the same parent tag.
				FillParentTags();
			}

			return true;
		}

		return false;
	}

	internal HashSet<Tag> RemoveTags(TagContainer otherContainer)
	{
		var tagsRemoved = new HashSet<Tag>();

		foreach (Tag tag in otherContainer)
		{
			tagsRemoved.UnionWith(RemoveTag(tag, true));
		}

		if (tagsRemoved.Count > 0)
		{
			// Rebuild once at the end.
			FillParentTags();
		}

		return tagsRemoved;
	}

	internal void RemoveTagsExact(TagContainer otherContainer)
	{
		var changed = false;

		foreach (Tag tag in otherContainer)
		{
			changed = InternalTags.Remove(tag) || changed;
		}

		if (changed)
		{
			// Rebuild once at the end.
			FillParentTags();
		}
	}

	internal void Reset(int capacity)
	{
		InternalTags.Clear();
		InternalTags.EnsureCapacity(capacity);

		// On average, the size of ParentTags is comparable to that of Tags.
		InternalParentTags.Clear();
		InternalParentTags.EnsureCapacity(capacity);
	}

	internal void AppendTags(TagContainer otherContainer)
	{
		InternalTags.UnionWith(otherContainer.InternalTags);
		InternalParentTags.UnionWith(otherContainer.InternalParentTags);
	}

	internal TagContainer GetExplicitTagParents()
	{
		TagContainer resultContainer = new(TagsManager, InternalTags);

		foreach (Tag tag in InternalParentTags)
		{
			resultContainer.InternalTags.Add(tag);
		}

		return resultContainer;
	}
#pragma warning restore T0009 // Internal Styling Rule T0009

	private void FillParentTags()
	{
		InternalParentTags.Clear();

		if (InternalTags.Count > 0)
		{
			foreach (Tag tag in InternalTags)
			{
				InternalParentTags.UnionWith(TagsManager.ExtractParentTags(tag));
			}
		}
	}
}
