// Copyright © 2024 Gamesmiths Guild.

using System.Diagnostics;
using Gamesmiths.Forge.GameplayEffects.Duration;
using Gamesmiths.Forge.GameplayEffects.Magnitudes;
using Gamesmiths.Forge.GameplayEffects.Modifiers;
using Gamesmiths.Forge.GameplayEffects.Periodic;
using Gamesmiths.Forge.GameplayEffects.Stacking;

namespace Gamesmiths.Forge.GameplayEffects;

/// <summary>
/// The configuration data for a gameplay effect.
/// </summary>
public readonly struct GameplayEffectData : IEquatable<GameplayEffectData>
{
	/// <summary>
	/// Gets the list of modifiers of this gameplay effect.
	/// </summary>
	/// TODO: public List <Executions> Executions { get; } = new ().
	public Modifier[] Modifiers { get; }

	/// <summary>
	/// Gets the name of this gameplay effect.
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// Gets the <see cref="DurationData"/> for this gameplay effect.
	/// </summary>
	public DurationData DurationData { get; }

	/// <summary>
	/// Gets the <see cref="Stacking.StackingData"/> for this stackable gameplay effect.
	/// </summary>
	/// <remarks>
	/// Having this data means the gameplay effect is stackable in some way.
	/// </remarks>
	public StackingData? StackingData { get; }

	/// <summary>
	/// Gets the <see cref="Periodic.PeriodicData"/> for this periodic gameplay effect.
	/// </summary>
	/// <remarks>
	/// Having this data means the gameplay effect is periodic, executing the effects over fixed periods of time.
	/// </remarks>
	public PeriodicData? PeriodicData { get; }

	/// <summary>
	/// Gets a value indicating whether this gameplay effect snapshots the level at the momment of creation.
	/// </summary>
	public bool SnapshopLevel { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="GameplayEffectData"/> struct.
	/// </summary>
	/// <param name="name">The name of this gameplay effect.</param>
	/// <param name="modifiers">The list of modifiers for this gameplay effect.</param>
	/// <param name="durationData">The duration data for this gameplay effect.</param>
	/// <param name="stackingData">The stacking data for this gameplay effect, if it's stackable.</param>
	/// <param name="periodicData">The periodic data for this gameplay effect, if it's periodic.</param>
	/// <param name="snapshopLevel">Whether or not this gameplay effect snapshots the level at the momment of creation.
	/// </param>
	public GameplayEffectData(
		string name,
		Modifier[] modifiers,
		DurationData durationData,
		StackingData? stackingData,
		PeriodicData? periodicData,
		bool snapshopLevel = true)
	{
		Debug.Assert(
			!(periodicData.HasValue && durationData.Type == DurationType.Instant),
			"Periodic effects can't be set as instant.");

		Debug.Assert(
			!(durationData.Type != DurationType.HasDuration && durationData.Duration.HasValue),
			$"Can't set duration if {nameof(DurationType)} is set to {durationData.Type}.");

		// TODO: Implement some kind of support for override on non-instant effects.
		Debug.Assert(
			!(durationData.Type != DurationType.Instant && !periodicData.HasValue
			&& Array.Exists(modifiers, x => x.Operation == ModifierOperation.Override)),
			$"Only {DurationType.Instant} or Periodic effects can have operation of type {ModifierOperation.Override}.");

		Debug.Assert(
			!(stackingData.HasValue && durationData.Type == DurationType.Instant),
			$"{DurationType.Instant} effects can't have stacks.");

		Debug.Assert(
			!(stackingData.HasValue
				&& (stackingData.Value.InitialStack.BaseValue > stackingData.Value.StackLimit.BaseValue
				|| stackingData.Value.InitialStack.BaseValue == 0)),
			"Shouldn't set InitialStack count to be higher than the StackLimit nor zero. It's probably a bad configuration.");

		Debug.Assert(
			!(stackingData.HasValue
			&& (stackingData.Value.StackPolicy == StackPolicy.AggregateByTarget !=
				stackingData.Value.OwnerDenialPolicy.HasValue)),
			$"If {nameof(StackPolicy)} is set {StackPolicy.AggregateByTarget}, {nameof(StackOwnerDenialPolicy)} must be defined. And not defined if otherwise.");

		Debug.Assert(
			!(stackingData.HasValue
			&& ((stackingData.Value.StackPolicy == StackPolicy.AggregateByTarget &&
				stackingData.Value.OwnerDenialPolicy == StackOwnerDenialPolicy.AlwaysAllow) !=
				stackingData.Value.OwnerOverridePolicy.HasValue)),
			$"If {nameof(StackPolicy)} is set {StackPolicy.AggregateByTarget} and {nameof(StackOwnerDenialPolicy)} is set to {StackOwnerDenialPolicy.AlwaysAllow}, {nameof(StackOwnerOverridePolicy)} must be defined. And not defined if otherwise.");

		Debug.Assert(
			!(stackingData.HasValue
			&& ((stackingData.Value.StackPolicy == StackPolicy.AggregateByTarget &&
				stackingData.Value.OwnerOverridePolicy.HasValue &&
				stackingData.Value.OwnerOverridePolicy.Value == StackOwnerOverridePolicy.Override) !=
				stackingData.Value.OwnerOverrideStackCountPolicy.HasValue)),
			$"If {nameof(StackOwnerOverridePolicy)} is set {StackOwnerOverridePolicy.Override}, {nameof(StackOwnerOverrideStackCountPolicy)} must be defined. And not defined if otherwise.");

		Debug.Assert(
			!(stackingData.HasValue
				&& (stackingData.Value.StackLevelPolicy == StackLevelPolicy.AggregateLevels !=
				stackingData.Value.LevelDenialPolicy.HasValue)),
			$"If {nameof(StackLevelPolicy)} is set {StackLevelPolicy.AggregateLevels}, {nameof(LevelComparison)} must be defined. And not defined if otherwise.");

		Debug.Assert(
			!(stackingData.HasValue
				&& (stackingData.Value.StackLevelPolicy == StackLevelPolicy.AggregateLevels !=
				stackingData.Value.LevelOverridePolicy.HasValue)),
			$"If {nameof(StackLevelPolicy)} is set {StackLevelPolicy.AggregateLevels}, LevelOverridePolicy must be defined. And not defined if otherwise.");

		Debug.Assert(
			!(stackingData.HasValue
				&& ((stackingData.Value.StackLevelPolicy == StackLevelPolicy.AggregateLevels &&
				stackingData.Value.LevelOverridePolicy.HasValue &&
				stackingData.Value.LevelOverridePolicy.Value != LevelComparison.None) !=
				stackingData.Value.LevelOverrideStackCountPolicy.HasValue)),
			$"If LevelOverridePolicy is different from {LevelComparison.None}, {nameof(StackLevelOverrideStackCountPolicy)} must be defined. And not defined if otherwise.");

		Debug.Assert(
			!(stackingData.HasValue
				&& stackingData.Value.LevelDenialPolicy.HasValue &&
				stackingData.Value.LevelOverridePolicy.HasValue &&
				stackingData.Value.LevelDenialPolicy.Value != LevelComparison.None &&
				(stackingData.Value.LevelDenialPolicy.Value & stackingData.Value.LevelOverridePolicy.Value) != 0),
			"LevelDenialPolicy and LevelOverridePolicy should't have the same value. If it's getting denied, how will it override?");

		Debug.Assert(
			!(stackingData.HasValue
				&& (durationData.Type == DurationType.HasDuration !=
				stackingData.Value.ApplicationRefreshPolicy.HasValue)),
			$"Effects set as {DurationType.HasDuration} must define {nameof(StackApplicationRefreshPolicy)} and not define it if otherwise.");

		Debug.Assert(
			!(stackingData.HasValue
				&& (stackingData.Value.ExecuteOnSuccessfulApplication.HasValue != periodicData.HasValue)),
			$"Both {nameof(PeriodicData)} and ExecuteOnSuccessfulApplication must be either defined or undefined.");

		Debug.Assert(
			!(stackingData.HasValue
				&& (stackingData.Value.ApplicationResetPeriodPolicy.HasValue != periodicData.HasValue)),
			$"Both {nameof(PeriodicData)} and {nameof(StackApplicationResetPeriodPolicy)} must be either defined or undefined.");

		Debug.Assert(
			!(durationData.Type == DurationType.Instant && Array.Exists(
				modifiers,
				x => x.Magnitude.MagnitudeCalculationType == MagnitudeCalculationType.AttributeBased
					&& x.Magnitude.AttributeBasedFloat.HasValue
					&& !x.Magnitude.AttributeBasedFloat.Value.BackingAttribute.Snapshot)),
			$"Effects set as {DurationType.Instant} and {MagnitudeCalculationType.AttributeBased} cannot be set as non Snapshot.");

		Debug.Assert(
			!(durationData.Type == DurationType.Instant && !snapshopLevel),
			$"Effects set as {DurationType.Instant} cannot be set as non Snapshot for Level.");

		Name = name;
		Modifiers = modifiers;
		DurationData = durationData;
		StackingData = stackingData;
		PeriodicData = periodicData;
		SnapshopLevel = snapshopLevel;
	}

	/// <inheritdoc/>
	public override int GetHashCode()
	{
		var hash = default(HashCode);
		hash.Add(Name);
		hash.Add(DurationData);
		hash.Add(StackingData);
		hash.Add(PeriodicData);
		hash.Add(SnapshopLevel);
		foreach (Modifier modifier in Modifiers)
		{
			hash.Add(modifier);
		}

		return hash.ToHashCode();
	}

	/// <inheritdoc/>
	public override bool Equals(object? obj)
	{
		if (obj is GameplayEffectData other)
		{
			return Equals(other);
		}

		return false;
	}

	/// <inheritdoc/>
	public bool Equals(GameplayEffectData other)
	{
		return Name == other.Name
			&& DurationData.Equals(other.DurationData)
			&& Nullable.Equals(StackingData, other.StackingData)
			&& Nullable.Equals(PeriodicData, other.PeriodicData)
			&& SnapshopLevel == other.SnapshopLevel
			&& Modifiers.SequenceEqual(other.Modifiers);
	}

	/// <summary>
	/// Determines if two <see cref="GameplayEffectData"/> objects are equal.
	/// </summary>
	/// <param name="lhs">The first <see cref="GameplayEffectData"/> to compare.</param>
	/// <param name="rhs">The second <see cref="GameplayEffectData"/> to compare.</param>
	/// <returns><see langword="true"/> if the values of <paramref name="lhs"/> and <paramref name="rhs"/> are equal;
	/// otherwise, <see langword="false"/>.</returns>
	public static bool operator ==(GameplayEffectData lhs, GameplayEffectData rhs)
	{
		return lhs.Equals(rhs);
	}

	/// <summary>
	/// Determines if two <see cref="GameplayEffectData"/> objects are not equal.
	/// </summary>
	/// <param name="lhs">The first <see cref="GameplayEffectData"/> to compare.</param>
	/// <param name="rhs">The second <see cref="GameplayEffectData"/> to compare.</param>
	/// <returns><see langword="true"/> if the values of <paramref name="lhs"/> and <paramref name="rhs"/> are not
	/// equal; otherwise, <see langword="false"/>.</returns>
	public static bool operator !=(GameplayEffectData lhs, GameplayEffectData rhs)
	{
		return !(lhs == rhs);
	}
}
