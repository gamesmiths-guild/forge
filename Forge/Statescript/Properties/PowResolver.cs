// Copyright © Gamesmiths Guild.

using System.Numerics;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a value raised to a specified power using two nested <see cref="IPropertyResolver"/> operands. Supports
/// <see langword="float"/> and <see langword="double"/> types as well as component-wise operations for
/// <see cref="Vector2"/>, <see cref="Vector3"/>, and <see cref="Vector4"/>. Integer operand types are promoted to
/// <see langword="double"/>. Decimal and quaternion types are not supported.
/// </summary>
/// <param name="baseOperand">The resolver for the base operand.</param>
/// <param name="exponent">The resolver for the exponent operand.</param>
public class PowResolver(IPropertyResolver baseOperand, IPropertyResolver exponent) : IPropertyResolver
{
	private readonly IPropertyResolver _base = baseOperand;

	private readonly IPropertyResolver _exponent = exponent;

	/// <inheritdoc/>
	public Type ValueType { get; } = DetermineResultType(baseOperand.ValueType, exponent.ValueType);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		Variant128 baseValue = _base.Resolve(graphContext);
		Variant128 expValue = _exponent.Resolve(graphContext);
		Type resultType = ValueType;

		if (resultType == typeof(float))
		{
			return new Variant128(
				MathF.Pow(
					MathTypeUtils.ResolveAsFloat(_base.ValueType, baseValue),
					MathTypeUtils.ResolveAsFloat(_exponent.ValueType, expValue)));
		}

		if (resultType == typeof(double))
		{
			return new Variant128(
				Math.Pow(
					MathTypeUtils.ResolveAsDouble(_base.ValueType, baseValue),
					MathTypeUtils.ResolveAsDouble(_exponent.ValueType, expValue)));
		}

		if (resultType == typeof(Vector2))
		{
			Vector2 baseVector = baseValue.AsVector2();
			float exp = (float)MathTypeUtils.ResolveAsDouble(_exponent.ValueType, expValue);
			return new Variant128(new Vector2(
				MathF.Pow(baseVector.X, exp),
				MathF.Pow(baseVector.Y, exp)));
		}

		if (resultType == typeof(Vector3))
		{
			Vector3 baseVector = baseValue.AsVector3();
			float exp = (float)MathTypeUtils.ResolveAsDouble(_exponent.ValueType, expValue);
			return new Variant128(new Vector3(
				MathF.Pow(baseVector.X, exp),
				MathF.Pow(baseVector.Y, exp),
				MathF.Pow(baseVector.Z, exp)));
		}

		if (resultType == typeof(Vector4))
		{
			Vector4 baseVector = baseValue.AsVector4();
			float exp = (float)MathTypeUtils.ResolveAsDouble(_exponent.ValueType, expValue);
			return new Variant128(new Vector4(
				MathF.Pow(baseVector.X, exp),
				MathF.Pow(baseVector.Y, exp),
				MathF.Pow(baseVector.Z, exp),
				MathF.Pow(baseVector.W, exp)));
		}

		throw new InvalidOperationException($"PowResolver encountered unexpected result type '{resultType}'.");
	}

	private static Type DetermineResultType(Type baseType, Type exponentType)
	{
		if (MathTypeUtils.IsVectorType(baseType))
		{
			if (MathTypeUtils.IsVectorOrQuaternionType(exponentType))
			{
				throw new ArgumentException(
					$"PowResolver requires a scalar numeric exponent when the base is a vector. Got base '{baseType}' and exponent '{exponentType}'.");
			}

			if (!MathTypeUtils.IsNumericType(exponentType))
			{
				throw new ArgumentException(
					$"PowResolver does not support exponent type '{exponentType}'.");
			}

			if (exponentType == typeof(decimal))
			{
				throw new ArgumentException("PowResolver does not support decimal operands. Use double instead.");
			}

			return baseType;
		}

		if (MathTypeUtils.IsVectorOrQuaternionType(exponentType))
		{
			throw new ArgumentException(
				$"PowResolver does not support scalar bases with vector or quaternion exponents. Got base '{baseType}' and exponent '{exponentType}'.");
		}

		return MathTypeUtils.DetermineFloatingPointBinaryResultType(
			nameof(PowResolver),
			baseType,
			exponentType);
	}
}
