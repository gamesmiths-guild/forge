// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Effects;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves an array of <see cref="EffectData"/> values.
/// </summary>
/// <param name="effectData">The effect data values to return.</param>
public class EffectDataArrayResolver(params EffectData[] effectData) : ObjectArrayResolver<EffectData>
{
	private readonly EffectData[] _effectData = effectData;

	/// <inheritdoc/>
	public override EffectData[] ResolveArray(GraphContext graphContext)
	{
		return [.. _effectData];
	}
}
