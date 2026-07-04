// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a <see cref="bool"/> indicating whether two nested object-backed resolvers produce the same instance,
/// using reference identity. Use this to check whether two object variables point at the same entity, effect, or
/// handle, e.g. "was this effect applied to the same target?".
/// </summary>
/// <remarks>
/// Comparison uses <see cref="object.ReferenceEquals(object, object)"/>: two <see langword="null"/> results compare as
/// equal. Combine with <see cref="IsValidResolver"/> when missing values must not count as a match.
/// </remarks>
/// <param name="left">The resolver for the left operand of the comparison.</param>
/// <param name="right">The resolver for the right operand of the comparison.</param>
public class ObjectEqualsResolver(IObjectResolver left, IObjectResolver right) : IPropertyResolver
{
	private readonly IObjectResolver _left = left ?? throw new ArgumentNullException(nameof(left));

	private readonly IObjectResolver _right = right ?? throw new ArgumentNullException(nameof(right));

	/// <inheritdoc/>
	public Type ValueType => typeof(bool);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		return new Variant128(ReferenceEquals(_left.Resolve(graphContext), _right.Resolve(graphContext)));
	}
}
