// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the sign of a single <see cref="IPropertyResolver"/> operand, returning <c>-1</c>, <c>0</c>, or <c>1</c>
/// as an <see langword="int"/>. Supports all numeric types in <see cref="Variant128"/>. Vector and quaternion types
/// are not supported. The result is always <see langword="int"/> regardless of the operand type.
/// </summary>
/// <param name="operand">The resolver for the operand.</param>
public class SignResolver(IPropertyResolver operand) : IPropertyResolver
{
	private readonly IPropertyResolver _operand = operand;

	/// <inheritdoc/>
	public Type ValueType { get; } = DetermineResultType(operand.ValueType);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		Variant128 value = _operand.Resolve(graphContext);
		Type operandType = _operand.ValueType;

		if (operandType == typeof(float))
		{
			return new Variant128(MathF.Sign(value.AsFloat()));
		}

		if (operandType == typeof(double))
		{
			return new Variant128(Math.Sign(value.AsDouble()));
		}

		if (operandType == typeof(decimal))
		{
			return new Variant128(Math.Sign(value.AsDecimal()));
		}

		if (operandType == typeof(long))
		{
			return new Variant128(Math.Sign(value.AsLong()));
		}

		if (operandType == typeof(int)
			|| operandType == typeof(short)
			|| operandType == typeof(sbyte))
		{
			return new Variant128(Math.Sign(MathTypeUtils.ResolveAsInt(operandType, value)));
		}

		throw new InvalidOperationException(
			$"SignResolver encountered unexpected operand type '{operandType}'.");
	}

	private static Type DetermineResultType(Type operandType)
	{
		if (MathTypeUtils.IsVectorOrQuaternionType(operandType))
		{
			throw new ArgumentException(
				$"SignResolver does not support operand type '{operandType}'.");
		}

		if (!MathTypeUtils.IsNumericType(operandType))
		{
			throw new ArgumentException(
				$"SignResolver does not support operand type '{operandType}'.");
		}

		if (operandType == typeof(byte)
			|| operandType == typeof(ushort)
			|| operandType == typeof(uint)
			|| operandType == typeof(ulong))
		{
			throw new ArgumentException(
				$"SignResolver does not support unsigned type '{operandType}'. " +
				"Sign is only meaningful for signed types.");
		}

		return typeof(int);
	}
}
