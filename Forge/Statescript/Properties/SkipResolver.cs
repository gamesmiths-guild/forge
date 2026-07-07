// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the elements of a nested array resolver after skipping the first N. The count is itself a nested numeric
/// resolver, allowing both constant and computed counts.
/// </summary>
/// <remarks>
/// Counts are clamped to the source length; negative counts skip nothing. Fractional count values are truncated.
/// </remarks>
/// <param name="source">The resolver providing the source array.</param>
/// <param name="count">The resolver providing the number of elements to skip. Must resolve to a numeric type.</param>
public class SkipResolver(IArrayPropertyResolver source, IPropertyResolver count) : IArrayPropertyResolver
{
	private readonly IArrayPropertyResolver _source = source;

	private readonly IPropertyResolver _count =
		ArrayResolverUtils.ValidateNumericOperand(nameof(SkipResolver), nameof(count), count);

	/// <inheritdoc/>
	public Type ElementType { get; } = source.ElementType;

	/// <inheritdoc/>
	public Variant128[] ResolveArray(GraphContext graphContext)
	{
		Variant128[] values = _source.ResolveArray(graphContext);
		int resolvedCount = Math.Clamp(ArrayResolverUtils.ResolveInt(graphContext, _count), 0, values.Length);

		if (resolvedCount == 0)
		{
			return values;
		}

		var result = new Variant128[values.Length - resolvedCount];
		Array.Copy(values, resolvedCount, result, 0, result.Length);
		return result;
	}
}
