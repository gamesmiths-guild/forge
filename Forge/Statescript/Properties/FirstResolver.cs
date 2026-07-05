// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the first element of a nested array resolver.
/// </summary>
/// <remarks>
/// If the source array is empty, a default <see cref="Variant128"/> (zero) is returned.
/// </remarks>
/// <param name="source">The resolver providing the source array.</param>
public class FirstResolver(IArrayPropertyResolver source) : IPropertyResolver
{
	private readonly IArrayPropertyResolver _source = source;

	/// <inheritdoc/>
	public Type ValueType { get; } = source.ElementType;

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		Variant128[] values = _source.ResolveArray(graphContext);
		return values.Length > 0 ? values[0] : default;
	}
}
