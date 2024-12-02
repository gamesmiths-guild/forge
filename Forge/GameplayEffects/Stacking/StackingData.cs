// Copyright © 2024 Gamesmiths Guild.

using Gamesmiths.Forge.GameplayEffects.Magnitudes;

namespace Gamesmiths.Forge.GameplayEffects.Stacking;

/// <summary>
/// The stacking data for a Gameplay Effect.
/// </summary>
/// <param name="stackLimit">Max number of stacks.</param>
/// <param name="initialStack">The initial number of stacks when first applied.</param>
/// <param name="stackPolicy">How stacks aggregate based on target and instigator.</param>
/// <param name="stackLevelPolicy">How stacks aggregate based on effect level.</param>
/// <param name="magnitudePolicy">How the magnitude value of the effect is handled.</param>
/// <param name="overflowPolicy">What happens when the stack limit is reached.</param>
/// <param name="expirationPolicy">What happens to stacks when the effect duration expires.</param>
/// <param name="instigatorDenialPolicy">How to handle applications from different instigators.</param>
/// <param name="instigatorOverridePolicy">How to handle the effect's instance instigator when accepting application
/// from different instigators.</param>
/// <param name="instigatorOverrideStackCountPolicy">How to handle the stack count when the instigator is overriden.
/// </param>
/// <param name="levelDenialPolicy">How to handle stack applications of different levels.</param>
/// <param name="levelOverridePolicy">How to handle the effect's instance level when accepting applications of different
/// levels.</param>
/// <param name="levelOverrideStackCountPolicy">How to handle the stack count when the level is overriden.</param>
/// <param name="applicationRefreshPolicy">What happens with the stack duration when a new stack is applied.</param>
/// <param name="applicationResetPeriodPolicy">What happens with periodic durations when a new stack is applied.</param>
/// <param name="executeOnSuccessfulApplication">Whether the effect executes when a new stack is applied.</param>
public readonly struct StackingData(
	ScalableInt stackLimit,
	ScalableInt initialStack,
	StackPolicy stackPolicy,
	StackLevelPolicy stackLevelPolicy,
	StackMagnitudePolicy magnitudePolicy,
	StackOverflowPolicy overflowPolicy,
	StackExpirationPolicy expirationPolicy,
	StackInstigatorDenialPolicy? instigatorDenialPolicy = null,
	StackInstigatorOverridePolicy? instigatorOverridePolicy = null,
	StackInstigatorOverrideStackCountPolicy? instigatorOverrideStackCountPolicy = null,
	LevelComparison? levelDenialPolicy = null,
	LevelComparison? levelOverridePolicy = null,
	StackLevelOverrideStackCountPolicy? levelOverrideStackCountPolicy = null,
	StackApplicationRefreshPolicy? applicationRefreshPolicy = null,
	StackApplicationResetPeriodPolicy? applicationResetPeriodPolicy = null,
	bool? executeOnSuccessfulApplication = null)
{
	/// <summary>
	/// Gets the stack limit for this stackable effect.
	/// </summary>
	public ScalableInt StackLimit { get; } = stackLimit;

	/// <summary>
	/// Gets the initial stack value for this stackable effect.
	/// </summary>
	public ScalableInt InitialStack { get; } = initialStack;

	/// <summary>
	/// Gets the stack policy for this stackable effect.
	/// </summary>
	public StackPolicy StackPolicy { get; } = stackPolicy;

	/// <summary>
	/// Gets the stack level policy for this stackable effect.
	/// </summary>
	public StackLevelPolicy StackLevelPolicy { get; } = stackLevelPolicy;

	/// <summary>
	/// Gets the magnitude policy for this stackable effect.
	/// </summary>
	public StackMagnitudePolicy MagnitudePolicy { get; } = magnitudePolicy;

	/// <summary>
	/// Gets the overflow policy for this stackable effect.
	/// </summary>
	public StackOverflowPolicy OverflowPolicy { get; } = overflowPolicy;

	/// <summary>
	/// Gets the expiration policy for this stackable effect.
	/// </summary>
	/// <remarks>
	/// Infinite effects removal will count as expiration.
	/// </remarks>
	public StackExpirationPolicy ExpirationPolicy { get; } = expirationPolicy;

	/// <summary>
	/// Gets the instigator denial policy for this stackable effect.
	/// </summary>
	/// <remarks>
	/// Only valid when <see cref="StackPolicy"/> == <see cref="StackPolicy.AggregateByTarget"/>.
	/// </remarks>
	public StackInstigatorDenialPolicy? InstigatorDenialPolicy { get; } = instigatorDenialPolicy;

	/// <summary>
	/// Gets the instigator override policy for this stackable effect.
	/// </summary>
	/// <remarks>
	/// Only valid when <see cref="StackPolicy"/> == <see cref="StackPolicy.AggregateByTarget"/>.
	/// </remarks>
	public StackInstigatorOverridePolicy? InstigatorOverridePolicy { get; } = instigatorOverridePolicy;

	/// <summary>
	/// Gets the instigator override stack count policy for this stackable effect.
	/// </summary>
	/// <remarks>
	/// Only valid when <see cref="StackPolicy"/> == <see cref="StackPolicy.AggregateByTarget"/> and
	/// <see cref="StackInstigatorOverridePolicy"/> == <see cref="StackInstigatorOverridePolicy.Override"/>.
	/// </remarks>
	public StackInstigatorOverrideStackCountPolicy? InstigatorOverrideStackCountPolicy { get; } =
		instigatorOverrideStackCountPolicy;

	/// <summary>
	/// Gets the level denial policy for this stackable effect.
	/// </summary>
	/// <remarks>
	/// Only valid when <see cref="StackLevelPolicy"/> == <see cref="StackLevelPolicy.AggregateLevels"/>.
	/// </remarks>
	public LevelComparison? LevelDenialPolicy { get; } = levelDenialPolicy;

	/// <summary>
	/// Gets the level override policy for this stackable effect.
	/// </summary>
	/// <remarks>
	/// Only valid when <see cref="StackLevelPolicy"/> == <see cref="StackLevelPolicy.AggregateLevels"/>.
	/// </remarks>
	public LevelComparison? LevelOverridePolicy { get; } = levelOverridePolicy;

	/// <summary>
	/// Gets the level override stack count policy for this stackable effect.
	/// </summary>
	/// <remarks>
	/// Gets stackLevelPolicy == AggregateLevels &amp;&amp; StackLevelOverridePolicy == Any.
	/// Only valid when <see cref="StackLevelPolicy"/> == <see cref="StackLevelPolicy.AggregateLevels"/> and
	/// <see cref="LevelOverridePolicy"/> != <see cref="LevelComparison.None"/>.
	/// </remarks>
	public StackLevelOverrideStackCountPolicy? LevelOverrideStackCountPolicy { get; } = levelOverrideStackCountPolicy;

	/// <summary>
	/// Gets the application refresh policy for this stackable effect.
	/// </summary>
	/// <remarks>
	/// Only valid for effects set as <see cref="Duration.DurationType.HasDuration"/>.
	/// </remarks>
	public StackApplicationRefreshPolicy? ApplicationRefreshPolicy { get; } = applicationRefreshPolicy;

	/// <summary>
	/// Gets the application reset period policy for this stackable effect.
	/// </summary>
	/// <remarks>
	/// Only valid for effects that have <see cref="Periodic.PeriodicData"/>.
	/// </remarks>
	public StackApplicationResetPeriodPolicy? ApplicationResetPeriodPolicy { get; } = applicationResetPeriodPolicy;

	/// <summary>
	/// Gets a value indicating whether this periodic effect executes on successful stack application.
	/// </summary>
	/// <remarks>
	/// Only valid for effects that have <see cref="Periodic.PeriodicData"/>.
	/// </remarks>
	public bool? ExecuteOnSuccessfulApplication { get; } = executeOnSuccessfulApplication;
}
