// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the angle (in radians) whose tangent is the quotient of two <see cref="IPropertyResolver"/> operands.
/// Computes <c>ATan2(y, x)</c>. Supports <see langword="float"/> and <see langword="double"/> types. Integer operand
/// types are promoted to <see langword="double"/>. Decimal, vector, and quaternion types are not supported.
/// </summary>
/// <param name="y">The resolver for the y-coordinate operand.</param>
/// <param name="x">The resolver for the x-coordinate operand.</param>
public class ATan2Resolver(IPropertyResolver y, IPropertyResolver x) : IPropertyResolver
{
	private readonly IPropertyResolver _y = y;

	private readonly IPropertyResolver _x = x;

	/// <inheritdoc/>
	public Type ValueType { get; } = MathTypeUtils.DetermineFloatingPointBinaryResultType(
		nameof(ATan2Resolver),
		y.ValueType,
		x.ValueType);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		Variant128 yValue = _y.Resolve(graphContext);
		Variant128 xValue = _x.Resolve(graphContext);
		Type resultType = ValueType;

		if (resultType == typeof(float))
		{
			return new Variant128(
				MathF.Atan2(
					MathTypeUtils.ResolveAsFloat(_y.ValueType, yValue),
					MathTypeUtils.ResolveAsFloat(_x.ValueType, xValue)));
		}

		if (resultType == typeof(double))
		{
			return new Variant128(
				Math.Atan2(
					MathTypeUtils.ResolveAsDouble(_y.ValueType, yValue),
					MathTypeUtils.ResolveAsDouble(_x.ValueType, xValue)));
		}

		throw new InvalidOperationException(
			$"ATan2Resolver encountered unexpected result type '{resultType}'.");
	}
}
