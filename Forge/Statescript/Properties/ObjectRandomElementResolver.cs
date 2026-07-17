// Copyright © Gamesmiths Guild.

using System.Diagnostics.CodeAnalysis;
using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a random element from a nested object-backed array resolver — the "pick a random target" staple.
/// </summary>
/// <remarks>
/// Empty arrays resolve to <see langword="null"/>.
/// </remarks>
/// <typeparam name="T">The element type to read.</typeparam>
/// <param name="source">The resolver providing the source array.</param>
/// <param name="randomProvider">The random provider used to pick the element.</param>
public class ObjectRandomElementResolver<T>(IObjectArrayResolver<T> source, IRandom randomProvider)
	: ObjectResolver<T>
{
	private readonly IObjectArrayResolver<T> _source = source;

	private readonly IRandom _randomProvider = randomProvider
		?? throw new ArgumentNullException(nameof(randomProvider));

	/// <inheritdoc/>
	[return: MaybeNull]
	public override T Resolve(GraphContext graphContext)
	{
		T[] values = _source.ResolveArray(graphContext);

		if (values.Length == 0)
		{
			return default;
		}

		return values[_randomProvider.NextInt(0, values.Length)];
	}
}
