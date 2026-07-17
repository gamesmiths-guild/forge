// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a value wrapped into a range using three nested <see cref="IPropertyResolver"/> operands, producing a
/// <see langword="float"/> in <c>[min, max)</c>.
/// </summary>
/// <remarks>
/// Non-positive ranges resolve to <c>min</c>.
/// </remarks>
/// <param name="value">The resolver for the value to wrap.</param>
/// <param name="min">The resolver for the range start (inclusive).</param>
/// <param name="max">The resolver for the range end (exclusive).</param>
public class WrapResolver(IPropertyResolver value, IPropertyResolver min, IPropertyResolver max) : IPropertyResolver
{
	private readonly IPropertyResolver _value = value;

	private readonly IPropertyResolver _min = min;

	private readonly IPropertyResolver _max = max;

	/// <inheritdoc/>
	public Type ValueType { get; } = GameplayMathUtils.DetermineFloatOnlyResultType(
		nameof(WrapResolver),
		value.ValueType,
		min.ValueType,
		max.ValueType);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		float floatValue = MathTypeUtils.ResolveAsFloat(_value.ValueType, _value.Resolve(graphContext));
		float floatMin = MathTypeUtils.ResolveAsFloat(_min.ValueType, _min.Resolve(graphContext));
		float floatMax = MathTypeUtils.ResolveAsFloat(_max.ValueType, _max.Resolve(graphContext));

		return new Variant128(GameplayMathUtils.Wrap(floatValue, floatMin, floatMax));
	}
}
