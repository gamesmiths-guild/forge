// Copyright © Gamesmiths Guild.

using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.Serialization;
using System.Text;

namespace Gamesmiths.Forge.GameplayTags;

/// <summary>
/// A <see cref="GameplayTagContainer"/> represets a collection of <see cref="GameplayTag"/>s, containing tags added
/// explicitly and implicitly through their parent-child tag hierarchy.
/// </summary>
public sealed class GameplayTagContainer : IEnumerable<GameplayTag>, IEquatable<GameplayTagContainer>
{
	/// <summary>
	/// Gets the set of <see cref="GameplayTag"/>s in this container.
	/// </summary>
	public IReadOnlySet<GameplayTag> GameplayTags => InternalGameplayTags;

	/// <summary>
	/// Gets a set of all parent tags, along with the current <see cref="GameplayTag"/>.
	/// </summary>
	/// <remarks>
	/// Used to optimize parent tag lookups.
	/// </remarks>
	public IReadOnlySet<GameplayTag> ParentTags => InternalParentTags;

	/// <summary>
	/// Gets the number of explicitly added tags.
	/// </summary>
	public int Count => GameplayTags.Count;

	/// <summary>
	/// Gets a value indicating whether the container is empty or not.
	/// </summary>
	public bool IsEmpty => GameplayTags.Count == 0;

	internal GameplayTagsManager GameplayTagsManager { get; }

	internal HashSet<GameplayTag> InternalGameplayTags { get; } = [];

	internal HashSet<GameplayTag> InternalParentTags { get; } = [];

