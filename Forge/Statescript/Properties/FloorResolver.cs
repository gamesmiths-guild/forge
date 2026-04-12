// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the largest integer less than or equal to the operand (floor). Supports <see langword="float"/>,
/// <see langword="double"/>, and <see langword="decimal"/> types only. Integer, vector, and quaternion types are not
/// supported.
/// </summary>
/// <param name="operand">The resolver for the operand.</param>
public class FloorResolver(IPropertyResolver operand) : IPropertyResolver
{
	private readonly IPropertyResolver _operand = operand;

	/// <inheritdoc/>
	public Type ValueType { get; } = MathTypeUtils.DetermineFloatingPointUnaryResultType(
		nameof(FloorResolver),
		operand.ValueType);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		Variant128 value = _operand.Resolve(graphContext);
		Type resultType = ValueType;

		if (resultType == typeof(float))
		{
			return new Variant128(MathF.Floor(value.AsFloat()));
		}

		if (resultType == typeof(double))
		{
			return new Variant128(Math.Floor(value.AsDouble()));
		}

		if (resultType == typeof(decimal))
		{
			return new Variant128(Math.Floor(value.AsDecimal()));
		}

		throw new InvalidOperationException($"FloorResolver encountered unexpected result type '{resultType}'.");
	}
}
