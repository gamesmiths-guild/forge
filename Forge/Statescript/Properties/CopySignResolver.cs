// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a value with the magnitude of the first operand and the sign of the second operand using two nested
/// <see cref="IPropertyResolver"/> operands. Computes <c>CopySign(magnitude, sign)</c>. Supports
/// <see langword="float"/> and <see langword="double"/> types. Integer operand types are promoted to
/// <see langword="double"/>. Decimal, vector, and quaternion types are not supported.
/// </summary>
/// <param name="magnitude">The resolver for the magnitude operand.</param>
/// <param name="sign">The resolver for the sign operand.</param>
public class CopySignResolver(IPropertyResolver magnitude, IPropertyResolver sign) : IPropertyResolver
{
	private readonly IPropertyResolver _magnitude = magnitude;

	private readonly IPropertyResolver _sign = sign;

	/// <inheritdoc/>
	public Type ValueType { get; } = MathTypeUtils.DetermineFloatingPointBinaryResultType(
		nameof(CopySignResolver),
		magnitude.ValueType,
		sign.ValueType);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		Variant128 magnitudeValue = _magnitude.Resolve(graphContext);
		Variant128 signValue = _sign.Resolve(graphContext);
		Type resultType = ValueType;

		if (resultType == typeof(float))
		{
			float mag = MathTypeUtils.ResolveAsFloat(_magnitude.ValueType, magnitudeValue);
			float sgn = MathTypeUtils.ResolveAsFloat(_sign.ValueType, signValue);
			return new Variant128(CopySignFloat(mag, sgn));
		}

		if (resultType == typeof(double))
		{
			double mag = MathTypeUtils.ResolveAsDouble(_magnitude.ValueType, magnitudeValue);
			double sgn = MathTypeUtils.ResolveAsDouble(_sign.ValueType, signValue);
			return new Variant128(CopySignDouble(mag, sgn));
		}

		throw new InvalidOperationException(
			$"CopySignResolver encountered unexpected result type '{resultType}'.");
	}

	private static float CopySignFloat(float magnitude, float sign)
	{
		const int signMask = unchecked((int)0x80000000);
		int magnitudeBits = BitConverter.SingleToInt32Bits(magnitude);
		int signBits = BitConverter.SingleToInt32Bits(sign);
		return BitConverter.Int32BitsToSingle((magnitudeBits & ~signMask) | (signBits & signMask));
	}

	private static double CopySignDouble(double magnitude, double sign)
	{
		const long signMask = unchecked((long)0x8000000000000000);
		long magnitudeBits = BitConverter.DoubleToInt64Bits(magnitude);
		long signBits = BitConverter.DoubleToInt64Bits(sign);
		return BitConverter.Int64BitsToDouble((magnitudeBits & ~signMask) | (signBits & signMask));
	}
}
