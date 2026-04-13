// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a conversion from degrees to radians using a single <see cref="IPropertyResolver"/> operand. Computes
/// <c>degrees * (π / 180)</c>. Supports <see langword="float"/> and <see langword="double"/> types. Integer operand
/// types are promoted to <see langword="double"/>. Decimal, vector, and quaternion types are not supported.
/// </summary>
/// <param name="operand">The resolver for the operand (angle in degrees).</param>
public class DegToRadResolver(IPropertyResolver operand) : IPropertyResolver
{
	private readonly IPropertyResolver _operand = operand;

	/// <inheritdoc/>
	public Type ValueType { get; } = MathTypeUtils.DetermineFloatingPointPromotedUnaryResultType(
		nameof(DegToRadResolver),
		operand.ValueType);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		Variant128 value = _operand.Resolve(graphContext);
		Type resultType = ValueType;

		if (resultType == typeof(float))
		{
			return new Variant128(value.AsFloat() * (MathF.PI / 180.0f));
		}

		if (resultType == typeof(double))
		{
			return new Variant128(
				MathTypeUtils.ResolveAsDouble(_operand.ValueType, value) * (Math.PI / 180.0));
		}

		throw new InvalidOperationException(
			$"DegToRadResolver encountered unexpected result type '{resultType}'.");
	}
}
