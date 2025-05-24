// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Tags;

/// <summary>
/// Represents a node in the <see cref="TagNode"/> tree, holding metadata for an individual tag.
/// </summary>
public class TagNode : IComparable<TagNode>, IComparable
{
	/// <summary>
	/// Standard value for the net index of an invalid tag.
	/// </summary>
	public const ushort InvalidTagNetIndex = ushort.MaxValue;

	private ushort _netIndex;

	/// <summary>
	/// Gets the key for this tag at current rank in the tree.
	/// </summary>
	public StringKey TagKey { get; }

	/// <summary>
	/// Gets the net index of this <see cref="TagNode"/>.
	/// </summary>
	public ushort NetIndex
	{
		get
		{
			System.Diagnostics.Debug.Assert(_netIndex != InvalidTagNetIndex, "NetIndex should never be invalid.");
			return _netIndex;
		}

		internal set => _netIndex = value;
	}

	/// <summary>
	/// Gets the parent <see cref="TagNode"/> for this node if present.
	/// </summary>
	public TagNode? ParentTagNode { get; }

	/// <summary>
	/// Gets a list of the child <see cref="TagNode"/>s for this node.
	/// </summary>
	public List<TagNode> ChildTags { get; } = [];

	/// <summary>
	/// Gets a container with only this tag, ideal for executing container-based queries.
	/// </summary>
	public TagContainer SingleTagContainer { get; }

	/// <summary>
	/// Gets the <see cref="Tag"/> for this node, including parent tags, delimited by periods.
	/// </summary>
	public Tag CompleteTag => SingleTagContainer.Count > 0
		? SingleTagContainer.Single()
		: Tag.Empty;

	/// <summary>
	/// Gets the complete <see cref="TagKey"/> for the tag represented by this <see cref="TagNode"/>.
	/// </summary>
	public StringKey CompleteTagKey => CompleteTag.TagKey;

