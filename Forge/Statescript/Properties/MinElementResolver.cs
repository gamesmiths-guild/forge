// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the smallest element of a nested numeric array resolver. Unlike the binary <see cref="MinResolver"/>, which
/// compares two operands, this resolver aggregates over an array.
/// </summary>
/// <remarks>
/// The original element value is returned unchanged, so the result type matches the source element type. If the source
/// array is empty, a default <see cref="Variant128"/> (zero) is returned. Ties resolve to the first occurrence.
/// </remarks>
/// <param name="source">The resolver providing the source array. Must have a numeric element type.</param>
public class MinElementResolver(IArrayPropertyResolver source) : IPropertyResolver
{
	private readonly IArrayPropertyResolver _source = source;

	/// <inheritdoc/>
	public Type ValueType { get; } =
		ArrayResolverUtils.ValidateNumericElementType(nameof(MinElementResolver), source.ElementType);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		Variant128[] values = _source.ResolveArray(graphContext);

		if (values.Length == 0)
		{
			return default;
		}

		int bestIndex = 0;
		double bestKey = ArrayResolverUtils.ResolveAsDouble(ValueType, values[0]);

		for (int i = 1; i < values.Length; i++)
		{
			double key = ArrayResolverUtils.ResolveAsDouble(ValueType, values[i]);

			if (key.CompareTo(bestKey) < 0)
			{
				bestIndex = i;
				bestKey = key;
			}
		}

		return values[bestIndex];
	}
}
