// Copyright © Gamesmiths Guild.

using System.Numerics;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the maximum of two nested <see cref="IPropertyResolver"/> operands. Supports all numeric types in
/// <see cref="Variant128"/> as well as <see cref="Vector2"/>, <see cref="Vector3"/>, and <see cref="Vector4"/>.
/// Quaternion types are not supported. When the two numeric operands have different types, standard numeric promotion
/// rules apply.
/// </summary>
/// <param name="left">The resolver for the left operand.</param>
/// <param name="right">The resolver for the right operand.</param>
public class MaxResolver(IPropertyResolver left, IPropertyResolver right) : IPropertyResolver
{
	private readonly IPropertyResolver _left = left;

	private readonly IPropertyResolver _right = right;

	/// <inheritdoc/>
	public Type ValueType { get; } = MathTypeUtils.DetermineBinaryResultType(
		nameof(MaxResolver),
		left.ValueType,
		right.ValueType,
		allowVectors: true,
		allowQuaternions: false);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		Variant128 leftValue = _left.Resolve(graphContext);
		Variant128 rightValue = _right.Resolve(graphContext);
		Type resultType = ValueType;

		if (resultType == typeof(int))
		{
			return new Variant128(
				Math.Max(
					MathTypeUtils.ResolveAsInt(_left.ValueType, leftValue),
					MathTypeUtils.ResolveAsInt(_right.ValueType, rightValue)));
		}

		if (resultType == typeof(long))
		{
			return new Variant128(
				Math.Max(
					MathTypeUtils.ResolveAsLong(_left.ValueType, leftValue),
					MathTypeUtils.ResolveAsLong(_right.ValueType, rightValue)));
		}

		if (resultType == typeof(float))
		{
			return new Variant128(
				Math.Max(
					MathTypeUtils.ResolveAsFloat(_left.ValueType, leftValue),
					MathTypeUtils.ResolveAsFloat(_right.ValueType, rightValue)));
		}

		if (resultType == typeof(double))
		{
			return new Variant128(
				Math.Max(
					MathTypeUtils.ResolveAsDouble(_left.ValueType, leftValue),
					MathTypeUtils.ResolveAsDouble(_right.ValueType, rightValue)));
		}

		if (resultType == typeof(decimal))
		{
			return new Variant128(
				Math.Max(
					MathTypeUtils.ResolveAsDecimal(_left.ValueType, leftValue),
					MathTypeUtils.ResolveAsDecimal(_right.ValueType, rightValue)));
		}

		if (resultType == typeof(Vector2))
		{
			return new Variant128(Vector2.Max(leftValue.AsVector2(), rightValue.AsVector2()));
		}

		if (resultType == typeof(Vector3))
		{
			return new Variant128(Vector3.Max(leftValue.AsVector3(), rightValue.AsVector3()));
		}

		if (resultType == typeof(Vector4))
		{
			return new Variant128(Vector4.Max(leftValue.AsVector4(), rightValue.AsVector4()));
		}

		throw new InvalidOperationException($"MaxResolver encountered unexpected result type '{resultType}'.");
	}
}
