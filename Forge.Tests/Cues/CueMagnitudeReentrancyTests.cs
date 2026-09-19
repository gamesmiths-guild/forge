// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Components;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Effects.Modifiers;
using Gamesmiths.Forge.Effects.Periodic;
using Gamesmiths.Forge.Effects.Stacking;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

using static Gamesmiths.Forge.Tests.Helpers.TagsAndCuesFixture;

namespace Gamesmiths.Forge.Tests.Cues;

/// <summary>
/// An <see cref="CueMagnitudeType.AttributeValueChange"/> cue and the modifier-success check read the changes the
/// effect's own operation made to its target, tallied apart from every other operation on that entity. A hook that
/// runs between the effect's attribute writes and its cues — an executed hook raising an event that activates an
/// ability, a changed hook applying threshold effects — can land another effect on the same target, and neither that
/// effect's cues nor the original's may read each other's changes. The cue handlers themselves are arbitrary code
/// too, so they keep running after the hooks: closing the tally early is what protects the magnitudes, not
/// dispatching early.
/// </summary>
/// <param name="tagsAndCuesFixture">The fixture providing the <see cref="TagsManager"/> and <see cref="CuesManager"/>.
/// </param>
public class CueMagnitudeReentrancyTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private const string CueAttribute = "TestAttributeSet.Attribute90";
	private const string SideAttribute = "TestAttributeSet.Attribute1";
	private const string KeptAttribute = "TestAttributeSet.Attribute1000";
	private const string ArrivingAttribute = "VitalAttributeSet.CurrentHealth";

	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;
	private readonly TestCue _cue = tagsAndCuesFixture.TestCueInstances[0];
	private readonly TestCue _sideCue = tagsAndCuesFixture.TestCueInstances[1];
	private readonly TestCue _otherCue = tagsAndCuesFixture.TestCueInstances[2];

	[Fact]
	[Trait("Execute", null)]
	public void Execute_cue_keeps_its_attribute_change_when_an_executed_hook_applies_an_effect_to_the_target()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);
		_cue.Reset();

		// The Thorns recipe: the executed event activates an ability whose commit lands its cost on the same entity.
		target.Events.Subscribe(EventTag(), _ => target.EffectsManager.ApplyEffect(CreateSideEffect(target)));

		target.EffectsManager.ApplyEffect(CreateInstantEffect(target));

		target.PlayerAttributeSet.Attribute90.CurrentValue.Should().Be(80);
		_cue.ExecuteData.Count.Should().Be(1);
		_cue.ExecuteData.Value.Should().Be(-10);
	}

	[Fact]
	[Trait("Update", null)]
	public void Update_cue_keeps_its_attribute_change_when_a_changed_hook_applies_an_effect_to_the_target()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);
		_cue.Reset();

		target.EffectsManager.OnActiveEffectChanged +=
			_ => target.EffectsManager.ApplyEffect(CreateSideEffect(target));

		Effect effect = CreateStackableEffect(target);
		target.EffectsManager.ApplyEffect(effect);
		target.EffectsManager.ApplyEffect(effect);

		target.PlayerAttributeSet.Attribute90.CurrentValue.Should().Be(80);
		_cue.UpdateData.Count.Should().Be(1);
		_cue.UpdateData.Value.Should().Be(-5);
	}

	[Fact]
	[Trait("Execute", null)]
	public void Execute_cue_handlers_run_after_the_components_so_an_accumulator_tallies_the_execution_they_reenter()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);
		var tallyTag = Tag.RequestTag(_tagsManager, "other.tag");
		_cue.Reset();

		// A cue handler landing another effect on the target flushes this execution's deltas; the accumulator has to
		// have tallied them by then, or its baseline moves past them and the execution counts for nothing.
		void ReenterFromCue()
		{
			target.EffectsManager.ApplyEffect(CreateSideEffect(target));
		}

		_cue.OnExecuted += ReenterFromCue;

		try
		{
			ActiveEffectHandle handle = target.EffectsManager.ApplyEffect(CreatePeriodicEffect(target, tallyTag))!;
			target.EffectsManager.UpdateEffects(1);

			target.PlayerAttributeSet.Attribute90.CurrentValue.Should().Be(70);
			handle.GetComponent<AttributeAccumulatorEffectComponent>()!.Total.Should().Be(20);
			_cue.ExecuteData.Count.Should().Be(2);
			_cue.ExecuteData.Value.Should().Be(-10);
		}
		finally
		{
			_cue.OnExecuted -= ReenterFromCue;
		}
	}

	[Fact]
	[Trait("Update", null)]
	public void Update_cue_handlers_run_after_the_changed_hooks_so_a_handler_removing_the_effect_leaves_them_a_live_handle()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);
		var cueTag = Tag.RequestTag(_tagsManager, "other.tag");
		var removeOnUpdate = new RemoveOnUpdateCueHandler();
		_cuesManager.RegisterCue(cueTag, removeOnUpdate);

		try
		{
			bool? handleWasValidInChangedHook = null;
			target.EffectsManager.OnActiveEffectChanged += changed => handleWasValidInChangedHook = changed.IsValid;

			Effect effect = CreateStackableEffect(target, cueTag);
			ActiveEffectHandle handle = target.EffectsManager.ApplyEffect(effect)!;
			removeOnUpdate.Handle = handle;
			target.EffectsManager.ApplyEffect(effect);

			handleWasValidInChangedHook.Should().BeTrue();
			handle.IsValid.Should().BeFalse();
			removeOnUpdate.LastMagnitude.Should().Be(-5);
		}
		finally
		{
			_cuesManager.UnregisterCue(cueTag, removeOnUpdate);
		}
	}

	[Fact]
	[Trait("Execute", null)]
	public void A_nested_effects_cues_read_the_changes_of_its_own_execution_and_not_the_outer_ones()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);
		_cue.Reset();
		_sideCue.Reset();
		_otherCue.Reset();

		// The side effect lands while the outer hit's change to Attribute90 is still pending on the entity. Its cue on
		// that attribute must read nothing, and only its cue on the attribute it touched must read its own change.
		target.Events.Subscribe(
			EventTag(),
			_ => target.EffectsManager.ApplyEffect(CreateSideEffect(
				target,
				cue: CreateCue("test.cue2"),
				otherCue: CreateCue("test.cue3", SideAttribute))));

		target.EffectsManager.ApplyEffect(CreateInstantEffect(target));

		_cue.ExecuteData.Value.Should().Be(-10);
		_sideCue.ExecuteData.Count.Should().Be(1);
		_sideCue.ExecuteData.Value.Should().Be(0);
		_otherCue.ExecuteData.Count.Should().Be(1);
		_otherCue.ExecuteData.Value.Should().Be(1);
	}

	[Fact]
	[Trait("Execute", null)]
	public void Modifier_success_is_judged_on_the_effects_own_execution()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);
		_cue.Reset();
		_sideCue.Reset();

		// A nested effect whose modifier changes nothing must not have its cues fired by the outer hit's pending
		// change.
		target.Events.Subscribe(
			EventTag(),
			_ => target.EffectsManager.ApplyEffect(CreateSideEffect(
				target,
				magnitude: 0,
				cue: CreateCue("test.cue2"),
				requireModifierSuccess: true)));

		target.EffectsManager.ApplyEffect(CreateInstantEffect(target));

		_cue.ExecuteData.Value.Should().Be(-10);
		_sideCue.ExecuteData.Count.Should().Be(0);
	}

	[Fact]
	[Trait("Execute", null)]
	public void Every_cue_of_an_execution_is_read_before_its_hooks_and_handlers_can_move_the_attributes()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);
		_cue.Reset();
		_sideCue.Reset();

		// Both an executed hook and the first cue's own handler take another 5 off the attribute; the second cue, read
		// after the execution's writes and before either ran, still reports the 80 this execution left.
		target.Events.Subscribe(
			EventTag(),
			_ => target.EffectsManager.ApplyEffect(CreateSideEffect(target, CueAttribute, -5)));

		void ReenterFromCue()
		{
			target.EffectsManager.ApplyEffect(CreateSideEffect(target, CueAttribute, -5));
		}

		_cue.OnExecuted += ReenterFromCue;

		try
		{
			target.EffectsManager.ApplyEffect(CreateInstantEffect(
				target,
				CreateCue("test.cue2", CueAttribute, CueMagnitudeType.AttributeCurrentValue)));

			target.PlayerAttributeSet.Attribute90.CurrentValue.Should().Be(70);
			_cue.ExecuteData.Value.Should().Be(-10);
			_sideCue.ExecuteData.Value.Should().Be(80);
		}
		finally
		{
			_cue.OnExecuted -= ReenterFromCue;
		}
	}

	[Fact]
	[Trait("Update", null)]
	public void Update_cues_after_an_attribute_set_leaves_report_the_modifier_it_took_with_it()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);
		var vitalSet = new VitalAttributeSet();
		target.Attributes.AddAttributeSet(vitalSet);
		_cue.Reset();
		_sideCue.Reset();

		target.EffectsManager.ApplyEffect(CreateCrossSetEffect(target));
		target.Attributes[ArrivingAttribute].CurrentValue.Should().Be(90);

		// The departing attribute is detached before the update cues run, but the unapply that gave it its 10 back was
		// this effect's doing and is still reported, the way its listeners still hear that last change.
		target.Attributes.RemoveAttributeSet(vitalSet);

		_cue.UpdateData.Count.Should().Be(1);
		_cue.UpdateData.Value.Should().Be(10);
		_sideCue.UpdateData.Value.Should().Be(0);
	}

	[Fact]
	[Trait("Update", null)]
	public void Update_cues_after_an_attribute_set_arrives_report_what_the_effect_now_contributes()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);
		_cue.Reset();
		_sideCue.Reset();

		// Applied while the entity lacks the set, so only the kept attribute is modified at first.
		target.EffectsManager.ApplyEffect(CreateCrossSetEffect(target));

		target.Attributes.AddAttributeSet(new VitalAttributeSet());

		// The arriving attribute takes the effect's full modifier; the kept one is unapplied and re-applied, which nets
		// to nothing. Both are read after every effect was rebuilt and the attributes flushed.
		target.Attributes[ArrivingAttribute].CurrentValue.Should().Be(90);
		_cue.UpdateData.Count.Should().Be(1);
		_cue.UpdateData.Value.Should().Be(-10);
		_sideCue.UpdateData.Count.Should().Be(1);
		_sideCue.UpdateData.Value.Should().Be(0);
	}

	private static Effect CreateSideEffect(
		TestEntity target,
		string attribute = SideAttribute,
		float magnitude = 1,
		CueData? cue = null,
		CueData? otherCue = null,
		bool requireModifierSuccess = false)
	{
		CueTriggerRequirement requirement = requireModifierSuccess
			? CueTriggerRequirement.OnExecute
			: CueTriggerRequirement.None;

		var effectData = new EffectData(
			"Side Effect",
			new DurationData(DurationType.Instant),
			[CreateModifier(attribute, magnitude)],
			requireModifierSuccessToTriggerCue: requirement,
			cues: [.. new[] { cue, otherCue }.OfType<CueData>()]);

		return new Effect(effectData, new EffectOwnership(target, target));
	}

	private static Modifier CreateModifier(string attribute, float magnitude)
	{
		return new Modifier(
			attribute,
			ModifierOperation.FlatBonus,
			new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(magnitude)));
	}

	private static CueData CreateCue(
		Tag cueTag,
		string attribute,
		CueMagnitudeType magnitudeType = CueMagnitudeType.AttributeValueChange)
	{
		return new CueData(cueTag.GetSingleTagContainer(), -100, 100, magnitudeType, attribute);
	}

	private CueData CreateCue(Tag? cueTag = null)
	{
		return CreateCue(cueTag ?? Tag.RequestTag(_tagsManager, "test.cue1"), CueAttribute);
	}

	private CueData CreateCue(
		string cueTagName,
		string attribute = CueAttribute,
		CueMagnitudeType magnitudeType = CueMagnitudeType.AttributeValueChange)
	{
		return CreateCue(Tag.RequestTag(_tagsManager, cueTagName), attribute, magnitudeType);
	}

	private Effect CreateCrossSetEffect(TestEntity target)
	{
		var effectData = new EffectData(
			"Cross Set Buff",
			new DurationData(DurationType.Infinite),
			[CreateModifier(KeptAttribute, 10), CreateModifier(ArrivingAttribute, -10)],
			cues: [CreateCue("test.cue1", ArrivingAttribute), CreateCue("test.cue2", KeptAttribute)]);

		return new Effect(effectData, new EffectOwnership(target, target));
	}

	private Effect CreateInstantEffect(TestEntity target, CueData? extraCue = null)
	{
		var effectData = new EffectData(
			"Instant Effect",
			new DurationData(DurationType.Instant),
			[CreateModifier(CueAttribute, -10)],
			effectComponents:
			[
				new RaiseEventEffectComponent(EventTag().GetSingleTagContainer()!, EffectEventTrigger.Executed)
			],
			cues: [.. new[] { CreateCue(), extraCue }.OfType<CueData>()]);

		return new Effect(effectData, new EffectOwnership(target, target));
	}

	private Effect CreatePeriodicEffect(TestEntity target, Tag tallyTag)
	{
		var effectData = new EffectData(
			"Periodic Effect",
			new DurationData(
				DurationType.HasDuration,
				new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(10))),
			[CreateModifier(CueAttribute, -10)],
			periodicData: new PeriodicData(new ScalableFloat(1), true, PeriodInhibitionRemovedPolicy.NeverReset),
			effectComponents: [new AttributeAccumulatorEffectComponent(CueAttribute, tallyTag)],
			cues: [CreateCue()]);

		return new Effect(effectData, new EffectOwnership(target, target));
	}

	private Effect CreateStackableEffect(TestEntity target, Tag? cueTag = null)
	{
		var effectData = new EffectData(
			"Stackable Effect",
			new DurationData(DurationType.Infinite),
			[CreateModifier(CueAttribute, -5)],
			new StackingData(
				new ScalableInt(2),
				new ScalableInt(1),
				StackPolicy.AggregateBySource,
				StackLevelPolicy.AggregateLevels,
				StackMagnitudePolicy.Sum,
				StackOverflowPolicy.DenyApplication,
				StackExpirationPolicy.ClearEntireStack),
			cues: [CreateCue(cueTag)]);

		return new Effect(effectData, new EffectOwnership(target, target));
	}

	private Tag EventTag()
	{
		return Tag.RequestTag(_tagsManager, "tag");
	}

	/// <summary>
	/// A cue handler that takes its effect off the target from the update, the way a "dispel on this cue" handler
	/// would.
	/// </summary>
	private sealed class RemoveOnUpdateCueHandler : ICueHandler
	{
		public ActiveEffectHandle? Handle { get; set; }

		public int? LastMagnitude { get; private set; }

		public void OnApply(IForgeEntity? target, CueParameters? parameters)
		{
		}

		public void OnExecute(IForgeEntity? target, CueParameters? parameters)
		{
		}

		public void OnRemove(IForgeEntity? target, bool interrupted)
		{
		}

		public void OnUpdate(IForgeEntity? target, CueParameters? parameters)
		{
			LastMagnitude = parameters?.Magnitude;
			Handle?.Target?.EffectsManager.RemoveEffect(Handle, true);
		}
	}
}