	internal bool IsExplicitTag { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="TagNode"/> class.
	/// </summary>
	/// <param name="tagsManager">The manager for handling tag indexing and associations for this node.</param>
	internal TagNode(TagsManager tagsManager)
	{
		SingleTagContainer = new TagContainer(tagsManager);
		ParentTagNode = null;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="TagNode"/> class.
	/// </summary>
	/// <param name="tagsManager">The manager for handling tag indexing and associations for this node.</param>
	/// <param name="tagKey">Short key of tag to insert.</param>
	/// <param name="fullKey">The full key for this tag, for performance.</param>
	/// <param name="parentTagNode">The parent <see cref="TagNode"/> of this node, if any.</param>
	/// <param name="isExplicit">Is the tag explicitly defined or is it implied by the existence of a child tag.</param>
	internal TagNode(
		TagsManager tagsManager,
		StringKey tagKey,
		StringKey fullKey,
		TagNode? parentTagNode,
		bool isExplicit)
	{
		TagKey = tagKey;
		ParentTagNode = parentTagNode;
		IsExplicitTag = isExplicit;

		SingleTagContainer = new TagContainer(tagsManager);
		SingleTagContainer.InternalTags.Add(new Tag(tagsManager, fullKey));

		if (parentTagNode is null)
		{
			return;
		}

		TagContainer parentContainer = parentTagNode.SingleTagContainer;

		if (!parentContainer.IsEmpty)
		{
			SingleTagContainer.InternalParentTags.Add(parentContainer.Single());
			SingleTagContainer.InternalParentTags.UnionWith(parentContainer.ParentTags);
		}
	}

	/// <inheritdoc/>
	public int CompareTo(TagNode? other)
	{
		if (other is null)
		{
			return 1;
		}

		return CompleteTagKey.CompareTo(other.CompleteTagKey);
	}

	/// <inheritdoc/>
	public int CompareTo(object? obj)
	{
		if (obj is null)
		{
			return 1;
		}

		if (obj is TagNode other)
		{
			return CompleteTagKey.CompareTo(other.CompleteTagKey);
		}

		throw new ArgumentException($"Object is not a valid {typeof(TagNode)}", nameof(obj));
	}

	/// <inheritdoc/>
	public override bool Equals(object? obj)
	{
		return ReferenceEquals(this, obj);
	}

	/// <inheritdoc/>
	public override int GetHashCode()
	{
		return CompleteTag.GetHashCode();
	}

	/// <summary>
	/// Determines if two <see cref="TagNode"/> objects are equal.
	/// </summary>
	/// <param name="lhs">The first <see cref="TagNode"/> to compare.</param>
	/// <param name="rhs">The second <see cref="TagNode"/> to compare.</param>
	/// <returns><see langword="true"/> if the values of <paramref name="lhs"/> and <paramref name="rhs"/> are equal;
	/// otherwise, <see langword="false"/>.</returns>
	public static bool operator ==(TagNode lhs, TagNode rhs)
	{
		return ReferenceEquals(lhs, rhs);
	}

	/// <summary>
	/// Determines if two <see cref="TagNode"/> objects are not equal.
	/// </summary>
	/// <param name="lhs">The first <see cref="TagNode"/> to compare.</param>
	/// <param name="rhs">The second <see cref="TagNode"/> to compare.</param>
	/// <returns><see langword="true"/> if the values of <paramref name="lhs"/> and <paramref name="rhs"/> are not
	/// equal; otherwise, <see langword="false"/>.</returns>
	public static bool operator !=(TagNode lhs, TagNode rhs)
	{
		return !(lhs == rhs);
	}

	/// <summary>
	/// Determines whether one <see cref="TagNode"/> object is less than another <see cref="TagNode"/>.
	/// </summary>
	/// <param name="lhs">The first <see cref="TagNode"/>.</param>
	/// <param name="rhs">The second <see cref="TagNode"/>.</param>
	/// <returns><see langword="true"/> if <paramref name="lhs"/> is less than <paramref name="rhs"/>; otherwise,
	/// <see langword="false"/>.</returns>
	public static bool operator <(TagNode lhs, TagNode rhs)
	{
		return lhs is null ? rhs is not null : lhs.CompareTo(rhs) < 0;
	}

	/// <summary>
	/// Determines whether one <see cref="TagNode"/> object is greater than another
	/// <see cref="TagNode"/>.
	/// </summary>
	/// <param name="lhs">The first <see cref="TagNode"/>.</param>
	/// <param name="rhs">The second <see cref="TagNode"/>.</param>
	/// <returns><see langword="true"/> if <paramref name="lhs"/> is greater than <paramref name="rhs"/>; otherwise,
	/// <see langword="false"/>.</returns>
	public static bool operator >(TagNode lhs, TagNode rhs)
	{
		return lhs?.CompareTo(rhs) > 0;
	}

	/// <summary>
	/// Determines whether one <see cref="TagNode"/> object is less than or equal to another
	/// <see cref="TagNode"/>.
	/// </summary>
	/// <param name="lhs">The first <see cref="TagNode"/>.</param>
	/// <param name="rhs">The second <see cref="TagNode"/>.</param>
	/// <returns><see langword="true"/> if <paramref name="lhs"/> is less than or equal <paramref name="rhs"/>;
	/// otherwise, <see langword="false"/>.</returns>
	public static bool operator <=(TagNode lhs, TagNode rhs)
	{
		return lhs is null || lhs.CompareTo(rhs) <= 0;
	}

	/// <summary>
	/// Determines whether one <see cref="TagNode"/> object is greater than or equal to another
	/// <see cref="TagNode"/>.
	/// </summary>
	/// <param name="lhs">The first <see cref="TagNode"/>.</param>
	/// <param name="rhs">The second <see cref="TagNode"/>.</param>
	/// <returns><see langword="true"/> if <paramref name="lhs"/> is greater than or equal <paramref name="rhs"/>;
	/// otherwise, <see langword="false"/>.</returns>
	public static bool operator >=(TagNode lhs, TagNode rhs)
	{
		return lhs is null ? rhs is null : lhs.CompareTo(rhs) >= 0;
	}
}
