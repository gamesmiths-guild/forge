// Copyright © Gamesmiths Guild.
namespace Gamesmiths.Forge.Abilities;

/// <summary>
/// Grant source for abilities granted by Statescript graph nodes, whose grant lifetime is tied to the granting
/// node's active state.
/// </summary>
/// <param name="removalPolicy">The policy for removing the ability when the grant source is removed.</param>
/// <param name="inhibitionPolicy">The policy for inhibiting the ability when the grant source is inhibited.</param>
internal sealed class StatescriptGrantSource(
	AbilityDeactivationPolicy removalPolicy,
	AbilityDeactivationPolicy inhibitionPolicy) : IAbilityGrantSource
{
	public bool IsInhibited => false;

	public AbilityDeactivationPolicy RemovalPolicy { get; init; } = removalPolicy;

	public AbilityDeactivationPolicy InhibitionPolicy { get; init; } = inhibitionPolicy;
}
