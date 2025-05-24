// Copyright © Gamesmiths Guild.

using System.Diagnostics;
using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Tags;

/// <summary>
/// Supports a streamlined, fluent syntax to configure the type and parameters for a <see cref="TagQuery"/>.
/// </summary>
/// <param name="tagsManager">The manager for handling tags for this query expression.
/// </param>
public class TagQueryExpression(TagsManager tagsManager)
{
	private readonly TagsManager _tagsManager = tagsManager;

	private readonly List<TagQueryExpression> _expressionSet = [];

	private readonly List<Tag> _tagSet = [];

	private TagQueryExpressionType _expressionType;

	/// <summary>
	/// Sets this query's expression type to <see cref="TagQueryExpressionType.AnyTagsMatch"/>.
	/// </summary>
	/// <returns>This <see cref="TagQueryExpression"/> itself.</returns>
	public TagQueryExpression AnyTagsMatch()
	{
		_expressionType = TagQueryExpressionType.AnyTagsMatch;
		return this;
	}

	/// <summary>
	/// Sets this query's expression type to <see cref="TagQueryExpressionType.AllTagsMatch"/>.
	/// </summary>
	/// <returns>This <see cref="TagQueryExpression"/> itself.</returns>
	public TagQueryExpression AllTagsMatch()
	{
		_expressionType = TagQueryExpressionType.AllTagsMatch;
		return this;
	}

	/// <summary>
	/// Sets this query's expression type to <see cref="TagQueryExpressionType.NoTagsMatch"/>.
	/// </summary>
	/// <returns>This <see cref="TagQueryExpression"/> itself.</returns>
	public TagQueryExpression NoTagsMatch()
	{
		_expressionType = TagQueryExpressionType.NoTagsMatch;
		return this;
	}

	/// <summary>
	/// Sets this query's expression type to <see cref="TagQueryExpressionType.AnyTagsMatchExact"/>.
	/// </summary>
	/// <returns>This <see cref="TagQueryExpression"/> itself.</returns>
	public TagQueryExpression AnyTagsMatchExact()
	{
		_expressionType = TagQueryExpressionType.AnyTagsMatchExact;
		return this;
	}

	/// <summary>
	/// Sets this query's expression type to <see cref="TagQueryExpressionType.AllTagsMatchExact"/>.
	/// </summary>
	/// <returns>This <see cref="TagQueryExpression"/> itself.</returns>
	public TagQueryExpression AllTagsMatchExact()
	{
		_expressionType = TagQueryExpressionType.AllTagsMatchExact;
		return this;
	}

	/// <summary>
	/// Sets this query's expression type to <see cref="TagQueryExpressionType.NoTagsMatchExact"/>.
	/// </summary>
	/// <returns>This <see cref="TagQueryExpression"/> itself.</returns>
	public TagQueryExpression NoTagsMatchExact()
	{
		_expressionType = TagQueryExpressionType.NoTagsMatchExact;
		return this;
	}

	/// <summary>
	/// Sets this query's expression type to <see cref="TagQueryExpressionType.AnyExpressionsMatch"/>.
	/// </summary>
	/// <returns>This <see cref="TagQueryExpression"/> itself.</returns>
	public TagQueryExpression AnyExpressionsMatch()
	{
		_expressionType = TagQueryExpressionType.AnyExpressionsMatch;
		return this;
	}

	/// <summary>
	/// Sets this query's expression type to <see cref="TagQueryExpressionType.AllExpressionsMatch"/>.
	/// </summary>
	/// <returns>This <see cref="TagQueryExpression"/> itself.</returns>
	public TagQueryExpression AllExpressionsMatch()
	{
		_expressionType = TagQueryExpressionType.AllExpressionsMatch;
		return this;
	}

	/// <summary>
	/// Sets this query's expression type to <see cref="TagQueryExpressionType.NoExpressionsMatch"/>.
	/// </summary>
	/// <returns>This <see cref="TagQueryExpression"/> itself.</returns>
	public TagQueryExpression NoExpressionsMatch()
	{
		_expressionType = TagQueryExpressionType.NoExpressionsMatch;
		return this;
	}

