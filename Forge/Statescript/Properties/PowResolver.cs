// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a value raised to a specified power using two nested <see cref="IPropertyResolver"/> operands. Supports
/// <see langword="float"/> and <see langword="double"/> types. Integer operand types are promoted to
/// <see langword="double"/>. Decimal, vector, and quaternion types are not supported.
/// </summary>
/// <param name="baseOperand">The resolver for the base operand.</param>
/// <param name="exponent">The resolver for the exponent operand.</param>
public class PowResolver(IPropertyResolver baseOperand, IPropertyResolver exponent) : IPropertyResolver
{
	private readonly IPropertyResolver _base = baseOperand;

	private readonly IPropertyResolver _exponent = exponent;

	/// <inheritdoc/>
	public Type ValueType { get; } = MathTypeUtils.DetermineFloatingPointBinaryResultType(
		nameof(PowResolver),
		baseOperand.ValueType,
		exponent.ValueType);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		Variant128 baseValue = _base.Resolve(graphContext);
		Variant128 expValue = _exponent.Resolve(graphContext);
		Type resultType = ValueType;

		if (resultType == typeof(float))
		{
			return new Variant128(
				MathF.Pow(
					MathTypeUtils.ResolveAsFloat(_base.ValueType, baseValue),
					MathTypeUtils.ResolveAsFloat(_exponent.ValueType, expValue)));
		}

		if (resultType == typeof(double))
		{
			return new Variant128(
				Math.Pow(
					MathTypeUtils.ResolveAsDouble(_base.ValueType, baseValue),
					MathTypeUtils.ResolveAsDouble(_exponent.ValueType, expValue)));
		}

		throw new InvalidOperationException($"PowResolver encountered unexpected result type '{resultType}'.");
	}
}
