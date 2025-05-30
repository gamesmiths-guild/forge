// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Effects.Periodic;

namespace Gamesmiths.Forge.Effects.Stacking;

/// <summary>
/// The stacking data for a effect.
/// </summary>
/// <param name="stackLimit">Max number of stacks.</param>
/// <param name="initialStack">The initial number of stacks when first applied.</param>
/// <param name="stackPolicy">How stacks aggregate based on target and owner.</param>
/// <param name="stackLevelPolicy">How stacks aggregate based on effect level.</param>
/// <param name="magnitudePolicy">How the magnitude value of the effect is handled.</param>
/// <param name="overflowPolicy">What happens when the stack limit is reached.</param>
/// <param name="expirationPolicy">What happens to stacks when the effect duration expires.</param>
/// <param name="ownerDenialPolicy">How to handle applications from different owner.</param>
/// <param name="ownerOverridePolicy">How to handle the effect's instance owner when accepting application
/// from different owner.</param>
/// <param name="ownerOverrideStackCountPolicy">How to handle the stack count when the owner is overriden.
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
	StackOwnerDenialPolicy? ownerDenialPolicy = null,
	StackOwnerOverridePolicy? ownerOverridePolicy = null,
	StackOwnerOverrideStackCountPolicy? ownerOverrideStackCountPolicy = null,
	LevelComparison? levelDenialPolicy = null,
	LevelComparison? levelOverridePolicy = null,
	StackLevelOverrideStackCountPolicy? levelOverrideStackCountPolicy = null,
	StackApplicationRefreshPolicy? applicationRefreshPolicy = null,
	StackApplicationResetPeriodPolicy? applicationResetPeriodPolicy = null,
	bool? executeOnSuccessfulApplication = null) : IEquatable<StackingData>
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
	/// Gets the owner denial policy for this stackable effect.
	/// </summary>
	/// <remarks>
	/// Only valid when <see cref="StackPolicy"/> == <see cref="StackPolicy.AggregateByTarget"/>.
	/// </remarks>
	public StackOwnerDenialPolicy? OwnerDenialPolicy { get; } = ownerDenialPolicy;

	/// <summary>
	/// Gets the owner override policy for this stackable effect.
	/// </summary>
	/// <remarks>
	/// Only valid when <see cref="StackPolicy"/> == <see cref="StackPolicy.AggregateByTarget"/>.
	/// </remarks>
	public StackOwnerOverridePolicy? OwnerOverridePolicy { get; } = ownerOverridePolicy;

	/// <summary>
	/// Gets the owner override stack count policy for this stackable effect.
	/// </summary>
	/// <remarks>
	/// Only valid when <see cref="StackPolicy"/> == <see cref="StackPolicy.AggregateByTarget"/> and
	/// <see cref="StackOwnerOverridePolicy"/> == <see cref="StackOwnerOverridePolicy.Override"/>.
	/// </remarks>
	public StackOwnerOverrideStackCountPolicy? OwnerOverrideStackCountPolicy { get; } =
		ownerOverrideStackCountPolicy;

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
	/// Only valid when <see cref="StackLevelPolicy"/> == <see cref="StackLevelPolicy.AggregateLevels"/> and
	/// <see cref="LevelOverridePolicy"/> != <see cref="LevelComparison.None"/>.
	/// </remarks>
	public StackLevelOverrideStackCountPolicy? LevelOverrideStackCountPolicy { get; } = levelOverrideStackCountPolicy;

	/// <summary>
	/// Gets the application refresh policy for this stackable effect.
	/// </summary>
	/// <remarks>
	/// Only valid for effects set as <see cref="DurationType.HasDuration"/>.
	/// </remarks>
	public StackApplicationRefreshPolicy? ApplicationRefreshPolicy { get; } = applicationRefreshPolicy;

	/// <summary>
	/// Gets the application reset period policy for this stackable effect.
	/// </summary>
	/// <remarks>
	/// Only valid for effects that have <see cref="PeriodicData"/>.
	/// </remarks>
	public StackApplicationResetPeriodPolicy? ApplicationResetPeriodPolicy { get; } = applicationResetPeriodPolicy;

	/// <summary>
	/// Gets a value indicating whether this periodic effect executes on successful stack application.
	/// </summary>
	/// <remarks>
	/// Only valid for effects that have <see cref="PeriodicData"/>.
	/// </remarks>
	public bool? ExecuteOnSuccessfulApplication { get; } = executeOnSuccessfulApplication;

	/// <inheritdoc/>
	public override int GetHashCode()
	{
		var hash = default(HashCode);
		hash.Add(StackLimit);
		hash.Add(InitialStack);
		hash.Add(StackPolicy);
		hash.Add(StackLevelPolicy);
		hash.Add(MagnitudePolicy);
		hash.Add(OverflowPolicy);
		hash.Add(ExpirationPolicy);
		hash.Add(OwnerDenialPolicy);
		hash.Add(OwnerOverridePolicy);
		hash.Add(OwnerOverrideStackCountPolicy);
		hash.Add(LevelDenialPolicy);
		hash.Add(LevelOverridePolicy);
		hash.Add(LevelOverrideStackCountPolicy);
		hash.Add(ApplicationRefreshPolicy);
		hash.Add(ApplicationResetPeriodPolicy);
		hash.Add(ExecuteOnSuccessfulApplication);

		return hash.ToHashCode();
	}

	/// <inheritdoc/>
	public override bool Equals(object? obj)
	{
		if (obj is StackingData other)
		{
			return Equals(other);
		}

		return false;
	}

	/// <inheritdoc/>
	public bool Equals(StackingData other)
	{
		return StackLimit.Equals(other.StackLimit)
			&& InitialStack.Equals(other.InitialStack)
			&& StackPolicy.Equals(other.StackPolicy)
			&& StackLevelPolicy.Equals(other.StackLevelPolicy)
			&& MagnitudePolicy.Equals(other.MagnitudePolicy)
			&& OverflowPolicy.Equals(other.OverflowPolicy)
			&& ExpirationPolicy.Equals(other.ExpirationPolicy)
			&& Nullable.Equals(OwnerDenialPolicy, other.OwnerDenialPolicy)
			&& Nullable.Equals(OwnerOverridePolicy, other.OwnerOverridePolicy)
			&& Nullable.Equals(OwnerOverrideStackCountPolicy, other.OwnerOverrideStackCountPolicy)
			&& Nullable.Equals(LevelDenialPolicy, other.LevelDenialPolicy)
			&& Nullable.Equals(LevelOverridePolicy, other.LevelOverridePolicy)
			&& Nullable.Equals(LevelOverrideStackCountPolicy, other.LevelOverrideStackCountPolicy)
			&& Nullable.Equals(ApplicationRefreshPolicy, other.ApplicationRefreshPolicy)
			&& Nullable.Equals(ApplicationResetPeriodPolicy, other.ApplicationResetPeriodPolicy)
			&& Nullable.Equals(ExecuteOnSuccessfulApplication, other.ExecuteOnSuccessfulApplication);
	}

	/// <summary>
	/// Determines if two <see cref="StackingData"/> objects are equal.
	/// </summary>
	/// <param name="lhs">The first <see cref="StackingData"/> to compare.</param>
	/// <param name="rhs">The second <see cref="StackingData"/> to compare.</param>
	/// <returns><see langword="true"/> if the values of <paramref name="lhs"/> and <paramref name="rhs"/> are equal;
	/// otherwise, <see langword="false"/>.</returns>
	public static bool operator ==(StackingData lhs, StackingData rhs)
	{
		return lhs.Equals(rhs);
	}

	/// <summary>
	/// Determines if two <see cref="StackingData"/> objects are not equal.
	/// </summary>
	/// <param name="lhs">The first <see cref="StackingData"/> to compare.</param>
	/// <param name="rhs">The second <see cref="StackingData"/> to compare.</param>
	/// <returns><see langword="true"/> if the values of <paramref name="lhs"/> and <paramref name="rhs"/> are not
	/// equal; otherwise, <see langword="false"/>.</returns>
	public static bool operator !=(StackingData lhs, StackingData rhs)
	{
		return !(lhs == rhs);
	}
}
