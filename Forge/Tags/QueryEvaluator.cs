// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Tags;

/// <summary>
/// Utility class for parsing and evaluating query tokens.
/// </summary>
/// <param name="query">The <see cref="TagQuery"/> to be used by this evaluator.</param>
internal sealed class QueryEvaluator(TagQuery query)
{
	private readonly TagQuery _query = query;

	private int _curStreamIdx;
	private bool _readError;

	/// <summary>
	/// Evaluates the given <see cref="TagContainer"/> against the provided <see cref="TagQuery"/>.
	/// </summary>
	/// <param name="container">The <see cref="TagContainer"/> containing the tags to be checked.</param>
	/// <returns><see langword="true"/> if the query is satisfied by the tags in the container; <see langword="false"/>
	/// otherwise.</returns>
	public bool Evaluate(TagContainer container)
	{
		_curStreamIdx = 0;

		bool returnValue = false;

		byte hasRootExpression = GetToken();
		if (!_readError && hasRootExpression != 0)
		{
			returnValue = EvaluateExpression(container);
		}

		Validation.Assert(
			_curStreamIdx == _query.QueryTokenStream.Count,
			"There shouldn't be remaining tokens into the stream.");

		return returnValue;
	}

	private static bool HasTag(TagContainer tags, Tag tag, bool exactMatch)
	{
		if (exactMatch)
		{
			return tags.HasTagExact(tag);
		}

		return tags.HasTag(tag);
	}

	private bool EvaluateExpression(TagContainer tags, bool skip = false)
	{
		// Emit expression data.
		switch ((TagQueryExpressionType)GetToken())
		{
			case TagQueryExpressionType.AnyTagsMatch:
				return EvaluateAnyTagsMatch(tags, false, skip);
			case TagQueryExpressionType.AllTagsMatch:
				return EvaluateAllTagsMatch(tags, false, skip);
			case TagQueryExpressionType.NoTagsMatch:
				return EvaluateNoTagsMatch(tags, false, skip);

			case TagQueryExpressionType.AnyTagsMatchExact:
				return EvaluateAnyTagsMatch(tags, true, skip);
			case TagQueryExpressionType.AllTagsMatchExact:
				return EvaluateAllTagsMatch(tags, true, skip);
			case TagQueryExpressionType.NoTagsMatchExact:
				return EvaluateNoTagsMatch(tags, true, skip);

			case TagQueryExpressionType.AnyExpressionsMatch:
				return EvaluateAnyExpressionsMatch(tags, skip);
			case TagQueryExpressionType.AllExpressionsMatch:
				return EvaluateAllExpressionsMatch(tags, skip);
			case TagQueryExpressionType.NoExpressionsMatch:
				return EvaluateNoExpressionsMatch(tags, skip);

			case TagQueryExpressionType.Undefined:
				Validation.Fail($"{typeof(TagQueryExpressionType)} should never be set as" +
					$"{TagQueryExpressionType.Undefined}.");
				return false;
		}

		return false;
	}

	private bool EvaluateAnyTagsMatch(TagContainer tags, bool exactMatch, bool skip)
	{
		bool shortCircuit = skip;
		bool result = false;

		// Parse tag set.
		int numTags = GetToken();
		if (_readError)
		{
			return false;
		}

		for (int i = 0; i < numTags; ++i)
		{
			int tagIndex = GetToken();
			if (_readError)
			{
				return false;
			}

			if (!shortCircuit)
			{
				Tag tag = _query.GetTagFromIndex(tagIndex);

				bool hasTag = HasTag(tags, tag, exactMatch);

				if (hasTag)
				{
					// One match is sufficient for a true result.
					shortCircuit = true;
					result = true;
				}
			}
		}

		return result;
	}

	private bool EvaluateAllTagsMatch(TagContainer tags, bool exactMatch, bool skip)
	{
		bool shortCircuit = skip;

		// Assume true until proven otherwise.
		bool result = true;

		// Parse tag set.
		int numTags = GetToken();
		if (_readError)
		{
			return false;
		}

		for (int i = 0; i < numTags; ++i)
		{
			int tagIndex = GetToken();
			if (_readError)
			{
				return false;
			}

			if (!shortCircuit)
			{
				Tag tag = _query.GetTagFromIndex(tagIndex);

				bool hasTag = HasTag(tags, tag, exactMatch);

				if (!hasTag)
				{
					// One failed match is sufficient for a false result.
					shortCircuit = true;
					result = false;
				}
			}
		}

		return result;
	}

	private bool EvaluateNoTagsMatch(TagContainer tags, bool exactMatch, bool skip)
	{
		bool shortCircuit = skip;

		// Assume true until proven otherwise.
		bool result = true;

		// Parse tag set.
		int numTags = GetToken();
		if (_readError)
		{
			return false;
		}

		for (int i = 0; i < numTags; ++i)
		{
			int tagIndex = GetToken();
			if (_readError)
			{
				return false;
			}

			if (!shortCircuit)
			{
				Tag tag = _query.GetTagFromIndex(tagIndex);

				bool hasTag = HasTag(tags, tag, exactMatch);

				if (hasTag)
				{
					// One match is sufficient for a false result.
					shortCircuit = true;
					result = false;
				}
			}
		}

		return result;
	}

	private bool EvaluateAnyExpressionsMatch(TagContainer tags, bool skip)
	{
		bool shortCircuit = skip;

		// Assume false until proven otherwise.
		bool result = false;

		// Parse expression set.
		int numExpressions = GetToken();
		if (_readError)
		{
			return false;
		}

		for (int i = 0; i < numExpressions; ++i)
		{
			bool expressionResult = EvaluateExpression(tags, shortCircuit);

			if (!shortCircuit && expressionResult)
			{
				// One match is sufficient for true result.
				result = true;
				shortCircuit = true;
			}
		}

		return result;
	}

	private bool EvaluateAllExpressionsMatch(TagContainer tags, bool skip)
	{
		bool shortCircuit = skip;

		// Assume true until proven otherwise.
		bool result = true;

		// Parse expression set.
		int numExpressions = GetToken();
		if (_readError)
		{
			return false;
		}

		for (int i = 0; i < numExpressions; ++i)
		{
			bool expressionResult = EvaluateExpression(tags, shortCircuit);

			if (!shortCircuit && !expressionResult)
			{
				// One fail is sufficient for false result.
				result = false;
				shortCircuit = true;
			}
		}

		return result;
	}

	private bool EvaluateNoExpressionsMatch(TagContainer tags, bool skip)
	{
		bool shortCircuit = skip;

		// Assume true until proven otherwise.
		bool result = true;

		// Parse expression set.
		int numExpressions = GetToken();
		if (_readError)
		{
			return false;
		}

		for (int i = 0; i < numExpressions; ++i)
		{
			bool expressionResult = EvaluateExpression(tags, shortCircuit);

			if (!shortCircuit && expressionResult)
			{
				// One match is sufficient for fail result.
				result = false;
				shortCircuit = true;
			}
		}

		return result;
	}

	private byte GetToken()
	{
		if (_query.QueryTokenStream.Count > _curStreamIdx)
		{
			return _query.QueryTokenStream[_curStreamIdx++];
		}

		_readError = true;
		Validation.Fail($"Error parsing {typeof(TagQuery)}! Code should not reach this point.");

		return 0;
	}
}
