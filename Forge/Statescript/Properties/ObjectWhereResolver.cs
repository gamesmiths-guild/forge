// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the elements of a nested object-backed array resolver that satisfy a nested boolean predicate resolver,
/// preserving their original order. The predicate is evaluated once per element with the current element published on
/// the element stack, so it can read the element through <see cref="ElementResolver{T}"/> (or
/// <see cref="ElementEntityResolver"/> for entity arrays).
/// </summary>
/// <remarks>
/// To remove matching elements instead, wrap the predicate in a <see cref="NotResolver"/>.
/// </remarks>
/// <typeparam name="T">The element type to read.</typeparam>
/// <param name="source">The resolver providing the source array.</param>
/// <param name="predicate">The resolver evaluated per element. Must resolve to <see langword="bool"/>.</param>
public class ObjectWhereResolver<T>(IObjectArrayResolver<T> source, IPropertyResolver predicate)
	: ObjectArrayResolver<T>
{
	private readonly IObjectArrayResolver<T> _source = source;

	private readonly IPropertyResolver _predicate =
		BooleanTypeUtils.ValidateBoolOperand(nameof(ObjectWhereResolver<T>), nameof(predicate), predicate);

	/// <inheritdoc/>
	public override T[] ResolveArray(GraphContext graphContext)
	{
		T[] values = _source.ResolveArray(graphContext);
		var result = new List<T>(values.Length);

		for (int i = 0; i < values.Length; i++)
		{
			var frame = new ElementFrame(values[i], ElementType, i);

			if (ElementLambda.EvaluatePredicate(graphContext, _predicate, in frame))
			{
				result.Add(values[i]);
			}
		}

		return [.. result];
	}
}
