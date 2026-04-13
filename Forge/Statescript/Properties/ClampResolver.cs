// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a value clamped between a minimum and maximum bound using three nested <see cref="IPropertyResolver"/>
/// operands. Supports all numeric types in <see cref="Variant128"/>. Vector and quaternion types are not supported.
/// When operands have different numeric types, standard numeric promotion rules apply.
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
	public Type ValueType { get; } = MathTypeUtils.DetermineTernaryResultType(
		nameof(ClampResolver),
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

		throw new InvalidOperationException($"ClampResolver encountered unexpected result type '{resultType}'.");
	}
}
