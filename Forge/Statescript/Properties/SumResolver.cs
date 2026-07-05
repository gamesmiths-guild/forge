// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the sum of all elements of a nested numeric array resolver. The result type follows the standard numeric
/// promotion rules (e.g. <see langword="int"/> elements sum to <see langword="int"/>, <see langword="float"/> elements
/// to <see langword="float"/>).
/// </summary>
/// <remarks>
/// An empty array sums to zero.
/// </remarks>
public class SumResolver : IPropertyResolver
{
	private readonly IArrayPropertyResolver _source;

	private readonly Type _elementType;

	/// <inheritdoc/>
	public Type ValueType { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="SumResolver"/> class.
	/// </summary>
	/// <param name="source">The resolver providing the source array. Must have a numeric element type.</param>
	public SumResolver(IArrayPropertyResolver source)
	{
		_elementType = ArrayResolverUtils.ValidateNumericElementType(nameof(SumResolver), source.ElementType);
		_source = source;
		ValueType = MathTypeUtils.PromoteNumericTypes(_elementType, _elementType);
	}

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		Variant128[] values = _source.ResolveArray(graphContext);

		if (ValueType == typeof(decimal))
		{
			decimal decimalSum = 0m;

			for (int i = 0; i < values.Length; i++)
			{
				decimalSum += MathTypeUtils.ResolveAsDecimal(_elementType, values[i]);
			}

			return new Variant128(decimalSum);
		}

		double sum = 0d;

		for (int i = 0; i < values.Length; i++)
		{
			sum += ArrayResolverUtils.ResolveAsDouble(_elementType, values[i]);
		}

		if (ValueType == typeof(int))
		{
			return new Variant128((int)sum);
		}

		if (ValueType == typeof(long))
		{
			return new Variant128((long)sum);
		}

		if (ValueType == typeof(float))
		{
			return new Variant128((float)sum);
		}

		return new Variant128(sum);
	}
}
