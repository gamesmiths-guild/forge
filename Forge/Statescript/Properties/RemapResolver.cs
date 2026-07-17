// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a value remapped from an input range to an output range using five nested
/// <see cref="IPropertyResolver"/> operands. Computes
/// <c>outMin + (value - inMin) / (inMax - inMin) * (outMax - outMin)</c>.
/// </summary>
/// <remarks>
/// Numeric operands are promoted to <see langword="float"/>, or <see langword="double"/> when any operand is a
/// <see langword="double"/>. When the input range is degenerate, the result is <paramref name="outMin"/>. Values
/// outside the input range extrapolate unless <paramref name="clamp"/> is enabled.
/// </remarks>
/// <param name="value">The resolver for the value to remap.</param>
/// <param name="inMin">The resolver for the input range start.</param>
/// <param name="inMax">The resolver for the input range end.</param>
/// <param name="outMin">The resolver for the output range start.</param>
/// <param name="outMax">The resolver for the output range end.</param>
/// <param name="clamp">Whether to clamp the result to the output range.</param>
public class RemapResolver(
	IPropertyResolver value,
	IPropertyResolver inMin,
	IPropertyResolver inMax,
	IPropertyResolver outMin,
	IPropertyResolver outMax,
	bool clamp = false) : IPropertyResolver
{
	private readonly IPropertyResolver _value = value;

	private readonly IPropertyResolver _inMin = inMin;

	private readonly IPropertyResolver _inMax = inMax;

	private readonly IPropertyResolver _outMin = outMin;

	private readonly IPropertyResolver _outMax = outMax;

	private readonly bool _clamp = clamp;

	/// <inheritdoc/>
	public Type ValueType { get; } = GameplayMathUtils.DetermineFloatingResultType(
		nameof(RemapResolver),
		value.ValueType,
		inMin.ValueType,
		inMax.ValueType,
		outMin.ValueType,
		outMax.ValueType);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		if (ValueType == typeof(double))
		{
			double doubleValue = MathTypeUtils.ResolveAsDouble(_value.ValueType, _value.Resolve(graphContext));
			double doubleInMin = MathTypeUtils.ResolveAsDouble(_inMin.ValueType, _inMin.Resolve(graphContext));
			double doubleInMax = MathTypeUtils.ResolveAsDouble(_inMax.ValueType, _inMax.Resolve(graphContext));
			double doubleOutMin = MathTypeUtils.ResolveAsDouble(_outMin.ValueType, _outMin.Resolve(graphContext));
			double doubleOutMax = MathTypeUtils.ResolveAsDouble(_outMax.ValueType, _outMax.Resolve(graphContext));

			if (Math.Abs(doubleInMax - doubleInMin) <= double.Epsilon)
			{
				return new Variant128(doubleOutMin);
			}

			double result = doubleOutMin
				+ ((doubleValue - doubleInMin) / (doubleInMax - doubleInMin) * (doubleOutMax - doubleOutMin));

			if (_clamp)
			{
				result = doubleOutMin <= doubleOutMax
					? Math.Clamp(result, doubleOutMin, doubleOutMax)
					: Math.Clamp(result, doubleOutMax, doubleOutMin);
			}

			return new Variant128(result);
		}

		float floatValue = MathTypeUtils.ResolveAsFloat(_value.ValueType, _value.Resolve(graphContext));
		float floatInMin = MathTypeUtils.ResolveAsFloat(_inMin.ValueType, _inMin.Resolve(graphContext));
		float floatInMax = MathTypeUtils.ResolveAsFloat(_inMax.ValueType, _inMax.Resolve(graphContext));
		float floatOutMin = MathTypeUtils.ResolveAsFloat(_outMin.ValueType, _outMin.Resolve(graphContext));
		float floatOutMax = MathTypeUtils.ResolveAsFloat(_outMax.ValueType, _outMax.Resolve(graphContext));

		if (MathF.Abs(floatInMax - floatInMin) <= float.Epsilon)
		{
			return new Variant128(floatOutMin);
		}

		float floatResult = floatOutMin
			+ ((floatValue - floatInMin) / (floatInMax - floatInMin) * (floatOutMax - floatOutMin));

		if (_clamp)
		{
			floatResult = floatOutMin <= floatOutMax
				? Math.Clamp(floatResult, floatOutMin, floatOutMax)
				: Math.Clamp(floatResult, floatOutMax, floatOutMin);
		}

		return new Variant128(floatResult);
	}
}
