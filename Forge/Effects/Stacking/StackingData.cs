// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects.Magnitudes;

namespace Gamesmiths.Forge.Effects.Stacking;

/// <summary>
/// The stacking data for a effect.
/// </summary>
/// <param name="StackLimit">Max number of stacks.</param>
/// <param name="InitialStack">The initial number of stacks when first applied.</param>
/// <param name="StackPolicy">How stacks aggregate based on target and owner.</param>
/// <param name="StackLevelPolicy">How stacks aggregate based on effect level.</param>
/// <param name="MagnitudePolicy">How the magnitude value of the effect is handled.</param>
/// <param name="OverflowPolicy">What happens when the stack limit is reached.</param>
/// <param name="ExpirationPolicy">What happens to stacks when the effect duration expires.</param>
/// <param name="OwnerDenialPolicy">How to handle applications from different owner.</param>
/// <param name="OwnerOverridePolicy">How to handle the effect's instance owner when accepting application
/// from different owner.</param>
/// <param name="OwnerOverrideStackCountPolicy">How to handle the stack count when the owner is overridden.
/// </param>
/// <param name="LevelDenialPolicy">How to handle stack applications of different levels.</param>
/// <param name="LevelOverridePolicy">How to handle the effect's instance level when accepting applications of different
/// levels.</param>
/// <param name="LevelOverrideStackCountPolicy">How to handle the stack count when the level is overridden.</param>
/// <param name="ApplicationRefreshPolicy">What happens with the stack duration when a new stack is applied.</param>
/// <param name="ApplicationResetPeriodPolicy">What happens with periodic durations when a new stack is applied.</param>
/// <param name="ExecuteOnSuccessfulApplication">Whether the effect executes when a new stack is applied.</param>
public readonly record struct StackingData(
	ScalableInt StackLimit,
	ScalableInt InitialStack,
	StackPolicy StackPolicy,
	StackLevelPolicy StackLevelPolicy,
	StackMagnitudePolicy MagnitudePolicy,
	StackOverflowPolicy OverflowPolicy,
	StackExpirationPolicy ExpirationPolicy,
	StackOwnerDenialPolicy? OwnerDenialPolicy = null,
	StackOwnerOverridePolicy? OwnerOverridePolicy = null,
	StackOwnerOverrideStackCountPolicy? OwnerOverrideStackCountPolicy = null,
	LevelComparison? LevelDenialPolicy = null,
	LevelComparison? LevelOverridePolicy = null,
	StackLevelOverrideStackCountPolicy? LevelOverrideStackCountPolicy = null,
	StackApplicationRefreshPolicy? ApplicationRefreshPolicy = null,
	StackApplicationResetPeriodPolicy? ApplicationResetPeriodPolicy = null,
	bool? ExecuteOnSuccessfulApplication = null);
