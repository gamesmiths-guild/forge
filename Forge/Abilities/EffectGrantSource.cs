// Copyright © Gamesmiths Guild.
using Gamesmiths.Forge.Effects;

namespace Gamesmiths.Forge.Abilities;

internal sealed class EffectGrantSource(
	ActiveEffectHandle effectHandle,
	AbilityDeactivationPolicy removalPolicy,
	AbilityDeactivationPolicy inhibitionPolicy) : IAbilityGrantSource
{
	public ActiveEffectHandle EffectHandle { get; init; } = effectHandle;

	public bool IsInhibited => EffectHandle.IsInhibited;

	public AbilityDeactivationPolicy RemovalPolicy { get; init; } = removalPolicy;

	public AbilityDeactivationPolicy InhibitionPolicy { get; init; } = inhibitionPolicy;
}
