// Copyright © Gamesmiths Guild.

using System.Numerics;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the arithmetic negation of a single <see cref="IPropertyResolver"/> operand. Supports all numeric types
/// in <see cref="Variant128"/> as well as <see cref="Vector2"/>, <see cref="Vector3"/>, <see cref="Vector4"/>, and
/// <see cref="Quaternion"/>. Sub-int types are promoted to <see langword="int"/> following standard C# rules.
/// </summary>
/// <param name="operand">The resolver for the operand to negate.</param>
public class NegateResolver(IPropertyResolver operand) : IPropertyResolver
{
	private readonly IPropertyResolver _operand = operand;

	/// <inheritdoc/>
	public Type ValueType { get; } = MathTypeUtils.DetermineUnaryResultType(
		nameof(NegateResolver),
		operand.ValueType);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		Variant128 value = _operand.Resolve(graphContext);
		Type resultType = ValueType;

		if (resultType == typeof(int))
		{
			return new Variant128(-MathTypeUtils.ResolveAsInt(_operand.ValueType, value));
		}

		if (resultType == typeof(long))
		{
			return new Variant128(-MathTypeUtils.ResolveAsLong(_operand.ValueType, value));
		}

		if (resultType == typeof(float))
		{
			return new Variant128(-MathTypeUtils.ResolveAsFloat(_operand.ValueType, value));
		}

		if (resultType == typeof(double))
		{
			return new Variant128(-MathTypeUtils.ResolveAsDouble(_operand.ValueType, value));
		}

		if (resultType == typeof(decimal))
		{
			return new Variant128(-MathTypeUtils.ResolveAsDecimal(_operand.ValueType, value));
		}

		if (resultType == typeof(Vector2))
		{
			return new Variant128(-value.AsVector2());
		}

		if (resultType == typeof(Vector3))
		{
			return new Variant128(-value.AsVector3());
		}

		if (resultType == typeof(Vector4))
		{
			return new Variant128(-value.AsVector4());
		}

		if (resultType == typeof(Quaternion))
		{
			return new Variant128(-value.AsQuaternion());
		}

		throw new InvalidOperationException(
			$"NegateResolver encountered unexpected result type '{resultType}'.");
	}
}
