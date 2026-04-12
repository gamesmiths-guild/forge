// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the nearest integer to the operand using banker's rounding (<see cref="MidpointRounding.ToEven"/>).
/// Supports <see langword="float"/>, <see langword="double"/>, and <see langword="decimal"/> types only. Integer,
/// vector, and quaternion types are not supported.
/// </summary>
/// <param name="operand">The resolver for the operand.</param>
public class RoundResolver(IPropertyResolver operand) : IPropertyResolver
{
	private readonly IPropertyResolver _operand = operand;

	/// <inheritdoc/>
	public Type ValueType { get; } = MathTypeUtils.DetermineFloatingPointUnaryResultType(
		nameof(RoundResolver),
		operand.ValueType);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		Variant128 value = _operand.Resolve(graphContext);
		Type resultType = ValueType;

		if (resultType == typeof(float))
		{
			return new Variant128(MathF.Round(value.AsFloat()));
		}

		if (resultType == typeof(double))
		{
			return new Variant128(Math.Round(value.AsDouble()));
		}

		if (resultType == typeof(decimal))
		{
			return new Variant128(Math.Round(value.AsDecimal()));
		}

		throw new InvalidOperationException($"RoundResolver encountered unexpected result type '{resultType}'.");
	}
}
