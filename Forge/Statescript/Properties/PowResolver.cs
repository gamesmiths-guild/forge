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
			Vector2 exponentVector = expValue.AsVector2();
			return new Variant128(new Vector2(
				MathF.Pow(baseVector.X, exponentVector.X),
				MathF.Pow(baseVector.Y, exponentVector.Y)));
		}

		if (resultType == typeof(Vector3))
		{
			Vector3 baseVector = baseValue.AsVector3();
			Vector3 exponentVector = expValue.AsVector3();
			return new Variant128(new Vector3(
				MathF.Pow(baseVector.X, exponentVector.X),
				MathF.Pow(baseVector.Y, exponentVector.Y),
				MathF.Pow(baseVector.Z, exponentVector.Z)));
		}

		if (resultType == typeof(Vector4))
		{
			Vector4 baseVector = baseValue.AsVector4();
			Vector4 exponentVector = expValue.AsVector4();
			return new Variant128(new Vector4(
				MathF.Pow(baseVector.X, exponentVector.X),
				MathF.Pow(baseVector.Y, exponentVector.Y),
				MathF.Pow(baseVector.Z, exponentVector.Z),
				MathF.Pow(baseVector.W, exponentVector.W)));
		}

		throw new InvalidOperationException($"PowResolver encountered unexpected result type '{resultType}'.");
	}

	private static Type DetermineResultType(Type baseType, Type exponentType)
	{
		if (baseType == exponentType && MathTypeUtils.IsVectorType(baseType))
		{
			return baseType;
		}

		return MathTypeUtils.DetermineFloatingPointBinaryResultType(
			nameof(PowResolver),
			baseType,
			exponentType);
	}
}
