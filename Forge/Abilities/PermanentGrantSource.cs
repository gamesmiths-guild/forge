// Copyright © Gamesmiths Guild.
namespace Gamesmiths.Forge.Abilities;

internal sealed class PermanentGrantSource : IAbilityGrantSource
{
	public bool IsInhibited => false;

	public AbilityDeactivationPolicy RemovalPolicy => AbilityDeactivationPolicy.Ignore;

	public AbilityDeactivationPolicy InhibitionPolicy => AbilityDeactivationPolicy.Ignore;
}
