// Copyright © Gamesmiths Guild.

using System.Diagnostics.CodeAnalysis;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves the first element of a nested object-backed array resolver.
/// </summary>
/// <remarks>
/// If the source array is empty, <see langword="null"/> is returned.
/// </remarks>
/// <typeparam name="T">The element type to read.</typeparam>
/// <param name="source">The resolver providing the source array.</param>
public class ObjectFirstResolver<T>(IObjectArrayResolver<T> source) : ObjectResolver<T>
{
	private readonly IObjectArrayResolver<T> _source = source;

	/// <inheritdoc/>
	[return: MaybeNull]
	public override T Resolve(GraphContext graphContext)
	{
		T[] values = _source.ResolveArray(graphContext);
		return values.Length > 0 ? values[0] : default;
	}
}
