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

namespace Gamesmiths.Forge.Tests.Effects;

public class EffectQueryTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private const string TargetAttribute = "TestAttributeSet.Attribute90";
	private const string OtherAttribute = "TestAttributeSet.Attribute1";

	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Query", "EffectDefinition")]
	public void Effect_query_matches_by_effect_definition()
	{
		TestEntity source = CreateEntity();
		EffectData poisonData = CreateEffectData("Poison", effectTagKeys: ["color.red"]);
		EffectData curseData = CreateEffectData("Curse", effectTagKeys: ["color.blue"]);

		var query = new EffectQuery(EffectDefinition: poisonData);

		query.Matches(CreateEffect(poisonData, source)).Should().BeTrue();
		query.Matches(CreateEffect(curseData, source)).Should().BeFalse();
	}

	[Fact]
	[Trait("Query", "EffectTags")]
	public void Effect_query_matches_by_effect_tags()
	{
		TestEntity source = CreateEntity();
		EffectData taggedData = CreateEffectData("Poison", effectTagKeys: ["color.red"]);
		EffectData otherData = CreateEffectData("Curse", effectTagKeys: ["color.blue"]);

		var query = new EffectQuery(EffectTagQuery: MakeAnyQuery("color.red"));

		query.Matches(CreateEffect(taggedData, source)).Should().BeTrue();
		query.Matches(CreateEffect(otherData, source)).Should().BeFalse();
	}

	[Fact]
	[Trait("Query", "EffectTags")]
	public void Effect_query_matches_effect_tags_hierarchically()
	{
		TestEntity source = CreateEntity();
		EffectData effectData = CreateEffectData("Poison", effectTagKeys: ["color.dark.red"]);

		new EffectQuery(EffectTagQuery: MakeAnyQuery("color.dark"))
			.Matches(CreateEffect(effectData, source)).Should().BeTrue();
	}

	[Fact]
	[Trait("Query", "EffectTags")]
	public void Effect_query_evaluates_untagged_effects_as_an_empty_container()
	{
		TestEntity source = CreateEntity();
		EffectData untaggedData = CreateEffectData("Plain");
		EffectData taggedData = CreateEffectData("Poison", effectTagKeys: ["color.red"]);

		var query = new EffectQuery(
			EffectTagQuery: TagQuery.MakeQueryMatchNoTags(MakeContainer("color.red")));

		query.Matches(CreateEffect(untaggedData, source)).Should().BeTrue();
		query.Matches(CreateEffect(taggedData, source)).Should().BeFalse();
	}

	[Fact]
	[Trait("Query", "GrantedTags")]
	public void Effect_query_matches_by_granted_tags()
	{
		TestEntity source = CreateEntity();
		EffectData grantingData = CreateEffectData("Slow", grantedTagKeys: ["color.blue"]);
		EffectData taggedData = CreateEffectData("Poison", effectTagKeys: ["color.blue"]);

		var query = new EffectQuery(GrantedTagQuery: MakeAnyQuery("color.blue"));

		query.Matches(CreateEffect(grantingData, source)).Should().BeTrue();

		// Effect tags are identity, not entity state: they never satisfy a granted tag query.
		query.Matches(CreateEffect(taggedData, source)).Should().BeFalse();
	}

	[Fact]
	[Trait("Query", "OwningTags")]
	public void Effect_query_matches_owning_tags_from_either_side()
	{
		TestEntity source = CreateEntity();
		EffectData taggedData = CreateEffectData("Poison", effectTagKeys: ["color.red"]);
		EffectData grantingData = CreateEffectData("Slow", grantedTagKeys: ["color.red"]);
		EffectData bothData = CreateEffectData(
			"Hex",
			effectTagKeys: ["color.blue"],
			grantedTagKeys: ["color.red"]);
		EffectData neitherData = CreateEffectData("Plain");

		var query = new EffectQuery(OwningTagQuery: MakeAnyQuery("color.red"));

		query.Matches(CreateEffect(taggedData, source)).Should().BeTrue();
		query.Matches(CreateEffect(grantingData, source)).Should().BeTrue();
		query.Matches(CreateEffect(bothData, source)).Should().BeTrue();
		query.Matches(CreateEffect(neitherData, source)).Should().BeFalse();
	}

	[Fact]
	[Trait("Query", "SourceTagRequirements")]
	public void Effect_query_matches_by_source_tag_requirements()
	{
		TestEntity venomousSource = CreateEntity();
		TestEntity plainSource = CreateEntity();
		GrantTags(venomousSource, "enemy.beast.wolf");

		EffectData effectData = CreateEffectData("Poison");

		var query = new EffectQuery(
			SourceTagRequirements: new TagRequirements(RequiredTags: MakeContainer("enemy.beast.wolf")));

		query.Matches(CreateEffect(effectData, venomousSource)).Should().BeTrue();
		query.Matches(CreateEffect(effectData, plainSource)).Should().BeFalse();
	}

	[Fact]
	[Trait("Query", "ModifyingAttribute")]
	public void Effect_query_matches_by_modifying_attribute()
	{
		TestEntity source = CreateEntity();
		EffectData effectData = CreateEffectData("Poison");

		new EffectQuery(ModifyingAttribute: TargetAttribute)
			.Matches(CreateEffect(effectData, source)).Should().BeTrue();
		new EffectQuery(ModifyingAttribute: OtherAttribute)
			.Matches(CreateEffect(effectData, source)).Should().BeFalse();
	}

	[Fact]
	[Trait("Query", "EffectSource")]
	public void Effect_query_matches_by_effect_source()
	{
		TestEntity source = CreateEntity();
		TestEntity otherSource = CreateEntity();
		EffectData effectData = CreateEffectData("Poison");

		var query = new EffectQuery(EffectSource: source);

		query.Matches(CreateEffect(effectData, source)).Should().BeTrue();
		query.Matches(CreateEffect(effectData, otherSource)).Should().BeFalse();
	}

	[Fact]
	[Trait("Query", "CustomMatch")]
	public void Effect_query_matches_with_a_custom_predicate()
	{
		TestEntity source = CreateEntity();
		EffectData effectData = CreateEffectData("Poison");

		var query = new EffectQuery(CustomMatch: x => x.Level >= 3);

		query.Matches(CreateEffect(effectData, source, level: 3)).Should().BeTrue();
		query.Matches(CreateEffect(effectData, source, level: 2)).Should().BeFalse();
	}

	[Fact]
	[Trait("Query", "Combined")]
	public void Effect_query_combines_every_defined_field_with_and()
	{
		TestEntity source = CreateEntity();
		EffectData matchingData = CreateEffectData(
			"Poison",
			effectTagKeys: ["color.red"],
			grantedTagKeys: ["color.blue"]);
		EffectData wrongGrantedTagData = CreateEffectData(
			"Poison",
			effectTagKeys: ["color.red"],
			grantedTagKeys: ["color.green"]);

		var query = new EffectQuery(
			EffectTagQuery: MakeAnyQuery("color.red"),
			GrantedTagQuery: MakeAnyQuery("color.blue"),
			ModifyingAttribute: TargetAttribute,
			EffectSource: source);

		query.Matches(CreateEffect(matchingData, source)).Should().BeTrue();
		query.Matches(CreateEffect(wrongGrantedTagData, source)).Should().BeFalse();
	}

	[Fact]
	[Trait("Query", "Empty")]
	public void Empty_effect_query_matches_every_effect()
	{
		TestEntity source = CreateEntity();

		var query = default(EffectQuery);

		query.IsEmpty.Should().BeTrue();
		query.Matches(CreateEffect(CreateEffectData("Plain"), source)).Should().BeTrue();
	}

	[Fact]
	[Trait("Query", "Empty")]
	public void Effect_query_is_not_empty_once_any_field_is_defined()
	{
		new EffectQuery(EffectTagQuery: MakeAnyQuery("color.red")).IsEmpty.Should().BeFalse();
		new EffectQuery(ModifyingAttribute: TargetAttribute).IsEmpty.Should().BeFalse();
		new EffectQuery(CustomMatch: _ => true).IsEmpty.Should().BeFalse();
	}

	[Fact]
	[Trait("Query", "Handles")]
	public void Effect_query_never_matches_invalid_or_ignored_handles()
	{
		TestEntity target = CreateEntity();
		EffectData effectData = CreateEffectData("Poison", effectTagKeys: ["color.red"]);

		ActiveEffectHandle handle = Apply(target, effectData)!;
		var query = new EffectQuery(EffectTagQuery: MakeAnyQuery("color.red"));

		query.Matches(handle).Should().BeTrue();
		query.Matches(handle, [handle]).Should().BeFalse();

		target.EffectsManager.RemoveEffect(handle, forceRemoval: true);

		query.Matches(handle).Should().BeFalse();
	}

	[Fact]
	[Trait("Query", "EffectsManager")]
	public void Get_active_effects_by_query_agrees_with_hand_filtered_results()
	{
		TestEntity target = CreateEntity();
		EffectData poisonData = CreateEffectData("Poison", effectTagKeys: ["color.red"]);
		EffectData curseData = CreateEffectData("Curse", effectTagKeys: ["color.blue"]);

		ActiveEffectHandle poisonHandle = Apply(target, poisonData)!;
		Apply(target, curseData);

		var query = new EffectQuery(EffectTagQuery: MakeAnyQuery("color.red"));

		ActiveEffectHandle[] handFiltered =
			[.. target.EffectsManager.GetActiveEffects().Where(x => query.Matches(x))];

		target.EffectsManager.GetActiveEffects(query).Should().Equal(handFiltered);
		target.EffectsManager.GetActiveEffects(query).Should().Equal(poisonHandle);
	}

	[Fact]
	[Trait("Query", "EffectsManager")]
	public void Get_active_effects_with_an_empty_query_returns_every_active_effect()
	{
		TestEntity target = CreateEntity();
		Apply(target, CreateEffectData("Poison", effectTagKeys: ["color.red"]));
		Apply(target, CreateEffectData("Curse", effectTagKeys: ["color.blue"]));

		target.EffectsManager.GetActiveEffects(default(EffectQuery))
			.Should().Equal(target.EffectsManager.GetActiveEffects());
	}

	[Fact]
	[Trait("Query", "EffectsManager")]
	public void Has_any_active_effect_answers_the_query_without_materializing_results()
	{
		TestEntity target = CreateEntity();
		var query = new EffectQuery(EffectTagQuery: MakeAnyQuery("color.red"));

		target.EffectsManager.HasAnyActiveEffect(query).Should().BeFalse();

		ActiveEffectHandle handle = Apply(target, CreateEffectData("Poison", effectTagKeys: ["color.red"]))!;

		target.EffectsManager.HasAnyActiveEffect(query).Should().BeTrue();

		target.EffectsManager.RemoveEffect(handle, forceRemoval: true);

		target.EffectsManager.HasAnyActiveEffect(query).Should().BeFalse();
	}

	[Fact]
	[Trait("Query", "EffectsManager")]
	public void Get_effect_stack_data_by_query_mirrors_the_effect_data_overload()
	{
		TestEntity target = CreateEntity();
		EffectData stackableData = CreateStackableEffectData("Bleed", ["color.red"]);

		Apply(target, stackableData);
		Apply(target, stackableData);

		var query = new EffectQuery(EffectTagQuery: MakeAnyQuery("color.red"));

		target.EffectsManager.GetEffectStackData(query)
			.Should().Equal(target.EffectsManager.GetEffectStackData(stackableData));
	}

	[Fact]
	[Trait("Query", "EffectsManager")]
	public void Remove_effects_by_query_removes_only_matching_effects()
	{
		TestEntity target = CreateEntity();
		ActiveEffectHandle poisonHandle = Apply(target, CreateEffectData("Poison", effectTagKeys: ["color.red"]))!;
		ActiveEffectHandle curseHandle = Apply(target, CreateEffectData("Curse", effectTagKeys: ["color.blue"]))!;

		int removed = target.EffectsManager.RemoveEffects(
			new EffectQuery(EffectTagQuery: MakeAnyQuery("color.red")));

		removed.Should().Be(1);
		poisonHandle.IsValid.Should().BeFalse();
		curseHandle.IsValid.Should().BeTrue();
		target.EffectsManager.GetActiveEffects().Should().Equal(curseHandle);
	}

	[Fact]
	[Trait("Query", "EffectsManager")]
	public void Remove_effects_by_query_removes_the_requested_number_of_stacks()
	{
		TestEntity target = CreateEntity();
		EffectData stackableData = CreateStackableEffectData("Bleed", ["color.red"]);

		Apply(target, stackableData);
		ActiveEffectHandle handle = Apply(target, stackableData)!;
		Apply(target, stackableData);

		handle.StackCount.Should().Be(3);

		var query = new EffectQuery(EffectTagQuery: MakeAnyQuery("color.red"));

		target.EffectsManager.RemoveEffects(query, stacksToRemove: 1).Should().Be(1);
		handle.StackCount.Should().Be(2);

		target.EffectsManager.RemoveEffects(query, stacksToRemove: 5).Should().Be(1);
		handle.IsValid.Should().BeFalse();
	}

	[Fact]
	[Trait("Query", "EffectsManager")]
	public void Remove_effects_by_query_removes_non_stackable_effects_on_the_first_stack()
	{
		TestEntity target = CreateEntity();
		ActiveEffectHandle handle = Apply(target, CreateEffectData("Poison", effectTagKeys: ["color.red"]))!;

		target.EffectsManager.RemoveEffects(
			new EffectQuery(EffectTagQuery: MakeAnyQuery("color.red")),
			stacksToRemove: 1).Should().Be(1);

		handle.IsValid.Should().BeFalse();
		target.EffectsManager.GetActiveEffects().Should().BeEmpty();
	}

	[Fact]
	[Trait("Query", "EffectsManager")]
	public void Remove_effects_by_query_honours_the_ignore_set()
	{
		TestEntity target = CreateEntity();
		EffectData poisonData = CreateEffectData("Poison", effectTagKeys: ["color.red"]);

		ActiveEffectHandle firstHandle = Apply(target, poisonData)!;
		ActiveEffectHandle secondHandle = Apply(target, poisonData)!;

		int removed = target.EffectsManager.RemoveEffects(
			new EffectQuery(EffectTagQuery: MakeAnyQuery("color.red")),
			stacksToRemove: -1,
			ignoredHandles: [firstHandle]);

		removed.Should().Be(1);
		firstHandle.IsValid.Should().BeTrue();
		secondHandle.IsValid.Should().BeFalse();
	}

	[Fact]
	[Trait("Query", "EffectsManager")]
	public void Remove_effects_by_query_is_a_no_op_for_zero_stacks()
	{
		TestEntity target = CreateEntity();
		ActiveEffectHandle handle = Apply(target, CreateEffectData("Poison", effectTagKeys: ["color.red"]))!;

		target.EffectsManager.RemoveEffects(
			new EffectQuery(EffectTagQuery: MakeAnyQuery("color.red")),
			stacksToRemove: 0).Should().Be(0);

		handle.IsValid.Should().BeTrue();
	}

	private static Effect CreateEffect(EffectData effectData, TestEntity source, int level = 1)
	{
		return new Effect(effectData, new EffectOwnership(source, source), level);
	}

	private static ActiveEffectHandle? Apply(TestEntity target, EffectData effectData)
	{
		return target.EffectsManager.ApplyEffect(
			new Effect(effectData, new EffectOwnership(target, target)));
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

	private TagContainer MakeContainer(params string[] tagKeys)
	{
		return new TagContainer(_tagsManager, TestUtils.StringToTag(_tagsManager, tagKeys));
	}

	private TagQuery MakeAnyQuery(params string[] tagKeys)
	{
		return TagQuery.MakeQueryMatchAnyTags(MakeContainer(tagKeys));
	}

	private EffectData CreateEffectData(
		string name,
		string[]? effectTagKeys = null,
		string[]? grantedTagKeys = null)
	{
		IEffectComponent[]? components = grantedTagKeys is null
			? null
			: [new ModifierTagsEffectComponent(MakeContainer(grantedTagKeys))];

		return new EffectData(
			name,
			new DurationData(DurationType.Infinite),
			CreateModifiers(),
			effectComponents: components,
			effectTags: effectTagKeys is null ? null : MakeContainer(effectTagKeys));
	}

	private EffectData CreateStackableEffectData(string name, string[] effectTagKeys)
	{
		var stackingData = new StackingData(
			new ScalableInt(5),
			new ScalableInt(1),
			StackPolicy.AggregateBySource,
			StackLevelPolicy.SegregateLevels,
			StackMagnitudePolicy.Sum,
			StackOverflowPolicy.DenyApplication,
			StackExpirationPolicy.ClearEntireStack);

		return new EffectData(
			name,
			new DurationData(DurationType.Infinite),
			CreateModifiers(),
			stackingData,
			effectTags: MakeContainer(effectTagKeys));
	}

	private TestEntity CreateEntity()
	{
		return new TestEntity(_tagsManager, _cuesManager);
	}

	private void GrantTags(TestEntity entity, params string[] tagKeys)
	{
		var tagEffectData = new EffectData(
			"Tag Granter",
			new DurationData(DurationType.Infinite),
			effectComponents: [new ModifierTagsEffectComponent(MakeContainer(tagKeys))]);

		entity.EffectsManager.ApplyEffect(new Effect(tagEffectData, new EffectOwnership(entity, entity)));
	}
}
