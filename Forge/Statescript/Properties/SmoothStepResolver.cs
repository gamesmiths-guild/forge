// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the smooth Hermite interpolation of a value between two edges using three nested
/// <see cref="IPropertyResolver"/> operands, producing a <see langword="float"/> from 0 to 1.
/// </summary>
/// <param name="edge0">The resolver for the lower edge.</param>
/// <param name="edge1">The resolver for the upper edge.</param>
/// <param name="value">The resolver for the value to interpolate.</param>
public class SmoothStepResolver(IPropertyResolver edge0, IPropertyResolver edge1, IPropertyResolver value)
	: IPropertyResolver
{
	private readonly IPropertyResolver _edge0 = edge0;

	private readonly IPropertyResolver _edge1 = edge1;

	private readonly IPropertyResolver _value = value;

	/// <inheritdoc/>
	public Type ValueType { get; } = GameplayMathUtils.DetermineFloatOnlyResultType(
		nameof(SmoothStepResolver),
		edge0.ValueType,
		edge1.ValueType,
		value.ValueType);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		float floatEdge0 = MathTypeUtils.ResolveAsFloat(_edge0.ValueType, _edge0.Resolve(graphContext));
		float floatEdge1 = MathTypeUtils.ResolveAsFloat(_edge1.ValueType, _edge1.Resolve(graphContext));
		float floatValue = MathTypeUtils.ResolveAsFloat(_value.ValueType, _value.Resolve(graphContext));

		return new Variant128(GameplayMathUtils.SmoothStep(floatEdge0, floatEdge1, floatValue));
	}
}
