// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Components;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Effects.Modifiers;
using Gamesmiths.Forge.Events;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Abilities;

public class AbilityBehaviorTests(TagsAndCuesFixture fixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = fixture.TagsManager;
	private readonly CuesManager _cuesManager = fixture.CuesManager;

	[Fact]
	[Trait("Lifecycle", null)]
	public void Behavior_OnStarted_and_OnEnded_are_invoked_per_instance()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var behavior = new TrackingBehavior();
		AbilityData data = CreateAbilityData("Tracked", behaviorFactory: () => behavior);
		AbilityHandle? handle = Grant(entity, data);
		handle.Should().NotBeNull();

		handle!.Activate(out AbilityActivationFailures failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);
		behavior.StartCount.Should().Be(1);
		behavior.EndCount.Should().Be(0);

		handle.Cancel();
		behavior.StartCount.Should().Be(1);
		behavior.EndCount.Should().Be(1);
	}

	[Fact]
	[Trait("Multiple Instances", null)]
	public void PerExecution_creates_distinct_behavior_instances()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var behaviors = new List<TrackingBehavior>();
		AbilityData data = CreateAbilityData(
			"Multi",
			behaviorFactory: () =>
			{
				var trackingBehavior = new TrackingBehavior();
				behaviors.Add(trackingBehavior);
				return trackingBehavior;
			},
			instancingPolicy: AbilityInstancingPolicy.PerExecution);
		AbilityHandle? handle = Grant(entity, data);
		handle.Should().NotBeNull();

		handle!.Activate(out _).Should().BeTrue();
		handle.Activate(out _).Should().BeTrue();
		handle.Activate(out _).Should().BeTrue();
		behaviors.Should().HaveCount(3);
		behaviors.Sum(x => x.StartCount).Should().Be(3);
		behaviors.Sum(x => x.EndCount).Should().Be(0);

		handle.Cancel();
		behaviors.Sum(x => x.EndCount).Should().Be(3);
	}

	[Fact]
	[Trait("Multiple Instances", null)]
	public void Blocked_ability_tags_are_removed_only_after_last_instance_ends()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);
		var behaviors = new List<TrackingBehavior>();

		var redTags = new TagContainer(_tagsManager, TestUtils.StringToTag(_tagsManager, ["color.red"]));

		AbilityData blocker = CreateAbilityData(
			"BlockerMulti",
			behaviorFactory: () =>
			{
				var trackingBehavior = new TrackingBehavior();
				behaviors.Add(trackingBehavior);
				return trackingBehavior;
			},
			instancingPolicy: AbilityInstancingPolicy.PerExecution,
			blockAbilitiesWithTag: redTags);

		AbilityData blocked = CreateAbilityData(
			"BlockedRed",
			abilityTags: redTags);

		AbilityHandle? blockerHandle = Grant(entity, blocker);
		AbilityHandle? blockedHandle = Grant(entity, blocked);

		blockerHandle!.Activate(out _).Should().BeTrue();
		blockerHandle!.Activate(out _).Should().BeTrue();

		// While any blocker instance active, blocked ability cannot activate.
		blockedHandle!.Activate(out AbilityActivationFailures failureFlags).Should().BeFalse();
		failureFlags.Should().Be(AbilityActivationFailures.BlockedByTags);
		blockedHandle.IsActive.Should().BeFalse();

		// End one blocker instance; still blocked.
		behaviors[0].End();
		blockedHandle.Activate(out failureFlags).Should().BeFalse();
		failureFlags.Should().Be(AbilityActivationFailures.BlockedByTags);
		blockedHandle.IsActive.Should().BeFalse();

		// End last blocker instance; now unblocked.
		behaviors[1].End();
		blockedHandle.Activate(out failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);
		blockedHandle.IsActive.Should().BeTrue();
	}

	[Fact]
	[Trait("Multiple Instances", null)]
	public void Activation_owned_tags_are_applied_on_activation_and_removed_after_last_instance_ends()
	{
		TestEntity entity = new(_tagsManager, _cuesManager);
		var behaviors = new List<TrackingBehavior>();

		var ownedTags = new TagContainer(_tagsManager, TestUtils.StringToTag(_tagsManager, ["tag"]));

		AbilityData abilityWithOwned = CreateAbilityData(
			"OwnedTagsAbility",
			behaviorFactory: () =>
			{
				var trackingBehavior = new TrackingBehavior();
				behaviors.Add(trackingBehavior);
				return trackingBehavior;
			},
			instancingPolicy: AbilityInstancingPolicy.PerExecution,
			activationOwnedTags: ownedTags);

		AbilityHandle? handle = Grant(entity, abilityWithOwned);

		handle!.Activate(out _).Should().BeTrue();
		handle!.Activate(out _).Should().BeTrue();
		handle!.Activate(out _).Should().BeTrue();

		entity.Tags.CombinedTags.HasAll(ownedTags).Should().BeTrue();

		behaviors[0].End();
		entity.Tags.CombinedTags.HasAll(ownedTags).Should().BeTrue();

		behaviors[1].End();
		behaviors[2].End();
		entity.Tags.CombinedTags.HasAny(ownedTags).Should().BeFalse();
	}

	[Fact]
	[Trait("Retrigger", null)]
	public void PerEntity_retrigger_invokes_previous_OnEnded_before_new_OnStarted()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var endedBeforeNew = false;
		TrackingBehavior? previous = null;

		AbilityData data = CreateAbilityData(
			"Retriggered",
			behaviorFactory: () =>
			{
				var behavior = new TrackingBehavior(() =>
				{
					if (previous?.EndCount == 1)
					{
						endedBeforeNew = true;
					}
				});

				previous ??= behavior;
				return behavior;
			},
			instancingPolicy: AbilityInstancingPolicy.PerEntity,
			retriggerInstancedAbility: true);

		AbilityHandle? handle = Grant(entity, data);
		handle.Should().NotBeNull();

		handle!.Activate(out _).Should().BeTrue();

		// Second activation retriggers: ability should call previous.OnEnded() before new.OnStarted().
		handle.Activate(out _).Should().BeTrue();

		endedBeforeNew.Should().BeTrue("the previous instance should have ended before the new one started");
	}

	[Fact]
	[Trait("Context", null)]
	public void Context_provides_expected_values()
	{
		var source = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);
		AbilityBehaviorContext? captured = null;
		var behavior = new CallbackBehavior(x => captured = x);

		AbilityData data = CreateAbilityData("ContextTest", behaviorFactory: () => behavior);
		AbilityHandle? handle = Grant(target, data, sourceEntity: source);
		handle.Should().NotBeNull();

		handle!.Activate(out AbilityActivationFailures failureFlags, target).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);
		captured.Should().NotBeNull();
		captured!.Owner.Should().Be(target);
		captured.Source.Should().Be(source);
		captured.Target.Should().Be(target);
		captured.Level.Should().Be(handle.Level);
		captured.AbilityHandle.Should().Be(handle);
		captured.InstanceHandle.IsActive.Should().BeTrue();
	}

	[Fact]
	[Trait("EndInsideStart", null)]
	public void Behavior_can_end_instance_during_OnStarted()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var behavior = new CallbackBehavior(x => x.InstanceHandle.End());

		AbilityData data = CreateAbilityData("EndInsideStart", behaviorFactory: () => behavior);
		AbilityHandle? handle = Grant(entity, data);
		handle.Should().NotBeNull();

		handle!.Activate(out AbilityActivationFailures failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);
		handle.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("CommitAbility", null)]
	public void Behavior_commits_cooldown_and_cost_on_start()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var behavior = new CallbackBehavior(x => x.AbilityHandle.CommitAbility());

		AbilityData data = CreateAbilityData(
			"CommitOnStart",
			behaviorFactory: () => behavior,
			instancingPolicy: AbilityInstancingPolicy.PerExecution,
			cooldownSeconds: 2f,
			costMagnitude: -5f);
		var baseBefore = entity.Attributes["TestAttributeSet.Attribute90"].BaseValue;
		AbilityHandle? handle = Grant(entity, data);
		handle.Should().NotBeNull();

		handle!.Activate(out AbilityActivationFailures failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);

		entity.Attributes["TestAttributeSet.Attribute90"].BaseValue.Should().Be(baseBefore - 5);

		// Attempt re-activate during cooldown should fail.
		handle.Activate(out failureFlags).Should().BeFalse();
		failureFlags.Should().Be(AbilityActivationFailures.Cooldown);

		// Advance time until cooldown expires.
		entity.EffectsManager.UpdateEffects(2f);
		handle.Activate(out failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);
	}

	[Fact]
	[Trait("ExceptionStart", null)]
	public void Exception_in_OnStarted_cancels_instance_and_does_not_crash()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var behavior = new ExceptionBehaviorOnStart();

		AbilityData data = CreateAbilityData("ThrowStart", behaviorFactory: () => behavior);
		AbilityHandle? handle = Grant(entity, data);
		handle.Should().NotBeNull();

		// Activation returns success (instance created then canceled).
		handle!.Activate(out AbilityActivationFailures failureFlags).Should().BeTrue();
		handle.IsActive.Should().BeFalse();
		behavior.StartAttempts.Should().Be(1);
	}

	[Fact]
	[Trait("ExceptionEnd", null)]
	public void Exception_in_OnEnded_does_not_prevent_deactivation()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var behavior = new ExceptionBehaviorOnEnd();

		AbilityData data = CreateAbilityData("ThrowEnd", behaviorFactory: () => behavior);
		AbilityHandle? handle = Grant(entity, data);
		handle.Should().NotBeNull();

		handle!.Activate(out _).Should().BeTrue();
		handle.IsActive.Should().BeTrue();

		handle.Cancel();
		behavior.EndAttempts.Should().Be(1);
		handle.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("NullFactoryReturn", null)]
	public void Null_behavior_instance_is_ignored()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		AbilityData data = CreateAbilityData("NullBehavior", behaviorFactory: () =>
		{
#pragma warning disable CS8603 // Possible null reference return.
			return null;
#pragma warning restore CS8603 // Possible null reference return.
		});

		AbilityHandle? handle = Grant(entity, data);
		handle.Should().NotBeNull();

		handle!.Activate(out _).Should().BeTrue();
		handle.IsActive.Should().BeTrue();

		handle.Cancel();
		handle.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("Ability Ended Event", null)]
	public void OnAbilityEnded_fires_when_ability_instance_ends()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var behavior = new TrackingBehavior();
		AbilityData data = CreateAbilityData("Tracked", behaviorFactory: () => behavior);
		AbilityHandle? handle = Grant(entity, data);
		handle.Should().NotBeNull();

		AbilityEndedData? capturedData = null;
		entity.Abilities.OnAbilityEnded += x => { capturedData = x; };

		handle!.Activate(out AbilityActivationFailures failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);
		behavior.StartCount.Should().Be(1);
		behavior.EndCount.Should().Be(0);

		behavior.End();
		behavior.StartCount.Should().Be(1);
		behavior.EndCount.Should().Be(1);

		// Verify event was fired
		capturedData.Should().NotBeNull();
		capturedData!.Value.Ability.Should().Be(handle);
		capturedData.Value.WasCanceled.Should().BeFalse();
	}

	[Fact]
	[Trait("Ability Ended Event", null)]
	public void Ability_is_granted_and_activated_once()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var behavior = new TrackingBehavior();

		AbilityData data = CreateAbilityData("Tracked", behaviorFactory: () => behavior);
		entity.Abilities.GrantAbilityAndActivateOnce(
			data,
			1,
			LevelComparison.None,
			out AbilityActivationFailures failureFlags);

		failureFlags.Should().Be(AbilityActivationFailures.None);

		entity.Abilities.GrantedAbilities.Should().ContainSingle();
		behavior.StartCount.Should().Be(1);
		behavior.EndCount.Should().Be(0);

		behavior.End();
		behavior.StartCount.Should().Be(1);
		behavior.EndCount.Should().Be(1);
		entity.Abilities.GrantedAbilities.Should().BeEmpty();
	}

	[Fact]
	[Trait("CustomContext", null)]
	public void Generic_activate_creates_typed_context_with_payload()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		AbilityBehaviorContext? capturedContext = null;

		AbilityData data = CreateAbilityData(
			"TypedContextAbility",
			behaviorFactory: () => new CallbackBehavior(ctx => capturedContext = ctx));

		AbilityHandle? handle = Grant(entity, data);
		handle.Should().NotBeNull();

		var activationData = new TestActivationData("TestValue", 42);
		handle!.Activate(activationData, out AbilityActivationFailures failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);

		capturedContext.Should().NotBeNull();
		capturedContext.Should().BeOfType<AbilityBehaviorContext<TestActivationData>>();

		var typedContext = (AbilityBehaviorContext<TestActivationData>)capturedContext!;
		typedContext.Payload.StringValue.Should().Be("TestValue");
		typedContext.Payload.IntValue.Should().Be(42);
	}

	[Fact]
	[Trait("CustomContext", null)]
	public void Non_generic_activate_creates_base_context()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		AbilityBehaviorContext? capturedContext = null;

		AbilityData data = CreateAbilityData(
			"NoContextFactoryAbility",
			behaviorFactory: () => new CallbackBehavior(ctx => capturedContext = ctx));

		AbilityHandle? handle = Grant(entity, data);
		handle.Should().NotBeNull();

		handle!.Activate(out AbilityActivationFailures failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);

		capturedContext.Should().NotBeNull();
		capturedContext.Should().BeOfType<AbilityBehaviorContext>();
	}

	[Fact]
	[Trait("CustomContext", null)]
	public void Value_type_payload_is_preserved_in_context()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		AbilityBehaviorContext? capturedContext = null;

		AbilityData data = CreateAbilityData(
			"ValueTypeContextAbility",
			behaviorFactory: () => new CallbackBehavior(ctx => capturedContext = ctx));

		AbilityHandle? handle = Grant(entity, data);
		handle.Should().NotBeNull();

		var activationData = new ValueTypeActivationData(1.5f, 2.5f, 3.5f);
		handle!.Activate(activationData, out AbilityActivationFailures failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);

		capturedContext.Should().NotBeNull();
		capturedContext.Should().BeOfType<AbilityBehaviorContext<ValueTypeActivationData>>();

		var typedContext = (AbilityBehaviorContext<ValueTypeActivationData>)capturedContext!;
		typedContext.Payload.X.Should().Be(1.5f);
		typedContext.Payload.Y.Should().Be(2.5f);
		typedContext.Payload.Z.Should().Be(3.5f);
	}

	[Fact]
	[Trait("CustomContext", null)]
	public void Context_data_is_passed_through_for_each_instance_in_PerExecution()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var capturedContexts = new List<AbilityBehaviorContext>();

		AbilityData data = CreateAbilityData(
			"PerExecutionContextAbility",
			behaviorFactory: () => new CallbackBehavior(ctx => capturedContexts.Add(ctx)),
			instancingPolicy: AbilityInstancingPolicy.PerExecution);

		AbilityHandle? handle = Grant(entity, data);
		handle.Should().NotBeNull();

		var activationData1 = new TestActivationData("First", 1);
		var activationData2 = new TestActivationData("Second", 2);

		handle!.Activate(activationData1, out _).Should().BeTrue();
		handle.Activate(activationData2, out _).Should().BeTrue();

		capturedContexts.Should().HaveCount(2);

		capturedContexts[0].Should().BeOfType<AbilityBehaviorContext<TestActivationData>>();
		capturedContexts[1].Should().BeOfType<AbilityBehaviorContext<TestActivationData>>();

		var context1 = (AbilityBehaviorContext<TestActivationData>)capturedContexts[0];
		var context2 = (AbilityBehaviorContext<TestActivationData>)capturedContexts[1];

		context1.Payload.StringValue.Should().Be("First");
		context1.Payload.IntValue.Should().Be(1);
		context2.Payload.StringValue.Should().Be("Second");
		context2.Payload.IntValue.Should().Be(2);
	}

	[Fact]
	[Trait("CustomContext", null)]
	public void PerEntity_retrigger_passes_new_context_data()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var capturedContexts = new List<AbilityBehaviorContext>();

		AbilityData data = CreateAbilityData(
			"RetriggerContextAbility",
			behaviorFactory: () => new CallbackBehavior(ctx => capturedContexts.Add(ctx)),
			retriggerInstancedAbility: true);

		AbilityHandle? handle = Grant(entity, data);
		handle.Should().NotBeNull();

		var activationData1 = new TestActivationData("First", 1);
		var activationData2 = new TestActivationData("Second", 2);

		handle!.Activate(activationData1, out _).Should().BeTrue();
		handle.Activate(activationData2, out _).Should().BeTrue();

		// Both activations should have succeeded with their own context data
		capturedContexts.Should().HaveCount(2);

		var context1 = (AbilityBehaviorContext<TestActivationData>)capturedContexts[0];
		var context2 = (AbilityBehaviorContext<TestActivationData>)capturedContexts[1];

		context1.Payload.StringValue.Should().Be("First");
		context2.Payload.StringValue.Should().Be("Second");
	}

	[Fact]
	[Trait("EventTrigger", null)]
	public void Event_triggered_ability_activates_on_event()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var behavior = new TrackingBehavior();
		var eventTag = Tag.RequestTag(_tagsManager, "color.red");

		AbilityData data = CreateAbilityData(
			"EventTriggered",
			behaviorFactory: () => behavior,
			abilityTriggerData: AbilityTriggerData.ForEvent(eventTag));

		AbilityHandle? handle = Grant(entity, data);
		handle.Should().NotBeNull();

		behavior.StartCount.Should().Be(0);

		// Raise the event
		entity.Events.Raise(new EventData
		{
			EventTags = eventTag.GetSingleTagContainer()!,
		});

		behavior.StartCount.Should().Be(1);
		handle!.IsActive.Should().BeTrue();

		handle.Cancel();
		behavior.EndCount.Should().Be(1);
	}

	[Fact]
	[Trait("EventTrigger", null)]
	public void Event_triggered_ability_with_typed_payload_receives_payload()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		TestEventPayload? capturedPayload = null;
		AbilityBehaviorContext? capturedContext = null;

		var eventTag = Tag.RequestTag(_tagsManager, "color.green");

		AbilityData data = CreateAbilityData(
			"TypedEventTriggered",
			behaviorFactory: () => new TypedPayloadBehavior<TestEventPayload>((ctx, payload) =>
			{
				capturedContext = ctx;
				capturedPayload = payload;
			}),
			abilityTriggerData: AbilityTriggerData.ForEvent<TestEventPayload>(eventTag));

		AbilityHandle? handle = Grant(entity, data);
		handle.Should().NotBeNull();

		var expectedPayload = new TestEventPayload("Damage received", 42, true);

		// Raise the typed event
		entity.Events.Raise(new EventData<TestEventPayload>
		{
			EventTags = eventTag.GetSingleTagContainer()!,
			Payload = expectedPayload,
		});

		capturedContext.Should().NotBeNull();
		capturedPayload.Should().NotBeNull();
		capturedPayload!.Message.Should().Be("Damage received");
		capturedPayload.Value.Should().Be(42);
		capturedPayload.IsActive.Should().BeTrue();
	}

	[Fact]
	[Trait("EventTrigger", null)]
	public void Event_triggered_ability_with_value_type_payload_receives_payload()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		ValueTypeActivationData capturedPayload = default;

		var eventTag = Tag.RequestTag(_tagsManager, "color.blue");

		AbilityData data = CreateAbilityData(
			"ValueTypeEventTriggered",
			behaviorFactory: () => new TypedPayloadBehavior<ValueTypeActivationData>((_, payload) =>
			{
				capturedPayload = payload;
			}),
			abilityTriggerData: AbilityTriggerData.ForEvent<ValueTypeActivationData>(eventTag));

		AbilityHandle? handle = Grant(entity, data);
		handle.Should().NotBeNull();

		var expectedPayload = new ValueTypeActivationData(1.5f, 2.5f, 3.5f);

		// Raise the typed event
		entity.Events.Raise(new EventData<ValueTypeActivationData>
		{
			EventTags = eventTag.GetSingleTagContainer()!,
			Payload = expectedPayload,
		});

		capturedPayload.X.Should().Be(1.5f);
		capturedPayload.Y.Should().Be(2.5f);
		capturedPayload.Z.Should().Be(3.5f);
	}

	[Fact]
	[Trait("EventTrigger", null)]
	public void Event_triggered_ability_respects_cooldown()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var behavior = new CallbackBehavior(x => x.AbilityHandle.CommitAbility());
		var eventTag = Tag.RequestTag(_tagsManager, "color.dark.red");

		AbilityData data = CreateAbilityData(
			"EventWithCooldown",
			behaviorFactory: () => behavior,
			cooldownSeconds: 5f,
			abilityTriggerData: AbilityTriggerData.ForEvent(eventTag));

		AbilityHandle? handle = Grant(entity, data);
		handle.Should().NotBeNull();

		// First event should activate
		entity.Events.Raise(new EventData { EventTags = eventTag.GetSingleTagContainer()! });
		handle!.IsActive.Should().BeTrue();
		handle.Cancel();

		// Second event should fail due to cooldown
		entity.Events.Raise(new EventData { EventTags = eventTag.GetSingleTagContainer()! });
		handle.IsActive.Should().BeFalse();

		// After cooldown expires, should work again
		entity.EffectsManager.UpdateEffects(5f);
		entity.Events.Raise(new EventData { EventTags = eventTag.GetSingleTagContainer()! });
		handle.IsActive.Should().BeTrue();
	}

	[Fact]
	[Trait("EventTrigger", null)]
	public void Event_triggered_ability_multiple_events_with_PerExecution()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var activationCount = 0;
		var eventTag = Tag.RequestTag(_tagsManager, "color.dark.green");

		AbilityData data = CreateAbilityData(
			"MultiEventPerExecution",
			behaviorFactory: () => new CallbackBehavior(_ => activationCount++),
			instancingPolicy: AbilityInstancingPolicy.PerExecution,
			abilityTriggerData: AbilityTriggerData.ForEvent(eventTag));

		AbilityHandle? handle = Grant(entity, data);
		handle.Should().NotBeNull();

		// Raise multiple events
		entity.Events.Raise(new EventData { EventTags = eventTag.GetSingleTagContainer()! });
		entity.Events.Raise(new EventData { EventTags = eventTag.GetSingleTagContainer()! });
		entity.Events.Raise(new EventData { EventTags = eventTag.GetSingleTagContainer()! });

		activationCount.Should().Be(3);
	}

	private static AbilityHandle? Grant(
		TestEntity target,
		AbilityData data,
		IForgeEntity? sourceEntity = null)
	{
		var grantConfig = new GrantAbilityConfig(
			data,
			new ScalableInt(1),
			AbilityDeactivationPolicy.CancelImmediately,
			AbilityDeactivationPolicy.CancelImmediately,
			LevelComparison.Higher);

		Effect grantEffect = CreateGrantEffect("Grant", grantConfig, sourceEntity);
		_ = target.EffectsManager.ApplyEffect(grantEffect);
		target.Abilities.TryGetAbility(data, out AbilityHandle? handle, sourceEntity);
		return handle;
	}

	private static Effect CreateGrantEffect(
		string name,
		GrantAbilityConfig config,
		IForgeEntity? sourceEntity)
	{
		var data = new EffectData(
			name,
			new DurationData(DurationType.Infinite),
			effectComponents: [new GrantAbilityEffectComponent([config])]);

		return new Effect(data, new EffectOwnership(null, sourceEntity));
	}

	private AbilityData CreateAbilityData(
		string name,
		IAbilityBehavior? behavior = null,
		Func<IAbilityBehavior>? behaviorFactory = null,
		AbilityInstancingPolicy instancingPolicy = AbilityInstancingPolicy.PerEntity,
		bool retriggerInstancedAbility = false,
		float cooldownSeconds = 3f,
		float costMagnitude = -1f,
		TagContainer? abilityTags = null,
		TagContainer? blockAbilitiesWithTag = null,
		TagContainer? activationOwnedTags = null,
		AbilityTriggerData? abilityTriggerData = null)
	{
		EffectData[] cooldownEffectData = [new EffectData(
			$"{name} Cooldown",
			new DurationData(
				DurationType.HasDuration,
				new ModifierMagnitude(
					MagnitudeCalculationType.ScalableFloat,
					scalableFloatMagnitude: new ScalableFloat(cooldownSeconds))),
			effectComponents:
			[
				new ModifierTagsEffectComponent(
					new TagContainer(_tagsManager, TestUtils.StringToTag(_tagsManager, ["simple.tag"])))
			])];

		var costEffectData = new EffectData(
			$"{name} Cost",
			new DurationData(DurationType.Instant),
			[
				new Modifier(
					"TestAttributeSet.Attribute90",
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						scalableFloatMagnitude: new ScalableFloat(costMagnitude)))
			]);

		return new AbilityData(
			name,
			costEffectData,
			cooldownEffectData,
			abilityTags: abilityTags,
			instancingPolicy: instancingPolicy,
			retriggerInstancedAbility: retriggerInstancedAbility,
			abilityTriggerData: abilityTriggerData,
			blockAbilitiesWithTag: blockAbilitiesWithTag,
			activationOwnedTags: activationOwnedTags,
			behaviorFactory: behaviorFactory ?? (() => behavior!));
	}

	private sealed record TestActivationData(string StringValue, int IntValue);

	private readonly record struct ValueTypeActivationData(float X, float Y, float Z);

	private sealed record TestEventPayload(string Message, int Value, bool IsActive);

	private sealed class TrackingBehavior(Action? onStartExtra = null) : IAbilityBehavior
	{
		private readonly Action? _onStartExtra = onStartExtra;
		private AbilityBehaviorContext? _context;

		public int StartCount { get; private set; }

		public int EndCount { get; private set; }

		public void OnStarted(AbilityBehaviorContext context)
		{
			_context = context;
			StartCount++;
			_onStartExtra?.Invoke();
		}

		public void OnEnded(AbilityBehaviorContext context)
		{
			EndCount++;
		}

		public void End()
		{
			_context?.InstanceHandle.End();
		}
	}

	private sealed class CallbackBehavior(Action<AbilityBehaviorContext> callback) : IAbilityBehavior
	{
		public void OnStarted(AbilityBehaviorContext context)
		{
			callback(context);
		}

		public void OnEnded(AbilityBehaviorContext context)
		{
			// No-op
		}
	}

	private sealed class TypedPayloadBehavior<TPayload>(
		Action<AbilityBehaviorContext, TPayload> callback) : IAbilityBehavior<TPayload>
	{
		public void OnStarted(AbilityBehaviorContext context, TPayload payload)
		{
			callback(context, payload);
			context.InstanceHandle.End();
		}

		public void OnEnded(AbilityBehaviorContext context)
		{
			// No-op
		}
	}

	private sealed class ExceptionBehaviorOnStart : IAbilityBehavior
	{
		public int StartAttempts { get; private set; }

		public void OnStarted(AbilityBehaviorContext context)
		{
			StartAttempts++;
			throw new InvalidOperationException("Start failure");
		}

		public void OnEnded(AbilityBehaviorContext context)
		{
			// No-op
		}
	}

	private sealed class ExceptionBehaviorOnEnd : IAbilityBehavior
	{
		public int EndAttempts { get; private set; }

		public void OnStarted(AbilityBehaviorContext context)
		{
			// No-op
		}

		public void OnEnded(AbilityBehaviorContext context)
		{
			EndAttempts++;
			throw new InvalidOperationException("End failure");
		}
	}
}
