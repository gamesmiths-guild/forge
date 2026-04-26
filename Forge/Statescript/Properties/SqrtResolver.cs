// Copyright © Gamesmiths Guild.

using System.Numerics;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the square root of a single <see cref="IPropertyResolver"/> operand. Supports <see langword="float"/> and
/// <see langword="double"/> types as well as <see cref="Vector2"/>, <see cref="Vector3"/>, and <see cref="Vector4"/>.
/// Integer operand types are promoted to <see langword="double"/>. Decimal and quaternion types are not supported.
/// </summary>
/// <param name="operand">The resolver for the operand.</param>
public class SqrtResolver(IPropertyResolver operand) : IPropertyResolver
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
			return new Variant128(MathF.Sqrt(value.AsFloat()));
		}

		if (resultType == typeof(double))
		{
			return new Variant128(Math.Sqrt(MathTypeUtils.ResolveAsDouble(_operand.ValueType, value)));
		}

		if (resultType == typeof(Vector2))
		{
			return new Variant128(Vector2.SquareRoot(value.AsVector2()));
		}

		if (resultType == typeof(Vector3))
		{
			return new Variant128(Vector3.SquareRoot(value.AsVector3()));
		}

		if (resultType == typeof(Vector4))
		{
			return new Variant128(Vector4.SquareRoot(value.AsVector4()));
		}

		throw new InvalidOperationException($"SqrtResolver encountered unexpected result type '{resultType}'.");
	}

	private static Type DetermineResultType(Type operandType)
	{
		if (MathTypeUtils.IsVectorType(operandType))
		{
			return operandType;
		}

		return MathTypeUtils.DetermineFloatingPointPromotedUnaryResultType(
			nameof(SqrtResolver),
			operandType);
	}
}
