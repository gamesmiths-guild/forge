// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the shortest signed angle difference between two angles in radians using two nested
/// <see cref="IPropertyResolver"/> operands, producing a <see langword="float"/> in <c>(-π, π]</c>.
/// </summary>
/// <param name="current">The resolver for the current angle in radians.</param>
/// <param name="target">The resolver for the target angle in radians.</param>
public class DeltaAngleResolver(IPropertyResolver current, IPropertyResolver target) : IPropertyResolver
{
	private readonly IPropertyResolver _current = current;

	private readonly IPropertyResolver _target = target;

	/// <inheritdoc/>
	public Type ValueType { get; } = GameplayMathUtils.DetermineFloatOnlyResultType(
		nameof(DeltaAngleResolver),
		current.ValueType,
		target.ValueType);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		float floatCurrent = MathTypeUtils.ResolveAsFloat(_current.ValueType, _current.Resolve(graphContext));
		float floatTarget = MathTypeUtils.ResolveAsFloat(_target.ValueType, _target.Resolve(graphContext));

		return new Variant128(GameplayMathUtils.DeltaAngle(floatCurrent, floatTarget));
	}
}
