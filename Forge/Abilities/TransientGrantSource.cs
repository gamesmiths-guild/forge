// Copyright © Gamesmiths Guild.
namespace Gamesmiths.Forge.Abilities;

internal sealed class TransientGrantSource : IAbilityGrantSource
{
	public bool IsInhibited => false;

	public AbilityDeactivationPolicy RemovalPolicy => AbilityDeactivationPolicy.RemoveOnEnd;

	public AbilityDeactivationPolicy InhibitionPolicy => AbilityDeactivationPolicy.Ignore;
}
