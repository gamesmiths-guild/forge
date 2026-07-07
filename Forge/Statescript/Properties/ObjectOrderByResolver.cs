// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the elements of a nested object-backed array resolver sorted by a nested numeric key selector resolver.
/// The key selector is evaluated once per element with the current element published on the element stack, so it can
/// read the element through <see cref="ElementResolver{T}"/> (or <see cref="ElementEntityResolver"/> for entity
/// arrays, composing with e.g. <see cref="AttributeResolver"/> or <see cref="DistanceResolver"/> for the key). The
/// sort is stable: elements with equal keys keep their original relative order.
/// </summary>
/// <typeparam name="T">The element type to read.</typeparam>
/// <param name="source">The resolver providing the source array.</param>
/// <param name="keySelector">The resolver evaluated per element to produce its sort key. Must resolve to a numeric
/// type.</param>
/// <param name="direction">The ordering to apply. Defaults to <see cref="SortDirection.Ascending"/>.</param>
public class ObjectOrderByResolver<T>(
	IObjectArrayResolver<T> source,
	IPropertyResolver keySelector,
	SortDirection direction = SortDirection.Ascending) : ObjectArrayResolver<T>
{
	private readonly IObjectArrayResolver<T> _source = source;

	private readonly IPropertyResolver _keySelector =
		ArrayResolverUtils.ValidateNumericOperand(nameof(ObjectOrderByResolver<T>), nameof(keySelector), keySelector);

	private readonly SortDirection _direction = direction;

	/// <inheritdoc/>
	public override T[] ResolveArray(GraphContext graphContext)
	{
		T[] values = _source.ResolveArray(graphContext);

		if (values.Length <= 1)
		{
			return values;
		}

		double[] keys = new double[values.Length];

		for (int i = 0; i < values.Length; i++)
		{
			var frame = new ElementFrame(values[i], ElementType, i);
			keys[i] = ElementLambda.EvaluateKey(graphContext, _keySelector, in frame);
		}

		int[] sortedIndexes = ArrayResolverUtils.SortIndexesByKey(keys, _direction);
		var result = new T[values.Length];

		for (int i = 0; i < values.Length; i++)
		{
			result[i] = values[sortedIndexes[i]];
		}

		return result;
	}
}
