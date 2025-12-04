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

		handle!.Activate(out AbilityActivationResult result).Should().BeTrue();
		result.Should().Be(AbilityActivationResult.Success);
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
		blockedHandle!.Activate(out AbilityActivationResult activationResult).Should().BeFalse();
		activationResult.Should().Be(AbilityActivationResult.FailedBlockedByTags);
		blockedHandle.IsActive.Should().BeFalse();

		// End one blocker instance; still blocked.
		behaviors[0].End();
		blockedHandle.Activate(out activationResult).Should().BeFalse();
		activationResult.Should().Be(AbilityActivationResult.FailedBlockedByTags);
		blockedHandle.IsActive.Should().BeFalse();

		// End last blocker instance; now unblocked.
		behaviors[1].End();
		blockedHandle.Activate(out activationResult).Should().BeTrue();
		activationResult.Should().Be(AbilityActivationResult.Success);
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

		handle!.Activate(out AbilityActivationResult result, target).Should().BeTrue();
		result.Should().Be(AbilityActivationResult.Success);
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

		handle!.Activate(out AbilityActivationResult result).Should().BeTrue();
		result.Should().Be(AbilityActivationResult.Success);
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

		handle!.Activate(out AbilityActivationResult result).Should().BeTrue();
		result.Should().Be(AbilityActivationResult.Success);

		entity.Attributes["TestAttributeSet.Attribute90"].BaseValue.Should().Be(baseBefore - 5);

		// Attempt re-activate during cooldown should fail.
		handle.Activate(out result).Should().BeFalse();
		result.Should().Be(AbilityActivationResult.FailedCooldown);

		// Advance time until cooldown expires.
		entity.EffectsManager.UpdateEffects(2f);
		handle.Activate(out result).Should().BeTrue();
		result.Should().Be(AbilityActivationResult.Success);
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
		handle!.Activate(out AbilityActivationResult result).Should().BeTrue();
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

		handle!.Activate(out AbilityActivationResult result).Should().BeTrue();
		result.Should().Be(AbilityActivationResult.Success);
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
		TagContainer? activationOwnedTags = null)
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
			blockAbilitiesWithTag: blockAbilitiesWithTag,
			activationOwnedTags: activationOwnedTags,
			behaviorFactory: behaviorFactory ?? (() => behavior!));
	}

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
