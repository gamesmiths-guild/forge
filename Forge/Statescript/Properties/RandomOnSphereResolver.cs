// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a random normalized 3D direction on the unit sphere.
/// </summary>
/// <param name="random">The random provider used when the graph has no <see cref="GraphContext.Random"/>.</param>
public class RandomOnSphereResolver(IRandom random) : IPropertyResolver
{
	private readonly IRandom _random = random;

	/// <inheritdoc/>
	public Type ValueType { get; } = typeof(System.Numerics.Vector3);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		return new Variant128(GameplayMathUtils.RandomOnSphere(graphContext.Random ?? _random));
	}
}
