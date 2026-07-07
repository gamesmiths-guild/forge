// Copyright © Gamesmiths Guild.

using System.Diagnostics.CodeAnalysis;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the element at a given index of a nested object-backed array resolver. The index is itself a nested
/// numeric resolver, allowing both constant indices and computed ones.
/// </summary>
/// <remarks>
/// If the index is out of range, <see langword="null"/> is returned. Fractional index values are truncated.
/// </remarks>
/// <typeparam name="T">The element type to read.</typeparam>
/// <param name="source">The resolver providing the source array.</param>
/// <param name="index">The resolver providing the zero-based element index. Must resolve to a numeric type.</param>
public class ObjectElementAtResolver<T>(IObjectArrayResolver<T> source, IPropertyResolver index) : ObjectResolver<T>
{
	private readonly IObjectArrayResolver<T> _source = source;

	private readonly IPropertyResolver _index =
		ArrayResolverUtils.ValidateNumericOperand(nameof(ObjectElementAtResolver<T>), nameof(index), index);

	/// <inheritdoc/>
	[return: MaybeNull]
	public override T Resolve(GraphContext graphContext)
	{
		T[] values = _source.ResolveArray(graphContext);
		int resolvedIndex = ArrayResolverUtils.ResolveInt(graphContext, _index);
		return resolvedIndex >= 0 && resolvedIndex < values.Length ? values[resolvedIndex] : default;
	}
}
