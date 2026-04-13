// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a conversion from radians to degrees using a single <see cref="IPropertyResolver"/> operand. Computes
/// <c>radians * (180 / π)</c>. Supports <see langword="float"/> and <see langword="double"/> types. Integer operand
/// types are promoted to <see langword="double"/>. Decimal, vector, and quaternion types are not supported.
/// </summary>
/// <param name="operand">The resolver for the operand (angle in radians).</param>
public class RadToDegResolver(IPropertyResolver operand) : IPropertyResolver
{
	private readonly IPropertyResolver _operand = operand;

	/// <inheritdoc/>
	public Type ValueType { get; } = MathTypeUtils.DetermineFloatingPointPromotedUnaryResultType(
		nameof(RadToDegResolver),
		operand.ValueType);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		Variant128 value = _operand.Resolve(graphContext);
		Type resultType = ValueType;

		if (resultType == typeof(float))
		{
			return new Variant128(value.AsFloat() * (180.0f / MathF.PI));
		}

		if (resultType == typeof(double))
		{
			return new Variant128(
				MathTypeUtils.ResolveAsDouble(_operand.ValueType, value) * (180.0 / Math.PI));
		}

		throw new InvalidOperationException(
			$"RadToDegResolver encountered unexpected result type '{resultType}'.");
	}
}
