// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects.Magnitudes;

namespace Gamesmiths.Forge.Effects.Components;

/// <summary>
/// Effect component for implementing a chance to apply the effect.
/// </summary>
/// <remarks>
/// The roll comes from the target's <see cref="IForgeEntity.Random"/> when it has one, so every machine that simulates
/// the target on the same tick rolls the same result. Otherwise it comes from the given provider.
/// </remarks>
/// <param name="randomProvider">The random provider used when the target has no <see cref="IForgeEntity.Random"/>.
/// </param>
/// <param name="chance">The chance to apply the effect.</param>
public class ChanceToApplyEffectComponent(IRandom randomProvider, ScalableFloat chance) : IEffectComponent
{
	private readonly ScalableFloat _chance = chance;

	private readonly IRandom _randomProvider = randomProvider;

	/// <inheritdoc/>
	public bool CanApplyEffect(in IForgeEntity target, in Effect effect)
	{
		IRandom random = target.Random ?? _randomProvider;
		return random.NextSingle() < _chance.GetValue(effect.Level);
	}
}
