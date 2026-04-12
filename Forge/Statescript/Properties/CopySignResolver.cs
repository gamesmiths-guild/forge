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
			var mag = MathTypeUtils.ResolveAsFloat(_magnitude.ValueType, magnitudeValue);
			var sgn = MathTypeUtils.ResolveAsFloat(_sign.ValueType, signValue);
			var result = MathF.Abs(mag) * (sgn >= 0 ? 1.0f : -1.0f);
			return new Variant128(result);
		}

		if (resultType == typeof(double))
		{
			var mag = MathTypeUtils.ResolveAsDouble(_magnitude.ValueType, magnitudeValue);
			var sgn = MathTypeUtils.ResolveAsDouble(_sign.ValueType, signValue);
			var result = Math.Abs(mag) * (sgn >= 0 ? 1.0 : -1.0);
			return new Variant128(result);
		}

		throw new InvalidOperationException(
			$"CopySignResolver encountered unexpected result type '{resultType}'.");
	}
}
