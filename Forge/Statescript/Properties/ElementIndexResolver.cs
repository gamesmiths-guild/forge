// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the zero-based index of the array element currently being iterated by an enclosing array resolver. Use
/// this inside nested "lambda" resolvers for index-aware predicates and projections.
/// </summary>
/// <remarks>
/// If no array element is currently being iterated, a default <see cref="Variant128"/> (zero) is returned.
/// </remarks>
public class ElementIndexResolver : IPropertyResolver
{
	/// <inheritdoc/>
	public Type ValueType => typeof(int);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		return graphContext.TryGetCurrentElement(out ElementFrame frame) ? new Variant128(frame.Index) : default;
	}
}
