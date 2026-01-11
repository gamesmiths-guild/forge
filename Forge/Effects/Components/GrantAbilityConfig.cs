// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects.Magnitudes;

namespace Gamesmiths.Forge.Effects.Components;

/// <summary>
/// Configuration for granting an ability to an entity.
/// </summary>
/// <param name="AbilityData">The data defining the ability to be granted.</param>
/// <param name="ScalableLevel">The level of the granted ability, which can scale based on the effect level.</param>
/// <param name="RemovalPolicy">Which policy to use when determining when to remove the granted ability.</param>
/// <param name="InhibitionPolicy">Which policy to use when determining how the granted ability behaves when it becomes
/// inhibited.</param>
/// <param name="TryActivateOnGrant">Whether to attempt to activate the ability immediately upon granting it.</param>
/// <param name="TryActivateOnEnable">Whether to attempt to activate the ability when the effect is enabled back from
/// inhibition.</param>
/// <param name="LevelOverridePolicy">How to override the level of the granted ability if it already exists on the
/// target.</param>
public readonly record struct GrantAbilityConfig(
	AbilityData AbilityData,
	ScalableInt ScalableLevel,
	AbilityDeactivationPolicy RemovalPolicy,
	AbilityDeactivationPolicy InhibitionPolicy,
	bool TryActivateOnGrant = false,
	bool TryActivateOnEnable = false,
	LevelComparison LevelOverridePolicy = LevelComparison.None);
