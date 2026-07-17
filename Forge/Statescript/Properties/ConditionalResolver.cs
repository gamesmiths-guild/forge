// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves one of two values based on a boolean condition — the ternary select for the value lane.
/// </summary>
/// <remarks>
/// Only the selected branch resolver is evaluated. Both branches must produce the same value type.
/// </remarks>
/// <param name="condition">The resolver for the boolean condition.</param>
/// <param name="whenTrue">The resolver evaluated when the condition is <see langword="true"/>.</param>
/// <param name="whenFalse">The resolver evaluated when the condition is <see langword="false"/>.</param>
public class ConditionalResolver(
	IPropertyResolver condition,
	IPropertyResolver whenTrue,
	IPropertyResolver whenFalse) : IPropertyResolver
{
	private readonly IPropertyResolver _condition = condition;

	private readonly IPropertyResolver _whenTrue = whenTrue;

	private readonly IPropertyResolver _whenFalse = whenFalse;

	/// <inheritdoc/>
	public Type ValueType { get; } = whenTrue.ValueType == whenFalse.ValueType
		? whenTrue.ValueType
		: throw new ArgumentException(
			$"{nameof(ConditionalResolver)} requires both branches to produce the same value type, but got " +
			$"'{whenTrue.ValueType}' and '{whenFalse.ValueType}'.",
			nameof(whenFalse));

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		return _condition.Resolve(graphContext).AsBool()
			? _whenTrue.Resolve(graphContext)
			: _whenFalse.Resolve(graphContext);
	}
}
