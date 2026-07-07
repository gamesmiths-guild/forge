// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the element at a given index of a nested array resolver. The index is itself a nested numeric resolver,
/// allowing both constant indices and computed ones (e.g. a variable or an <see cref="ElementIndexResolver"/>).
/// </summary>
/// <remarks>
/// If the index is out of range, a default <see cref="Variant128"/> (zero) is returned. Fractional index values are
/// truncated.
/// </remarks>
/// <param name="source">The resolver providing the source array.</param>
/// <param name="index">The resolver providing the zero-based element index. Must resolve to a numeric type.</param>
public class ElementAtResolver(IArrayPropertyResolver source, IPropertyResolver index) : IPropertyResolver
{
	private readonly IArrayPropertyResolver _source = source;

	private readonly IPropertyResolver _index =
		ArrayResolverUtils.ValidateNumericOperand(nameof(ElementAtResolver), nameof(index), index);

	/// <inheritdoc/>
	public Type ValueType { get; } = source.ElementType;

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		Variant128[] values = _source.ResolveArray(graphContext);
		int resolvedIndex = ArrayResolverUtils.ResolveInt(graphContext, _index);
		return resolvedIndex >= 0 && resolvedIndex < values.Length ? values[resolvedIndex] : default;
	}
}
