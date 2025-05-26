// Copyright © Gamesmiths Guild.

using System.Diagnostics;
using System.Globalization;
using System.Text;
#if DEBUG
using System.Text.RegularExpressions;
#endif
using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Tags;

/// <summary>
/// A singleton class for efficiently managing all valid tags.
/// </summary>
public sealed class TagsManager
{
#if DEBUG
	private const string InvalidTagCharacters = @"\, ";
#endif

	private readonly Dictionary<Tag, TagNode> _tagNodeMap = [];

	private readonly List<TagNode> _networkTagNodeIndex = [];

	private bool _networkIndexInvalidated = true;

	/// <summary>
	/// Gets the current value for the net index of invalid tags.
	/// </summary>
	public ushort InvalidTagNetIndex { get; private set; }

	/// <summary>
	/// Gets the number of <see cref="TagNode"/> registered in this manager.
	/// </summary>
	public int NodesCount => _tagNodeMap.Count;

	/// <summary>
	/// Gets the numbers of bits to use for replicating container size.
	/// </summary>
	public int NumBitsForContainerSize { get; } = 6;

	/// <summary>
	/// Gets the root node for the tag tree.
	/// </summary>
	public TagNode RootNode { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="TagsManager"/> class from a list of strings.
	/// </summary>
	/// <param name="tags">List of tags to initialize.</param>
	public TagsManager(string[] tags)
	{
		RootNode = new TagNode(this);

		foreach (var tag in tags)
		{
			AddTagToTree(tag);
		}

		ConstructNetIndex();
	}

	/// <summary>
	/// Destroys the <see cref="TagNode"/> tree.
	/// </summary>
	public void DestroyTagTree()
	{
		_tagNodeMap.Clear();
		_networkTagNodeIndex.Clear();
		_networkIndexInvalidated = true;
		InvalidTagNetIndex = 1;
	}

	/// <summary>
	/// Gets a <see cref="TagContainer"/> with all registered tags.
	/// </summary>
	/// <remarks>Use <paramref name="explicitTagsOnly"/> to limit the container to explicit tags only.
	/// </remarks>
	/// <param name="explicitTagsOnly">Whether to include only explicit tags or not.</param>
	/// <returns>A <see cref="TagContainer"/> with all registered tags.</returns>
	public TagContainer RequestAllTags(bool explicitTagsOnly)
	{
		TagContainer tagContainer = new(this);

		foreach (TagNode? tagNode in _tagNodeMap.Select(x => x.Value))
		{
			if (!explicitTagsOnly || tagNode.IsExplicitTag)
			{
				tagContainer.AddTagFast(tagNode.CompleteTag);
			}
		}

		return tagContainer;
	}

	/// <summary>
	/// Returns the count of parent tags for a given <see cref="Tag"/>. This can be a quick way to gauge
	/// how "specific" a tag is without verifying if they share the same hierarchy.
	/// </summary>
	/// <remarks>
	/// Example: "enemy.undead" contains 2 <see cref="TagNode"/>s, while "enemy.undead.zombie" contains 3
	/// <see cref="TagNode"/>s.
	/// </remarks>
	/// <param name="tag">The <see cref="Tag"/> to retrieve the number of <see cref="TagNode"/>s from.
	/// </param>
	/// <returns>The number of parent tags for the given <see cref="Tag"/>.</returns>
	public int GetNumberOfTagNodes(Tag tag)
	{
		var count = 0;

		TagNode? tagNode = FindTagNode(tag);
		while (tagNode is not null)
		{
			++count;
			tagNode = tagNode.ParentTagNode;
		}

		return count;
	}

	/// <summary>
	/// Splits a <see cref="Tag"/>, such as "enemy.undead.zombie", into an array of segments such as {"enemy",
	/// "undead", "zombie"}.
	/// </summary>
	/// <param name="tag">The <see cref="Tag"/> to split into its component keys.</param>
	/// <returns>An array of <see cref="string"/> with each key segment from the <see cref="Tag"/> hierarchy.
	/// </returns>
	public string[] SplitTagKey(Tag tag)
	{
		var outKeys = new List<string>();
		TagNode? currentNode = FindTagNode(tag);
		while (currentNode is not null)
		{
			outKeys.Insert(0, currentNode.TagKey.ToString());
			currentNode = currentNode.ParentTagNode;
		}

		return [.. outKeys];
	}

	/// <summary>
	/// Creates a <see cref="TagContainer"/> containing the tags specified by the keys in
	/// <paramref name="tags"/>.
	/// </summary>
	/// <param name="tags">An array of tag keys to include.</param>
	/// <param name="errorIfNotFound">If <see langword="true"/>, throws an exception for any tag not found.</param>
	/// <returns>A <see cref="TagContainer"/> containing the matching tags.</returns>
	/// <exception cref="TagNotRegisteredException">Throws if any tag is not found and
	/// <paramref name="errorIfNotFound"/>is set to <see langword="true"/>.</exception>
	public TagContainer RequestTagContainer(string[] tags, bool errorIfNotFound = true)
	{
		TagContainer tagContainer = new(this);

		foreach (var tagString in tags)
		{
			Tag requestedTag = RequestTag(tagString, errorIfNotFound);

			if (requestedTag != Tag.Empty)
			{
				tagContainer.AddTag(requestedTag);
			}
		}

		return tagContainer;
	}

	/// <summary>
	/// Creates a new <see cref="TagContainer"/> that contains the supplied <see cref="Tag"/> along with
	/// all of its parent tags.
	/// </summary>
	/// <remarks>
	/// For example, calling this on "enemy.undead.zombie" would produce a <see cref="TagContainer"/> containing
	/// "enemy.undead.zombie", "enemy.undead", and "enemy".
	/// </remarks>
	/// <param name="tag">The <see cref="Tag"/> to use at the child most tag for this container.</param>
	/// <returns>A <see cref="TagContainer"/> with this tag and all of its parent tags added explicitly, or an
	/// empty container in case of failure.</returns>
	public TagContainer RequestTagParents(Tag tag)
	{
		TagContainer parentTags = GetSingleTagContainer(tag);

		if (!parentTags.IsEmpty)
		{
			return parentTags.GetExplicitTagParents();
		}

		return new TagContainer(this);
	}

	/// <summary>
	/// Creates a <see cref="TagContainer"/> containing all tags that are children of this tag in the hierarchy,
	/// omitting the original tag.
	/// </summary>
	/// <param name="tag">The root <see cref="Tag"/> to use as the starting point.</param>
	/// <returns>A <see cref="TagContainer"/> that includes all child tags explicitly.</returns>
	public TagContainer RequestTagChildren(Tag tag)
	{
		TagContainer tagContainer = new(this);

		// Intentionally leaves out the input tag from the returned container.
		TagNode? tagNode = FindTagNode(tag);
		if (tagNode is not null)
		{
			AddChildrenTags(tagContainer, tagNode, true);
		}

		return tagContainer;
	}

	/// <inheritdoc />
	public override string ToString()
	{
		var result = new StringBuilder();

		foreach (TagNode node in _networkTagNodeIndex)
		{
			result.AppendLine(node.CompleteTagKey);
		}

		return result.ToString();
	}

#if DEBUG
	internal static bool IsValidTagKey(string tagString, out string error, out string fixedString)
	{
		fixedString = tagString;

		if (string.IsNullOrEmpty(fixedString))
		{
			error = "Tag key is empty";
			return false;
		}

		var isValid = true;
		var errorStringBuilder = new StringBuilder();

		while (fixedString.StartsWith('.'))
		{
			errorStringBuilder.AppendLine("Tag keys can't start with '.'");
			fixedString = fixedString[1..];
			isValid = false;
		}

		while (fixedString.EndsWith('.'))
		{
			errorStringBuilder.AppendLine("Tag keys can't end with '.'");
			fixedString = fixedString[..^1];
			isValid = false;
		}

		if (fixedString.StartsWith(' ') || fixedString.EndsWith(' '))
		{
			errorStringBuilder.AppendLine("Tag keys can't start or end with space");
			fixedString = fixedString.Trim();
			isValid = false;
		}

		if (Regex.IsMatch(fixedString, $"[{Regex.Escape(InvalidTagCharacters)}]"))
		{
			errorStringBuilder.AppendLine("Tag key has invalid characters");
			fixedString = Regex.Replace(fixedString, $"[{Regex.Escape(InvalidTagCharacters)}]", "_");
			isValid = false;
		}

		error = errorStringBuilder.ToString();

		return isValid;
	}
#endif

	internal TagNode? FindTagNode(Tag tag)
	{
		if (_tagNodeMap.TryGetValue(tag, out TagNode? tagNode))
		{
			return tagNode;
		}

		return null;
	}

	internal Tag RequestTag(StringKey tagKey, bool errorIfNotFound)
	{
		var possibleTag = new Tag(this, tagKey);

		if (_tagNodeMap.ContainsKey(possibleTag))
		{
			return possibleTag;
		}

		if (errorIfNotFound)
		{
			throw new TagNotRegisteredException(tagKey);
		}

		return Tag.Empty;
	}

	internal int TagsMatchDepth(Tag tagA, Tag tagB)
	{
		var tags1 = new HashSet<StringKey>();
		var tags2 = new HashSet<StringKey>();

		TagNode? tagNode = FindTagNode(tagA);
		if (tagNode is not null)
		{
			tags1 = GetAllParentNodeKeys(tagNode);
		}

		tagNode = FindTagNode(tagB);
		if (tagNode is not null)
		{
			tags2 = GetAllParentNodeKeys(tagNode);
		}

		return tags1.Intersect(tags2).ToList().Count;
	}

	internal HashSet<Tag> ExtractParentTags(Tag tag)
	{
		var parentTags = new HashSet<Tag>();

		if (!tag.IsValid)
		{
			return parentTags;
		}

		if (_tagNodeMap.TryGetValue(tag, out TagNode? tagNode))
		{
			TagContainer singleContainer = tagNode.SingleTagContainer;
			foreach (Tag tagParent in singleContainer.ParentTags)
			{
				parentTags.Add(tagParent);
			}
#if DEBUG
			var validationCopy = new HashSet<Tag>(parentTags);
			validationCopy.UnionWith(tag.ParseParentTags());
			Debug.Assert(
				validationCopy.SetEquals(parentTags),
				$"ExtractParentTags results are inconsistent for tag \"{tag}\"");
#endif
		}

		return parentTags;
	}

	internal Tag RequestTagDirectParent(Tag tag)
	{
		if (_tagNodeMap.TryGetValue(tag, out TagNode? tagNode))
		{
			TagNode? parentTag = tagNode.ParentTagNode;
			if (parentTag is not null)
			{
				return parentTag.CompleteTag;
			}
		}

		return Tag.Empty;
	}

	internal ushort GetNetIndexFromTag(Tag tag)
	{
		VerifyNetworkIndex();

		TagNode? tagNode = FindTagNode(tag);

		if (tagNode is not null)
		{
			return tagNode.NetIndex;
		}

		return InvalidTagNetIndex;
	}

	internal StringKey GetTagKeyFromNetIndex(ushort tagIndex)
	{
		VerifyNetworkIndex();

		if (tagIndex >= _networkTagNodeIndex.Count)
		{
			if (tagIndex != InvalidTagNetIndex)
			{
				throw new InvalidTagNetIndexException(tagIndex);
			}

			return StringKey.Empty;
		}

		return _networkTagNodeIndex[tagIndex].CompleteTagKey;
	}

	private static HashSet<StringKey> GetAllParentNodeKeys(TagNode tagNode)
	{
		var keysList = new HashSet<StringKey>();
		TagNode? currentNode = tagNode;

		while (currentNode is not null)
		{
			keysList.Add(currentNode.CompleteTagKey);
			currentNode = currentNode.ParentTagNode;
		}

		return keysList;
	}

	private static void AddChildrenTags(
		TagContainer container,
		TagNode tagNode,
		bool recurseAll)
	{
		if (tagNode is not null)
		{
			foreach (TagNode? childNode in tagNode.ChildTags.OfType<TagNode>())
			{
				container.AddTag(childNode.CompleteTag);

				if (recurseAll)
				{
					AddChildrenTags(container, childNode, true);
				}
			}
		}
	}

	private void AddTagToTree(string tagKey)
	{
		TagNode currentNode = RootNode;

		StringKey originalTagKey = tagKey;
		var fullTagString = new StringBuilder(tagKey);

#if DEBUG
		if (!IsValidTagKey(fullTagString.ToString(), out var outError, out var outFixedString))
		{
			if (string.IsNullOrEmpty(outFixedString))
			{
				return;
			}

			fullTagString = new StringBuilder(outFixedString);
			originalTagKey = fullTagString.ToString();
		}
#endif

		var subTags = fullTagString.ToString().Split('.');

		fullTagString.Clear();
		var numSubTags = subTags.Length;

		for (var i = 0; i < numSubTags; i++)
		{
			var isExplicitTag = i == (numSubTags - 1);
			StringKey shortTagKey = subTags[i];
			StringKey fullTagKey;

			if (isExplicitTag)
			{
				// We already know the final key.
				fullTagKey = originalTagKey;
			}
			else if (i == 0)
			{
				// Full tag is the same as short tag, and start building full tag string.
				fullTagKey = shortTagKey;
				fullTagString = new StringBuilder(subTags[i]);
			}
			else
			{
				// Add .Tag and use that as full tag.
				fullTagString.AppendFormat(CultureInfo.InvariantCulture, ".{0}", subTags[i]);
				fullTagKey = fullTagString.ToString();
			}

			List<TagNode> childTags = currentNode.ChildTags;
			var insertionIdx = InsertTagIntoNodeArray(shortTagKey, fullTagKey, currentNode, childTags, isExplicitTag);

			currentNode = childTags[insertionIdx];
		}
	}

	private int InsertTagIntoNodeArray(
		StringKey tagKey,
		StringKey fullTagKey,
		TagNode parentNode,
		List<TagNode> nodeArray,
		bool isExplicitTag)
	{
		int? foundNodeIdx = null;
		int? whereToInsert = null;

		// See if the tag is already in the array.
		var lowerBoundIndex = 0;
		if (nodeArray.Count > 0)
		{
			lowerBoundIndex = nodeArray.FindIndex(x =>
			{
				return x.TagKey >= tagKey;
			});

			if (lowerBoundIndex < 0)
			{
				lowerBoundIndex = nodeArray.Count;
			}
		}

		if (lowerBoundIndex < nodeArray.Count)
		{
			TagNode currentNode = nodeArray[lowerBoundIndex];
			if (currentNode.TagKey == tagKey)
			{
				foundNodeIdx = lowerBoundIndex;
			}
			else
			{
				// Insert new node before this.
				whereToInsert = lowerBoundIndex;
			}
		}

		if (foundNodeIdx is null)
		{
			// Insert at end.
			whereToInsert ??= nodeArray.Count;

			// Don't add the root node as parent.
			var tagNode = new TagNode(
				this,
				tagKey,
				fullTagKey,
				parentNode != RootNode ? parentNode : null,
				isExplicitTag);

			// Add at the sorted location.
			nodeArray.Insert((int)whereToInsert, tagNode);
			foundNodeIdx = whereToInsert;

			Tag tag = tagNode.CompleteTag;

			_tagNodeMap.Add(tag, tagNode);

			_networkIndexInvalidated = true;

			Debug.Assert(
				tag.TagKey == fullTagKey,
				$"tag.TagKey: \"{tag.TagKey}\" and fullTagKey: \"{fullTagKey}\" should always match.");
		}

		return (int)foundNodeIdx;
	}

	private TagContainer GetSingleTagContainer(Tag tag)
	{
		if (_tagNodeMap.TryGetValue(tag, out TagNode? tagNode))
		{
			return tagNode.SingleTagContainer;
		}

		return new TagContainer(this);
	}

	private void VerifyNetworkIndex()
	{
		if (_networkIndexInvalidated)
		{
			ConstructNetIndex();
		}
	}

	private void ConstructNetIndex()
	{
		_networkIndexInvalidated = false;

		_networkTagNodeIndex.Clear();

		_networkTagNodeIndex.AddRange(_tagNodeMap.Values);

		_networkTagNodeIndex.Sort();

		checked
		{
			InvalidTagNetIndex = (ushort)(_networkTagNodeIndex.Count + 1);
		}

		// Should make some checks.
		// Then move commonly replicated tags to the beginning for optimization.
		// For now I'm naively serializing everything as is.
		for (ushort i = 0; i < _networkTagNodeIndex.Count; ++i)
		{
			if (_networkTagNodeIndex[i] is not null)
			{
				_networkTagNodeIndex[i].NetIndex = i;
			}
			else
			{
				throw new InvalidTagNetIndexException(i);
			}
		}
	}
}
