// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects.Calculator;
using Gamesmiths.Forge.Effects.Components;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Effects.Modifiers;
using Gamesmiths.Forge.Effects.Periodic;
using Gamesmiths.Forge.Effects.Stacking;

namespace Gamesmiths.Forge.Effects;

/// <summary>
/// The configuration data for a effect.
/// </summary>
public readonly struct EffectData : IEquatable<EffectData>
{
	/// <summary>
	/// Gets the name of this effect.
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// Gets the list of custom executions for this effect.
	/// </summary>
	public CustomExecution[] CustomExecutions { get; }

	/// <summary>
	/// Gets the list of modifiers of this effect.
	/// </summary>
	public Modifier[] Modifiers { get; }

	/// <summary>
	/// Gets the <see cref="DurationData"/> for this effect.
	/// </summary>
	public DurationData DurationData { get; }

	/// <summary>
	/// Gets the <see cref="Stacking.StackingData"/> for this stackable effect.
	/// </summary>
	/// <remarks>
	/// Having this data means the effect is stackable in some way.
	/// </remarks>
	public StackingData? StackingData { get; }

	/// <summary>
	/// Gets the <see cref="Periodic.PeriodicData"/> for this periodic effect.
	/// </summary>
	/// <remarks>
	/// Having this data means the effect is periodic, executing the effects over fixed periods of time.
	/// </remarks>
	public PeriodicData? PeriodicData { get; }

	/// <summary>
	/// Gets a value indicating whether this effect snapshots the level at the momment of creation.
	/// </summary>
	public bool SnapshopLevel { get; }

	/// <summary>
	/// Gets the list of effect components that further customize this effect behaviour.
	/// </summary>
	public IEffectComponent[] EffectComponents { get; }

	/// <summary>
	/// Gets a value indicating whether this effect requires the modifier to be successful to trigger cues.
	/// </summary>
	public bool RequireModifierSuccessToTriggerCue { get; }

	/// <summary>
	/// Gets a value indicating whether this effect suppresses stacking cues.
	/// </summary>
	public bool SuppressStackingCues { get; }

	/// <summary>
	/// Gets the cues associated with this effect.
	/// </summary>
	public CueData[] Cues { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="EffectData"/> struct.
	/// </summary>
	/// <param name="name">The name of this effect.</param>
	/// <param name="durationData">The duration data for this effect.</param>
	/// <param name="modifiers">The list of modifiers for this effect.</param>
	/// <param name="stackingData">The stacking data for this effect, if it's stackable.</param>
	/// <param name="periodicData">The periodic data for this effect, if it's periodic.</param>
	/// <param name="snapshopLevel">Whether or not this effect snapshots the level at the momment of creation.
	/// </param>
	/// <param name="effectComponents">The list of effects components for this effect.</param>
	/// <param name="requireModifierSuccessToTriggerCue">Wheter or not trigger cues only when modifiers are successfully
	/// applied.</param>
	/// <param name="suppressStackingCues">Whether or not to trigger cues when applying stacks.</param>
	/// <param name="customExecutions">The list of custom executions for this effect.</param>
	/// <param name="cues">The cues associated with this effect.</param>
	public EffectData(
		string name,
		DurationData durationData,
		Modifier[]? modifiers = null,
		StackingData? stackingData = null,
		PeriodicData? periodicData = null,
		bool snapshopLevel = true,
		IEffectComponent[]? effectComponents = null,
		bool requireModifierSuccessToTriggerCue = false,
		bool suppressStackingCues = false,
		CustomExecution[]? customExecutions = null,
		CueData[]? cues = null)
	{
		Name = name;
		DurationData = durationData;
		Modifiers = modifiers ?? [];
		StackingData = stackingData;
		PeriodicData = periodicData;
		SnapshopLevel = snapshopLevel;
		EffectComponents = effectComponents ?? [];
		RequireModifierSuccessToTriggerCue = requireModifierSuccessToTriggerCue;
		SuppressStackingCues = suppressStackingCues;
		CustomExecutions = customExecutions ?? [];
		Cues = cues ?? [];

		if (!Validation.Enabled)
		{
			return;
		}

		Validation.Assert(
			!(periodicData.HasValue && durationData.Type == DurationType.Instant),
			"Periodic effects can't be set as instant.");

		Validation.Assert(
			!(durationData.Type != DurationType.HasDuration && durationData.Duration.HasValue),
			$"Can't set duration if {nameof(DurationType)} is set to {durationData.Type}.");

		Validation.Assert(
			!(stackingData.HasValue && durationData.Type == DurationType.Instant),
			$"{DurationType.Instant} effects can't have stacks.");

		Validation.Assert(
			!(stackingData.HasValue
				&& (stackingData.Value.InitialStack.BaseValue > stackingData.Value.StackLimit.BaseValue
				|| stackingData.Value.InitialStack.BaseValue == 0)),
			"Shouldn't set InitialStack count to be higher than the StackLimit nor zero. It's probably a bad configuration.");

		Validation.Assert(
			!(stackingData.HasValue
			&& (stackingData.Value.StackPolicy == StackPolicy.AggregateByTarget !=
				stackingData.Value.OwnerDenialPolicy.HasValue)),
			$"If {nameof(StackPolicy)} is set {StackPolicy.AggregateByTarget}, {nameof(StackOwnerDenialPolicy)} must be defined. And not defined if otherwise.");

		Validation.Assert(
			!(stackingData.HasValue
			&& ((stackingData.Value.StackPolicy == StackPolicy.AggregateByTarget &&
				stackingData.Value.OwnerDenialPolicy == StackOwnerDenialPolicy.AlwaysAllow) !=
				stackingData.Value.OwnerOverridePolicy.HasValue)),
			$"If {nameof(StackPolicy)} is set {StackPolicy.AggregateByTarget} and {nameof(StackOwnerDenialPolicy)} is set to {StackOwnerDenialPolicy.AlwaysAllow}, {nameof(StackOwnerOverridePolicy)} must be defined. And not defined if otherwise.");

		Validation.Assert(
			!(stackingData.HasValue
			&& ((stackingData.Value.StackPolicy == StackPolicy.AggregateByTarget &&
				stackingData.Value.OwnerOverridePolicy.HasValue &&
				stackingData.Value.OwnerOverridePolicy.Value == StackOwnerOverridePolicy.Override) !=
				stackingData.Value.OwnerOverrideStackCountPolicy.HasValue)),
			$"If {nameof(StackOwnerOverridePolicy)} is set {StackOwnerOverridePolicy.Override}, {nameof(StackOwnerOverrideStackCountPolicy)} must be defined. And not defined if otherwise.");

		Validation.Assert(
			!(stackingData.HasValue
				&& (stackingData.Value.StackLevelPolicy == StackLevelPolicy.AggregateLevels !=
				stackingData.Value.LevelDenialPolicy.HasValue)),
			$"If {nameof(StackLevelPolicy)} is set {StackLevelPolicy.AggregateLevels}, {nameof(LevelComparison)} must be defined. And not defined if otherwise.");

		Validation.Assert(
			!(stackingData.HasValue
				&& (stackingData.Value.StackLevelPolicy == StackLevelPolicy.AggregateLevels !=
				stackingData.Value.LevelOverridePolicy.HasValue)),
			$"If {nameof(StackLevelPolicy)} is set {StackLevelPolicy.AggregateLevels}, LevelOverridePolicy must be defined. And not defined if otherwise.");

		Validation.Assert(
			!(stackingData.HasValue
				&& ((stackingData.Value.StackLevelPolicy == StackLevelPolicy.AggregateLevels &&
				stackingData.Value.LevelOverridePolicy.HasValue &&
				stackingData.Value.LevelOverridePolicy.Value != LevelComparison.None) !=
				stackingData.Value.LevelOverrideStackCountPolicy.HasValue)),
			$"If LevelOverridePolicy is different from {LevelComparison.None}, {nameof(StackLevelOverrideStackCountPolicy)} must be defined. And not defined if otherwise.");

		Validation.Assert(
			!(stackingData.HasValue
				&& stackingData.Value.LevelDenialPolicy.HasValue &&
				stackingData.Value.LevelOverridePolicy.HasValue &&
				stackingData.Value.LevelDenialPolicy.Value != LevelComparison.None &&
				(stackingData.Value.LevelDenialPolicy.Value & stackingData.Value.LevelOverridePolicy.Value) != 0),
			"LevelDenialPolicy and LevelOverridePolicy should't have the same value. If it's getting denied, how will it override?");

		Validation.Assert(
			!(stackingData.HasValue
				&& (durationData.Type == DurationType.HasDuration !=
				stackingData.Value.ApplicationRefreshPolicy.HasValue)),
			$"Effects set as {DurationType.HasDuration} must define {nameof(StackApplicationRefreshPolicy)} and not define it if otherwise.");

		Validation.Assert(
			!(stackingData.HasValue
				&& (stackingData.Value.ExecuteOnSuccessfulApplication.HasValue != periodicData.HasValue)),
			$"Both {nameof(PeriodicData)} and ExecuteOnSuccessfulApplication must be either defined or undefined.");

		Validation.Assert(
			!(stackingData.HasValue
				&& (stackingData.Value.ApplicationResetPeriodPolicy.HasValue != periodicData.HasValue)),
			$"Both {nameof(PeriodicData)} and {nameof(StackApplicationResetPeriodPolicy)} must be either defined or undefined.");

		Validation.Assert(
			!(durationData.Type == DurationType.Instant && modifiers is not null && Array.Exists(
				modifiers,
				x => x.Magnitude.MagnitudeCalculationType == MagnitudeCalculationType.AttributeBased
					&& x.Magnitude.AttributeBasedFloat.HasValue
					&& !x.Magnitude.AttributeBasedFloat.Value.BackingAttribute.Snapshot)),
			$"Effects set as {DurationType.Instant} and {MagnitudeCalculationType.AttributeBased} cannot be set as non Snapshot.");

		Validation.Assert(
			!(durationData.Type == DurationType.Instant && !snapshopLevel),
			$"Effects set as {DurationType.Instant} cannot be set as non Snapshot for Level.");

		Validation.Assert(
			effectComponents is null ||
			!Array.Exists(effectComponents, x => x is ModifierTagsEffectComponent) ||
			(Array.Exists(effectComponents, x => x is ModifierTagsEffectComponent) &&
			durationData.Type != DurationType.Instant),
			$"Instant effects cannot apply tags from the {nameof(ModifierTagsEffectComponent)}.");

		foreach (CueData cue in Cues)
		{
			Validation.Assert(
				cue.MagnitudeType != CueMagnitudeType.StackCount || !suppressStackingCues,
				"StackCount magnitude type is not allowed when SuppressStackingCues is set to true.");

			Validation.Assert(
				cue.MagnitudeType != CueMagnitudeType.StackCount || stackingData.HasValue,
				"StackCount magnitude type can only be used if StackingData is configured.");

			Validation.Assert(
				cue.MagnitudeType == CueMagnitudeType.AttributeValueChange
					|| cue.MagnitudeType == CueMagnitudeType.AttributeBaseValue
					|| cue.MagnitudeType == CueMagnitudeType.AttributeCurrentValue
					|| cue.MagnitudeType == CueMagnitudeType.AttributeModifier
					|| cue.MagnitudeType == CueMagnitudeType.AttributeOverflow
					|| cue.MagnitudeType == CueMagnitudeType.AttributeValidModifier
					|| cue.MagnitudeType == CueMagnitudeType.AttributeMin
					|| cue.MagnitudeType == CueMagnitudeType.AttributeMax
					|| cue.MagnitudeType == CueMagnitudeType.AttributeMagnitudeEvaluatedUpToChannel
					|| cue.MagnitudeAttribute is null,
				"Attribute magnitudes type must have a configured MagnitudeAttribute, and not configured otherwise.");
		}
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
		hash.Add(RequireModifierSuccessToTriggerCue);
		hash.Add(SuppressStackingCues);

		foreach (CustomExecution execution in CustomExecutions)
		{
			hash.Add(execution);
		}

		foreach (Modifier modifier in Modifiers)
		{
			hash.Add(modifier);
		}

		foreach (IEffectComponent component in EffectComponents)
		{
			hash.Add(component);
		}

		foreach (CueData cue in Cues)
		{
			hash.Add(cue);
		}

		return hash.ToHashCode();
	}

	/// <inheritdoc/>
	public override bool Equals(object? obj)
	{
		if (obj is EffectData other)
		{
			return Equals(other);
		}

		return false;
	}

	/// <inheritdoc/>
	public bool Equals(EffectData other)
	{
		return Name == other.Name
			&& DurationData.Equals(other.DurationData)
			&& Nullable.Equals(StackingData, other.StackingData)
			&& Nullable.Equals(PeriodicData, other.PeriodicData)
			&& SnapshopLevel == other.SnapshopLevel
			&& RequireModifierSuccessToTriggerCue == other.RequireModifierSuccessToTriggerCue
			&& SuppressStackingCues == other.SuppressStackingCues
			&& CustomExecutions.SequenceEqual(other.CustomExecutions)
			&& Modifiers.SequenceEqual(other.Modifiers)
			&& EffectComponents.SequenceEqual(other.EffectComponents)
			&& Cues.SequenceEqual(other.Cues);
	}

	/// <summary>
	/// Determines if two <see cref="EffectData"/> objects are equal.
	/// </summary>
	/// <param name="lhs">The first <see cref="EffectData"/> to compare.</param>
	/// <param name="rhs">The second <see cref="EffectData"/> to compare.</param>
	/// <returns><see langword="true"/> if the values of <paramref name="lhs"/> and <paramref name="rhs"/> are equal;
	/// otherwise, <see langword="false"/>.</returns>
	public static bool operator ==(EffectData lhs, EffectData rhs)
	{
		return lhs.Equals(rhs);
	}

	/// <summary>
	/// Determines if two <see cref="EffectData"/> objects are not equal.
	/// </summary>
	/// <param name="lhs">The first <see cref="EffectData"/> to compare.</param>
	/// <param name="rhs">The second <see cref="EffectData"/> to compare.</param>
	/// <returns><see langword="true"/> if the values of <paramref name="lhs"/> and <paramref name="rhs"/> are not
	/// equal; otherwise, <see langword="false"/>.</returns>
	public static bool operator !=(EffectData lhs, EffectData rhs)
	{
		return !(lhs == rhs);
	}
}
