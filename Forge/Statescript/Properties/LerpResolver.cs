// Copyright © Gamesmiths Guild.

using System.Numerics;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a linear interpolation between two values using three nested <see cref="IPropertyResolver"/> operands.
/// Computes <c>a + (b - a) * t</c> for scalar types, or uses the built-in <c>Lerp</c> methods for
/// <see cref="Vector2"/>, <see cref="Vector3"/>, <see cref="Vector4"/>, and <see cref="Quaternion"/>. Supports
/// <see langword="float"/> and <see langword="double"/> scalar types. For vector and quaternion types, the <c>t</c>
/// parameter must be <see langword="float"/>. Integer and decimal types are not supported.
/// </summary>
/// <param name="a">The resolver for the start value.</param>
/// <param name="b">The resolver for the end value.</param>
/// <param name="t">The resolver for the interpolation parameter (typically 0 to 1).</param>
public class LerpResolver(IPropertyResolver a, IPropertyResolver b, IPropertyResolver t) : IPropertyResolver
{
	private readonly IPropertyResolver _a = a;

	private readonly IPropertyResolver _b = b;

	private readonly IPropertyResolver _t = t;

	/// <inheritdoc/>
	public Type ValueType { get; } = DetermineLerpResultType(a.ValueType, b.ValueType, t.ValueType);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		Variant128 aValue = _a.Resolve(graphContext);
		Variant128 bValue = _b.Resolve(graphContext);
		Variant128 tValue = _t.Resolve(graphContext);
		Type resultType = ValueType;

		if (resultType == typeof(float))
		{
			float floatA = MathTypeUtils.ResolveAsFloat(_a.ValueType, aValue);
			float floatB = MathTypeUtils.ResolveAsFloat(_b.ValueType, bValue);
			float floatT = MathTypeUtils.ResolveAsFloat(_t.ValueType, tValue);
			return new Variant128(floatA + ((floatB - floatA) * floatT));
		}

		if (resultType == typeof(double))
		{
			double doubleA = MathTypeUtils.ResolveAsDouble(_a.ValueType, aValue);
			double doubleB = MathTypeUtils.ResolveAsDouble(_b.ValueType, bValue);
			double doubleT = MathTypeUtils.ResolveAsDouble(_t.ValueType, tValue);
			return new Variant128(doubleA + ((doubleB - doubleA) * doubleT));
		}

		if (resultType == typeof(Vector2))
		{
			return new Variant128(
				Vector2.Lerp(aValue.AsVector2(), bValue.AsVector2(), tValue.AsFloat()));
		}

		if (resultType == typeof(Vector3))
		{
			return new Variant128(
				Vector3.Lerp(aValue.AsVector3(), bValue.AsVector3(), tValue.AsFloat()));
		}

		if (resultType == typeof(Vector4))
		{
			return new Variant128(
				Vector4.Lerp(aValue.AsVector4(), bValue.AsVector4(), tValue.AsFloat()));
		}

		if (resultType == typeof(Quaternion))
		{
			return new Variant128(
				Quaternion.Lerp(aValue.AsQuaternion(), bValue.AsQuaternion(), tValue.AsFloat()));
		}

		throw new InvalidOperationException($"LerpResolver encountered unexpected result type '{resultType}'.");
	}

	private static Type DetermineLerpResultType(Type aType, Type bType, Type tType)
	{
		if (MathTypeUtils.IsVectorOrQuaternionType(aType)
			|| MathTypeUtils.IsVectorOrQuaternionType(bType))
		{
			if (aType != bType)
			{
				throw new ArgumentException(
					$"LerpResolver requires matching types for 'a' and 'b'. Got '{aType}' and '{bType}'.");
			}

			if (tType != typeof(float))
			{
				throw new ArgumentException(
					$"LerpResolver requires 't' to be float for vector/quaternion lerp. Got '{tType}'.");
			}

			return aType;
		}

		ValidateScalarOperandType(aType, "a");
		ValidateScalarOperandType(bType, "b");
		ValidateScalarOperandType(tType, "t");

		if (aType == typeof(double) || bType == typeof(double) || tType == typeof(double))
		{
			return typeof(double);
		}

		return typeof(float);
	}

	private static void ValidateScalarOperandType(Type type, string paramName)
	{
		if (type != typeof(float) && type != typeof(double))
		{
			throw new ArgumentException(
				"LerpResolver only supports float, double, Vector2, Vector3, Vector4, and Quaternion " +
				$"operands. Parameter '{paramName}' has type '{type}'.");
		}
	}
}
