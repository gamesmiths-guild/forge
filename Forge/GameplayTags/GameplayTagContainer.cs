// Copyright © 2024 Gamesmiths Guild.

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
	public HashSet<GameplayTag> GameplayTags { get; } = [];

	/// <summary>
	/// Gets a set of all parent tags, along with the current <see cref="GameplayTag"/>.
	/// </summary>
	/// <remarks>
	/// Used to optimize parent tag lookups.
	/// </remarks>
	public HashSet<GameplayTag> ParentTags { get; } = [];

	/// <summary>
	/// Gets the number of explicitly added tags.
	/// </summary>
	public int Count => GameplayTags.Count;

	/// <summary>
	/// Gets a value indicating whether the container has any valid tags.
	/// </summary>
	public bool IsValid => GameplayTags.Count > 0;

	/// <summary>
	/// Gets a value indicating whether the container is empty or not.
	/// </summary>
	public bool IsEmpty => GameplayTags.Count == 0;

	internal GameplayTagsManager GameplayTagsManager { get; }

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

		AddTag(tag);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="GameplayTagContainer"/> class copying the values from another
	/// <see cref="GameplayTagContainer"/>.
	/// </summary>
	/// <param name="other">The other <see cref="GameplayTagContainer"/> to copy the values from.</param>
	public GameplayTagContainer(GameplayTagContainer other)
		: this(other.GameplayTagsManager)
	{
		GameplayTags.UnionWith(other.GameplayTags);
		ParentTags.UnionWith(other.ParentTags);
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
		GameplayTags.UnionWith(sourceTags);
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
		var isEmpty = (container.GameplayTags.Count == 0) ? (byte)1 : (byte)0;
		containerStream.Add(isEmpty);

		// Early return if it's empty.
		if (isEmpty == 1)
		{
			if (container.GameplayTags.Count > 0)
			{
				container.Reset(container.GameplayTags.Count);
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

		var numTags = (byte)container.GameplayTags.Count;
		var maxSize = (1 << gameplayTagsManager.NumBitsForContainerSize) - 1;

		if (numTags > maxSize)
		{
			throw new SerializationException($"Container has {numTags} elements when max is {maxSize}!\n\nTags: " +
				$"{container}");
		}

		containerStream.Add(numTags);

		foreach (GameplayTag tag in container.GameplayTags)
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
	public static bool NetDeserialize(GameplayTagsManager gameplayTagsManager, byte[] stream, out GameplayTagContainer deserializedContainer)
	{
		deserializedContainer = new GameplayTagContainer(gameplayTagsManager);

		// Empty container.
		if (stream[0] == 1)
		{
			return true;
		}

		var numTags = stream[1];
		deserializedContainer.GameplayTags.EnsureCapacity(numTags);

		for (var i = 0; i < numTags; i++)
		{
			var tagStream = new byte[2];
			Array.Copy(stream, 2 + (2 * i), tagStream, 0, 2);

			GameplayTag.NetDeserialize(gameplayTagsManager, tagStream, out GameplayTag deserializedTag);

			deserializedContainer.AddTag(deserializedTag);
		}

		deserializedContainer.FillParentTags();
		return true;
	}

	/// <summary>
	/// Adds the given <see cref="GameplayTag"/> to this container.
	/// </summary>
	/// <param name="tag">The <see cref="GameplayTag"/> to be added to the container.</param>
	public void AddTag(GameplayTag tag)
	{
		if (tag != GameplayTag.Empty && GameplayTags.Add(tag))
		{
			ParentTags.UnionWith(GameplayTagsManager.ExtractParentTags(tag));
		}
	}

	/// <summary>
	/// Removes a specific <see cref="GameplayTag"/> from the container, if present.
	/// </summary>
	/// <param name="tag">The <see cref="GameplayTag"/> to remove from the container.</param>
	/// <param name="deferParentTags">Bypasses calling <see cref="FillParentTags"/> for performance reasons.
	/// <para>If <see langword="true"/>, <see cref="FillParentTags"/> must be handled by the caller.</para></param>
	/// <returns><see langword="true"/> if the tag was removed; otherwise, <see langword="false"/>.</returns>
	public bool RemoveTag(GameplayTag tag, bool deferParentTags = false)
	{
		if (GameplayTags.Remove(tag))
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

	/// <summary>
	/// Removes all tags in <paramref name="otherContainer"/> from this container.
	/// </summary>
	/// <param name="otherContainer">A <see cref="GameplayTagContainer"/> with the tags to remove from the container.
	/// </param>
	public void RemoveTags(GameplayTagContainer otherContainer)
	{
		var changed = false;

		foreach (GameplayTag tag in otherContainer)
		{
			changed = GameplayTags.Remove(tag) || changed;
		}

		if (changed)
		{
			// Rebuild once at the end.
			FillParentTags();
		}
	}

	/// <summary>
	/// Clears all tags from the container while maintaining the internal capacity as specified.
	/// </summary>
	/// <param name="capacity">The desired initial capacity after the reset.</param>
	public void Reset(int capacity)
	{
		GameplayTags.Clear();
		GameplayTags.EnsureCapacity(capacity);

		// On average, the size of ParentTags is comparable to that of GameplayTags.
		ParentTags.Clear();
		ParentTags.EnsureCapacity(capacity);
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

		return GameplayTags.Contains(tag) || ParentTags.Contains(tag);
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

		return GameplayTags.Contains(tag);
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

		return otherContainer.Any(x => GameplayTags.Contains(x) || ParentTags.Contains(x));
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

		return otherContainer.Any(GameplayTags.Contains);
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

		return otherContainer.All(x => GameplayTags.Contains(x) || ParentTags.Contains(x));
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

		return otherContainer.All(GameplayTags.Contains);
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

		foreach (GameplayTag tag in GameplayTags.Where(x => x.MatchesAny(otherContainer)))
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

		foreach (GameplayTag tag in GameplayTags.Where(x => x.MatchesAnyExact(otherContainer)))
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

	/// <summary>
	/// Merges all tags from another container into this one.
	/// </summary>
	/// <remarks>
	/// NOTE: This operation effectively computes the union of the tags in this container and
	/// <paramref name="otherContainer"/>.
	/// </remarks>
	/// <param name="otherContainer"><see cref="GameplayTagContainer"/> with tags to be included in this container.
	/// </param>
	public void AppendTags(GameplayTagContainer otherContainer)
	{
		GameplayTags.EnsureCapacity(GameplayTags.Count + otherContainer.GameplayTags.Count);
		ParentTags.EnsureCapacity(ParentTags.Count + otherContainer.ParentTags.Count);

		foreach (GameplayTag otherTag in otherContainer.GameplayTags)
		{
			GameplayTags.Add(otherTag);
		}

		foreach (GameplayTag otherTag in otherContainer.ParentTags)
		{
			ParentTags.Add(otherTag);
		}
	}

	/// <inheritdoc />
	public override string ToString()
	{
		if (IsEmpty)
		{
			return string.Empty;
		}

		var stringBuilder = new StringBuilder();

		foreach (GameplayTag tag in GameplayTags)
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

		if (GameplayTags.Count != other.GameplayTags.Count)
		{
			return false;
		}

		return HasAllExact(other);
	}

	/// <inheritdoc />
	public override int GetHashCode()
	{
		return GameplayTags?.GetHashCode() ?? 0;
	}

	/// <inheritdoc />
	public IEnumerator<GameplayTag> GetEnumerator()
	{
		return GameplayTags.GetEnumerator();
	}

	/// <inheritdoc />
	IEnumerator IEnumerable.GetEnumerator()
	{
		return GameplayTags.GetEnumerator();
	}

#pragma warning disable T0009 // Internal Styling Rule T0009
	internal void AddTagFast(GameplayTag tag)
	{
		if (GameplayTags.Add(tag))
		{
			ParentTags.UnionWith(GameplayTagsManager.ExtractParentTags(tag));
		}
	}

	internal GameplayTagContainer GetExplicitGameplayTagParents()
	{
		GameplayTagContainer resultContainer = new(GameplayTagsManager, GameplayTags);

		foreach (GameplayTag tag in ParentTags)
		{
			resultContainer.GameplayTags.Add(tag);
		}

		return resultContainer;
	}
#pragma warning restore T0009 // Internal Styling Rule T0009

	private void FillParentTags()
	{
		ParentTags.Clear();

		if (GameplayTags.Count > 0)
		{
			foreach (GameplayTag tag in GameplayTags)
			{
				ParentTags.UnionWith(GameplayTagsManager.ExtractParentTags(tag));
			}
		}
	}
}
