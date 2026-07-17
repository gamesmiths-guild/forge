// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a value bounced back and forth between 0 and a length using two nested <see cref="IPropertyResolver"/>
/// operands, producing a <see langword="float"/>.
/// </summary>
/// <remarks>
/// Non-positive lengths resolve to <c>0</c>.
/// </remarks>
/// <param name="value">The resolver for the value to bounce.</param>
/// <param name="length">The resolver for the bounce length.</param>
public class PingPongResolver(IPropertyResolver value, IPropertyResolver length) : IPropertyResolver
{
	private readonly IPropertyResolver _value = value;

	private readonly IPropertyResolver _length = length;

	/// <inheritdoc/>
	public Type ValueType { get; } = GameplayMathUtils.DetermineFloatOnlyResultType(
		nameof(PingPongResolver),
		value.ValueType,
		length.ValueType);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		float floatValue = MathTypeUtils.ResolveAsFloat(_value.ValueType, _value.Resolve(graphContext));
		float floatLength = MathTypeUtils.ResolveAsFloat(_length.ValueType, _length.Resolve(graphContext));

		return new Variant128(GameplayMathUtils.PingPong(floatValue, floatLength));
	}
}
