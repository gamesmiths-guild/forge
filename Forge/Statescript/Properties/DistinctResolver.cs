// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the distinct elements of a nested array resolver, keeping the first occurrence of each value and preserving
/// the original order.
/// </summary>
/// <remarks>
/// Floating-point elements are compared exactly.
/// </remarks>
/// <param name="source">The resolver providing the source array.</param>
public class DistinctResolver(IArrayPropertyResolver source) : IArrayPropertyResolver
{
	private readonly IArrayPropertyResolver _source = source;

	/// <inheritdoc/>
	public Type ElementType { get; } = source.ElementType;

	/// <inheritdoc/>
	public Variant128[] ResolveArray(GraphContext graphContext)
	{
		Variant128[] values = _source.ResolveArray(graphContext);

		if (values.Length <= 1)
		{
			return values;
		}

		var result = new List<Variant128>(values.Length);

		for (int i = 0; i < values.Length; i++)
		{
			if (!ContainsValue(result, values[i]))
			{
				result.Add(values[i]);
			}
		}

		return [.. result];
	}

	private bool ContainsValue(List<Variant128> values, Variant128 value)
	{
		for (int i = 0; i < values.Count; i++)
		{
			if (VariantEquality.AreEqual(values[i], value, ElementType))
			{
				return true;
			}
		}

		return false;
	}
}
