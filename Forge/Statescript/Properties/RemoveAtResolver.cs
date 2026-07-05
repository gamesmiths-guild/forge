// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a nested array resolver with the element at a given index removed. The index is itself a nested numeric
/// resolver, allowing both constant indices and computed ones.
/// </summary>
/// <remarks>
/// If the index is out of range, the source array is returned unchanged. Fractional index values are truncated.
/// </remarks>
/// <param name="source">The resolver providing the source array.</param>
/// <param name="index">The resolver providing the zero-based index to remove. Must resolve to a numeric type.</param>
public class RemoveAtResolver(IArrayPropertyResolver source, IPropertyResolver index) : IArrayPropertyResolver
{
	private readonly IArrayPropertyResolver _source = source;

	private readonly IPropertyResolver _index =
		ArrayResolverUtils.ValidateNumericOperand(nameof(RemoveAtResolver), nameof(index), index);

	/// <inheritdoc/>
	public Type ElementType { get; } = source.ElementType;

	/// <inheritdoc/>
	public Variant128[] ResolveArray(GraphContext graphContext)
	{
		Variant128[] values = _source.ResolveArray(graphContext);
		int resolvedIndex = ArrayResolverUtils.ResolveInt(graphContext, _index);

		if (resolvedIndex < 0 || resolvedIndex >= values.Length)
		{
			return values;
		}

		var result = new Variant128[values.Length - 1];
		Array.Copy(values, result, resolvedIndex);
		Array.Copy(values, resolvedIndex + 1, result, resolvedIndex, values.Length - resolvedIndex - 1);
		return result;
	}
}
