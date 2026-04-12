// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the maximum of two nested <see cref="IPropertyResolver"/> operands. Supports all numeric types in
/// <see cref="Variant128"/>. Vector and quaternion types are not supported. When the two operands have different
/// numeric types, standard numeric promotion rules apply.
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
		allowVectors: false,
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

		throw new InvalidOperationException($"MaxResolver encountered unexpected result type '{resultType}'.");
	}
}
