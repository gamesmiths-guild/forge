// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the value-typed array element currently being iterated by an enclosing array resolver (e.g.
/// <see cref="WhereResolver"/>, <see cref="OrderByResolver"/>, <see cref="SelectResolver"/>). Use this inside nested
/// "lambda" resolvers as the stand-in for the lambda parameter.
/// </summary>
/// <remarks>
/// If no array element is currently being iterated (the resolver is evaluated outside an array operation), a default
/// <see cref="Variant128"/> (zero) is returned.
/// </remarks>
/// <param name="valueType">The element type this resolver produces. Must match the iterated array's element type.
/// </param>
public class ElementValueResolver(Type valueType) : IPropertyResolver
{
	/// <inheritdoc/>
	public Type ValueType { get; } = valueType;

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		return graphContext.TryGetCurrentElement(out ElementFrame frame) ? frame.Value : default;
	}
}
