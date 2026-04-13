// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the inverse hyperbolic tangent of a single <see cref="IPropertyResolver"/> operand. Supports
/// <see langword="float"/> and <see langword="double"/> types. Integer operand types are promoted to
/// <see langword="double"/>. Decimal, vector, and quaternion types are not supported.
/// </summary>
/// <param name="operand">The resolver for the operand (value in the range (-1, 1)).</param>
public class ATanHResolver(IPropertyResolver operand) : IPropertyResolver
{
	private readonly IPropertyResolver _operand = operand;

	/// <inheritdoc/>
	public Type ValueType { get; } = MathTypeUtils.DetermineFloatingPointPromotedUnaryResultType(
		nameof(ATanHResolver),
		operand.ValueType);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		Variant128 value = _operand.Resolve(graphContext);
		Type resultType = ValueType;

		if (resultType == typeof(float))
		{
			return new Variant128(MathF.Atanh(value.AsFloat()));
		}

		if (resultType == typeof(double))
		{
			return new Variant128(Math.Atanh(MathTypeUtils.ResolveAsDouble(_operand.ValueType, value)));
		}

		throw new InvalidOperationException(
			$"ATanHResolver encountered unexpected result type '{resultType}'.");
	}
}
