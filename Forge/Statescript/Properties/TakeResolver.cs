// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the first N elements of a nested array resolver. The count is itself a nested numeric resolver, allowing
/// both constant and computed counts.
/// </summary>
/// <remarks>
/// Counts are clamped to the source length; negative counts produce an empty array. Fractional count values are
/// truncated.
/// </remarks>
/// <param name="source">The resolver providing the source array.</param>
/// <param name="count">The resolver providing the number of elements to keep. Must resolve to a numeric type.</param>
public class TakeResolver(IArrayPropertyResolver source, IPropertyResolver count) : IArrayPropertyResolver
{
	private readonly IArrayPropertyResolver _source = source;

	private readonly IPropertyResolver _count =
		ArrayResolverUtils.ValidateNumericOperand(nameof(TakeResolver), nameof(count), count);

	/// <inheritdoc/>
	public Type ElementType { get; } = source.ElementType;

	/// <inheritdoc/>
	public Variant128[] ResolveArray(GraphContext graphContext)
	{
		Variant128[] values = _source.ResolveArray(graphContext);
		int resolvedCount = Math.Clamp(ArrayResolverUtils.ResolveInt(graphContext, _count), 0, values.Length);

		if (resolvedCount == values.Length)
		{
			return values;
		}

		var result = new Variant128[resolvedCount];
		Array.Copy(values, result, resolvedCount);
		return result;
	}
}
