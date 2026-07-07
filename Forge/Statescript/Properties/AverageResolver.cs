// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the arithmetic mean of all elements of a nested numeric array resolver. <see langword="float"/> elements
/// average to <see langword="float"/>, <see langword="decimal"/> elements to <see langword="decimal"/>, and all other
/// numeric element types to <see langword="double"/>.
/// </summary>
/// <remarks>
/// An empty array averages to zero.
/// </remarks>
public class AverageResolver : IPropertyResolver
{
	private readonly IArrayPropertyResolver _source;

	private readonly Type _elementType;

	/// <inheritdoc/>
	public Type ValueType { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="AverageResolver"/> class.
	/// </summary>
	/// <param name="source">The resolver providing the source array. Must have a numeric element type.</param>
	public AverageResolver(IArrayPropertyResolver source)
	{
		_elementType = ArrayResolverUtils.ValidateNumericElementType(nameof(AverageResolver), source.ElementType);
		_source = source;

		if (_elementType == typeof(float))
		{
			ValueType = typeof(float);
		}
		else if (_elementType == typeof(decimal))
		{
			ValueType = typeof(decimal);
		}
		else
		{
			ValueType = typeof(double);
		}
	}

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		Variant128[] values = _source.ResolveArray(graphContext);

		if (values.Length == 0)
		{
			return default;
		}

		if (ValueType == typeof(decimal))
		{
			decimal decimalSum = 0m;

			for (int i = 0; i < values.Length; i++)
			{
				decimalSum += MathTypeUtils.ResolveAsDecimal(_elementType, values[i]);
			}

			return new Variant128(decimalSum / values.Length);
		}

		double sum = 0d;

		for (int i = 0; i < values.Length; i++)
		{
			sum += ArrayResolverUtils.ResolveAsDouble(_elementType, values[i]);
		}

		double average = sum / values.Length;
		return ValueType == typeof(float) ? new Variant128((float)average) : new Variant128(average);
	}
}
