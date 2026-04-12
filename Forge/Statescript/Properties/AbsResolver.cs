// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the absolute value of a single <see cref="IPropertyResolver"/> operand. Supports all signed numeric types
/// in <see cref="Variant128"/>. Unsigned types, vector types, and quaternion types are not supported. Sub-int types
/// are promoted to <see langword="int"/> following standard C# rules.
/// </summary>
/// <param name="operand">The resolver for the operand.</param>
public class AbsResolver(IPropertyResolver operand) : IPropertyResolver
{
	private readonly IPropertyResolver _operand = operand;

	/// <inheritdoc/>
	public Type ValueType { get; } = DetermineResultType(operand.ValueType);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		Variant128 value = _operand.Resolve(graphContext);
		Type resultType = ValueType;

		if (resultType == typeof(int))
		{
			return new Variant128(Math.Abs(MathTypeUtils.ResolveAsInt(_operand.ValueType, value)));
		}

		if (resultType == typeof(long))
		{
			return new Variant128(Math.Abs(MathTypeUtils.ResolveAsLong(_operand.ValueType, value)));
		}

		if (resultType == typeof(float))
		{
			return new Variant128(Math.Abs(MathTypeUtils.ResolveAsFloat(_operand.ValueType, value)));
		}

		if (resultType == typeof(double))
		{
			return new Variant128(Math.Abs(MathTypeUtils.ResolveAsDouble(_operand.ValueType, value)));
		}

		if (resultType == typeof(decimal))
		{
			return new Variant128(Math.Abs(MathTypeUtils.ResolveAsDecimal(_operand.ValueType, value)));
		}

		throw new InvalidOperationException(
			$"AbsResolver encountered unexpected result type '{resultType}'.");
	}

	private static Type DetermineResultType(Type operandType)
	{
		if (IsUnsignedType(operandType))
		{
			throw new ArgumentException(
				$"AbsResolver does not support unsigned type '{operandType}'. " +
				"Absolute value is only meaningful for signed types.");
		}

		return MathTypeUtils.DetermineUnaryResultType(
			nameof(AbsResolver),
			operandType,
			allowVectors: false,
			allowQuaternions: false);
	}

	private static bool IsUnsignedType(Type type)
	{
		return type == typeof(byte)
			|| type == typeof(ushort)
			|| type == typeof(uint)
			|| type == typeof(ulong);
	}
}
