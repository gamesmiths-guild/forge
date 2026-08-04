// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Components;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Abilities;

public class EntityAbilitiesEventsTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Lifecycle", null)]
	public void Granting_an_ability_reports_granted_once()
	{
		TestEntity entity = CreateEntity();
		var log = new EventLog(entity.Abilities);

		AbilityHandle handle = entity.Abilities.GrantAbilityPermanently(
			CreateAbilityData("Fireball"), 1, LevelComparison.Higher, null);

		log.Entries.Should().Equal("Granted");
		log.Granted.Should().ContainSingle().Which.Should().BeSameAs(handle);
	}

	[Fact]
	[Trait("Lifecycle", null)]
	public void A_repeat_grant_that_overrides_the_level_reports_changed_not_granted()
	{
		TestEntity entity = CreateEntity();
		AbilityData abilityData = CreateAbilityData("Fireball");

		AbilityHandle handle = entity.Abilities.GrantAbilityPermanently(abilityData, 1, LevelComparison.Higher, null);

		var log = new EventLog(entity.Abilities);
		entity.Abilities.GrantAbilityPermanently(abilityData, 3, LevelComparison.Higher, null);

		log.Entries.Should().Equal("Changed");
		log.Changed.Should().ContainSingle().Which.Should().BeSameAs(handle);
		handle.Level.Should().Be(3);
	}

	[Fact]
	[Trait("Lifecycle", null)]
	public void A_repeat_grant_that_changes_nothing_reports_nothing()
	{
		TestEntity entity = CreateEntity();
		AbilityData abilityData = CreateAbilityData("Fireball");

		entity.Abilities.GrantAbilityPermanently(abilityData, 3, LevelComparison.Higher, null);

		var log = new EventLog(entity.Abilities);

		// The same level under a Higher-only override policy leaves both observable values where they were.
		entity.Abilities.GrantAbilityPermanently(abilityData, 3, LevelComparison.Higher, null);

		log.Entries.Should().BeEmpty();
	}

	[Fact]
	[Trait("Lifecycle", null)]
	public void An_ability_losing_its_last_grant_source_reports_removed()
	{
		TestEntity entity = CreateEntity();
		AbilityData abilityData = CreateAbilityData("Fireball");

		ActiveEffectHandle? effectHandle = GrantThroughEffect(entity, abilityData);
		entity.Abilities.TryGetAbility(abilityData, out AbilityHandle? handle).Should().BeTrue();

		var log = new EventLog(entity.Abilities);
		entity.EffectsManager.RemoveEffect(effectHandle!);

		log.Removed.Should().ContainSingle().Which.Should().BeSameAs(handle);
		entity.Abilities.GrantedAbilities.Should().BeEmpty();
	}

	[Fact]
	[Trait("Lifecycle", null)]
	public void The_removed_handle_is_readable_inside_the_handler_and_invalid_afterwards()
	{
		TestEntity entity = CreateEntity();
		AbilityData abilityData = CreateAbilityData("Fireball");

		ActiveEffectHandle? effectHandle = GrantThroughEffect(entity, abilityData);
		entity.Abilities.TryGetAbility(abilityData, out AbilityHandle? handle).Should().BeTrue();

		bool validInsideHandler = false;
		int levelInsideHandler = 0;

		entity.Abilities.OnAbilityRemoved += removed =>
		{
			validInsideHandler = removed.IsValid;
			levelInsideHandler = removed.Level;
		};

		entity.EffectsManager.RemoveEffect(effectHandle!);

		validInsideHandler.Should().BeTrue();
		levelInsideHandler.Should().Be(1);
		handle!.IsValid.Should().BeFalse();
	}

	[Fact]
	[Trait("Lifecycle", null)]
	public void Losing_one_of_several_grant_sources_reports_no_removal()
	{
		TestEntity entity = CreateEntity();
		AbilityData abilityData = CreateAbilityData("Fireball");

		ActiveEffectHandle? first = GrantThroughEffect(entity, abilityData);
		GrantThroughEffect(entity, abilityData);

		var log = new EventLog(entity.Abilities);
		entity.EffectsManager.RemoveEffect(first!);

		log.Removed.Should().BeEmpty();
		entity.Abilities.GrantedAbilities.Should().ContainSingle();
	}

	[Fact]
	[Trait("Lifecycle", null)]
	public void Inhibiting_and_uninhibiting_an_ability_reports_changed()
	{
		TestEntity entity = CreateEntity();
		AbilityData abilityData = CreateAbilityData("Fireball");

		// The granting effect is inhibited while the target carries color.red, taking the ability with it.
		ActiveEffectHandle? grantHandle = GrantThroughEffect(
			entity,
			abilityData,
			new TargetTagRequirementsEffectComponent(
				ongoingTagRequirements: new TagRequirements(IgnoreTags: MakeContainer("color.red"))));

		entity.Abilities.TryGetAbility(abilityData, out AbilityHandle? handle).Should().BeTrue();
		handle!.IsInhibited.Should().BeFalse();

		var log = new EventLog(entity.Abilities);

		ActiveEffectHandle? tagHandle = ApplyTag(entity, "color.red");
		handle.IsInhibited.Should().BeTrue();

		entity.EffectsManager.RemoveEffect(tagHandle!);
		handle.IsInhibited.Should().BeFalse();

		log.Changed.Should().HaveCount(2);
		log.Removed.Should().BeEmpty();
		grantHandle.Should().NotBeNull();
	}

	[Fact]
	[Trait("Activation", null)]
	public void Activating_an_ability_reports_activated_then_ended()
	{
		TestEntity entity = CreateEntity();
		AbilityHandle handle = entity.Abilities.GrantAbilityPermanently(
			CreateAbilityData("Fireball"), 1, LevelComparison.Higher, null);

		var log = new EventLog(entity.Abilities);

		handle.Activate(out AbilityActivationFailures failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);

		log.Entries.Should().Equal("Activated");

		handle.Cancel();

		log.Entries.Should().Equal("Activated", "Ended");
		log.Activated.Should().ContainSingle().Which.Should().BeSameAs(handle);
	}

	// Regression: a behavior that finishes inside OnStarted ends the ability before the activation call returns. The
	// activation notification is raised before the behavior starts precisely so it cannot arrive second.
	[Fact]
	[Trait("Activation", null)]
	public void A_behavior_that_finishes_synchronously_still_reports_activated_before_ended()
	{
		TestEntity entity = CreateEntity();
		AbilityHandle handle = entity.Abilities.GrantAbilityPermanently(
			CreateAbilityData("Fireball", behaviorFactory: () => new InstantBehavior()),
			1,
			LevelComparison.Higher,
			null);

		var log = new EventLog(entity.Abilities);

		handle.Activate(out _).Should().BeTrue();

		log.Entries.Should().Equal("Activated", "Ended");
		handle.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("Activation", null)]
	public void A_second_concurrent_instance_reports_nothing_until_the_last_one_ends()
	{
		TestEntity entity = CreateEntity();
		AbilityHandle handle = entity.Abilities.GrantAbilityPermanently(
			CreateAbilityData("Fireball", instancingPolicy: AbilityInstancingPolicy.PerExecution),
			1,
			LevelComparison.Higher,
			null);

		var log = new EventLog(entity.Abilities);

		handle.Activate(out _).Should().BeTrue();
		handle.Activate(out _).Should().BeTrue();

		// Both events track the ability, not its instances, so the second concurrent instance is silent.
		log.Entries.Should().Equal("Activated");

		handle.Cancel();

		log.Entries.Should().Equal("Activated", "Ended");
	}

	[Fact]
	[Trait("Activation", null)]
	public void A_refused_activation_reports_the_failure_flags()
	{
		TestEntity entity = CreateEntity();
		AbilityData abilityData = CreateAbilityData(
			"Fireball",
			activationRequiredTags: MakeContainer("color.red"));

		AbilityHandle handle = entity.Abilities.GrantAbilityPermanently(abilityData, 1, LevelComparison.Higher, null);

		var log = new EventLog(entity.Abilities);

		handle.Activate(out AbilityActivationFailures failureFlags).Should().BeFalse();

		log.Entries.Should().Equal("ActivationFailed");
		log.Failures.Should().ContainSingle();
		log.Failures[0].Handle.Should().BeSameAs(handle);
		log.Failures[0].Flags.Should().Be(failureFlags);
		failureFlags.Should().HaveFlag(AbilityActivationFailures.OwnerTagRequirements);
	}

	[Fact]
	[Trait("Activation", null)]
	public void A_successful_activation_reports_no_failure()
	{
		TestEntity entity = CreateEntity();
		AbilityHandle handle = entity.Abilities.GrantAbilityPermanently(
			CreateAbilityData("Fireball"), 1, LevelComparison.Higher, null);

		var log = new EventLog(entity.Abilities);
		handle.Activate(out _).Should().BeTrue();

		log.Failures.Should().BeEmpty();
	}

	[Fact]
	[Trait("Scope", null)]
	public void The_events_only_report_what_happens_to_their_own_owner()
	{
		TestEntity watched = CreateEntity();
		TestEntity other = CreateEntity();

		var log = new EventLog(watched.Abilities);

		other.Abilities.GrantAbilityPermanently(CreateAbilityData("Fireball"), 1, LevelComparison.Higher, null);

		log.Entries.Should().BeEmpty();
	}

	private static AbilityData CreateAbilityData(
		string name,
		AbilityInstancingPolicy instancingPolicy = AbilityInstancingPolicy.PerEntity,
		TagContainer? activationRequiredTags = null,
		Func<IAbilityBehavior>? behaviorFactory = null)
	{
		return new AbilityData(
			name,
			instancingPolicy: instancingPolicy,
			activationRequiredTags: activationRequiredTags,
			behaviorFactory: behaviorFactory);
	}

	private static ActiveEffectHandle? GrantThroughEffect(
		TestEntity entity,
		AbilityData abilityData,
		IEffectComponent? extraComponent = null)
	{
		List<IEffectComponent> components =
		[
			new GrantAbilityEffectComponent(
			[
				new GrantAbilityConfig(
					abilityData,
					new ScalableInt(1),
					AbilityDeactivationPolicy.CancelImmediately,
					AbilityDeactivationPolicy.CancelImmediately)
			])
		];

		if (extraComponent is not null)
		{
			components.Add(extraComponent);
		}

		var effectData = new EffectData(
			"Grant Ability Effect",
			new DurationData(DurationType.Infinite),
			effectComponents: [.. components]);

		return entity.EffectsManager.ApplyEffect(new Effect(effectData, new EffectOwnership(entity, entity)));
	}

	private TagContainer MakeContainer(params string[] tagKeys)
	{
		return new TagContainer(_tagsManager, TestUtils.StringToTag(_tagsManager, tagKeys));
	}

	private ActiveEffectHandle? ApplyTag(TestEntity entity, string tagKey)
	{
		var effectData = new EffectData(
			"Tag Effect",
			new DurationData(DurationType.Infinite),
			effectComponents: [new ModifierTagsEffectComponent(MakeContainer(tagKey))]);

		return entity.EffectsManager.ApplyEffect(new Effect(effectData, new EffectOwnership(entity, entity)));
	}

	private TestEntity CreateEntity()
	{
		return new TestEntity(_tagsManager, _cuesManager);
	}

	/// <summary>
	/// Subscribes to every ability event and records them in the order they arrive, so tests can assert on both what
	/// was raised and the sequence.
	/// </summary>
	private sealed class EventLog
	{
		public List<string> Entries { get; } = [];

		public List<AbilityHandle> Granted { get; } = [];

		public List<AbilityHandle> Changed { get; } = [];

		public List<AbilityHandle> Removed { get; } = [];

		public List<AbilityHandle> Activated { get; } = [];

		public List<AbilityEndedData> Ended { get; } = [];

		public List<(AbilityHandle Handle, AbilityActivationFailures Flags)> Failures { get; } = [];

		public EventLog(EntityAbilities abilities)
		{
			abilities.OnAbilityGranted += handle =>
			{
				Entries.Add("Granted");
				Granted.Add(handle);
			};

			abilities.OnAbilityChanged += handle =>
			{
				Entries.Add("Changed");
				Changed.Add(handle);
			};

			abilities.OnAbilityRemoved += handle =>
			{
				Entries.Add("Removed");
				Removed.Add(handle);
			};

			abilities.OnAbilityActivated += handle =>
			{
				Entries.Add("Activated");
				Activated.Add(handle);
			};

			abilities.OnAbilityEnded += endedData =>
			{
				Entries.Add("Ended");
				Ended.Add(endedData);
			};

			abilities.OnAbilityActivationFailed += (handle, flags) =>
			{
				Entries.Add("ActivationFailed");
				Failures.Add((handle, flags));
			};
		}
	}

	/// <summary>
	/// Ends its own instance from inside <see cref="OnStarted"/>, so the ability is over before the activation call
	/// returns.
	/// </summary>
	private sealed class InstantBehavior : IAbilityBehavior
	{
		public void OnStarted(AbilityBehaviorContext context)
		{
			context.InstanceHandle.End();
		}

		public void OnEnded(AbilityBehaviorContext context)
		{
		}
	}
}
