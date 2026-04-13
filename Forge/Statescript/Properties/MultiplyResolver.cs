// Copyright © Gamesmiths Guild.

using System.Numerics;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the product of two nested <see cref="IPropertyResolver"/> operands. Supports all numeric types in
/// <see cref="Variant128"/> as well as <see cref="Vector2"/>, <see cref="Vector3"/>, <see cref="Vector4"/>
/// (component-wise multiplication), and <see cref="Quaternion"/> (quaternion multiplication). When the two operands
/// have different numeric types, standard numeric promotion rules apply. Vector and quaternion operands must match
/// exactly.
/// </summary>
/// <param name="left">The resolver for the left operand.</param>
/// <param name="right">The resolver for the right operand.</param>
public class MultiplyResolver(IPropertyResolver left, IPropertyResolver right) : IPropertyResolver
{
	private readonly IPropertyResolver _left = left;

	private readonly IPropertyResolver _right = right;

	/// <inheritdoc/>
	public Type ValueType { get; } = MathTypeUtils.DetermineBinaryResultType(
		nameof(MultiplyResolver),
		left.ValueType,
		right.ValueType);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		Variant128 leftValue = _left.Resolve(graphContext);
		Variant128 rightValue = _right.Resolve(graphContext);
		Type resultType = ValueType;

		if (resultType == typeof(int))
		{
			return new Variant128(
				MathTypeUtils.ResolveAsInt(_left.ValueType, leftValue)
				* MathTypeUtils.ResolveAsInt(_right.ValueType, rightValue));
		}

		if (resultType == typeof(long))
		{
			return new Variant128(
				MathTypeUtils.ResolveAsLong(_left.ValueType, leftValue)
				* MathTypeUtils.ResolveAsLong(_right.ValueType, rightValue));
		}

		if (resultType == typeof(float))
		{
			return new Variant128(
				MathTypeUtils.ResolveAsFloat(_left.ValueType, leftValue)
				* MathTypeUtils.ResolveAsFloat(_right.ValueType, rightValue));
		}

		if (resultType == typeof(double))
		{
			return new Variant128(
				MathTypeUtils.ResolveAsDouble(_left.ValueType, leftValue)
				* MathTypeUtils.ResolveAsDouble(_right.ValueType, rightValue));
		}

		if (resultType == typeof(decimal))
		{
			return new Variant128(
				MathTypeUtils.ResolveAsDecimal(_left.ValueType, leftValue)
				* MathTypeUtils.ResolveAsDecimal(_right.ValueType, rightValue));
		}

		if (resultType == typeof(Vector2))
		{
			return new Variant128(leftValue.AsVector2() * rightValue.AsVector2());
		}

		if (resultType == typeof(Vector3))
		{
			return new Variant128(leftValue.AsVector3() * rightValue.AsVector3());
		}

		if (resultType == typeof(Vector4))
		{
			return new Variant128(leftValue.AsVector4() * rightValue.AsVector4());
		}

		if (resultType == typeof(Quaternion))
		{
			return new Variant128(leftValue.AsQuaternion() * rightValue.AsQuaternion());
		}

		throw new InvalidOperationException(
			$"MultiplyResolver encountered unexpected result type '{resultType}'.");
	}
}
