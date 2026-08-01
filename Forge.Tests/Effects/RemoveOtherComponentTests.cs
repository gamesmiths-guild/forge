// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Components;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Effects.Modifiers;
using Gamesmiths.Forge.Effects.Stacking;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Effects;

public class RemoveOtherComponentTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private const string TargetAttribute = "TestAttributeSet.Attribute1";

	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Theory]
	[Trait("Removal", null)]
	[InlineData("color.red", "color.blue")]
	[InlineData("enemy.beast.wolf", "enemy.undead.ghoul")]
	[InlineData("item.equipment.weapon.axe", "item.consumable.potion.health")]
	public void Application_removes_matching_effects_and_spares_the_rest(string removedTagKey, string keptTagKey)
	{
		TestEntity target = CreateEntity();

		ActiveEffectHandle removed = Apply(target, CreateTaggedData(removedTagKey))!;
		ActiveEffectHandle kept = Apply(target, CreateTaggedData(keptTagKey))!;

		ApplyRemover(target, [MakeQuery(removedTagKey)]);

		removed.IsValid.Should().BeFalse();
		kept.IsValid.Should().BeTrue();
	}

	[Fact]
	[Trait("Removal", null)]
	public void An_instant_effect_can_dispel()
	{
		TestEntity target = CreateEntity();

		ActiveEffectHandle poison = Apply(target, CreateTaggedData("color.red"))!;

		// The case tag-driven removal cannot reach: ModifierTagsEffectComponent is rejected on instant effects, so a
		// cleanse would otherwise have to become a duration effect just to hold a tag.
		ApplyRemover(target, [MakeQuery("color.red")], durationType: DurationType.Instant);

		poison.IsValid.Should().BeFalse();
	}

	[Fact]
	[Trait("Removal", null)]
	public void Removal_matches_effect_tags_hierarchically()
	{
		TestEntity target = CreateEntity();

		ActiveEffectHandle darkRed = Apply(target, CreateTaggedData("color.dark.red"))!;
		ActiveEffectHandle red = Apply(target, CreateTaggedData("color.red"))!;

		ApplyRemover(target, [MakeQuery("color.dark")]);

		darkRed.IsValid.Should().BeFalse();
		red.IsValid.Should().BeTrue();
	}

	[Fact]
	[Trait("Removal", null)]
	public void Every_query_gets_its_own_removal_pass()
	{
		TestEntity target = CreateEntity();

		ActiveEffectHandle first = Apply(target, CreateTaggedData("color.red"))!;
		ActiveEffectHandle second = Apply(target, CreateTaggedData("color.blue"))!;
		ActiveEffectHandle third = Apply(target, CreateTaggedData("color.green"))!;

		ApplyRemover(target, [MakeQuery("color.red"), MakeQuery("color.blue")]);

		first.IsValid.Should().BeFalse();
		second.IsValid.Should().BeFalse();
		third.IsValid.Should().BeTrue();
	}

	[Fact]
	[Trait("SelfExclusion", null)]
	public void It_never_removes_itself_even_when_it_matches_its_own_query()
	{
		TestEntity target = CreateEntity();

		ActiveEffectHandle poison = Apply(target, CreateTaggedData("color.red"))!;

		// The remover carries the very tag it removes.
		ActiveEffectHandle remover = Apply(
			target,
			CreateRemoverData([MakeQuery("color.red")], effectTagKey: "color.red"))!;

		poison.IsValid.Should().BeFalse();
		remover.IsValid.Should().BeTrue();
	}

	[Fact]
	[Trait("SelfExclusion", null)]
	public void It_excludes_only_itself_when_the_same_effect_data_is_active_twice()
	{
		TestEntity target = CreateEntity();

		EffectData removerData = CreateRemoverData([MakeQuery("color.red")], effectTagKey: "color.red");

		ActiveEffectHandle first = Apply(target, removerData)!;
		ActiveEffectHandle second = Apply(target, removerData)!;

		// The second application removed the first, which matched its query, but never itself.
		first.IsValid.Should().BeFalse();
		second.IsValid.Should().BeTrue();
	}

	[Fact]
	[Trait("Stacks", null)]
	public void Stacks_to_remove_takes_only_that_many_stacks()
	{
		TestEntity target = CreateEntity();

		EffectData stackableData = CreateStackableData("color.red");

		Apply(target, stackableData);
		ActiveEffectHandle stacked = Apply(target, stackableData)!;
		Apply(target, stackableData);

		stacked.StackCount.Should().Be(3);

		ApplyRemover(target, [MakeQuery("color.red")], stacksToRemove: 2);

		stacked.IsValid.Should().BeTrue();
		stacked.StackCount.Should().Be(1);

		ApplyRemover(target, [MakeQuery("color.red")], stacksToRemove: 2);

		stacked.IsValid.Should().BeFalse();
	}

	[Fact]
	[Trait("Stacks", null)]
	public void Each_stack_application_of_the_remover_runs_the_removal_again()
	{
		TestEntity target = CreateEntity();

		EffectData removerData = CreateRemoverData([MakeQuery("color.red")], stackable: true);

		ActiveEffectHandle firstPoison = Apply(target, CreateTaggedData("color.red"))!;
		ActiveEffectHandle remover = Apply(target, removerData)!;

		firstPoison.IsValid.Should().BeFalse();
		remover.StackCount.Should().Be(1);

		ActiveEffectHandle secondPoison = Apply(target, CreateTaggedData("color.red"))!;

		// The second application only adds a stack to the existing remover, and dispels again on the way in.
		Apply(target, removerData).Should().BeSameAs(remover);

		remover.StackCount.Should().Be(2);
		secondPoison.IsValid.Should().BeFalse();
	}

	[Fact]
	[Trait("Removal", null)]
	public void An_empty_query_removes_nothing()
	{
		TestEntity target = CreateEntity();

		ActiveEffectHandle poison = Apply(target, CreateTaggedData("color.red"))!;

		// Validation is disabled here; EffectQueryComponentsValidationTests covers the assert that rejects it.
		ApplyRemover(target, [default]);

		poison.IsValid.Should().BeTrue();
	}

	[Fact]
	[Trait("Removal", null)]
	public void It_only_removes_effects_from_its_own_target()
	{
		TestEntity target = CreateEntity();
		TestEntity bystander = CreateEntity();

		ActiveEffectHandle targetPoison = Apply(target, CreateTaggedData("color.red"))!;
		ActiveEffectHandle bystanderPoison = Apply(bystander, CreateTaggedData("color.red"))!;

		ApplyRemover(target, [MakeQuery("color.red")]);

		targetPoison.IsValid.Should().BeFalse();
		bystanderPoison.IsValid.Should().BeTrue();
	}

	[Fact]
	[Trait("Removal", null)]
	public void Removal_can_select_by_source_which_tags_cannot_reach()
	{
		TestEntity target = CreateEntity();
		TestEntity caster = CreateEntity();
		TestEntity otherCaster = CreateEntity();

		ActiveEffectHandle fromCaster = ApplyFrom(target, caster, CreateTaggedData("color.red"))!;
		ActiveEffectHandle fromOther = ApplyFrom(target, otherCaster, CreateTaggedData("color.red"))!;

		ApplyRemover(target, [new EffectQuery(EffectSource: caster)]);

		fromCaster.IsValid.Should().BeFalse();
		fromOther.IsValid.Should().BeTrue();
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

	private static ActiveEffectHandle? Apply(IForgeEntity target, EffectData effectData)
	{
		return ApplyFrom(target, target, effectData);
	}

	private static ActiveEffectHandle? ApplyFrom(IForgeEntity target, IForgeEntity source, EffectData effectData)
	{
		return target.EffectsManager.ApplyEffect(new Effect(effectData, new EffectOwnership(source, source)));
	}

	private static StackingData CreateStackingData()
	{
		return new StackingData(
			new ScalableInt(5),
			new ScalableInt(1),
			StackPolicy.AggregateBySource,
			StackLevelPolicy.SegregateLevels,
			StackMagnitudePolicy.Sum,
			StackOverflowPolicy.DenyApplication,
			StackExpirationPolicy.ClearEntireStack);
	}

	private EffectData CreateTaggedData(string effectTagKey)
	{
		return new EffectData(
			$"Effect {effectTagKey}",
			new DurationData(DurationType.Infinite),
			CreateModifiers(),
			effectTags: MakeContainer(effectTagKey));
	}

	private EffectData CreateStackableData(string effectTagKey)
	{
		return new EffectData(
			$"Stackable {effectTagKey}",
			new DurationData(DurationType.Infinite),
			CreateModifiers(),
			CreateStackingData(),
			effectTags: MakeContainer(effectTagKey));
	}

	private EffectData CreateRemoverData(
		EffectQuery[] queries,
		int stacksToRemove = -1,
		DurationType durationType = DurationType.Infinite,
		string? effectTagKey = null,
		bool stackable = false)
	{
		return new EffectData(
			"Remover",
			new DurationData(durationType),
			stackingData: stackable ? CreateStackingData() : null,
			effectComponents: [new RemoveOtherEffectComponent(queries, stacksToRemove)],
			effectTags: effectTagKey is null ? null : MakeContainer(effectTagKey));
	}

	private void ApplyRemover(
		TestEntity target,
		EffectQuery[] queries,
		int stacksToRemove = -1,
		DurationType durationType = DurationType.Infinite)
	{
		Apply(target, CreateRemoverData(queries, stacksToRemove, durationType));
	}

	private TagContainer MakeContainer(params string[] tagKeys)
	{
		return new TagContainer(_tagsManager, TestUtils.StringToTag(_tagsManager, tagKeys));
	}

	private EffectQuery MakeQuery(params string[] tagKeys)
	{
		return new EffectQuery(EffectTagQuery: TagQuery.MakeQueryMatchAnyTags(MakeContainer(tagKeys)));
	}

	private TestEntity CreateEntity()
	{
		return new TestEntity(_tagsManager, _cuesManager);
	}
}
