// Copyright © Gamesmiths Guild.

using System.Numerics;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the squared Euclidean distance between two vector operands. Returns a <see langword="float"/>. This is more
/// efficient than <see cref="DistanceResolver"/> when only relative comparisons are needed, as it avoids the square
/// root computation. Both operands must be the same vector type. Supports <see cref="Vector2"/>, <see cref="Vector3"/>,
/// and <see cref="Vector4"/>. Scalar, quaternion, and plane types are not supported.
/// </summary>
/// <param name="left">The resolver for the first point.</param>
/// <param name="right">The resolver for the second point.</param>
public class DistanceSquaredResolver(IPropertyResolver left, IPropertyResolver right) : IPropertyResolver
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
			return new Variant128(
				Vector2.DistanceSquared(leftValue.AsVector2(), rightValue.AsVector2()));
		}

		if (_operandType == typeof(Vector3))
		{
			return new Variant128(
				Vector3.DistanceSquared(leftValue.AsVector3(), rightValue.AsVector3()));
		}

		if (_operandType == typeof(Vector4))
		{
			return new Variant128(
				Vector4.DistanceSquared(leftValue.AsVector4(), rightValue.AsVector4()));
		}

		throw new InvalidOperationException(
			$"DistanceSquaredResolver encountered unexpected operand type '{_operandType}'.");
	}

	private static Type DetermineOperandType(Type leftType, Type rightType)
	{
		if (leftType != rightType)
		{
			throw new ArgumentException(
				$"DistanceSquaredResolver requires matching vector types. Got '{leftType}' and '{rightType}'.");
		}

		if (!MathTypeUtils.IsVectorType(leftType))
		{
			throw new ArgumentException(
				$"DistanceSquaredResolver only supports vector types (Vector2, Vector3, Vector4). Got '{leftType}'.");
		}

		return leftType;
	}
}
