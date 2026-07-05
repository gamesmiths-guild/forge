// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a nested object-backed array resolver with the element at a given index removed. The index is itself a
/// nested numeric resolver, allowing both constant indices and computed ones.
/// </summary>
/// <remarks>
/// If the index is out of range, the source array is returned unchanged. Fractional index values are truncated.
/// </remarks>
/// <typeparam name="T">The element type to read.</typeparam>
/// <param name="source">The resolver providing the source array.</param>
/// <param name="index">The resolver providing the zero-based index to remove. Must resolve to a numeric type.</param>
public class ObjectRemoveAtResolver<T>(IObjectArrayResolver<T> source, IPropertyResolver index)
	: ObjectArrayResolver<T>
{
	private readonly IObjectArrayResolver<T> _source = source;

	private readonly IPropertyResolver _index =
		ArrayResolverUtils.ValidateNumericOperand(nameof(ObjectRemoveAtResolver<T>), nameof(index), index);

	/// <inheritdoc/>
	public override T[] ResolveArray(GraphContext graphContext)
	{
		T[] values = _source.ResolveArray(graphContext);
		int resolvedIndex = ArrayResolverUtils.ResolveInt(graphContext, _index);

		if (resolvedIndex < 0 || resolvedIndex >= values.Length)
		{
			return values;
		}

		var result = new T[values.Length - 1];
		Array.Copy(values, result, resolvedIndex);
		Array.Copy(values, resolvedIndex + 1, result, resolvedIndex, values.Length - resolvedIndex - 1);
		return result;
	}
}
