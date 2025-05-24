// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects.Magnitudes;

namespace Gamesmiths.Forge.Effects.Components;

/// <summary>
/// Effect component for implementing a chance to apply the effect.
/// </summary>
/// <param name="randomProvider">An <see cref="IRandom"/> random provider implementation.</param>
/// <param name="chance">The chance to apply the effect.</param>
public class ChanceToApplyEffectComponent(IRandom randomProvider, ScalableFloat chance) : IEffectComponent
{
	private readonly ScalableFloat _chance = chance;

	private readonly IRandom _randomProvider = randomProvider;

	/// <inheritdoc/>
	public bool CanApplyEffect(in IForgeEntity target, in Effect effect)
	{
		return _randomProvider.NextSingle() < _chance.GetValue(effect.Level);
	}
}
