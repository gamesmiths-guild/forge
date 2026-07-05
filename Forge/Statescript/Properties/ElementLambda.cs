// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Shared helpers for evaluating nested "lambda" resolvers against a single array element. The element is published on
/// the <see cref="GraphContext"/> element stack for the duration of the evaluation so that element resolvers
/// (<see cref="ElementValueResolver"/>, <see cref="ElementResolver{T}"/>, <see cref="ElementIndexResolver"/>) can read
/// it back.
/// </summary>
internal static class ElementLambda
{
	internal static Variant128 Evaluate(GraphContext graphContext, IPropertyResolver lambda, in ElementFrame frame)
	{
		graphContext.PushElement(in frame);

		try
		{
			return lambda.Resolve(graphContext);
		}
		finally
		{
			graphContext.PopElement();
		}
	}

	internal static object? Evaluate(GraphContext graphContext, IObjectResolver lambda, in ElementFrame frame)
	{
		graphContext.PushElement(in frame);

		try
		{
			return lambda.Resolve(graphContext);
		}
		finally
		{
			graphContext.PopElement();
		}
	}

	internal static bool EvaluatePredicate(GraphContext graphContext, IPropertyResolver predicate, in ElementFrame frame)
	{
		return Evaluate(graphContext, predicate, in frame).AsBool();
	}

	internal static double EvaluateKey(GraphContext graphContext, IPropertyResolver keySelector, in ElementFrame frame)
	{
		return ArrayResolverUtils.ResolveAsDouble(
			keySelector.ValueType,
			Evaluate(graphContext, keySelector, in frame));
	}
}
