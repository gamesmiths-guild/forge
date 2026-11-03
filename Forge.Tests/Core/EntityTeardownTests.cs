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

namespace Gamesmiths.Forge.Tests.Core;

/// <summary>
/// A torn-down entity ends for good: its abilities are cancelled and cleared, its effects end the way a removal ends
/// them, as destroyed, its input lets go, and nothing applies to it from then on.
/// </summary>
/// <param name="fixture">The fixture providing the <see cref="TagsManager"/> and <see cref="CuesManager"/>.</param>
public class EntityTeardownTests(TagsAndCuesFixture fixture) : IClassFixture<TagsAndCuesFixture>
{
	private const double Step = 1.0 / 64.0;

	private readonly TagsManager _tagsManager = fixture.TagsManager;
	private readonly CuesManager _cuesManager = fixture.CuesManager;

	[Fact]
	[Trait("Teardown", null)]
	public void Teardown_cancels_running_abilities_and_their_triggers_stop_listening()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var trigger = Tag.RequestTag(_tagsManager, "color.red");
		AbilityHandle handle = entity.Abilities.GrantAbilityPermanently(
			new AbilityData("Triggered", abilityTriggerData: AbilityTriggerData.ForEvent(trigger)),
			1,
			LevelComparison.None,
			entity);
		var ended = new List<AbilityEndedData>();
		int activations = 0;
		entity.Abilities.OnAbilityEnded += ended.Add;
		entity.Abilities.OnAbilityActivated += _ => activations++;

		Raise(entity, trigger);
		handle.IsActive.Should().BeTrue();

		EntitySimulation.Teardown(entity);

		ended.Should().ContainSingle().Which.WasCanceled.Should().BeTrue();
		handle.IsValid.Should().BeFalse();
		entity.Abilities.GrantedAbilities.Should().BeEmpty();

		Raise(entity, trigger);

		activations.Should().Be(1, "the trigger stopped listening");
	}

	[Fact]
	[Trait("Teardown", null)]
	public void Teardown_ends_every_effect_as_destroyed_and_takes_its_modifiers_back()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		int baseValue = entity.PlayerAttributeSet.Attribute1.CurrentValue;
		var removals = new List<EffectRemovalReason>();
		entity.EffectsManager.OnActiveEffectRemoved += (_, reason) => removals.Add(reason);

		Apply(entity, entity, Buff("Haste", DurationType.HasDuration));
		Apply(entity, entity, Buff("Aura", DurationType.Infinite));
		entity.PlayerAttributeSet.Attribute1.CurrentValue.Should().Be(baseValue + 2);

		EntitySimulation.Teardown(entity);

		entity.EffectsManager.GetActiveEffects().Should().BeEmpty();
		removals.Should().Equal(EffectRemovalReason.Destroyed, EffectRemovalReason.Destroyed);
		entity.PlayerAttributeSet.Attribute1.CurrentValue.Should().Be(baseValue);
	}

	[Fact]
	[Trait("Teardown", null)]
	public void Nothing_applies_to_a_torn_down_entity_not_even_what_its_effects_apply_as_they_end()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		EffectData aftermath = Buff("Aftermath", DurationType.Infinite);
		var curse = new EffectData(
			"Curse",
			new DurationData(DurationType.Infinite),
			effectComponents:
			[
				new AdditionalEffectsEffectComponent(onCompletePrematurely: [new ConditionalEffect(aftermath)])
			]);

		Apply(entity, entity, curse);

		EntitySimulation.Teardown(entity);

		entity.EffectsManager.GetActiveEffects().Should().BeEmpty("the curse's aftermath would outlive the teardown");
		Apply(entity, entity, aftermath).Should().BeNull();
		entity.EffectsManager.GetActiveEffects().Should().BeEmpty();
	}

	[Fact]
	[Trait("Teardown", null)]
	public void Effects_a_torn_down_entity_applied_to_others_keep_running()
	{
		var caster = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);

		Apply(target, caster, Buff("Blessing", DurationType.Infinite));

		EntitySimulation.Teardown(caster);

		target.EffectsManager.GetActiveEffects().Should().ContainSingle();
	}

	[Fact]
	[Trait("Teardown", null)]
	public void Teardown_lets_go_of_every_action_of_the_input()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager)
		{
			Input = new EntityInput(new InputActionSet(["jump"])),
		};
		entity.Input.Submit(new InputFrame { Held = 1, Pressed = 1 });
		EntitySimulation.Tick(entity, 1, Step);
		entity.Input.Submit(new InputFrame { Held = 1 });

		entity.Input.IsHeld("jump").Should().BeTrue();

		EntitySimulation.Teardown(entity);

		entity.Input.IsHeld("jump").Should().BeFalse();
		EntitySimulation.Tick(entity, 2, Step);
		entity.Input.IsHeld("jump").Should().BeFalse("the frame waiting for the next tick went too");
	}

	[Fact]
	[Trait("Teardown", null)]
	public void An_entity_cannot_be_torn_down_while_its_effects_change()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		InvalidOperationException? thrown = null;

		entity.EffectsManager.OnEffectApplied += _ =>
		{
			try
			{
				EntitySimulation.Teardown(entity);
			}
			catch (InvalidOperationException exception)
			{
				thrown = exception;
			}
		};

		Apply(entity, entity, Buff("Haste", DurationType.HasDuration));

		thrown.Should().NotBeNull();
		entity.EffectsManager.GetActiveEffects().Should().ContainSingle();
	}

	private static ActiveEffectHandle? Apply(TestEntity target, TestEntity source, EffectData effectData)
	{
		return target.EffectsManager.ApplyEffect(new Effect(effectData, new EffectOwnership(source, source)));
	}

	private static EffectData Buff(string name, DurationType durationType)
	{
		ModifierMagnitude? duration = durationType == DurationType.HasDuration
			? new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(10f))
			: null;

		return new EffectData(
			name,
			new DurationData(durationType, duration),
			[
				new Modifier(
					"TestAttributeSet.Attribute1",
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(1)))
			]);
	}

	private void Raise(TestEntity entity, Tag tag)
	{
		var eventData = new EventData
		{
			EventTags = new TagContainer(_tagsManager, [tag]),
			Source = entity,
			Target = entity,
		};

		entity.Events.Raise(in eventData);
	}
}
