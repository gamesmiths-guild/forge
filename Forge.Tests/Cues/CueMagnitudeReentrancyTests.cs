// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Components;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Effects.Modifiers;
using Gamesmiths.Forge.Effects.Stacking;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

using static Gamesmiths.Forge.Tests.Helpers.TagsAndCuesFixture;

namespace Gamesmiths.Forge.Tests.Cues;

/// <summary>
/// An <see cref="CueMagnitudeType.AttributeValueChange"/> cue reads the deltas an effect has left pending on its
/// target, and any effect landing on that same target flushes them. A hook that runs between the effect's attribute
/// writes and its cues — an executed hook raising an event that activates an ability, a changed hook applying
/// threshold effects — can therefore land such an effect, and the cue must still report what its own effect did.
/// </summary>
/// <param name="tagsAndCuesFixture">The fixture providing the <see cref="TagsManager"/> and <see cref="CuesManager"/>.
/// </param>
public class CueMagnitudeReentrancyTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private const string CueAttribute = "TestAttributeSet.Attribute90";
	private const string SideAttribute = "TestAttributeSet.Attribute1";

	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;
	private readonly TestCue _cue = tagsAndCuesFixture.TestCueInstances[0];

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

	private static Effect CreateSideEffect(TestEntity target)
	{
		var effectData = new EffectData(
			"Side Effect",
			new DurationData(DurationType.Instant),
			[CreateModifier(SideAttribute, 1)]);

		return new Effect(effectData, new EffectOwnership(target, target));
	}

	private static Modifier CreateModifier(string attribute, float magnitude)
	{
		return new Modifier(
			attribute,
			ModifierOperation.FlatBonus,
			new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(magnitude)));
	}

	private Effect CreateInstantEffect(TestEntity target)
	{
		var effectData = new EffectData(
			"Instant Effect",
			new DurationData(DurationType.Instant),
			[CreateModifier(CueAttribute, -10)],
			effectComponents:
			[
				new RaiseEventEffectComponent(EventTag().GetSingleTagContainer()!, EffectEventTrigger.Executed)
			],
			cues: [CreateCue()]);

		return new Effect(effectData, new EffectOwnership(target, target));
	}

	private Effect CreateStackableEffect(TestEntity target)
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
			cues: [CreateCue()]);

		return new Effect(effectData, new EffectOwnership(target, target));
	}

	private CueData CreateCue()
	{
		return new CueData(
			Tag.RequestTag(_tagsManager, "test.cue1").GetSingleTagContainer(),
			-100,
			100,
			CueMagnitudeType.AttributeValueChange,
			CueAttribute);
	}

	private Tag EventTag()
	{
		return Tag.RequestTag(_tagsManager, "tag");
	}
}
