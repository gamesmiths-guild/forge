// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.GameplayEffects.Magnitudes;

namespace Gamesmiths.Forge.GameplayEffects.Components;

/// <summary>
/// Gameplay effect component for implementing a chance to apply the effect.
/// </summary>
/// <param name="randomProvider">An <see cref="IRandom"/> random provider implementation.</param>
/// <param name="chance">The chance to apply the effect.</param>
public class ChanceToApplyEffectComponent(IRandom randomProvider, ScalableFloat chance)
		: IGameplayEffectComponent
{
	private readonly ScalableFloat _chance = chance;

	private readonly IRandom _randomProvider = randomProvider;

	/// <inheritdoc/>
	public bool CanApplyGameplayEffect(in IForgeEntity target, in GameplayEffect effect)
	{
		return _randomProvider.NextSingle() < _chance.GetValue(effect.Level);
	}
}
