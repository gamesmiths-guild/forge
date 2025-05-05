// Copyright © Gamesmiths Guild.

using System.Diagnostics;

namespace Gamesmiths.Forge.GameplayTags;

/// <summary>
/// Utility class for parsing and evaluating query tokens.
/// </summary>
/// <param name="query">The <see cref="GameplayTagQuery"/> to be used by this evaluator.</param>
internal class QueryEvaluator(GameplayTagQuery query)
{
	private readonly GameplayTagQuery _query = query;

	private int _curStreamIdx;
	private bool _readError;

	/// <summary>
	/// Evaluates the given <see cref="GameplayTagContainer"/> against the provided <see cref="GameplayTagQuery"/>.
	/// </summary>
	/// <param name="container">The <see cref="GameplayTagContainer"/> containing the tags to be checked.</param>
	/// <returns><see langword="true"/> if the query is satisfied by the tags in the container; <see langword="false"/>
	/// otherwise.</returns>
	public bool Evaluate(GameplayTagContainer container)
	{
		_curStreamIdx = 0;

		var returnValue = false;

		var hasRootExpression = GetToken();
		if (!_readError && hasRootExpression != 0)
		{
			returnValue = EvaluateExpression(container);
		}

		Debug.Assert(
			_curStreamIdx == _query.QueryTokenStream.Count,
			"There shouldn't be remaining tokens into the stream.");

		return returnValue;
	}

	private static bool HasTag(GameplayTagContainer tags, GameplayTag tag, bool exactMatch)
	{
		if (exactMatch)
		{
			return tags.HasTagExact(tag);
		}

		return tags.HasTag(tag);
	}

	private bool EvaluateExpression(GameplayTagContainer tags, bool skip = false)
	{
		// Emit expression data.
		switch ((GameplayTagQueryExpressionType)GetToken())
		{
			case GameplayTagQueryExpressionType.AnyTagsMatch:
				return EvaluateAnyTagsMatch(tags, false, skip);
			case GameplayTagQueryExpressionType.AllTagsMatch:
				return EvaluateAllTagsMatch(tags, false, skip);
			case GameplayTagQueryExpressionType.NoTagsMatch:
				return EvaluateNoTagsMatch(tags, false, skip);

			case GameplayTagQueryExpressionType.AnyTagsMatchExact:
				return EvaluateAnyTagsMatch(tags, true, skip);
			case GameplayTagQueryExpressionType.AllTagsMatchExact:
				return EvaluateAllTagsMatch(tags, true, skip);
			case GameplayTagQueryExpressionType.NoTagsMatchExact:
				return EvaluateNoTagsMatch(tags, true, skip);

			case GameplayTagQueryExpressionType.AnyExpressionsMatch:
				return EvaluateAnyExpressionsMatch(tags, skip);
			case GameplayTagQueryExpressionType.AllExpressionsMatch:
				return EvaluateAllExpressionsMatch(tags, skip);
			case GameplayTagQueryExpressionType.NoExpressionsMatch:
				return EvaluateNoExpressionsMatch(tags, skip);

			case GameplayTagQueryExpressionType.Undefined:
				Debug.Fail($"{typeof(GameplayTagQueryExpressionType)} should never be set as" +
					$"{GameplayTagQueryExpressionType.Undefined}.");
				return false;
		}

		return false;
	}

	private bool EvaluateAnyTagsMatch(GameplayTagContainer tags, bool exactMatch, bool skip)
	{
		var shortCircuit = skip;
		var result = false;

		// Parse tag set.
		int numTags = GetToken();
		if (_readError)
		{
			return false;
		}

		for (var i = 0; i < numTags; ++i)
		{
			int tagIndex = GetToken();
			if (_readError)
			{
				return false;
			}

			if (!shortCircuit)
			{
				GameplayTag tag = _query.GetTagFromIndex(tagIndex);

				var hasTag = HasTag(tags, tag, exactMatch);

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

	private bool EvaluateAllTagsMatch(GameplayTagContainer tags, bool exactMatch, bool skip)
	{
		var shortCircuit = skip;

		// Assume true until proven otherwise.
		var result = true;

		// Parse tag set.
		int numTags = GetToken();
		if (_readError)
		{
			return false;
		}

		for (var i = 0; i < numTags; ++i)
		{
			int tagIndex = GetToken();
			if (_readError)
			{
				return false;
			}

			if (!shortCircuit)
			{
				GameplayTag tag = _query.GetTagFromIndex(tagIndex);

				var hasTag = HasTag(tags, tag, exactMatch);

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

	private bool EvaluateNoTagsMatch(GameplayTagContainer tags, bool exactMatch, bool skip)
	{
		var shortCircuit = skip;

		// Assume true until proven otherwise.
		var result = true;

		// Parse tag set.
		int numTags = GetToken();
		if (_readError)
		{
			return false;
		}

		for (var i = 0; i < numTags; ++i)
		{
			int tagIndex = GetToken();
			if (_readError)
			{
				return false;
			}

			if (!shortCircuit)
			{
				GameplayTag tag = _query.GetTagFromIndex(tagIndex);

				var hasTag = HasTag(tags, tag, exactMatch);

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

	private bool EvaluateAnyExpressionsMatch(GameplayTagContainer tags, bool skip)
	{
		var shortCircuit = skip;

		// Assume false until proven otherwise.
		var result = false;

		// Parse expression set.
		int numExpressions = GetToken();
		if (_readError)
		{
			return false;
		}

		for (var i = 0; i < numExpressions; ++i)
		{
			var expressionResult = EvaluateExpression(tags, shortCircuit);

			if (!shortCircuit && expressionResult)
			{
				// One match is sufficient for true result.
				result = true;
				shortCircuit = true;
			}
		}

		return result;
	}

	private bool EvaluateAllExpressionsMatch(GameplayTagContainer tags, bool skip)
	{
		var shortCircuit = skip;

		// Assume true until proven otherwise.
		var result = true;

		// Parse expression set.
		int numExpressions = GetToken();
		if (_readError)
		{
			return false;
		}

		for (var i = 0; i < numExpressions; ++i)
		{
			var expressionResult = EvaluateExpression(tags, shortCircuit);

			if (!shortCircuit && !expressionResult)
			{
				// One fail is sufficient for false result.
				result = false;
				shortCircuit = true;
			}
		}

		return result;
	}

	private bool EvaluateNoExpressionsMatch(GameplayTagContainer tags, bool skip)
	{
		var shortCircuit = skip;

		// Assume true until proven otherwise.
		var result = true;

		// Parse expression set.
		int numExpressions = GetToken();
		if (_readError)
		{
			return false;
		}

		for (var i = 0; i < numExpressions; ++i)
		{
			var expressionResult = EvaluateExpression(tags, shortCircuit);

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
		Debug.Fail($"Error parsing {typeof(GameplayTagQuery)}! Code should not reach this point.");

		return 0;
	}
}
