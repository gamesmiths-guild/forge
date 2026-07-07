// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the elements of a nested object-backed array resolver after skipping the first N. The count is itself a
/// nested numeric resolver, allowing both constant and computed counts.
/// </summary>
/// <remarks>
/// Counts are clamped to the source length; negative counts skip nothing. Fractional count values are truncated.
/// </remarks>
/// <typeparam name="T">The element type to read.</typeparam>
/// <param name="source">The resolver providing the source array.</param>
/// <param name="count">The resolver providing the number of elements to skip. Must resolve to a numeric type.</param>
public class ObjectSkipResolver<T>(IObjectArrayResolver<T> source, IPropertyResolver count) : ObjectArrayResolver<T>
{
	private readonly IObjectArrayResolver<T> _source = source;

	private readonly IPropertyResolver _count =
		ArrayResolverUtils.ValidateNumericOperand(nameof(ObjectSkipResolver<T>), nameof(count), count);

	/// <inheritdoc/>
	public override T[] ResolveArray(GraphContext graphContext)
	{
		T[] values = _source.ResolveArray(graphContext);
		int resolvedCount = Math.Clamp(ArrayResolverUtils.ResolveInt(graphContext, _count), 0, values.Length);

		if (resolvedCount == 0)
		{
			return values;
		}

		var result = new T[values.Length - resolvedCount];
		Array.Copy(values, resolvedCount, result, 0, result.Length);
		return result;
	}
}
