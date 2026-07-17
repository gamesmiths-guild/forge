// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a random element from a nested array resolver.
/// </summary>
/// <remarks>
/// Empty arrays resolve to a default <see cref="Variant128"/> (zero).
/// </remarks>
/// <param name="source">The resolver providing the source array.</param>
/// <param name="randomProvider">The random provider used to pick the element.</param>
public class RandomElementResolver(IArrayPropertyResolver source, IRandom randomProvider) : IPropertyResolver
{
	private readonly IArrayPropertyResolver _source = source;

	private readonly IRandom _randomProvider = randomProvider
		?? throw new ArgumentNullException(nameof(randomProvider));

	/// <inheritdoc/>
	public Type ValueType { get; } = source.ElementType;

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		Variant128[] values = _source.ResolveArray(graphContext);

		if (values.Length == 0)
		{
			return default;
		}

		return values[_randomProvider.NextInt(0, values.Length)];
	}
}
