// Copyright © Gamesmiths Guild.

using System.Numerics;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a value clamped between a minimum and maximum bound using three nested <see cref="IPropertyResolver"/>
/// operands. Supports all numeric types in <see cref="Variant128"/> as well as <see cref="Vector2"/>,
/// <see cref="Vector3"/>, and <see cref="Vector4"/>. Quaternion types are not supported. When numeric operands have
/// different types, standard numeric promotion rules apply.
/// </summary>
/// <param name="value">The resolver for the value to clamp.</param>
/// <param name="min">The resolver for the minimum bound.</param>
/// <param name="max">The resolver for the maximum bound.</param>
public class ClampResolver(IPropertyResolver value, IPropertyResolver min, IPropertyResolver max)
	: IPropertyResolver
{
	private readonly IPropertyResolver _value = value;

	private readonly IPropertyResolver _min = min;

	private readonly IPropertyResolver _max = max;

	/// <inheritdoc/>
	public Type ValueType { get; } = DetermineResultType(
		  value.ValueType,
		  min.ValueType,
		  max.ValueType);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		Variant128 valueToClamp = _value.Resolve(graphContext);
		Variant128 minValue = _min.Resolve(graphContext);
		Variant128 maxValue = _max.Resolve(graphContext);
		Type resultType = ValueType;

		if (resultType == typeof(int))
		{
			return new Variant128(
				Math.Clamp(
					MathTypeUtils.ResolveAsInt(_value.ValueType, valueToClamp),
					MathTypeUtils.ResolveAsInt(_min.ValueType, minValue),
					MathTypeUtils.ResolveAsInt(_max.ValueType, maxValue)));
		}

		if (resultType == typeof(long))
		{
			return new Variant128(
				Math.Clamp(
					MathTypeUtils.ResolveAsLong(_value.ValueType, valueToClamp),
					MathTypeUtils.ResolveAsLong(_min.ValueType, minValue),
					MathTypeUtils.ResolveAsLong(_max.ValueType, maxValue)));
		}

		if (resultType == typeof(float))
		{
			return new Variant128(
				Math.Clamp(
					MathTypeUtils.ResolveAsFloat(_value.ValueType, valueToClamp),
					MathTypeUtils.ResolveAsFloat(_min.ValueType, minValue),
					MathTypeUtils.ResolveAsFloat(_max.ValueType, maxValue)));
		}

		if (resultType == typeof(double))
		{
			return new Variant128(
				Math.Clamp(
					MathTypeUtils.ResolveAsDouble(_value.ValueType, valueToClamp),
					MathTypeUtils.ResolveAsDouble(_min.ValueType, minValue),
					MathTypeUtils.ResolveAsDouble(_max.ValueType, maxValue)));
		}

		if (resultType == typeof(decimal))
		{
			return new Variant128(
				Math.Clamp(
					MathTypeUtils.ResolveAsDecimal(_value.ValueType, valueToClamp),
					MathTypeUtils.ResolveAsDecimal(_min.ValueType, minValue),
					MathTypeUtils.ResolveAsDecimal(_max.ValueType, maxValue)));
		}

		if (resultType == typeof(Vector2))
		{
			return new Variant128(
				Vector2.Clamp(valueToClamp.AsVector2(), minValue.AsVector2(), maxValue.AsVector2()));
		}

		if (resultType == typeof(Vector3))
		{
			return new Variant128(
				Vector3.Clamp(valueToClamp.AsVector3(), minValue.AsVector3(), maxValue.AsVector3()));
		}

		if (resultType == typeof(Vector4))
		{
			return new Variant128(
				Vector4.Clamp(valueToClamp.AsVector4(), minValue.AsVector4(), maxValue.AsVector4()));
		}

		throw new InvalidOperationException($"ClampResolver encountered unexpected result type '{resultType}'.");
	}

	private static Type DetermineResultType(Type valueType, Type minType, Type maxType)
	{
		if (MathTypeUtils.IsVectorType(valueType)
			|| MathTypeUtils.IsVectorType(minType)
			|| MathTypeUtils.IsVectorType(maxType))
		{
			if (valueType != minType || valueType != maxType)
			{
				throw new ArgumentException(
					$"ClampResolver requires matching vector types for value, min, and max. Got '{valueType}', " +
					$"'{minType}', and '{maxType}'.");
			}

			if (!MathTypeUtils.IsVectorType(valueType))
			{
				throw new ArgumentException(
					$"ClampResolver only supports Vector2, Vector3, and Vector4 vector operands. Got '{valueType}'.");
			}

			return valueType;
		}

		return MathTypeUtils.DetermineTernaryResultType(
			nameof(ClampResolver),
			valueType,
			minType,
			maxType);
	}
}
