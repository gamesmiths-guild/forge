// Copyright © Gamesmiths Guild.

using System.Numerics;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the dot product of two vector operands. Returns a <see langword="float"/> for all vector types.
/// Supports <see cref="Vector2"/>, <see cref="Vector3"/>, and <see cref="Vector4"/>. Both operands must be the same
/// vector type. Scalar, quaternion, and plane types are not supported.
/// </summary>
/// <param name="left">The resolver for the left vector operand.</param>
/// <param name="right">The resolver for the right vector operand.</param>
public class DotResolver(IPropertyResolver left, IPropertyResolver right) : IPropertyResolver
{
	private readonly IPropertyResolver _left = left;

	private readonly IPropertyResolver _right = right;

	private readonly Type _operandType = DetermineOperandType(left.ValueType, right.ValueType);

	/// <inheritdoc/>
	public Type ValueType { get; } = typeof(float);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		Variant128 leftValue = _left.Resolve(graphContext);
		Variant128 rightValue = _right.Resolve(graphContext);

		if (_operandType == typeof(Vector2))
		{
			return new Variant128(Vector2.Dot(leftValue.AsVector2(), rightValue.AsVector2()));
		}

		if (_operandType == typeof(Vector3))
		{
			return new Variant128(Vector3.Dot(leftValue.AsVector3(), rightValue.AsVector3()));
		}

		if (_operandType == typeof(Vector4))
		{
			return new Variant128(Vector4.Dot(leftValue.AsVector4(), rightValue.AsVector4()));
		}

		throw new InvalidOperationException(
			$"DotResolver encountered unexpected operand type '{_operandType}'.");
	}

	private static Type DetermineOperandType(Type leftType, Type rightType)
	{
		if (leftType != rightType)
		{
			throw new ArgumentException(
				$"DotResolver requires matching vector types. Got '{leftType}' and '{rightType}'.");
		}

		if (!MathTypeUtils.IsVectorType(leftType))
		{
			throw new ArgumentException(
				$"DotResolver only supports vector types (Vector2, Vector3, Vector4). Got '{leftType}'.");
		}

		return leftType;
	}
}