	/// <summary>
	/// Initializes a new instance of the <see cref="GameplayTagContainer"/> class.
	/// </summary>
	/// <param name="gameplayTagsManager">The manager for handling tag indexing and associations for this container.
	/// </param>
	public GameplayTagContainer(GameplayTagsManager gameplayTagsManager)
	{
		GameplayTagsManager = gameplayTagsManager;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="GameplayTagContainer"/> class.
	/// </summary>
	/// <param name="tag">A <see cref="GameplayTag"/> to be added in the container.</param>
	/// <exception cref="ArgumentException">Throws if the tag used to initialize this container is not registered with a
	/// <see cref="GameplayTagsManager"/>.</exception>
	public GameplayTagContainer(GameplayTag tag)
	{
		if (tag.GameplayTagsManager is null)
		{
			throw new ArgumentException("Tag must be registered with a manager.", nameof(tag));
		}

		GameplayTagsManager = tag.GameplayTagsManager;

		AddTagFast(tag);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="GameplayTagContainer"/> class copying the values from another
	/// <see cref="GameplayTagContainer"/>.
	/// </summary>
	/// <param name="other">The other <see cref="GameplayTagContainer"/> to copy the values from.</param>
	public GameplayTagContainer(GameplayTagContainer other)
		: this(other.GameplayTagsManager)
	{
		InternalGameplayTags.UnionWith(other.InternalGameplayTags);
		InternalParentTags.UnionWith(other.InternalParentTags);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="GameplayTagContainer"/> class based on a list of
	/// <see cref="GameplayTag"/>s.
	/// </summary>
	/// <param name="gameplayTagsManager">The manager for handling tag indexing and associations for this container.
	/// </param>
	/// <param name="sourceTags">The set of <see cref="GameplayTag"/>s to initialize this container with.</param>
	public GameplayTagContainer(GameplayTagsManager gameplayTagsManager, HashSet<GameplayTag> sourceTags)
		: this(gameplayTagsManager)
	{
		Debug.Assert(
			sourceTags.All(x => x.GameplayTagsManager == gameplayTagsManager),
			"All tags must have the same GameplayTagsManager.");

		InternalGameplayTags.UnionWith(sourceTags);
		FillParentTags();
	}

	/// <summary>
	/// Efficient network serialization, leveraging the dictionary for optimized performance.
	/// </summary>
	/// <param name="gameplayTagsManager">The manager responsible for tag lookup and net index handling.</param>
	/// <param name="container">The <see cref="GameplayTagContainer"/> to be serialized.</param>
	/// <param name="serializedContainerStream">The serialized stream for this caontainer.</param>
	/// <returns><see langword="true"/> if successfully serialized; <see langword="false"/> otherwise.</returns>
	/// <exception cref="SerializationException">Throws if there are more tags than the configured max size.</exception>
	public static bool NetSerialize(
		GameplayTagsManager gameplayTagsManager,
		GameplayTagContainer container,
		out byte[] serializedContainerStream)
	{
		var containerStream = new List<byte>();

		// The first bit indicates an empty container, which is often replicated.
		var isEmpty = (container.InternalGameplayTags.Count == 0) ? (byte)1 : (byte)0;
		containerStream.Add(isEmpty);

		// Early return if it's empty.
		if (isEmpty == 1)
		{
			if (container.InternalGameplayTags.Count > 0)
			{
				container.Reset(container.InternalGameplayTags.Count);
			}

			serializedContainerStream = [.. containerStream];

			return true;
		}

		// Containers at this point should always have a designated manager.
		Debug.Assert(
			container.GameplayTagsManager is not null,
			$"Container isn't properly registred in a {typeof(GameplayTagsManager)}.");

		if (gameplayTagsManager != container.GameplayTagsManager)
		{
			serializedContainerStream = [];
			return false;
		}

		var numTags = (byte)container.InternalGameplayTags.Count;
		var maxSize = (1 << gameplayTagsManager.NumBitsForContainerSize) - 1;

		if (numTags > maxSize)
		{
			throw new SerializationException($"Container has {numTags} elements when max is {maxSize}!\n\nTags: " +
				$"{container}");
		}

		containerStream.Add(numTags);

		foreach (GameplayTag tag in container.InternalGameplayTags)
		{
			GameplayTag.NetSerialize(gameplayTagsManager, tag, out var index);

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
	/// <param name="gameplayTagsManager">The manager responsible for tag lookup and net index handling.</param>
	/// <param name="stream">The data stream to be deserialized.</param>
	/// <param name="deserializedContainer">The resulting <see cref="GameplayTagContainer"/> from deserialization.
	/// </param>
	/// <returns><see langword="true"/> if successfully deserialized; <see langword="false"/> otherwise.</returns>
	public static bool NetDeserialize(
		GameplayTagsManager gameplayTagsManager,
		byte[] stream,
		out GameplayTagContainer deserializedContainer)
	{
		deserializedContainer = new GameplayTagContainer(gameplayTagsManager);

		// Empty container.
		if (stream[0] == 1)
		{
			return true;
		}

		var numTags = stream[1];
		deserializedContainer.InternalGameplayTags.EnsureCapacity(numTags);

		for (var i = 0; i < numTags; i++)
		{
			var tagStream = new byte[2];
			Array.Copy(stream, 2 + (2 * i), tagStream, 0, 2);

			if (GameplayTag.NetDeserialize(gameplayTagsManager, tagStream, out GameplayTag deserializedTag))
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
	/// <param name="tag"><see cref="GameplayTag"/> to search for within this container.</param>
	/// <returns><see langword="true"/> if <paramref name="tag"/> is found in this container, <see langword="false"/>
	/// otherwise.
	/// <para>If <paramref name="tag"/> is not valid, it will always return <see langword="false"/>.</para></returns>
	public bool HasTag(GameplayTag tag)
	{
		if (!tag.IsValid)
		{
			return false;
		}

		return InternalGameplayTags.Contains(tag) || InternalParentTags.Contains(tag);
	}

	/// <summary>
	/// Checks if <paramref name="tag"/> is explicitly present in this container, allowing only exact matches.
	/// </summary>
	/// <remarks>
	/// {"enemy.undead"}.HasTagExact("enemy") will return <see langword="false"/>.
	/// </remarks>
	/// <param name="tag"><see cref="GameplayTag"/> to verify in this container.</param>
	/// <returns><see langword="true"/> if <paramref name="tag"/> is exactly in this container; otherwise,
	/// <see langword="false"/>.
	/// <para>Returns <see langword="false"/> if <paramref name="tag"/> is invalid.</para></returns>
	public bool HasTagExact(GameplayTag tag)
	{
		if (!tag.IsValid)
		{
			return false;
		}

		return InternalGameplayTags.Contains(tag);
	}

	/// <summary>
	/// Checks if this container contains any tag from the given container, considering parent tags as well.
	/// </summary>
	/// <remarks>
	/// {"enemy.undead"}.HasAny({"enemy", "item"}) will return <see langword="true"/>, while
	/// {"enemy"}.HasAny({"enemy.undead", "item"}) will return <see langword="false"/>.
	/// </remarks>
	/// <param name="otherContainer"><see cref="GameplayTagContainer"/> to compare with this container.</param>
	/// <returns>
	/// <see langword="true"/> if any tag from <paramref name="otherContainer"/> is found in this container,
	/// <see langword="false"/> otherwise.
	/// <para>Returns <see langword="false"/> if <paramref name="otherContainer"/> is empty or invalid.</para></returns>
	public bool HasAny(GameplayTagContainer otherContainer)
	{
		if (otherContainer.IsEmpty)
		{
			return false;
		}

		return otherContainer.Any(x => InternalGameplayTags.Contains(x) || InternalParentTags.Contains(x));
	}

	/// <summary>
	/// Checks if this container contains any tags from the given container, only matching tags exactly.
	/// </summary>
	/// <remarks>
	/// {"enemy.undead"}.HasAnyExact({"enemy", "item"}) will return <see langword="false"/>.
	/// </remarks>
	/// <param name="otherContainer"><see cref="GameplayTagContainer"/> to compare with this container.</param>
	/// <returns>
	/// <see langword="true"/> if any tag from <paramref name="otherContainer"/> is an exact match in this container;
	/// otherwise, <see langword="false"/>.
	/// <para>Returns <see langword="false"/> if <paramref name="otherContainer"/> is empty or invalid.</para></returns>
	public bool HasAnyExact(GameplayTagContainer otherContainer)
	{
		if (otherContainer.IsEmpty)
		{
			return false;
		}

		return otherContainer.Any(InternalGameplayTags.Contains);
	}

	/// <summary>
	/// Checks if this container contains all tags from the given container, considering parent tags as well.
	/// </summary>
	/// <remarks>
	/// {"enemy.undead", "item.consumable"}.HasAll({"enemy", "item"}) returns <see langword="true"/>, but
	/// {"enemy", "item"}.HasAll({"enemy.undead", "item.consumable"}) will return <see langword="false"/>.
	/// </remarks>
	/// <param name="otherContainer"><see cref="GameplayTagContainer"/> to compare with this container.</param>
	/// <returns><see langword="true"/> if this container includes all tags from <paramref name="otherContainer"/>, or
	/// if <paramref name="otherContainer"/> is empty.
	/// <para>If <paramref name="otherContainer"/> is empty or invalid, it will return <see langword="true"/> since
	/// there are no tags to check.</para></returns>
	public bool HasAll(GameplayTagContainer otherContainer)
	{
		if (otherContainer.IsEmpty)
		{
			return true;
		}

		return otherContainer.All(x => InternalGameplayTags.Contains(x) || InternalParentTags.Contains(x));
	}

	/// <summary>
	/// Checks if this container contains all tags from the given container, only matching tags exactly.
	/// </summary>
	/// <remarks>
	/// {"enemy.undead", "item.consumable"}.HasAllExact({"enemy", "item"}) will return <see langword="false"/>.
	/// </remarks>
	/// <param name="otherContainer"><see cref="GameplayTagContainer"/> containing the tags to compare exactly.</param>
	/// <returns><see langword="true"/> if all tags from <paramref name="otherContainer"/> are exactly present in this
	/// container, or if it is empty.
	/// <para>If <paramref name="otherContainer"/> is empty or invalid, it will return <see langword="true"/> as there
	/// are no tags to compare.</para></returns>
	public bool HasAllExact(GameplayTagContainer otherContainer)
	{
		if (otherContainer.IsEmpty)
		{
			return true;
		}

		return otherContainer.All(InternalGameplayTags.Contains);
	}

	/// <summary>
	/// Returns a subset of this container that contains all tags that match any tag in
	/// <paramref name="otherContainer"/>, considering parent tags as well.
	/// </summary>
	/// <param name="otherContainer">The <see cref="GameplayTagContainer"/> used to filter this container.</param>
	/// <returns>A <see cref="GameplayTagContainer"/> with the tags that match any tags any from
	/// <paramref name="otherContainer"/>.</returns>
	public GameplayTagContainer Filter(GameplayTagContainer otherContainer)
	{
		GameplayTagContainer resultContainer = new(GameplayTagsManager);

		foreach (GameplayTag tag in InternalGameplayTags.Where(x => x.MatchesAny(otherContainer)))
		{
			resultContainer.AddTagFast(tag);
		}

		return resultContainer;
	}

	/// <summary>
	/// Returns a subset of this container that contains only the tags that exactly match any tag from
	/// <paramref name="otherContainer"/>.
	/// </summary>
	/// <param name="otherContainer">The <see cref="GameplayTagContainer"/> to filter against.</param>
	/// <returns>A <see cref="GameplayTagContainer"/> with tags that exactly match those in
	/// <paramref name="otherContainer"/>.</returns>
	public GameplayTagContainer FilterExact(GameplayTagContainer otherContainer)
	{
		GameplayTagContainer resultContainer = new(GameplayTagsManager);

		foreach (GameplayTag tag in InternalGameplayTags.Where(x => x.MatchesAnyExact(otherContainer)))
		{
			resultContainer.AddTagFast(tag);
		}

		return resultContainer;
	}

	/// <summary>
	/// Evaluates whether this container satisfies the specified query.
	/// </summary>
	/// <param name="query"><see cref="GameplayTagQuery"/> to compare against.</param>
	/// <returns><see langword="true"/> if this container meets the criteria of the query, <see langword="false"/>
	/// otherwise.</returns>
	public bool MatchesQuery(GameplayTagQuery query)
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

		foreach (GameplayTag tag in InternalGameplayTags)
		{
			stringBuilder.Append(CultureInfo.InvariantCulture, $"'{tag}'")
				.Append(", ");
		}

		stringBuilder.Remove(stringBuilder.Length - 2, 2);

		return stringBuilder.ToString();
	}

	/// <inheritdoc />
	public override bool Equals(object? obj)
	{
		if (obj is GameplayTagContainer other)
		{
			return Equals(other);
		}

		return false;
	}

	/// <inheritdoc />
	public bool Equals(GameplayTagContainer? other)
	{
		if (other is null)
		{
			return false;
		}

		if (InternalGameplayTags.Count != other.InternalGameplayTags.Count)
		{
			return false;
		}

		return HasAllExact(other);
	}

	/// <inheritdoc />
	public override int GetHashCode()
	{
		return InternalGameplayTags?.GetHashCode() ?? 0;
	}

	/// <inheritdoc />
	public IEnumerator<GameplayTag> GetEnumerator()
	{
		return InternalGameplayTags.GetEnumerator();
	}

	/// <inheritdoc />
	IEnumerator IEnumerable.GetEnumerator()
	{
		return InternalGameplayTags.GetEnumerator();
	}

#pragma warning disable T0009 // Internal Styling Rule T0009

	internal void AddTag(GameplayTag tag)
	{
		if (tag.IsValid && InternalGameplayTags.Add(tag))
		{
			InternalParentTags.UnionWith(GameplayTagsManager.ExtractParentTags(tag));
		}
	}

	internal void AddTagFast(GameplayTag tag)
	{
		if (InternalGameplayTags.Add(tag))
		{
			InternalParentTags.UnionWith(GameplayTagsManager.ExtractParentTags(tag));
		}
	}

	internal HashSet<GameplayTag> RemoveTag(GameplayTag tag, bool deferParentTags = false)
	{
		var tagsToRemove = InternalGameplayTags
			.Where(x => x.MatchesTag(tag))
			.ToHashSet();

		if (tagsToRemove.Count == 0)
		{
			return tagsToRemove;
		}

		foreach (GameplayTag tagToRemove in tagsToRemove)
		{
			InternalGameplayTags.Remove(tagToRemove);
		}

		if (!deferParentTags)
		{
			// Rebuild the parent table from scratch, as tags may provide the same parent tag.
			FillParentTags();
		}

		return tagsToRemove;
	}

	internal bool RemoveTagExact(GameplayTag tag, bool deferParentTags = false)
	{
		if (InternalGameplayTags.Remove(tag))
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

	internal HashSet<GameplayTag> RemoveTags(GameplayTagContainer otherContainer)
	{
		var tagsRemoved = new HashSet<GameplayTag>();

		foreach (GameplayTag tag in otherContainer)
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

	internal void RemoveTagsExact(GameplayTagContainer otherContainer)
	{
		var changed = false;

		foreach (GameplayTag tag in otherContainer)
		{
			changed = InternalGameplayTags.Remove(tag) || changed;
		}

		if (changed)
		{
			// Rebuild once at the end.
			FillParentTags();
		}
	}

	internal void Reset(int capacity)
	{
		InternalGameplayTags.Clear();
		InternalGameplayTags.EnsureCapacity(capacity);

		// On average, the size of ParentTags is comparable to that of GameplayTags.
		InternalParentTags.Clear();
		InternalParentTags.EnsureCapacity(capacity);
	}

	internal void AppendTags(GameplayTagContainer otherContainer)
	{
		InternalGameplayTags.UnionWith(otherContainer.InternalGameplayTags);
		InternalParentTags.UnionWith(otherContainer.InternalParentTags);
	}

	internal GameplayTagContainer GetExplicitGameplayTagParents()
	{
		GameplayTagContainer resultContainer = new(GameplayTagsManager, InternalGameplayTags);

		foreach (GameplayTag tag in InternalParentTags)
		{
			resultContainer.InternalGameplayTags.Add(tag);
		}

		return resultContainer;
	}
#pragma warning restore T0009 // Internal Styling Rule T0009

	private void FillParentTags()
	{
		InternalParentTags.Clear();

		if (InternalGameplayTags.Count > 0)
		{
			foreach (GameplayTag tag in InternalGameplayTags)
			{
				InternalParentTags.UnionWith(GameplayTagsManager.ExtractParentTags(tag));
			}
		}
	}
}
