// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the normalized position of a value within a range using three nested <see cref="IPropertyResolver"/>
/// operands. Computes <c>(value - a) / (b - a)</c> clamped to 0-1 — the inverse of a Lerp.
/// </summary>
/// <remarks>
/// Numeric operands are promoted to <see langword="float"/>, or <see langword="double"/> when any operand is a
/// <see langword="double"/>. When <paramref name="a"/> and <paramref name="b"/> resolve to the same value, the result
/// is <c>0</c>.
/// </remarks>
/// <param name="a">The resolver for the range start.</param>
/// <param name="b">The resolver for the range end.</param>
/// <param name="value">The resolver for the value to normalize.</param>
public class InverseLerpResolver(IPropertyResolver a, IPropertyResolver b, IPropertyResolver value)
	: IPropertyResolver
{
	private readonly IPropertyResolver _a = a;

	private readonly IPropertyResolver _b = b;

	private readonly IPropertyResolver _value = value;

	/// <inheritdoc/>
	public Type ValueType { get; } = GameplayMathUtils.DetermineFloatingResultType(
		nameof(InverseLerpResolver),
		a.ValueType,
		b.ValueType,
		value.ValueType);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		if (ValueType == typeof(double))
		{
			double doubleA = MathTypeUtils.ResolveAsDouble(_a.ValueType, _a.Resolve(graphContext));
			double doubleB = MathTypeUtils.ResolveAsDouble(_b.ValueType, _b.Resolve(graphContext));
			double doubleValue = MathTypeUtils.ResolveAsDouble(_value.ValueType, _value.Resolve(graphContext));

			if (Math.Abs(doubleB - doubleA) <= double.Epsilon)
			{
				return new Variant128(0d);
			}

			return new Variant128(Math.Clamp((doubleValue - doubleA) / (doubleB - doubleA), 0d, 1d));
		}

		float floatA = MathTypeUtils.ResolveAsFloat(_a.ValueType, _a.Resolve(graphContext));
		float floatB = MathTypeUtils.ResolveAsFloat(_b.ValueType, _b.Resolve(graphContext));
		float floatValue = MathTypeUtils.ResolveAsFloat(_value.ValueType, _value.Resolve(graphContext));

		if (MathF.Abs(floatB - floatA) <= float.Epsilon)
		{
			return new Variant128(0f);
		}

		return new Variant128(Math.Clamp((floatValue - floatA) / (floatB - floatA), 0f, 1f));
	}
}
