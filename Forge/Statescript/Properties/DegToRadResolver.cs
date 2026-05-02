// Copyright © Gamesmiths Guild.

using System.Numerics;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a conversion from degrees to radians using a single <see cref="IPropertyResolver"/> operand. Computes
/// <c>degrees * (π / 180)</c>. Supports <see langword="float"/> and <see langword="double"/> types as well as
/// component-wise conversion for <see cref="Vector2"/>, <see cref="Vector3"/>, and <see cref="Vector4"/>. Integer
/// operand types are promoted to <see langword="double"/>. Decimal and quaternion types are not supported.
/// </summary>
/// <param name="operand">The resolver for the operand (angle in degrees).</param>
public class DegToRadResolver(IPropertyResolver operand) : IPropertyResolver
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
			return new Variant128(value.AsFloat() * (MathF.PI / 180.0f));
		}

		if (resultType == typeof(double))
		{
			return new Variant128(
				MathTypeUtils.ResolveAsDouble(_operand.ValueType, value) * (Math.PI / 180.0));
		}

		if (resultType == typeof(Vector2))
		{
			return new Variant128(value.AsVector2() * (MathF.PI / 180.0f));
		}

		if (resultType == typeof(Vector3))
		{
			return new Variant128(value.AsVector3() * (MathF.PI / 180.0f));
		}

		if (resultType == typeof(Vector4))
		{
			return new Variant128(value.AsVector4() * (MathF.PI / 180.0f));
		}

		throw new InvalidOperationException(
			$"DegToRadResolver encountered unexpected result type '{resultType}'.");
	}

	private static Type DetermineResultType(Type operandType)
	{
		if (MathTypeUtils.IsVectorType(operandType))
		{
			return operandType;
		}

		return MathTypeUtils.DetermineFloatingPointPromotedUnaryResultType(
			nameof(DegToRadResolver),
			operandType);
	}
}
