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
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Effects;

/// <summary>
/// The configuration data for a effect.
/// </summary>
public readonly record struct EffectData
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
	/// Gets a value indicating whether this effect snapshots the level at the moment of creation.
	/// </summary>
	public bool SnapshotLevel { get; }

	/// <summary>
	/// Gets the list of effect components that further customize this effect behavior.
	/// </summary>
	public IEffectComponent[] EffectComponents { get; }

	/// <summary>
	/// Gets a value indicating whether this effect requires the modifier to be successful to trigger cues for each
	/// types of cues.
	/// </summary>
	public CueTriggerRequirement RequireModifierSuccessToTriggerCue { get; }

	/// <summary>
	/// Gets a value indicating whether this effect suppresses stacking cues.
	/// </summary>
	public bool SuppressStackingCues { get; }

	/// <summary>
	/// Gets the cues associated with this effect.
	/// </summary>
	public CueData[] Cues { get; }

	/// <summary>
	/// Gets the tags that identify this effect, or <see langword="null"/> when it carries none.
	/// </summary>
	/// <remarks>
	/// <para>
	/// These tags classify the effect itself; they are never granted to the target. Use them to answer "what kind of
	/// effect is this?" — <c>effect.debuff.poison</c>, <c>effect.curse</c> — so that <see cref="EffectQuery"/> can
	/// select effects by category without every effect having to also change the target's state.
	/// </para>
	/// <para>
	/// This is the counterpart of <see cref="ModifierTagsEffectComponent"/>, which grants tags <i>to the target</i>.
	/// The rule of thumb: granted tags for entity state, effect tags for identity.
	/// </para>
	/// </remarks>
	public TagContainer? EffectTags { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="EffectData"/> struct.
	/// </summary>
	/// <param name="name">The name of this effect.</param>
	/// <param name="durationData">The duration data for this effect.</param>
	/// <param name="modifiers">The list of modifiers for this effect.</param>
	/// <param name="stackingData">The stacking data for this effect, if it's stackable.</param>
	/// <param name="periodicData">The periodic data for this effect, if it's periodic.</param>
	/// <param name="snapshotLevel">Whether or not this effect snapshots the level at the moment of creation.
	/// </param>
	/// <param name="effectComponents">The list of effects components for this effect.</param>
	/// <param name="requireModifierSuccessToTriggerCue">Flags indicating whether or not, and which types of cues are
	/// are triggered when modifiers are successfully applied.</param>
	/// <param name="suppressStackingCues">Whether or not to trigger cues when applying stacks.</param>
	/// <param name="customExecutions">The list of custom executions for this effect.</param>
	/// <param name="cues">The cues associated with this effect.</param>
	/// <param name="effectTags">The tags identifying this effect, used by <see cref="EffectQuery"/>. These are never
	/// granted to the target.</param>
	public EffectData(
		string name,
		DurationData durationData,
		Modifier[]? modifiers = null,
		StackingData? stackingData = null,
		PeriodicData? periodicData = null,
		bool snapshotLevel = true,
		IEffectComponent[]? effectComponents = null,
		CueTriggerRequirement requireModifierSuccessToTriggerCue = CueTriggerRequirement.None,
		bool suppressStackingCues = false,
		CustomExecution[]? customExecutions = null,
		CueData[]? cues = null,
		TagContainer? effectTags = null)
	{
		Name = name;
		DurationData = durationData;
		Modifiers = modifiers ?? [];
		StackingData = stackingData;
		PeriodicData = periodicData;
		SnapshotLevel = snapshotLevel;
		EffectComponents = effectComponents ?? [];
		RequireModifierSuccessToTriggerCue = requireModifierSuccessToTriggerCue;
		SuppressStackingCues = suppressStackingCues;
		CustomExecutions = customExecutions ?? [];
		Cues = cues ?? [];
		EffectTags = effectTags;

		if (Validation.Enabled)
		{
			ValidateData();
		}
	}

#pragma warning disable SA1118 // Parameter should not span multiple lines
	private void ValidateData()
	{
		Validation.Assert(
			!(PeriodicData.HasValue && DurationData.DurationType == DurationType.Instant),
			"Periodic effects can't be set as instant.");

		Validation.Assert(
			!(DurationData.DurationType != DurationType.HasDuration && DurationData.DurationMagnitude.HasValue),
			$"Can't set duration if {nameof(DurationType)} is set to {DurationData.DurationType}.");

		Validation.Assert(
			!(StackingData.HasValue && DurationData.DurationType == DurationType.Instant),
			$"{DurationType.Instant} effects can't have stacks.");

		Validation.Assert(
			!(StackingData.HasValue
				&& (StackingData.Value.InitialStack.BaseValue > StackingData.Value.StackLimit.BaseValue
				|| StackingData.Value.InitialStack.BaseValue == 0)),
			"Shouldn't set InitialStack count to be higher than the StackLimit nor zero. It's probably a bad " +
			"configuration.");

		Validation.Assert(
			!(StackingData.HasValue
			&& (StackingData.Value.StackPolicy == StackPolicy.AggregateByTarget !=
				StackingData.Value.OwnerDenialPolicy.HasValue)),
			$"If {nameof(StackPolicy)} is set {StackPolicy.AggregateByTarget}, {nameof(StackOwnerDenialPolicy)} must" +
			" be defined. And not defined if otherwise.");

		Validation.Assert(
			!(StackingData.HasValue
			&& ((StackingData.Value.StackPolicy == StackPolicy.AggregateByTarget &&
				StackingData.Value.OwnerDenialPolicy == StackOwnerDenialPolicy.AlwaysAllow) !=
				StackingData.Value.OwnerOverridePolicy.HasValue)),
			$"If {nameof(StackPolicy)} is set {StackPolicy.AggregateByTarget} and {nameof(StackOwnerDenialPolicy)} " +
			$"is set to {StackOwnerDenialPolicy.AlwaysAllow}, {nameof(StackOwnerOverridePolicy)} must be defined. " +
			"And not defined if otherwise.");

		Validation.Assert(
			!(StackingData.HasValue
			&& ((StackingData.Value.StackPolicy == StackPolicy.AggregateByTarget &&
				StackingData.Value.OwnerOverridePolicy.HasValue &&
				StackingData.Value.OwnerOverridePolicy.Value == StackOwnerOverridePolicy.Override) !=
				StackingData.Value.OwnerOverrideStackCountPolicy.HasValue)),
			$"If {nameof(StackOwnerOverridePolicy)} is set {StackOwnerOverridePolicy.Override}, " +
			$"{nameof(StackOwnerOverrideStackCountPolicy)} must be defined. And not defined if otherwise.");

		Validation.Assert(
			!(StackingData.HasValue
				&& (StackingData.Value.StackLevelPolicy == StackLevelPolicy.AggregateLevels !=
				StackingData.Value.LevelDenialPolicy.HasValue)),
			$"If {nameof(StackLevelPolicy)} is set {StackLevelPolicy.AggregateLevels}, {nameof(LevelComparison)} " +
			"must be defined. And not defined if otherwise.");

		Validation.Assert(
			!(StackingData.HasValue
				&& (StackingData.Value.StackLevelPolicy == StackLevelPolicy.AggregateLevels !=
				StackingData.Value.LevelOverridePolicy.HasValue)),
			$"If {nameof(StackLevelPolicy)} is set {StackLevelPolicy.AggregateLevels}, LevelOverridePolicy must be " +
			"defined. And not defined if otherwise.");

		Validation.Assert(
			!(StackingData.HasValue
				&& ((StackingData.Value.StackLevelPolicy == StackLevelPolicy.AggregateLevels &&
				StackingData.Value.LevelOverridePolicy.HasValue &&
				StackingData.Value.LevelOverridePolicy.Value != LevelComparison.None) !=
				StackingData.Value.LevelOverrideStackCountPolicy.HasValue)),
			$"If LevelOverridePolicy is different from {LevelComparison.None}, " +
			$"{nameof(StackLevelOverrideStackCountPolicy)} must be defined. And not defined if otherwise.");

		Validation.Assert(
			!(StackingData.HasValue
				&& StackingData.Value.LevelDenialPolicy.HasValue &&
				StackingData.Value.LevelOverridePolicy.HasValue &&
				StackingData.Value.LevelDenialPolicy.Value != LevelComparison.None &&
				(StackingData.Value.LevelDenialPolicy.Value & StackingData.Value.LevelOverridePolicy.Value) != 0),
			"LevelDenialPolicy and LevelOverridePolicy shouldn't have the same value. If it's getting denied, how " +
			"will it override?");

		Validation.Assert(
			!(StackingData.HasValue
				&& (DurationData.DurationType == DurationType.HasDuration !=
				StackingData.Value.ApplicationRefreshPolicy.HasValue)),
			$"Effects set as {DurationType.HasDuration} must define {nameof(StackApplicationRefreshPolicy)} and not " +
			"define it if otherwise.");

		Validation.Assert(
			!(StackingData.HasValue
				&& (StackingData.Value.ExecuteOnSuccessfulApplication.HasValue != PeriodicData.HasValue)),
			$"Both {nameof(PeriodicData)} and ExecuteOnSuccessfulApplication must be either defined or undefined.");

		Validation.Assert(
			!(StackingData.HasValue
				&& (StackingData.Value.ApplicationResetPeriodPolicy.HasValue != PeriodicData.HasValue)),
			$"Both {nameof(PeriodicData)} and {nameof(StackApplicationResetPeriodPolicy)} must be either defined or " +
			"undefined.");

		Validation.Assert(
			!(DurationData.DurationType == DurationType.Instant && Modifiers is not null && Array.Exists(
				Modifiers,
				x => x.Magnitude.MagnitudeCalculationType == MagnitudeCalculationType.AttributeBased
					&& x.Magnitude.AttributeBasedFloat.HasValue
					&& !x.Magnitude.AttributeBasedFloat.Value.BackingAttribute.Snapshot)),
			$"Effects set as {DurationType.Instant} and {MagnitudeCalculationType.AttributeBased} cannot be set as " +
			"non Snapshot.");

		Validation.Assert(
			!(DurationData.DurationType == DurationType.Instant && !SnapshotLevel),
			$"Effects set as {DurationType.Instant} cannot be set as non Snapshot for Level.");

		Validation.Assert(
			!((DurationData.DurationType == DurationType.Instant || PeriodicData.HasValue)
				&& Modifiers is not null
				&& Array.Exists(Modifiers, x => x.AggregationMode != AggregationMode.Sum)),
			$"Effects set as {DurationType.Instant} and periodic effects execute their modifiers against the " +
			$"attribute's base value, so they can't use an {nameof(AggregationMode)} other than " +
			$"{AggregationMode.Sum}.");

		Validation.Assert(
			EffectComponents is null ||
			!Array.Exists(EffectComponents, x => x is ModifierTagsEffectComponent) ||
			(Array.Exists(EffectComponents, x => x is ModifierTagsEffectComponent) &&
			DurationData.DurationType != DurationType.Instant),
			$"Instant effects cannot apply tags from the {nameof(ModifierTagsEffectComponent)}.");

		Validation.Assert(
			EffectComponents is null ||
			!Array.Exists(EffectComponents, x => x is BlockAbilityTagsEffectComponent) ||
			DurationData.DurationType != DurationType.Instant,
			$"Instant effects cannot block abilities from the {nameof(BlockAbilityTagsEffectComponent)}.");

		Validation.Assert(
			EffectComponents is null ||
			!Array.Exists(
				EffectComponents,
				x => x is CancelAbilityTagsEffectComponent
				{
					Policy: CancelAbilityTagsPolicy.OnExecution
				}) ||
			DurationData.DurationType == DurationType.Instant ||
			PeriodicData.HasValue,
			$"{nameof(CancelAbilityTagsPolicy)}.{nameof(CancelAbilityTagsPolicy.OnExecution)} never fires on a " +
			"duration effect that isn't periodic.");

		Validation.Assert(
			EffectComponents is null ||
			!Array.Exists(
				EffectComponents,
				x => x is CancelAbilityTagsEffectComponent { HasAnyFilter: false }),
			$"{nameof(CancelAbilityTagsEffectComponent)} must define at least one of its tag filters. Leaving both " +
			"empty is treated as a misconfiguration and is rejected.");

		Validation.Assert(
			EffectComponents is null ||
			!Array.Exists(
				EffectComponents,
				x => x is AttributeRequirementsEffectComponent { HasVacuousRequirement: true }
					or SourceAttributeRequirementsEffectComponent { HasVacuousRequirement: true }),
			$"Every {nameof(AttributeRequirement)} must define a {nameof(AttributeRequirement.MinValue)}, " +
			$"a {nameof(AttributeRequirement.MaxValue)}, or both. One with neither bound always passes.");

		Validation.Assert(
			EffectComponents is null ||
			DurationData.DurationType != DurationType.Instant ||
			!Array.Exists(EffectComponents, x => x is ImmunityEffectComponent),
			$"{nameof(ImmunityEffectComponent)} requires a non-{DurationType.Instant} effect, since the immunity " +
			"lasts exactly as long as its effect is active and instant effects never become active.");

		Validation.Assert(
			EffectComponents is null ||
			!PeriodicData.HasValue ||
			!Array.Exists(EffectComponents, x => x is RemoveOtherEffectComponent),
			$"{nameof(RemoveOtherEffectComponent)} removes effects when its own effect is applied, never on a " +
			"periodic execution, so a periodic owner only ever removes once. Use " +
			$"{nameof(TargetTagRequirementsEffectComponent)} removal requirements for a recurring removal instead.");

		Validation.Assert(
			EffectComponents is null ||
			!Array.Exists(
				EffectComponents,
				x => x is ImmunityEffectComponent { HasEmptyQuery: true }
					or RemoveOtherEffectComponent { HasEmptyQuery: true }),
			$"An empty {nameof(EffectQuery)} matches every effect, so {nameof(ImmunityEffectComponent)} would block " +
			$"everything the target could receive and {nameof(RemoveOtherEffectComponent)} would strip it of " +
			"everything it has. Define at least one filter on every query.");

		Validation.Assert(
			EffectComponents is null ||
			DurationData.DurationType != DurationType.Instant ||
			!Array.Exists(
				EffectComponents,
				x => x is AdditionalEffectsEffectComponent { HasCompletionEffects: true }),
			$"The completion effects of {nameof(AdditionalEffectsEffectComponent)} are applied when the effect is " +
			$"removed, which an {DurationType.Instant} effect never is, since it never becomes active. Use the " +
			"application effects instead.");

		Validation.Assert(
			EffectComponents is null ||
			DurationData.DurationType != DurationType.Instant ||
			!Array.Exists(
				EffectComponents,
				x => x is AdditionalEffectsEffectComponent { HasRemoveOnEndEffect: true }),
			$"{nameof(ConditionalEffectRemovalPolicy)}.{nameof(ConditionalEffectRemovalPolicy.RemoveOnEnd)} takes an " +
			$"applied effect back when the effect that applied it ends, and an {DurationType.Instant} effect never " +
			$"becomes active, so it has no end to hook. Use {ConditionalEffectRemovalPolicy.Ignore} instead.");

		Validation.Assert(
			EffectComponents is null ||
			!Array.Exists(
				EffectComponents,
				x => x is AdditionalEffectsEffectComponent { HasInstantRemoveOnEndEffect: true }),
			$"{nameof(ConditionalEffectRemovalPolicy)}.{nameof(ConditionalEffectRemovalPolicy.RemoveOnEnd)} cannot " +
			$"be used with an {DurationType.Instant} applied effect: it executes and is gone immediately, so there " +
			"is nothing left to take back when the effect that applied it ends.");

		Validation.Assert(
			EffectComponents is null ||
			!Array.Exists(
				EffectComponents,
				x => x is AdditionalEffectsEffectComponent { HasRemoveOnEndCompletionEffect: true }),
			$"{nameof(ConditionalEffectRemovalPolicy)}.{nameof(ConditionalEffectRemovalPolicy.RemoveOnEnd)} has no " +
			"meaning on a completion effect, since the end it would be taken back at is the one applying it. Use " +
			$"{ConditionalEffectRemovalPolicy.Ignore} there, and the application effects when something has to be " +
			"taken back.");

		Validation.Assert(
			EffectComponents is null ||
			!Array.Exists(EffectComponents, x => x is RaiseEventEffectComponent { RaisesOnExecution: true }) ||
			DurationData.DurationType == DurationType.Instant ||
			PeriodicData.HasValue,
			$"{nameof(EffectEventTrigger)}.{nameof(EffectEventTrigger.Executed)} never fires on a duration effect " +
			"that isn't periodic, since only instant and periodic effects execute.");

		Validation.Assert(
			EffectComponents is null ||
			DurationData.DurationType != DurationType.Instant ||
			!Array.Exists(EffectComponents, x => x is RaiseEventEffectComponent { RaisesOnRemoval: true }),
			$"{nameof(EffectEventTrigger)}.{nameof(EffectEventTrigger.ExpiredNormally)} and " +
			$"{nameof(EffectEventTrigger)}.{nameof(EffectEventTrigger.RemovedPrematurely)} never fire on an " +
			$"{DurationType.Instant} effect, since it never becomes active and so is never removed. Use " +
			$"{nameof(EffectEventTrigger)}.{nameof(EffectEventTrigger.Applied)} instead.");

		Validation.Assert(
			EffectComponents is null ||
			DurationData.DurationType != DurationType.Infinite ||
			!Array.Exists(EffectComponents, x => x is RaiseEventEffectComponent { RaisesOnExpiration: true }),
			$"{nameof(EffectEventTrigger)}.{nameof(EffectEventTrigger.ExpiredNormally)} never fires on an " +
			$"{DurationType.Infinite} effect, since it has no duration to run out of and every removal of one is " +
			$"reported as {nameof(EffectRemovalReason)}.{nameof(EffectRemovalReason.Removed)}. Use " +
			$"{nameof(EffectEventTrigger)}.{nameof(EffectEventTrigger.RemovedPrematurely)} instead.");

		Validation.Assert(
			EffectComponents is null ||
			!Array.Exists(EffectComponents, x => x is RaiseEventEffectComponent { RaisesOnStackRemoval: true }) ||
			StackingData.HasValue,
			$"{nameof(EffectEventTrigger)}.{nameof(EffectEventTrigger.StackRemoved)} never fires on an effect that " +
			$"isn't stackable, since it has no stack to lose and survive. Define {nameof(StackingData)} or use " +
			$"{nameof(EffectEventTrigger)}.{nameof(EffectEventTrigger.ExpiredNormally)} / " +
			$"{nameof(EffectEventTrigger)}.{nameof(EffectEventTrigger.RemovedPrematurely)} instead.");

		Validation.Assert(
			EffectComponents is null ||
			!Array.Exists(
				EffectComponents,
				x => x is RaiseEventEffectComponent { HasNoEventTags: true } or RaiseEventEffectComponent
				{
					HasNoTrigger: true
				}),
			$"{nameof(RaiseEventEffectComponent)} must carry at least one event tag and at least one trigger. " +
			"Subscribers match on the tags, so an untagged event reaches nobody, and a component with no trigger " +
			"never raises at all.");

		Validation.Assert(
			EffectComponents is null ||
			!Array.Exists(EffectComponents, x => x is StackThresholdEffectComponent) ||
			StackingData.HasValue,
			$"{nameof(StackThresholdEffectComponent)} watches the effect's stack count, which only ever changes on a " +
			$"stackable effect. Define {nameof(StackingData)} or use {nameof(AdditionalEffectsEffectComponent)} to " +
			"apply the effect on every application instead.");

		Validation.Assert(
			EffectComponents is null ||
			!Array.Exists(
				EffectComponents,
				x => x is StackThresholdEffectComponent { HasUnreachableLowThreshold: true }),
			$"A {nameof(StackThresholdEffectComponent)} threshold of one or less is met by the initial stack of " +
			$"every application, which is what the application effects of {nameof(AdditionalEffectsEffectComponent)} " +
			"already express. Use a threshold of two or more.");

		Validation.Assert(
			EffectComponents is null ||
			!Array.Exists(
				EffectComponents,
				x => x is StackThresholdEffectComponent { HasInstantSustainedEffect: true }),
			$"{nameof(ConditionalEffectRemovalPolicy)}.{nameof(ConditionalEffectRemovalPolicy.RemoveOnEnd)} takes " +
			"the threshold effect back when the stack count drops below the threshold, and an " +
			$"{DurationType.Instant} effect executes and is gone immediately, so there is nothing left to take back. " +
			$"Use {ConditionalEffectRemovalPolicy.Ignore} instead.");

		Validation.Assert(
			EffectComponents is null ||
			!Array.Exists(EffectComponents, x => x is AttributeAccumulatorEffectComponent) ||
			DurationData.DurationType == DurationType.Instant ||
			PeriodicData.HasValue,
			$"{nameof(AttributeAccumulatorEffectComponent)} measures its attribute as the effect executes, and only " +
			$"{DurationType.Instant} and periodic effects execute, so a duration effect that isn't periodic would " +
			"always publish zero. Make the effect periodic, or measure it from the effect that does the executing.");

		Validation.Assert(
			EffectComponents is null ||
			DurationData.DurationType != DurationType.Instant ||
			!Array.Exists(
				EffectComponents,
				x => x is TargetTagRequirementsEffectComponent { HasOngoingRequirements: true }
					or SourceTagRequirementsEffectComponent { HasOngoingRequirements: true }
					or AttributeRequirementsEffectComponent { HasOngoingRequirements: true }
					or SourceAttributeRequirementsEffectComponent { HasOngoingRequirements: true }),
			$"Ongoing requirements never fire on an {DurationType.Instant} effect, since inhibition acts on an " +
			"active effect and instant effects never become one. Use application requirements instead.");

		foreach (CueData cue in Cues)
		{
			Validation.Assert(
				cue.MagnitudeType != CueMagnitudeType.StackCount || !SuppressStackingCues,
				"StackCount magnitude type is not allowed when SuppressStackingCues is set to true.");

			Validation.Assert(
				cue.MagnitudeType != CueMagnitudeType.StackCount || StackingData.HasValue,
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
#pragma warning restore SA1118 // Parameter should not span multiple lines
}
