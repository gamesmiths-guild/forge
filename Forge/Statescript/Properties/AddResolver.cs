// Copyright © Gamesmiths Guild.

using System.Numerics;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the sum of two nested <see cref="IPropertyResolver"/> operands. Supports all numeric types in
/// <see cref="Variant128"/> as well as <see cref="Vector2"/>, <see cref="Vector3"/>, <see cref="Vector4"/>, and
/// <see cref="Quaternion"/>. When the two operands have different numeric types, standard numeric promotion rules
/// apply (e.g., <see langword="int"/> + <see langword="float"/> → <see langword="float"/>). Vector and quaternion
/// operands must match exactly.
/// </summary>
/// <remarks>
/// <para>This resolver enables data-driven arithmetic expressions in graph properties without requiring custom node
/// subclasses. Operand resolvers can be any <see cref="IPropertyResolver"/> implementation, including other
/// <see cref="AddResolver"/> instances, enabling arbitrarily nested expressions.</para>
/// </remarks>
/// <param name="left">The resolver for the left operand.</param>
/// <param name="right">The resolver for the right operand.</param>
public class AddResolver(IPropertyResolver left, IPropertyResolver right) : IPropertyResolver
{
	private readonly IPropertyResolver _left = left;

	private readonly IPropertyResolver _right = right;

	/// <inheritdoc/>
	public Type ValueType { get; } = MathTypeUtils.DetermineBinaryResultType(
		nameof(AddResolver),
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
				+ MathTypeUtils.ResolveAsInt(_right.ValueType, rightValue));
		}

		if (resultType == typeof(long))
		{
			return new Variant128(
				MathTypeUtils.ResolveAsLong(_left.ValueType, leftValue)
				+ MathTypeUtils.ResolveAsLong(_right.ValueType, rightValue));
		}

		if (resultType == typeof(float))
		{
			return new Variant128(
				MathTypeUtils.ResolveAsFloat(_left.ValueType, leftValue)
				+ MathTypeUtils.ResolveAsFloat(_right.ValueType, rightValue));
		}

		if (resultType == typeof(double))
		{
			return new Variant128(
				MathTypeUtils.ResolveAsDouble(_left.ValueType, leftValue)
				+ MathTypeUtils.ResolveAsDouble(_right.ValueType, rightValue));
		}

		if (resultType == typeof(decimal))
		{
			return new Variant128(
				MathTypeUtils.ResolveAsDecimal(_left.ValueType, leftValue)
				+ MathTypeUtils.ResolveAsDecimal(_right.ValueType, rightValue));
		}

		if (resultType == typeof(Vector2))
		{
			return new Variant128(leftValue.AsVector2() + rightValue.AsVector2());
		}

		if (resultType == typeof(Vector3))
		{
			return new Variant128(leftValue.AsVector3() + rightValue.AsVector3());
		}

		if (resultType == typeof(Vector4))
		{
			return new Variant128(leftValue.AsVector4() + rightValue.AsVector4());
		}

		if (resultType == typeof(Quaternion))
		{
			return new Variant128(leftValue.AsQuaternion() + rightValue.AsQuaternion());
		}

		throw new InvalidOperationException(
			$"AddResolver encountered unexpected result type '{resultType}'.");
	}
}
