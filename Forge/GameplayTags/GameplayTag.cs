// Copyright © 2024 Gamesmiths Guild.

using System.Diagnostics;
using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.GameplayTags;

/// <summary>
/// A gameplay tag is an immutable structured label, following a hierarchy like "enemy.undead.zombie" or
/// "item.consumable.potion.health", that gets registered and managed by the <see cref="GameplayTagsManager"/>.
/// </summary>
public readonly struct GameplayTag : IEquatable<GameplayTag>
{
	/// <summary>
	/// Gets a static representation of an empty <see cref="GameplayTag"/>.
	/// </summary>
	/// <remarks>
	/// Generally used for tag validation.
	/// </remarks>
	public static GameplayTag Empty { get; }

	/// <summary>
	/// Gets the <see cref="StringKey"/> representing this tag.
	/// </summary>
	public readonly StringKey TagKey { get; }

	/// <summary>
	/// Gets a value indicating whether this tag is a valid tag.
	/// </summary>
	public bool IsValid => this != Empty;

	internal readonly GameplayTagsManager? GameplayTagsManager { get; }

	internal GameplayTag(GameplayTagsManager gameplayTagsManager, StringKey tagKey)
	{
		GameplayTagsManager = gameplayTagsManager;
		TagKey = tagKey;
	}

	/// <summary>
	/// Gets the <see cref="GameplayTag"/> that corresponds to the given <see cref="StringKey"/>.
	/// </summary>
	/// <param name="gameplayTagsManager">The manager from which to request gameplay tags.</param>
	/// <param name="tagKey">The key of the tag to search for.</param>
	/// <param name="errorIfNotFound">If <see langword="true"/>, throws an exception if the tag is not found.</param>
	/// <returns>Will return the corresponding <see cref="GameplayTag"/> or an <see cref="Empty"/> if not
	/// found.</returns>
	/// <exception cref="GameplayTagNotRegisteredException">Thrown for when a <see cref="GameplayTag"/> is not properly
	/// registered with the <see cref="GameplayTagsManager"/> and <paramref name="errorIfNotFound"/>is set to
	/// <see langword="true"/>.</exception>
	public static GameplayTag RequestTag(GameplayTagsManager gameplayTagsManager, StringKey tagKey, bool errorIfNotFound = true)
	{
		return gameplayTagsManager.RequestTag(tagKey, errorIfNotFound);
	}

	/// <summary>
	/// Serializes the given <see cref="GameplayTag"/> into a <see cref="ushort"/> net index.
	/// </summary>
	/// <remarks>
	/// TODO: Use a propper BitStream or similar solution in the future.
	/// </remarks>
	/// <param name="gameplayTagsManager">The manager responsible for tag lookup and net index handling.</param>
	/// <param name="tag">The <see cref="GameplayTag"/> to be serialized.</param>
	/// <param name="netIndex">The serialized index for this tag.</param>
	/// <returns><see langword="true"/> if serialized successfully.</returns>
	public static bool NetSerialize(GameplayTagsManager gameplayTagsManager, GameplayTag tag, out ushort netIndex)
	{
		if (!tag.IsValid)
		{
			netIndex = gameplayTagsManager.InvalidTagNetIndex;
			return true;
		}

		// Tags at this point should always have a designated manager.
		Debug.Assert(
			tag.GameplayTagsManager is not null,
			$"Tag \"{tag.TagKey}\" isn't properly registred in a {typeof(GameplayTagsManager)}.");

		if (gameplayTagsManager != tag.GameplayTagsManager)
		{
			netIndex = gameplayTagsManager.InvalidTagNetIndex;
			return false;
		}

		netIndex = tag.GameplayTagsManager.GetNetIndexFromTag(tag);
		return true;
	}

	/// <summary>
	/// Deserializes a <see cref="GameplayTag"/> from a given <see cref="ushort"/> net index value.
	/// </summary>
	/// <remarks>
	/// TODO: Use a propper BitStream or similar solution in the future.
	/// </remarks>
	/// <param name="gameplayTagsManager">The manager responsible for tag lookup and net index handling.</param>
	/// <param name="stream">The data stream to be deserialized.</param>
	/// <param name="tag">The resulting <see cref="GameplayTag"/> from deserialization.</param>
	/// <returns><see langword="true"/> if deserialized successfully.</returns>
	/// <exception cref="InvalidTagNetIndexException">Thrown if tried to deserialize a value that is not a valid tag net
	/// index.</exception>
	public static bool NetDeserialize(GameplayTagsManager gameplayTagsManager, byte[] stream, out GameplayTag tag)
	{
		// Read netIndex from buffer. This is just a practical example, use a BitStream reader here instead.
		var netIndex = new ushort[stream.Length / 2];
		Buffer.BlockCopy(stream, 0, netIndex, 0, stream.Length);

		StringKey tagKey = gameplayTagsManager.GetTagKeyFromNetIndex(netIndex[0]);

		tag = gameplayTagsManager.RequestTag(tagKey, false);

		return true;
	}

#if DEBUG
	/// <summary>
	/// Validates whether the provided string is a proper <see cref="GameplayTag"/> key (e.g., "enemy.undead.zombie").
	/// Returns <see langword="true"/> if valid; otherwise, <see langword="false"/> with further error details and
	/// attempts to fix the string.
	/// </summary>
	/// <param name="tagString">The string to check for proper tag formatting.</param>
	/// <param name="error">If the string is invalid and this is not null, an error message is provided.</param>
	/// <param name="fixedString">If invalid and this is not null, it will try to repair the string. If no fix can be
	/// applied, this will remain empty.</param>
	/// <returns><see langword="true"/> if the tag format is correct, <see langword="false"/> if it contains errors.
	/// </returns>
	public static bool IsValidKey(string tagString, out string error, out string fixedString)
	{
		return GameplayTagsManager.IsValidTagKey(tagString, out error, out fixedString);
	}
#endif

	/// <summary>
	/// Gets a <see cref="GameplayTagContainer"/> containing only this <see cref="GameplayTag"/>.
	/// </summary>
	/// <returns>A <see cref="GameplayTagContainer"/> containing only this <see cref="GameplayTag"/>.Will return
	/// <see langword="null"/> in case it's requested from an <see cref="Empty"/> tag.</returns>
	public readonly GameplayTagContainer? GetSingleTagContainer()
	{
		if (!IsValid)
		{
			return null;
		}

		// Tags at this point should always have a designated manager.
		Debug.Assert(
			GameplayTagsManager is not null,
			$"Tag \"{TagKey}\" isn't properly registred in a {typeof(GameplayTagsManager)}.");

		GameplayTagNode? tagNode = GameplayTagsManager.FindTagNode(this);

		if (tagNode is not null)
		{
			return tagNode.SingleTagContainer;
		}

		// Tags at this point should always be invalid for some other reason.
		Debug.Assert(
			!IsValid,
			$"Tag \"{TagKey}\" isn't properly registred in the {nameof(GameplayTagsManager)}.");

		return new GameplayTagContainer(GameplayTagsManager);
	}

	/// <summary>
	/// Retrieves the direct parent <see cref="GameplayTag"/> of this tag.
	/// </summary>
	/// <remarks>
	/// For instance, if called on a tag like "enemy.undead.zombie", it will return "enemy.undead".
	/// </remarks>
	/// <returns>The direct parent of this <see cref="GameplayTag"/>.</returns>
	public readonly GameplayTag RequestDirectParent()
	{
		if (!IsValid)
		{
			return Empty;
		}

		// Tags at this point should always have a designated manager.
		Debug.Assert(
			GameplayTagsManager is not null,
			$"Tag \"{TagKey}\" isn't properly registred in a {typeof(GameplayTagsManager)}.");

		return GameplayTagsManager.RequestTagDirectParent(this);
	}

	/// <summary>
	/// Creates a new <see cref="GameplayTagContainer"/> that contains this <see cref="GameplayTag"/> along with all of
	/// its parent tags.
	/// </summary>
	/// <remarks>
	/// For example, calling this on "enemy.undead.zombie" would produce a <see cref="GameplayTagContainer"/> containing
	/// "enemy.undead.zombie", "enemy.undead", and "enemy".
	/// </remarks>
	/// <returns>A <see cref="GameplayTagContainer"/> with this tag and all of its parent tags added explicitly. Will
	/// return <see langword="null"/> in case it's requested from an <see cref="Empty"/> tag.</returns>
	public readonly GameplayTagContainer? GetTagParents()
	{
		if (!IsValid)
		{
			return null;
		}

		// Tags at this point should always have a designated manager.
		Debug.Assert(
			GameplayTagsManager is not null,
			$"Tag \"{TagKey}\" isn't properly registred in a {typeof(GameplayTagsManager)}.");

		return GameplayTagsManager.RequestTagParents(this);
	}

#if DEBUG
	/// <summary>
	/// Parses the tag key and returns a list of raw parent tags, skipping validation with the
	/// <see cref="GameplayTagsManager"/>.
	/// </summary>
	/// <remarks>
	/// For example, calling this on "enemy.undead.zombie" will add "enemy.undead" and "enemy" to the returned list.
	/// </remarks>
	/// <returns>A list containing all raw parent tags for the given tag.</returns>
	public readonly HashSet<GameplayTag> ParseParentTags()
	{
		var uniqueParentTags = new HashSet<GameplayTag>();

		if (!IsValid)
		{
			return uniqueParentTags;
		}

		// Tags at this point should always have a designated manager.
		Debug.Assert(
			GameplayTagsManager is not null,
			$"Tag \"{TagKey}\" isn't properly registred in a {typeof(GameplayTagsManager)}.");

		// Tags should follow the order of the GameplayTag node's ParentTags, starting with the immediate parent.
		StringKey rawTag = TagKey;

		var dotIndex = rawTag.ToString().LastIndexOf('.');

		while (dotIndex != -1)
		{
			// Remove everything after the last dot.
			var parent = rawTag.ToString()[..dotIndex];

			dotIndex = parent.LastIndexOf('.');

			var parentTag = new GameplayTag(GameplayTagsManager, parent);
			uniqueParentTags.Add(parentTag);
		}

		return uniqueParentTags;
	}
#endif

	/// <summary>
	/// Determines if this tag is equivalent to <paramref name="otherTag"/>, including its parent tags.
	/// </summary>
	/// <remarks>
	/// "enemy.undead".MatchesTag("enemy") will return <see langword = "true" />, "enemy".MatchesTag("enemy.undead")
	/// will return <see langword="false"/>.
	/// </remarks>
	/// <param name="otherTag"><see cref="GameplayTag"/> to compare with this tag.</param>
	/// <returns><see langword="true"/> if this <see cref="GameplayTag"/> matches <paramref name="otherTag"/>.
	/// <para>If <paramref name="otherTag"/> is not valid, the method will always return <see langword="false"/>.</para>
	/// </returns>
	public readonly bool MatchesTag(GameplayTag otherTag)
	{
		// Handles Empty tag.
		if (!IsValid)
		{
			return !otherTag.IsValid;
		}

		// Tags at this point should always have a designated manager.
		Debug.Assert(
			GameplayTagsManager is not null,
			$"Tag \"{TagKey}\" isn't properly registred in a {typeof(GameplayTagsManager)}.");

		GameplayTagNode? tagNode = GameplayTagsManager.FindTagNode(this);

		if (tagNode is not null)
		{
			return tagNode.SingleTagContainer.HasTag(otherTag);
		}

		return false;
	}

	/// <summary>
	/// Determines if <paramref name="otherTag"/> is valid and matches this tag exactly.
	/// </summary>
	/// <remarks>
	/// "enemy.undead".MatchesTagExact("enemy") will yield <see langword="false"/>.
	/// </remarks>
	/// <param name="otherTag"><see cref="GameplayTag"/> to compare with this tag.</param>
	/// <returns><see langword="true"/> if <paramref name="otherTag"/> is valid and exactly equals this tag.
	/// <para>If <paramref name="otherTag"/> is not valid, the method will always return <see langword="false"/>.</para>
	/// </returns>
	public readonly bool MatchesTagExact(GameplayTag otherTag)
	{
		if (!otherTag.IsValid)
		{
			return !IsValid;
		}

		return TagKey == otherTag.TagKey;
	}

	/// <summary>
	/// Determines if this tag matches any tag in the specified <see cref="GameplayTagContainer"/>, including checks
	/// against its parent tags.
	/// </summary>
	/// <remarks>
	/// "enemy.undead".MatchesAny({"enemy","beast"}) will return <see langword="true"/>, while
	/// "enemy".MatchesAny({"enemy.undead","beast"}) will return <see langword="false"/>.
	/// </remarks>
	/// <param name="container"><see cref="GameplayTagContainer"/> to compare against this tag.</param>
	/// <returns><see langword="true"/> if this tag matches any tag in <paramref name="container"/>.
	/// <para>If <paramref name="container"/> is empty or invalid, the method will always return<see langword="false"/>.
	/// </para></returns>
	public readonly bool MatchesAny(GameplayTagContainer container)
	{
		// Handles Empty tag.
		if (!IsValid)
		{
			return false;
		}

		// Tags at this point should always have a designated manager.
		Debug.Assert(
			GameplayTagsManager is not null,
			$"Tag \"{TagKey}\" isn't properly registred in a {typeof(GameplayTagsManager)}.");

		GameplayTagNode? tagNode = GameplayTagsManager.FindTagNode(this);

		if (tagNode is not null)
		{
			return tagNode.SingleTagContainer.HasAny(container);
		}

		return false;
	}

	/// <summary>
	/// Determines if this tag matches any tag in the specified <see cref="GameplayTagContainer"/>, allowing only
	/// exact matches.
	/// </summary>
	/// <remarks>
	/// "enemy.undead".MatchesAny({"enemy","beast"}) will return <see langword="false"/>.
	/// </remarks>
	/// <param name="container"><see cref="GameplayTagContainer"/> to compare against this tag.</param>
	/// <returns><see langword="true"/> if this tag exactly matches any tag in <paramref name="container"/>.
	/// <para>If <paramref name="container"/> is empty or invalid, it will always return <see langword="false"/>.</para>
	/// </returns>
	public readonly bool MatchesAnyExact(GameplayTagContainer container)
	{
		if (container.IsEmpty)
		{
			return false;
		}

		return container.GameplayTags.Contains(this);
	}

	/// <summary>
	/// Evaluates how similar two <see cref="GameplayTag"/>s are. Higher values represent a greater number of matching
	/// segments between the tags.
	/// </summary>
	/// <param name="otherTag"><see cref="GameplayTag"/> to check against this tag.</param>
	/// <returns>A numerical value representing the depth of the match, where higher numbers indicate closer similarity.
	/// </returns>
	public readonly int MatchesTagDepth(GameplayTag otherTag)
	{
		// Handles Empty tags.
		if (!IsValid || !otherTag.IsValid)
		{
			return 0;
		}

		// Tags at this point should always have a designated manager.
		Debug.Assert(
			GameplayTagsManager is not null,
			$"Tag \"{TagKey}\" isn't properly registred in a {typeof(GameplayTagsManager)}.");

		return GameplayTagsManager.GameplayTagsMatchDepth(this, otherTag);
	}

	/// <inheritdoc />
	public override readonly string ToString()
	{
		return TagKey.ToString();
	}

	/// <inheritdoc />
	public override readonly bool Equals(object? obj)
	{
		if (obj is null)
		{
			return false;
		}

		if (obj is not GameplayTag gameplayTag)
		{
			return false;
		}

		return TagKey.Equals(gameplayTag.TagKey);
	}

	/// <inheritdoc />
	public readonly bool Equals(GameplayTag other)
	{
		return TagKey.Equals(other.TagKey);
	}

	/// <inheritdoc />
	public override readonly int GetHashCode()
	{
		return TagKey.GetHashCode();
	}

	/// <summary>
	/// Determines if two <see cref="GameplayTag"/> objects are equal.
	/// </summary>
	/// <param name="lhs">The first <see cref="GameplayTag"/> to compare.</param>
	/// <param name="rhs">The second <see cref="GameplayTag"/> to compare.</param>
	/// <returns><see langword="true"/> if the values of <paramref name="lhs"/> and <paramref name="rhs"/> are equal;
	/// otherwise, <see langword="false"/>.</returns>
	public static bool operator ==(GameplayTag lhs, GameplayTag rhs)
	{
		return lhs.Equals(rhs);
	}

	/// <summary>
	/// Determines if two <see cref="GameplayTag"/> objects are not equal.
	/// </summary>
	/// <param name="lhs">The first <see cref="GameplayTag"/> to compare.</param>
	/// <param name="rhs">The second <see cref="GameplayTag"/> to compare.</param>
	/// <returns><see langword="true"/> if the values of <paramref name="lhs"/> and <paramref name="rhs"/> are not
	/// equal; otherwise, <see langword="false"/>.</returns>
	public static bool operator !=(GameplayTag lhs, GameplayTag rhs)
	{
		return !lhs.Equals(rhs);
	}
}
