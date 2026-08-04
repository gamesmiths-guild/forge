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

namespace Gamesmiths.Forge.Tests.Effects;

public class EffectsManagerEventsTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private const string TargetAttribute = "TestAttributeSet.Attribute1";

	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Lifecycle", null)]
	public void An_active_effect_reports_applied_and_added_once_each()
	{
		TestEntity target = CreateEntity();
		var log = new EventLog(target.EffectsManager);

		ActiveEffectHandle? handle = ApplyEffect(target, CreateEffectData(DurationType.Infinite));

		log.Entries.Should().Equal("Applied", "Added");
		log.AddedHandles.Should().ContainSingle().Which.Should().BeSameAs(handle);
	}

	[Fact]
	[Trait("Lifecycle", null)]
	public void An_added_effect_reports_a_settled_handle()
	{
		TestEntity target = CreateEntity();

		int stacksWhenAdded = 0;
		int valueWhenAdded = 0;

		target.EffectsManager.OnActiveEffectAdded += handle =>
		{
			stacksWhenAdded = handle.StackCount;
			valueWhenAdded = target.PlayerAttributeSet.Attribute1.CurrentValue;
		};

		ApplyEffect(target, CreateStackableEffectData(initialStack: 2));

		// The event lands after the modifiers have been applied and the stack count settled, which is what a buff bar
		// reads straight out of the handle.
		stacksWhenAdded.Should().Be(2);
		valueWhenAdded.Should().Be(11);
	}

	[Fact]
	[Trait("Lifecycle", null)]
	public void An_instant_effect_reports_applied_and_executed_but_never_added()
	{
		TestEntity target = CreateEntity();
		var log = new EventLog(target.EffectsManager);

		ApplyEffect(target, CreateEffectData(DurationType.Instant)).Should().BeNull();

		log.Entries.Should().Equal("Applied", "Executed");
	}

	[Fact]
	[Trait("Lifecycle", null)]
	public void A_periodic_effect_reports_executed_on_every_tick()
	{
		TestEntity target = CreateEntity();
		var log = new EventLog(target.EffectsManager);

		ApplyEffect(target, CreatePeriodicEffectData());

		log.ExecutedCount.Should().Be(1, "the effect executes on application");

		target.EffectsManager.UpdateEffects(3);

		log.ExecutedCount.Should().Be(4);
		log.AddedHandles.Should().ContainSingle("a periodic effect is added once, not once per tick");
	}

	[Theory]
	[Trait("Lifecycle", null)]
	[InlineData(true, EffectRemovalReason.Expired)]
	[InlineData(false, EffectRemovalReason.Removed)]
	public void An_ending_effect_reports_removed_with_the_reason(bool letItExpire, EffectRemovalReason expected)
	{
		TestEntity target = CreateEntity();
		var log = new EventLog(target.EffectsManager);

		ActiveEffectHandle? handle = ApplyEffect(target, CreateEffectData(DurationType.HasDuration));

		if (letItExpire)
		{
			target.EffectsManager.UpdateEffects(11);
		}
		else
		{
			target.EffectsManager.RemoveEffect(handle!);
		}

		log.Removals.Should().ContainSingle();
		log.Removals[0].Handle.Should().BeSameAs(handle);
		log.Removals[0].Reason.Should().Be(expected);
	}

	[Fact]
	[Trait("Lifecycle", null)]
	public void The_removed_handle_is_readable_inside_the_handler_and_invalid_afterwards()
	{
		TestEntity target = CreateEntity();

		bool validInsideHandler = false;
		string? nameInsideHandler = null;

		target.EffectsManager.OnActiveEffectRemoved += (handle, _) =>
		{
			validInsideHandler = handle.IsValid;
			nameInsideHandler = handle.Effect?.EffectData.Name;
		};

		ActiveEffectHandle? applied = ApplyEffect(target, CreateEffectData(DurationType.Infinite));
		target.EffectsManager.RemoveEffect(applied!);

		validInsideHandler.Should().BeTrue();
		nameInsideHandler.Should().Be("Observable Effect");
		applied!.IsValid.Should().BeFalse();
	}

	[Fact]
	[Trait("Lifecycle", null)]
	public void A_new_stack_reports_applied_and_changed_but_not_added()
	{
		TestEntity target = CreateEntity();
		EffectData effectData = CreateStackableEffectData();

		ApplyEffect(target, effectData);

		var log = new EventLog(target.EffectsManager);
		ActiveEffectHandle? handle = ApplyEffect(target, effectData);

		log.Entries.Should().Equal("Changed", "Applied");
		log.ChangedHandles.Should().ContainSingle().Which.Should().BeSameAs(handle);
		handle!.StackCount.Should().Be(2);
	}

	[Fact]
	[Trait("Lifecycle", null)]
	public void Losing_one_stack_of_a_surviving_effect_reports_changed_not_removed()
	{
		TestEntity target = CreateEntity();
		EffectData effectData = CreateStackableEffectData();

		ApplyEffect(target, effectData);
		ActiveEffectHandle? handle = ApplyEffect(target, effectData);

		var log = new EventLog(target.EffectsManager);
		target.EffectsManager.RemoveEffect(handle!, stacksToRemove: 1);

		log.Removals.Should().BeEmpty();
		log.ChangedHandles.Should().ContainSingle();
		handle!.StackCount.Should().Be(1);
	}

	[Fact]
	[Trait("Lifecycle", null)]
	public void Inhibition_reports_changed()
	{
		TestEntity target = CreateEntity();
		ActiveEffectHandle? handle = ApplyEffect(target, CreateEffectData(DurationType.Infinite));

		var log = new EventLog(target.EffectsManager);

		handle!.SetInhibit(true);
		handle.SetInhibit(false);

		log.ChangedHandles.Should().HaveCount(2);
		log.AddedHandles.Should().BeEmpty();
		log.Removals.Should().BeEmpty();
	}

	[Fact]
	[Trait("Denial", null)]
	public void A_stack_denied_at_the_limit_reports_stack_denied_and_nothing_else()
	{
		TestEntity target = CreateEntity();
		EffectData effectData = CreateStackableEffectData(stackLimit: 1);

		ActiveEffectHandle? handle = ApplyEffect(target, effectData);

		var log = new EventLog(target.EffectsManager);
		var denied = new Effect(effectData, new EffectOwnership(target, target));

		target.EffectsManager.ApplyEffect(denied).Should().BeSameAs(handle);

		log.Entries.Should().Equal("StackDenied");
		log.StackDenials.Should().ContainSingle();
		log.StackDenials[0].Effect.Should().BeSameAs(denied);
		log.StackDenials[0].Handle.Should().BeSameAs(handle);
	}

	[Fact]
	[Trait("Denial", null)]
	public void A_stack_denied_by_its_owner_policy_reports_stack_denied()
	{
		TestEntity target = CreateEntity();
		TestEntity otherOwner = CreateEntity();
		EffectData effectData = CreateStackableEffectData(ownerDenialPolicy: StackOwnerDenialPolicy.DenyIfDifferent);

		ApplyEffect(target, effectData);

		var log = new EventLog(target.EffectsManager);
		target.EffectsManager.ApplyEffect(new Effect(effectData, new EffectOwnership(otherOwner, otherOwner)));

		log.Entries.Should().Equal("StackDenied");
	}

	[Fact]
	[Trait("Denial", null)]
	public void An_effect_denied_before_it_reaches_an_active_one_reports_nothing()
	{
		TestEntity target = CreateEntity();
		var log = new EventLog(target.EffectsManager);

		// The target does not carry color.red, so the effect denies itself before any stacking rule is consulted.
		var effectData = new EffectData(
			"Self Denied Effect",
			new DurationData(DurationType.Infinite),
			CreateModifiers(),
			effectComponents:
			[
				new TargetTagRequirementsEffectComponent(
					applicationTagRequirements: new TagRequirements(RequiredTags: MakeContainer("color.red")))
			]);

		target.EffectsManager.ApplyEffect(new Effect(effectData, new EffectOwnership(target, target)))
			.Should().BeNull();

		log.Entries.Should().BeEmpty();
	}

	[Fact]
	[Trait("Scope", null)]
	public void The_events_only_report_what_lands_on_their_own_owner()
	{
		TestEntity watched = CreateEntity();
		TestEntity other = CreateEntity();

		var log = new EventLog(watched.EffectsManager);

		ApplyEffect(other, CreateEffectData(DurationType.Infinite));

		log.Entries.Should().BeEmpty();
	}

	// Regression: a component reacting to the application can take the effect straight back off, and announcing an
	// addition that already ended leaves every listener holding an entry nothing ever removes.
	[Fact]
	[Trait("Lifecycle", null)]
	public void An_effect_removed_during_its_own_application_never_reports_as_added()
	{
		TestEntity target = CreateEntity();
		var log = new EventLog(target.EffectsManager);

		var effectData = new EffectData(
			"Self Removing Effect",
			new DurationData(DurationType.Infinite),
			CreateModifiers(),
			effectComponents: [new SelfRemovingComponent()]);

		target.EffectsManager.ApplyEffect(new Effect(effectData, new EffectOwnership(target, target)));

		log.AddedHandles.Should().BeEmpty();
		log.Removals.Should().ContainSingle();
		target.EffectsManager.GetActiveEffects().Should().BeEmpty();
	}

	private static Modifier[] CreateModifiers()
	{
		return
		[
			new Modifier(
				TargetAttribute,
				ModifierOperation.FlatBonus,
				new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(5)))
		];
	}

	private static EffectData CreateEffectData(DurationType durationType)
	{
		ModifierMagnitude? duration = durationType == DurationType.HasDuration
			? new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(10f))
			: null;

		return new EffectData(
			"Observable Effect",
			new DurationData(durationType, duration),
			CreateModifiers());
	}

	private static EffectData CreatePeriodicEffectData()
	{
		return new EffectData(
			"Observable Periodic Effect",
			new DurationData(DurationType.Infinite),
			CreateModifiers(),
			periodicData: new PeriodicData(
				new ScalableFloat(1f),
				true,
				PeriodInhibitionRemovedPolicy.NeverReset));
	}

	private static EffectData CreateStackableEffectData(
		int stackLimit = 3,
		int initialStack = 1,
		StackOwnerDenialPolicy ownerDenialPolicy = StackOwnerDenialPolicy.AlwaysAllow)
	{
		return new EffectData(
			"Observable Stackable Effect",
			new DurationData(DurationType.Infinite),
			CreateModifiers(),
			new StackingData(
				new ScalableInt(stackLimit),
				new ScalableInt(initialStack),
				StackPolicy.AggregateByTarget,
				StackLevelPolicy.SegregateLevels,
				StackMagnitudePolicy.Sum,
				StackOverflowPolicy.DenyApplication,
				StackExpirationPolicy.ClearEntireStack,
				ownerDenialPolicy,
				StackOwnerOverridePolicy.KeepCurrent,
				StackOwnerOverrideStackCountPolicy.IncreaseStacks));
	}

	private static ActiveEffectHandle? ApplyEffect(TestEntity target, EffectData effectData)
	{
		return target.EffectsManager.ApplyEffect(new Effect(effectData, new EffectOwnership(target, target)));
	}

	private TagContainer MakeContainer(params string[] tagKeys)
	{
		return new TagContainer(_tagsManager, TestUtils.StringToTag(_tagsManager, tagKeys));
	}

	private TestEntity CreateEntity()
	{
		return new TestEntity(_tagsManager, _cuesManager);
	}

	/// <summary>
	/// Subscribes to every manager event and records them in the order they arrive, so tests can assert on both what
	/// was raised and the sequence.
	/// </summary>
	private sealed class EventLog
	{
		public List<string> Entries { get; } = [];

		public List<ActiveEffectHandle> AddedHandles { get; } = [];

		public List<ActiveEffectHandle> ChangedHandles { get; } = [];

		public List<(ActiveEffectHandle Handle, EffectRemovalReason Reason)> Removals { get; } = [];

		public List<(Effect Effect, ActiveEffectHandle Handle)> StackDenials { get; } = [];

		public int AppliedCount { get; private set; }

		public int ExecutedCount { get; private set; }

		public EventLog(EffectsManager effectsManager)
		{
			effectsManager.OnEffectApplied += _ =>
			{
				Entries.Add("Applied");
				AppliedCount++;
			};

			effectsManager.OnEffectExecuted += _ =>
			{
				Entries.Add("Executed");
				ExecutedCount++;
			};

			effectsManager.OnActiveEffectAdded += handle =>
			{
				Entries.Add("Added");
				AddedHandles.Add(handle);
			};

			effectsManager.OnActiveEffectChanged += handle =>
			{
				Entries.Add("Changed");
				ChangedHandles.Add(handle);
			};

			effectsManager.OnActiveEffectRemoved += (handle, reason) =>
			{
				Entries.Add("Removed");
				Removals.Add((handle, reason));
			};

			effectsManager.OnEffectStackDenied += (effect, handle) =>
			{
				Entries.Add("StackDenied");
				StackDenials.Add((effect, handle));
			};
		}
	}

	/// <summary>
	/// Takes its own effect back off from inside the very application that added it.
	/// </summary>
	private sealed class SelfRemovingComponent : IEffectComponent
	{
		public void OnPostActiveEffectAdded(
			IForgeEntity target,
			in ActiveEffectEvaluatedData activeEffectEvaluatedData)
		{
			target.EffectsManager.RemoveEffect(activeEffectEvaluatedData.ActiveEffectHandle);
		}
	}
}