	/// <summary>
	/// Adds a tag to this expression.
	/// </summary>
	/// <param name="tagKey"><see cref="StringKey"/> that represents the tag to be added.</param>
	/// <returns>This <see cref="TagQueryExpression"/> itself.</returns>
	public TagQueryExpression AddTag(StringKey tagKey)
	{
		return AddTag(Tag.RequestTag(_tagsManager, tagKey));
	}

	/// <summary>
	/// Adds a tag to this expression.
	/// </summary>
	/// <param name="tag"><see cref="Tag"/> to be added.</param>
	/// <returns>This <see cref="TagQueryExpression"/> itself.</returns>
	public TagQueryExpression AddTag(Tag tag)
	{
		Debug.Assert(UsesTagSet(), "Shouldn't be adding expressions to a query of TagSet type.");

		_tagSet.Add(tag);
		return this;
	}

	/// <summary>
	/// Adds multiple tags to this expression.
	/// </summary>
	/// <param name="tags"><see cref="TagContainer"/> containing the tags to be added.</param>
	/// <returns>This <see cref="TagContainer"/> itself.</returns>
	public TagQueryExpression AddTags(TagContainer tags)
	{
		Debug.Assert(UsesTagSet(), "Shouldn't be adding expressions to a query of TagSet type.");

		_tagSet.AddRange(tags.Tags);
		return this;
	}

	/// <summary>
	/// Adds a expression to this expression.
	/// </summary>
	/// <param name="expression"><see cref="TagQueryExpression"/> to be added.</param>
	/// <returns>This <see cref="TagQueryExpression"/> itself.</returns>
	public TagQueryExpression AddExpression(TagQueryExpression expression)
	{
		Debug.Assert(UsesExpressionSet(), "Shouldn't be adding tags to a query of ExpressionSet type.");

		_expressionSet.Add(expression);
		return this;
	}

	internal void EmitTokens(List<byte> tokenStream, List<Tag> tagDictionary)
	{
		// Emit expression type.
		tokenStream.Add((byte)_expressionType);

		// Emit expression data.
		switch (_expressionType)
		{
			case TagQueryExpressionType.AnyTagsMatch:
			case TagQueryExpressionType.AllTagsMatch:
			case TagQueryExpressionType.NoTagsMatch:
			case TagQueryExpressionType.AnyTagsMatchExact:
			case TagQueryExpressionType.AllTagsMatchExact:
			case TagQueryExpressionType.NoTagsMatchExact:
				// Emit tag set.
				var numTags = (byte)_tagSet.Count;
				tokenStream.Add(numTags);

				foreach (Tag tag in _tagSet)
				{
					var tagIndex = tagDictionary.AddUnique(tag);

					// Token 255 is reserved for internal use, so 254 is max unique tags.
					Debug.Assert(tagIndex <= 254, "Stream can't hold more than 254 tags.");

					tokenStream.Add((byte)tagIndex);
				}

				break;

			case TagQueryExpressionType.AnyExpressionsMatch:
			case TagQueryExpressionType.AllExpressionsMatch:
			case TagQueryExpressionType.NoExpressionsMatch:
				// Emit expression set.
				var numExpressions = (byte)_expressionSet.Count;
				tokenStream.Add(numExpressions);

				foreach (TagQueryExpression expression in _expressionSet)
				{
					expression.EmitTokens(tokenStream, tagDictionary);
				}

				break;

			case TagQueryExpressionType.Undefined:
				Debug.Fail($"{typeof(TagQueryExpressionType)} should never be set as" +
					$"{TagQueryExpressionType.Undefined}.");
				break;
		}
	}

	private bool UsesTagSet()
	{
		return (_expressionType == TagQueryExpressionType.AllTagsMatch)
			|| (_expressionType == TagQueryExpressionType.AnyTagsMatch)
			|| (_expressionType == TagQueryExpressionType.NoTagsMatch)
			|| (_expressionType == TagQueryExpressionType.AllTagsMatchExact)
			|| (_expressionType == TagQueryExpressionType.AnyTagsMatchExact)
			|| (_expressionType == TagQueryExpressionType.NoTagsMatchExact);
	}

	private bool UsesExpressionSet()
	{
		return (_expressionType == TagQueryExpressionType.AllExpressionsMatch)
			|| (_expressionType == TagQueryExpressionType.AnyExpressionsMatch)
			|| (_expressionType == TagQueryExpressionType.NoExpressionsMatch);
	}
}
