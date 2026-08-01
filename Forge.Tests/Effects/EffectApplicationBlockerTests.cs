// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Components;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Effects.Modifiers;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Effects;

public class EffectApplicationBlockerTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private const string TargetAttribute = "TestAttributeSet.Attribute1";

	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Registry", null)]
	public void A_registered_blocker_denies_applications()
	{
		TestEntity target = CreateEntity();
		var blocker = new RecordingBlocker(allow: false);

		target.EffectsManager.RegisterApplicationBlocker(blocker);

		target.EffectsManager.ApplyEffect(CreateEffect(target)).Should().BeNull();

		blocker.Calls.Should().Be(1);
		target.EffectsManager.GetActiveEffects().Should().BeEmpty();
	}

	[Fact]
	[Trait("Registry", null)]
	public void Unregistering_a_blocker_lets_applications_through_again()
	{
		TestEntity target = CreateEntity();
		var blocker = new RecordingBlocker(allow: false);

		target.EffectsManager.RegisterApplicationBlocker(blocker);
		target.EffectsManager.ApplyEffect(CreateEffect(target)).Should().BeNull();

		target.EffectsManager.UnregisterApplicationBlocker(blocker);

		target.EffectsManager.ApplyEffect(CreateEffect(target)).Should().NotBeNull();
		blocker.Calls.Should().Be(1);
	}

	[Fact]
	[Trait("Registry", null)]
	public void Registering_the_same_blocker_twice_consults_it_once_and_unregisters_in_one_call()
	{
		TestEntity target = CreateEntity();
		var blocker = new RecordingBlocker(allow: false);

		target.EffectsManager.RegisterApplicationBlocker(blocker);
		target.EffectsManager.RegisterApplicationBlocker(blocker);

		target.EffectsManager.ApplyEffect(CreateEffect(target)).Should().BeNull();
		blocker.Calls.Should().Be(1);

		target.EffectsManager.UnregisterApplicationBlocker(blocker);

		target.EffectsManager.ApplyEffect(CreateEffect(target)).Should().NotBeNull();
		blocker.Calls.Should().Be(1);
	}

	[Fact]
	[Trait("Registry", null)]
	public void The_first_denial_wins_and_the_remaining_blockers_are_not_consulted()
	{
		TestEntity target = CreateEntity();
		var denying = new RecordingBlocker(allow: false);
		var allowing = new RecordingBlocker(allow: true);

		target.EffectsManager.RegisterApplicationBlocker(denying);
		target.EffectsManager.RegisterApplicationBlocker(allowing);

		target.EffectsManager.ApplyEffect(CreateEffect(target)).Should().BeNull();

		denying.Calls.Should().Be(1);
		allowing.Calls.Should().Be(0);
	}

	[Fact]
	[Trait("Registry", null)]
	public void An_application_allowed_by_every_blocker_goes_through()
	{
		TestEntity target = CreateEntity();
		var first = new RecordingBlocker(allow: true);
		var second = new RecordingBlocker(allow: true);

		target.EffectsManager.RegisterApplicationBlocker(first);
		target.EffectsManager.RegisterApplicationBlocker(second);

		target.EffectsManager.ApplyEffect(CreateEffect(target)).Should().NotBeNull();

		first.Calls.Should().Be(1);
		second.Calls.Should().Be(1);
	}

	[Fact]
	[Trait("Registry", null)]
	public void Blockers_veto_instant_effects_too()
	{
		TestEntity target = CreateEntity();
		target.EffectsManager.RegisterApplicationBlocker(new RecordingBlocker(allow: false));

		target.EffectsManager.ApplyEffect(CreateEffect(target, DurationType.Instant));

		// The instant effect never executed, so the attribute keeps its initial value.
		target.PlayerAttributeSet.Attribute1.CurrentValue.Should().Be(1);
	}

	[Fact]
	[Trait("Registry", null)]
	public void A_blocker_only_vetoes_the_manager_it_is_registered_on()
	{
		TestEntity blocked = CreateEntity();
		TestEntity unblocked = CreateEntity();

		blocked.EffectsManager.RegisterApplicationBlocker(new RecordingBlocker(allow: false));

		blocked.EffectsManager.ApplyEffect(CreateEffect(blocked)).Should().BeNull();
		unblocked.EffectsManager.ApplyEffect(CreateEffect(unblocked)).Should().NotBeNull();
	}

	[Fact]
	[Trait("Event", null)]
	public void A_denied_application_raises_the_event_with_the_effect_and_the_blocker()
	{
		TestEntity target = CreateEntity();
		var blocker = new RecordingBlocker(allow: false);
		target.EffectsManager.RegisterApplicationBlocker(blocker);

		Effect? blockedEffect = null;
		IEffectApplicationBlocker? reportedBlocker = null;

		target.EffectsManager.OnEffectApplicationBlocked += (effect, source) =>
		{
			blockedEffect = effect;
			reportedBlocker = source;
		};

		Effect effect = CreateEffect(target);
		target.EffectsManager.ApplyEffect(effect);

		blockedEffect.Should().BeSameAs(effect);
		reportedBlocker.Should().BeSameAs(blocker);
	}

	[Fact]
	[Trait("Event", null)]
	public void An_allowed_application_raises_no_event()
	{
		TestEntity target = CreateEntity();
		target.EffectsManager.RegisterApplicationBlocker(new RecordingBlocker(allow: true));

		bool raised = false;
		target.EffectsManager.OnEffectApplicationBlocked += (_, _) => raised = true;

		target.EffectsManager.ApplyEffect(CreateEffect(target)).Should().NotBeNull();

		raised.Should().BeFalse();
	}

	[Fact]
	[Trait("Event", null)]
	public void An_effect_denied_by_its_own_components_never_reaches_the_blockers()
	{
		TestEntity target = CreateEntity();
		var blocker = new RecordingBlocker(allow: true);
		target.EffectsManager.RegisterApplicationBlocker(blocker);

		bool raised = false;
		target.EffectsManager.OnEffectApplicationBlocked += (_, _) => raised = true;

		// The target does not carry color.red, so the effect denies itself before any blocker is consulted.
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

		blocker.Calls.Should().Be(0);
		raised.Should().BeFalse();
	}

	[Fact]
	[Trait("Registry", null)]
	public void A_blocker_can_unregister_itself_while_being_consulted()
	{
		TestEntity target = CreateEntity();
		var blocker = new SelfUnregisteringBlocker(target.EffectsManager);

		target.EffectsManager.RegisterApplicationBlocker(blocker);

		// Blocks once, then takes itself out of the registry from inside the very call that denied the application.
		target.EffectsManager.ApplyEffect(CreateEffect(target)).Should().BeNull();
		target.EffectsManager.ApplyEffect(CreateEffect(target)).Should().NotBeNull();

		blocker.Calls.Should().Be(1);
	}

	[Fact]
	[Trait("Registry", null)]
	public void A_blocker_unregistering_itself_does_not_skip_the_next_one()
	{
		TestEntity target = CreateEntity();

		// The first blocker allows and takes itself out of the registry, shifting the denying one down into the index
		// the loop is standing on. Advancing blindly there would let the effect through unvetoed.
		var allowing = new SelfUnregisteringBlocker(target.EffectsManager, allow: true);
		var denying = new RecordingBlocker(allow: false);

		target.EffectsManager.RegisterApplicationBlocker(allowing);
		target.EffectsManager.RegisterApplicationBlocker(denying);

		target.EffectsManager.ApplyEffect(CreateEffect(target)).Should().BeNull();

		allowing.Calls.Should().Be(1);
		denying.Calls.Should().Be(1);
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

	private static Effect CreateEffect(TestEntity target, DurationType durationType = DurationType.Infinite)
	{
		var effectData = new EffectData(
			"Blockable Effect",
			new DurationData(durationType),
			CreateModifiers());

		return new Effect(effectData, new EffectOwnership(target, target));
	}

	private TagContainer MakeContainer(params string[] tagKeys)
	{
		return new TagContainer(_tagsManager, TestUtils.StringToTag(_tagsManager, tagKeys));
	}

	private TestEntity CreateEntity()
	{
		return new TestEntity(_tagsManager, _cuesManager);
	}

	private sealed class RecordingBlocker(bool allow) : IEffectApplicationBlocker
	{
		public int Calls { get; private set; }

		public bool AllowEffectApplication(in Effect effect)
		{
			Calls++;
			return allow;
		}
	}

	private sealed class SelfUnregisteringBlocker(EffectsManager effectsManager, bool allow = false)
		: IEffectApplicationBlocker
	{
		public int Calls { get; private set; }

		public bool AllowEffectApplication(in Effect effect)
		{
			Calls++;
			effectsManager.UnregisterApplicationBlocker(this);
			return allow;
		}
	}
}
