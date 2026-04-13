// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the smallest integer greater than or equal to the operand (ceiling). Supports <see langword="float"/>,
/// <see langword="double"/>, and <see langword="decimal"/> types only. Integer, vector, and quaternion types are not
/// supported.
/// </summary>
/// <param name="operand">The resolver for the operand.</param>
public class CeilResolver(IPropertyResolver operand) : IPropertyResolver
{
	private readonly IPropertyResolver _operand = operand;

	/// <inheritdoc/>
	public Type ValueType { get; } = MathTypeUtils.DetermineFloatingPointUnaryResultType(
		nameof(CeilResolver),
		operand.ValueType);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		Variant128 value = _operand.Resolve(graphContext);
		Type resultType = ValueType;

		if (resultType == typeof(float))
		{
			return new Variant128(MathF.Ceiling(value.AsFloat()));
		}

		if (resultType == typeof(double))
		{
			return new Variant128(Math.Ceiling(value.AsDouble()));
		}

		if (resultType == typeof(decimal))
		{
			return new Variant128(Math.Ceiling(value.AsDecimal()));
		}

		throw new InvalidOperationException($"CeilResolver encountered unexpected result type '{resultType}'.");
	}
}
