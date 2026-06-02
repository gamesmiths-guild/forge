// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Effects;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Resolves a single <see cref="EffectData"/> value.
/// </summary>
/// <param name="effectData">The effect data to return.</param>
public class EffectDataResolver(EffectData effectData) : ObjectResolver<EffectData>
{
	private readonly EffectData _effectData = effectData;

	/// <inheritdoc/>
	public override EffectData Resolve(GraphContext graphContext)
	{
		return _effectData;
	}
}
