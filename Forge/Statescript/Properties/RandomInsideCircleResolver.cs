// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a random point inside the unit circle.
/// </summary>
/// <param name="random">The random provider to use.</param>
public class RandomInsideCircleResolver(IRandom random) : IPropertyResolver
{
	private readonly IRandom _random = random;

	/// <inheritdoc/>
	public Type ValueType { get; } = typeof(System.Numerics.Vector2);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		return new Variant128(GameplayMathUtils.RandomInsideCircle(_random));
	}
}
