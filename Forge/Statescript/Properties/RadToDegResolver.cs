// Copyright © Gamesmiths Guild.

using System.Numerics;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a conversion from radians to degrees using a single <see cref="IPropertyResolver"/> operand. Computes
/// <c>radians * (180 / π)</c>. Supports <see langword="float"/> and <see langword="double"/> types as well as
/// component-wise conversion for <see cref="Vector2"/>, <see cref="Vector3"/>, and <see cref="Vector4"/>. Integer
/// operand types are promoted to <see langword="double"/>. Decimal and quaternion types are not supported.
/// </summary>
/// <param name="operand">The resolver for the operand (angle in radians).</param>
public class RadToDegResolver(IPropertyResolver operand) : IPropertyResolver
{
	private readonly IPropertyResolver _operand = operand;

	/// <inheritdoc/>
	public Type ValueType { get; } = DetermineResultType(operand.ValueType);

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

		if (resultType == typeof(Vector2))
		{
			return new Variant128(value.AsVector2() * (180.0f / MathF.PI));
		}

		if (resultType == typeof(Vector3))
		{
			return new Variant128(value.AsVector3() * (180.0f / MathF.PI));
		}

		if (resultType == typeof(Vector4))
		{
			return new Variant128(value.AsVector4() * (180.0f / MathF.PI));
		}

		throw new InvalidOperationException(
			$"RadToDegResolver encountered unexpected result type '{resultType}'.");
	}

	private static Type DetermineResultType(Type operandType)
	{
		if (MathTypeUtils.IsVectorType(operandType))
		{
			return operandType;
		}

		return MathTypeUtils.DetermineFloatingPointPromotedUnaryResultType(
			nameof(RadToDegResolver),
			operandType);
	}
}
