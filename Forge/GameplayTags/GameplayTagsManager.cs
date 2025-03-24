// Copyright © 2025 Gamesmiths Guild.

using System.Diagnostics;
using System.Globalization;
using System.Text;
#if DEBUG
using System.Text.RegularExpressions;
#endif
using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.GameplayTags;

/// <summary>
/// A singleton class for efficiently managing all valid tags.
/// </summary>
public sealed class GameplayTagsManager
{
#if DEBUG
	private const string InvalidTagCharacters = @"\, ";
#endif

	private readonly Dictionary<GameplayTag, GameplayTagNode> _gameplayTagNodeMap = [];

	private readonly List<GameplayTagNode> _networkGameplayTagNodeIndex = [];

	private bool _networkIndexInvalidated = true;

	/// <summary>
	/// Gets the current value for the net index of invalid tags.
	/// </summary>
	public ushort InvalidTagNetIndex { get; private set; }

	/// <summary>
	/// Gets the number of <see cref="GameplayTagNode"/> registered in this manager.
	/// </summary>
	public int NodesCount => _gameplayTagNodeMap.Count;

	/// <summary>
	/// Gets the numbers of bits to use for replicating container size.
	/// </summary>
	public int NumBitsForContainerSize { get; } = 6;

	/// <summary>
	/// Gets the root node for the tag tree.
	/// </summary>
	public GameplayTagNode RootNode { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="GameplayTagsManager"/> class from a list of strings.
	/// </summary>
	/// <param name="tags">List of tags to initialize.</param>
	public GameplayTagsManager(string[] tags)
	{
		RootNode = new GameplayTagNode(this);

		foreach (var tag in tags)
		{
			AddTagToTree(tag);
		}

		ConstructNetIndex();
	}

	/// <summary>
	/// Destroys the <see cref="GameplayTagNode"/> tree.
	/// </summary>
	public void DestroyTagTree()
	{
		_gameplayTagNodeMap.Clear();
		_networkGameplayTagNodeIndex.Clear();
		_networkIndexInvalidated = true;
		InvalidTagNetIndex = 1;
	}

	/// <summary>
	/// Gets a <see cref="GameplayTagContainer"/> with all registered tags.
	/// </summary>
	/// <remarks>Use <paramref name="explicitTagsOnly"/> to limit the container to explicit tags only.
	/// </remarks>
	/// <param name="explicitTagsOnly">Whether to include only explicit tags or not.</param>
	/// <returns>A <see cref="GameplayTagContainer"/> with all registered tags.</returns>
	public GameplayTagContainer RequestAllTags(bool explicitTagsOnly)
	{
		GameplayTagContainer tagContainer = new(this);

		foreach (GameplayTagNode? tagNode in _gameplayTagNodeMap.Select(x => x.Value))
		{
			if (!explicitTagsOnly || tagNode.IsExplicitTag)
			{
				tagContainer.AddTagFast(tagNode.CompleteTag);
			}
		}

		return tagContainer;
	}

	/// <summary>
	/// Returns the count of parent tags for a given <see cref="GameplayTag"/>. This can be a quick way to gauge
	/// how "specific" a tag is without verifying if they share the same hierarchy.
	/// </summary>
	/// <remarks>
	/// Example: "enemy.undead" contains 2 <see cref="GameplayTagNode"/>s, while "enemy.undead.zombie" contains 3
	/// <see cref="GameplayTagNode"/>s.
	/// </remarks>
	/// <param name="tag">The <see cref="GameplayTag"/> to retrieve the number of <see cref="GameplayTagNode"/>s from.
	/// </param>
	/// <returns>The number of parent tags for the given <see cref="GameplayTag"/>.</returns>
	public int GetNumberOfTagNodes(GameplayTag tag)
	{
		var count = 0;

		GameplayTagNode? tagNode = FindTagNode(tag);
		while (tagNode is not null)
		{
			++count;
			tagNode = tagNode.ParentTagNode;
		}

		return count;
	}

	/// <summary>
	/// Splits a <see cref="GameplayTag"/>, such as "enemy.undead.zombie", into an array of segments such as {"enemy",
	/// "undead", "zombie"}.
	/// </summary>
	/// <param name="tag">The <see cref="GameplayTag"/> to split into its component keys.</param>
	/// <returns>An array of <see cref="string"/> with each key segment from the <see cref="GameplayTag"/> hierarchy.
	/// </returns>
	public string[] SplitTagKey(GameplayTag tag)
	{
		var outKeys = new List<string>();
		GameplayTagNode? currentNode = FindTagNode(tag);
		while (currentNode is not null)
		{
			outKeys.Insert(0, currentNode.TagKey.ToString());
			currentNode = currentNode.ParentTagNode;
		}

		return [.. outKeys];
	}

	/// <summary>
	/// Creates a <see cref="GameplayTagContainer"/> containing the tags specified by the keys in
	/// <paramref name="tags"/>.
	/// </summary>
	/// <param name="tags">An array of tag keys to include.</param>
	/// <param name="errorIfNotFound">If <see langword="true"/>, throws an exception for any tag not found.</param>
	/// <returns>A <see cref="GameplayTagContainer"/> containing the matching tags.</returns>
	/// <exception cref="GameplayTagNotRegisteredException">Throws if any tag is not found and
	/// <paramref name="errorIfNotFound"/>is set to <see langword="true"/>.</exception>
	public GameplayTagContainer RequestTagContainer(string[] tags, bool errorIfNotFound = true)
	{
		GameplayTagContainer gameplayTagContainer = new(this);

		foreach (var tagString in tags)
		{
			GameplayTag requestedTag = RequestTag(tagString, errorIfNotFound);

			if (requestedTag != GameplayTag.Empty)
			{
				gameplayTagContainer.AddTag(requestedTag);
			}
		}

		return gameplayTagContainer;
	}

	/// <summary>
	/// Creates a new <see cref="GameplayTagContainer"/> that contains the supplied <see cref="GameplayTag"/> along with
	/// all of its parent tags.
	/// </summary>
	/// <remarks>
	/// For example, calling this on "enemy.undead.zombie" would produce a <see cref="GameplayTagContainer"/> containing
	/// "enemy.undead.zombie", "enemy.undead", and "enemy".
	/// </remarks>
	/// <param name="tag">The <see cref="GameplayTag"/> to use at the child most tag for this container.</param>
	/// <returns>A <see cref="GameplayTagContainer"/> with this tag and all of its parent tags added explicitly, or an
	/// empty container in case of failure.</returns>
	public GameplayTagContainer RequestTagParents(GameplayTag tag)
	{
		GameplayTagContainer parentTags = GetSingleTagContainer(tag);

		if (!parentTags.IsEmpty)
		{
			return parentTags.GetExplicitGameplayTagParents();
		}

		return new GameplayTagContainer(this);
	}

	/// <summary>
	/// Creates a <see cref="GameplayTagContainer"/> containing all tags that are children of this tag in the hierarchy,
	/// omitting the original tag.
	/// </summary>
	/// <param name="tag">The root <see cref="GameplayTag"/> to use as the starting point.</param>
	/// <returns>A <see cref="GameplayTagContainer"/> that includes all child tags explicitly.</returns>
	public GameplayTagContainer RequestTagChildren(GameplayTag tag)
	{
		GameplayTagContainer tagContainer = new(this);

		// Intentionally leaves out the input GameplayTag from the returned container.
		GameplayTagNode? gameplayTagNode = FindTagNode(tag);
		if (gameplayTagNode is not null)
		{
			AddChildrenTags(tagContainer, gameplayTagNode, true);
		}

		return tagContainer;
	}

	/// <inheritdoc />
	public override string ToString()
	{
		var result = new StringBuilder();

		foreach (GameplayTagNode node in _networkGameplayTagNodeIndex)
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

	internal GameplayTagNode? FindTagNode(GameplayTag tag)
	{
		if (_gameplayTagNodeMap.TryGetValue(tag, out GameplayTagNode? gameplayTagNode))
		{
			return gameplayTagNode;
		}

		return null;
	}

	internal GameplayTag RequestTag(StringKey tagKey, bool errorIfNotFound)
	{
		var possibleTag = new GameplayTag(this, tagKey);

		if (_gameplayTagNodeMap.ContainsKey(possibleTag))
		{
			return possibleTag;
		}

		if (errorIfNotFound)
		{
			throw new GameplayTagNotRegisteredException(tagKey);
		}

		return GameplayTag.Empty;
	}

	internal int GameplayTagsMatchDepth(GameplayTag tagA, GameplayTag tagB)
	{
		var tags1 = new HashSet<StringKey>();
		var tags2 = new HashSet<StringKey>();

		GameplayTagNode? tagNode = FindTagNode(tagA);
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

	internal HashSet<GameplayTag> ExtractParentTags(GameplayTag tag)
	{
		var parentTags = new HashSet<GameplayTag>();

		if (!tag.IsValid)
		{
			return parentTags;
		}

		if (_gameplayTagNodeMap.TryGetValue(tag, out GameplayTagNode? gameplayTagNode))
		{
			GameplayTagContainer singleContainer = gameplayTagNode.SingleTagContainer;
			foreach (GameplayTag tagParent in singleContainer.ParentTags)
			{
				parentTags.Add(tagParent);
			}
#if DEBUG
			var validationCopy = new HashSet<GameplayTag>(parentTags);
			validationCopy.UnionWith(tag.ParseParentTags());
			Debug.Assert(
				validationCopy.SetEquals(parentTags),
				$"ExtractParentTags results are inconsistent for tag \"{tag}\"");
#endif
		}

		return parentTags;
	}

	internal GameplayTag RequestTagDirectParent(GameplayTag gameplayTag)
	{
		if (_gameplayTagNodeMap.TryGetValue(gameplayTag, out GameplayTagNode? gameplayTagNode))
		{
			GameplayTagNode? parentTag = gameplayTagNode.ParentTagNode;
			if (parentTag is not null)
			{
				return parentTag.CompleteTag;
			}
		}

		return GameplayTag.Empty;
	}

	internal ushort GetNetIndexFromTag(GameplayTag tag)
	{
		VerifyNetworkIndex();

		GameplayTagNode? gameplayTagNode = FindTagNode(tag);

		if (gameplayTagNode is not null)
		{
			return gameplayTagNode.NetIndex;
		}

		return InvalidTagNetIndex;
	}

	internal StringKey GetTagKeyFromNetIndex(ushort tagIndex)
	{
		VerifyNetworkIndex();

		if (tagIndex >= _networkGameplayTagNodeIndex.Count)
		{
			if (tagIndex != InvalidTagNetIndex)
			{
				throw new InvalidTagNetIndexException(tagIndex);
			}

			return StringKey.Empty;
		}

		return _networkGameplayTagNodeIndex[tagIndex].CompleteTagKey;
	}

	private static HashSet<StringKey> GetAllParentNodeKeys(GameplayTagNode tagNode)
	{
		var keysList = new HashSet<StringKey>();
		GameplayTagNode? currentNode = tagNode;

		while (currentNode is not null)
		{
			keysList.Add(currentNode.CompleteTagKey);
			currentNode = currentNode.ParentTagNode;
		}

		return keysList;
	}

	private static void AddChildrenTags(
		GameplayTagContainer container,
		GameplayTagNode tagNode,
		bool recurseAll)
	{
		if (tagNode is not null)
		{
			foreach (GameplayTagNode? childNode in tagNode.ChildTags.OfType<GameplayTagNode>())
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
		GameplayTagNode currentNode = RootNode;

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
				fullTagString.Append(CultureInfo.InvariantCulture, $".{subTags[i]}");
				fullTagKey = fullTagString.ToString();
			}

			List<GameplayTagNode> childTags = currentNode.ChildTags;
			var insertionIdx = InsertTagIntoNodeArray(shortTagKey, fullTagKey, currentNode, childTags, isExplicitTag);

			currentNode = childTags[insertionIdx];
		}
	}

	private int InsertTagIntoNodeArray(
		StringKey tagKey,
		StringKey fullTagKey,
		GameplayTagNode parentNode,
		List<GameplayTagNode> nodeArray,
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
			GameplayTagNode currentNode = nodeArray[lowerBoundIndex];
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
			var tagNode = new GameplayTagNode(
				this,
				tagKey,
				fullTagKey,
				parentNode != RootNode ? parentNode : null,
				isExplicitTag);

			// Add at the sorted location.
			nodeArray.Insert((int)whereToInsert, tagNode);
			foundNodeIdx = whereToInsert;

			GameplayTag gameplayTag = tagNode.CompleteTag;

			_gameplayTagNodeMap.Add(gameplayTag, tagNode);

			_networkIndexInvalidated = true;

			Debug.Assert(
				gameplayTag.TagKey == fullTagKey,
				$"gameplayTag.TagKey: \"{gameplayTag.TagKey}\" and fullTagKey: \"{fullTagKey}\" should always match.");
		}

		return (int)foundNodeIdx;
	}

	private GameplayTagContainer GetSingleTagContainer(GameplayTag tag)
	{
		if (_gameplayTagNodeMap.TryGetValue(tag, out GameplayTagNode? gameplayTagNode))
		{
			return gameplayTagNode.SingleTagContainer;
		}

		return new GameplayTagContainer(this);
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

		_networkGameplayTagNodeIndex.Clear();

		_networkGameplayTagNodeIndex.AddRange(_gameplayTagNodeMap.Values);

		_networkGameplayTagNodeIndex.Sort();

		checked
		{
			InvalidTagNetIndex = (ushort)(_networkGameplayTagNodeIndex.Count + 1);
		}

		// Should make some checks.
		// Then move commonly replicated tags to the beginning for optimization.
		// For now I'm naively serializing everything as is.
		for (ushort i = 0; i < _networkGameplayTagNodeIndex.Count; ++i)
		{
			if (_networkGameplayTagNodeIndex[i] is not null)
			{
				_networkGameplayTagNodeIndex[i].NetIndex = i;
			}
			else
			{
				throw new InvalidTagNetIndexException(i);
			}
		}
	}
}
