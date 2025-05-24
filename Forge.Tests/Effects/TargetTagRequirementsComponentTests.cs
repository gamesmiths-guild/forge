// Copyright © Gamesmiths Guild.

using System.Diagnostics;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Components;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Effects.Modifiers;
using Gamesmiths.Forge.Effects.Periodic;
using Gamesmiths.Forge.Effects.Stacking;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Cues;
using Gamesmiths.Forge.Tests.Helpers;
using Gamesmiths.Forge.Tests.Tags;

namespace Gamesmiths.Forge.Tests.Effects;

public class TargetTagRequirementsComponentTests(
	TagsManagerFixture tagsManagerFixture,
	CuesManagerFixture cuesManagerFixture)
	: IClassFixture<TagsManagerFixture>, IClassFixture<CuesManagerFixture>
{
	private readonly TagsManager _tagsManager = tagsManagerFixture.TagsManager;
	private readonly CuesManager _cuesManager = cuesManagerFixture.CuesManager;

	[Theory]
	[Trait("Can Apply", null)]
	[InlineData(
		new string[] { "color.green" },
		new string[] { "color.red", "color.blue" },
		null,
		null)]
	[InlineData(
		new string[] { "color", "enemy.undead" },
		new string[] { "color.dark.green" },
		null,
		null)]
	[InlineData(
		null,
		null,
		new string[] { "color.dark.green" },
		new string[] { "color.green" })]
	public void Effect_meets_application_requirements(
		string[]? requiredAplicationTagKeys,
		string[]? ignoreApplicationTagKeys,
		string[]? requiredRemovalTagKeys,
		string[]? ignoreRemovalTagKeys)
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		EffectData effectData = CreateTagRequirementsEffectData(
			[requiredAplicationTagKeys, ignoreApplicationTagKeys],
			[requiredRemovalTagKeys, ignoreRemovalTagKeys]);
		var effect = new Effect(effectData, new EffectOwnership(entity, entity));

		entity.EffectsManager.ApplyEffect(effect);

		TestUtils.TestStackData(
			entity.EffectsManager.GetEffectInfo(effectData),
			1,
			[new int[] { 3, 1, 0 }],
			entity,
			entity);
	}

	[Theory]
	[Trait("Can't Apply", null)]
	[InlineData(
		new string[] { "color.red", "color.blue" },
		new string[] { "color.green" },
		null,
		null)]
	[InlineData(
		null,
		new string[] { "enemy" },
		null,
		null)]
	[InlineData(
		null,
		null,
		new string[] { "color.green" },
		null)]
	[InlineData(
		null,
		null,
		new string[] { "color" },
		null)]
	public void Effect_does_not_meet_application_requirements(
		string[]? requiredAplicationTagKeys,
		string[]? ignoreApplicationTagKeys,
		string[]? requiredRemovalTagKeys,
		string[]? ignoreRemovalTagKeys)
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		EffectData effectData = CreateTagRequirementsEffectData(
			[requiredAplicationTagKeys, ignoreApplicationTagKeys],
			[requiredRemovalTagKeys, ignoreRemovalTagKeys]);
		var effect = new Effect(effectData, new EffectOwnership(entity, entity));

		entity.EffectsManager.ApplyEffect(effect);

		TestUtils.TestStackData(
			entity.EffectsManager.GetEffectInfo(effectData),
			0,
			[],
			entity,
			entity);
	}

	[Theory]
	[Trait("Can Apply", null)]
	[InlineData(
		new string[] { "color.dark.green" },
		new string[] { "color.green", "color.dark.green" },
		null,
		null,
		null)]
	[InlineData(
		new string[] { "item.equipment.weapon.axe" },
		new string[] { "color.green", "item.equipment.weapon.axe" },
		null,
		null,
		null)]
	[InlineData(
		new string[] { "item.equipment.weapon.axe" },
		new string[] { "color.green" },
		new string[] { "color.red", "color.blue" },
		null,
		null)]
	[InlineData(
		new string[] { "item.equipment.weapon.axe", "color.dark.green" },
		new string[] { "color.dark", "item.equipment.weapon" },
		null,
		null,
		null)]
	[InlineData(
		new string[] { "item.equipment.weapon.axe" },
		null,
		null,
		new string[] { "item.equipment.weapon.sword" },
		new string[] { "item.equipment" })]
	public void Effect_meets_application_requirements_with_modifier_tags(
		string[] modifierTagKeys,
		string[]? requiredAplicationTagKeys,
		string[]? ignoreApplicationTagKeys,
		string[]? requiredRemovalTagKeys,
		string[]? ignoreRemovalTagKeys)
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		EffectData effectData = CreateTagRequirementsEffectData(
			[requiredAplicationTagKeys, ignoreApplicationTagKeys],
			[requiredRemovalTagKeys, ignoreRemovalTagKeys]);
		var effect = new Effect(effectData, new EffectOwnership(entity, entity));

		EffectData modifierTagEffectData = CreateModifierTagEffectData(modifierTagKeys);
		var modifierTagEffect = new Effect(modifierTagEffectData, new EffectOwnership(entity, entity));

		entity.EffectsManager.ApplyEffect(modifierTagEffect);
		entity.EffectsManager.ApplyEffect(effect);

		TestUtils.TestStackData(
			entity.EffectsManager.GetEffectInfo(effectData),
			1,
			[new int[] { 3, 1, 0 }],
			entity,
			entity);
	}

	[Theory]
	[Trait("Can't Apply", null)]
	[InlineData(
		new string[] { "item.equipment.weapon.axe" },
		new string[] { "color.red", "color.blue" },
		new string[] { "color.green" },
		null,
		null)]
	[InlineData(
		new string[] { "item.equipment.weapon.axe" },
		null,
		new string[] { "item.equipment.weapon.axe" },
		null,
		null)]
	[InlineData(
		new string[] { "item.equipment.weapon.axe" },
		null,
		null,
		new string[] { "item.equipment.weapon" },
		null)]
	[InlineData(
		new string[] { "item.equipment.weapon.axe" },
		null,
		null,
		new string[] { "color" },
		null)]
	public void Effect_does_not_meet_application_requirements_with_modifier_tags(
		string[] modifierTagKeys,
		string[]? requiredAplicationTagKeys,
		string[]? ignoreApplicationTagKeys,
		string[]? requiredRemovalTagKeys,
		string[]? ignoreRemovalTagKeys)
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		EffectData effectData = CreateTagRequirementsEffectData(
			[requiredAplicationTagKeys, ignoreApplicationTagKeys],
			[requiredRemovalTagKeys, ignoreRemovalTagKeys]);
		var effect = new Effect(effectData, new EffectOwnership(entity, entity));

		EffectData modifierTagEffectData = CreateModifierTagEffectData(modifierTagKeys);
		var modifierTagEffect = new Effect(modifierTagEffectData, new EffectOwnership(entity, entity));

		entity.EffectsManager.ApplyEffect(modifierTagEffect);
		entity.EffectsManager.ApplyEffect(effect);

		TestUtils.TestStackData(
			entity.EffectsManager.GetEffectInfo(effectData),
			0,
			[],
			entity,
			entity);
	}

	[Theory]
	[Trait("Removed", null)]
	[InlineData(
		new string[] { "color.dark.green" },
		null,
		null,
		new string[] { "color.dark.green" },
		null)]
	[InlineData(
		new string[] { "item.equipment.weapon.axe" },
		null,
		null,
		new string[] { "item.equipment.weapon" },
		null)]
	[InlineData(
		new string[] { "item.equipment.weapon.axe", "color.dark.green" },
		null,
		null,
		new string[] { "item.equipment", "color.dark" },
		null)]
	public void Effect_gets_removed_after_modifier_tag_is_applied(
		string[] modifierTagKeys,
		string[]? requiredAplicationTagKeys,
		string[]? ignoreApplicationTagKeys,
		string[]? requiredRemovalTagKeys,
		string[]? ignoreRemovalTagKeys)
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		EffectData effectData = CreateTagRequirementsEffectData(
			[requiredAplicationTagKeys, ignoreApplicationTagKeys],
			[requiredRemovalTagKeys, ignoreRemovalTagKeys]);
		var effect = new Effect(effectData, new EffectOwnership(entity, entity));

		EffectData modifierTagEffectData = CreateModifierTagEffectData(modifierTagKeys);
		var modifierTagEffect = new Effect(modifierTagEffectData, new EffectOwnership(entity, entity));

		entity.EffectsManager.ApplyEffect(effect);
		entity.EffectsManager.ApplyEffect(modifierTagEffect);

		TestUtils.TestStackData(
			entity.EffectsManager.GetEffectInfo(effectData),
			0,
			[],
			entity,
			entity);
	}

	[Theory]
	[Trait("Removed", null)]
	[InlineData(
		new string[] { "color.dark.green" },
		null,
		null,
		null,
		new string[] { "color.dark.green" })]
	[InlineData(
		new string[] { "item.equipment.weapon.axe" },
		null,
		null,
		null,
		new string[] { "item.equipment.weapon" })]
	[InlineData(
		new string[] { "item.equipment.weapon.axe", "color.dark.green" },
		null,
		null,
		null,
		new string[] { "item.equipment", "color.dark" })]
	public void Effect_gets_removed_after_modifier_tag_is_removed(
		string[] modifierTagKeys,
		string[]? requiredAplicationTagKeys,
		string[]? ignoreApplicationTagKeys,
		string[]? requiredRemovalTagKeys,
		string[]? ignoreRemovalTagKeys)
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		EffectData effectData = CreateTagRequirementsEffectData(
			[requiredAplicationTagKeys, ignoreApplicationTagKeys],
			[requiredRemovalTagKeys, ignoreRemovalTagKeys]);
		var effect = new Effect(effectData, new EffectOwnership(entity, entity));

		EffectData modifierTagEffectData = CreateModifierTagEffectData(modifierTagKeys);
		var modifierTagEffect = new Effect(modifierTagEffectData, new EffectOwnership(entity, entity));

		ActiveEffectHandle? activeModifierEffectHandle = entity.EffectsManager.ApplyEffect(modifierTagEffect);
		Debug.Assert(activeModifierEffectHandle is not null, "Effect handle should have a value.");

		entity.EffectsManager.ApplyEffect(effect);

		TestUtils.TestStackData(
			entity.EffectsManager.GetEffectInfo(effectData),
			1,
			[new int[] { 3, 1, 0 }],
			entity,
			entity);

		entity.EffectsManager.UnapplyEffect(activeModifierEffectHandle);

		TestUtils.TestStackData(
			entity.EffectsManager.GetEffectInfo(effectData),
			0,
			[],
			entity,
			entity);
	}

	[Fact]
	[Trait("Can Apply", null)]
	public void Effect_meets_application_requirements_with_query()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);

		var query = new TagQuery();
		query.Build(new TagQueryExpression(_tagsManager)
			.AllExpressionsMatch()
				.AddExpression(new TagQueryExpression(_tagsManager)
					.AnyTagsMatch()
						.AddTag("color.green")
						.AddTag("color.blue"))
				.AddExpression(new TagQueryExpression(_tagsManager)
					.NoExpressionsMatch()
						.AddExpression(new TagQueryExpression(_tagsManager)
							.AllTagsMatch()
								.AddTag("color.green")
								.AddTag("color.blue"))
						.AddExpression(new TagQueryExpression(_tagsManager)
							.AnyTagsMatch()
								.AddTag("color.red"))));

		var effectData = new EffectData(
			"Tag Requirements Effect with Query",
			[],
			new DurationData(DurationType.Infinite),
			null,
			null,
			effectComponents:
			[
				new TargetTagRequirementsEffectComponent(
					new TagRequirements(
						new TagContainer(_tagsManager),
						new TagContainer(_tagsManager),
						query),
					new TagRequirements(
						new TagContainer(_tagsManager),
						new TagContainer(_tagsManager),
						new TagQuery()),
					new TagRequirements(
						new TagContainer(_tagsManager),
						new TagContainer(_tagsManager),
						new TagQuery()))
			]);

		var effect = new Effect(effectData, new EffectOwnership(entity, entity));

		entity.EffectsManager.ApplyEffect(effect);

		TestUtils.TestStackData(
			entity.EffectsManager.GetEffectInfo(effectData),
			1,
			[new int[] { 1, 1, 0 }],
			entity,
			entity);
	}

	[Fact]
	[Trait("Can't Apply", null)]
	public void Effect_does_not_meet_application_requirements_with_query()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);

		var query = new TagQuery();
		query.Build(new TagQueryExpression(_tagsManager)
			.AllExpressionsMatch()
				.AddExpression(new TagQueryExpression(_tagsManager)
					.AnyTagsMatch()
						.AddTag("color.green")
						.AddTag("color.blue"))
				.AddExpression(new TagQueryExpression(_tagsManager)
					.NoExpressionsMatch()
						.AddExpression(new TagQueryExpression(_tagsManager)
							.AllTagsMatch()
								.AddTag("color.green")
								.AddTag("color.blue"))
						.AddExpression(new TagQueryExpression(_tagsManager)
							.AnyTagsMatch()
								.AddTag("enemy.undead"))));

		var effectData = new EffectData(
			"Tag Requirements Effect with Query",
			[],
			new DurationData(DurationType.Infinite),
			null,
			null,
			effectComponents:
			[
				new TargetTagRequirementsEffectComponent(
					new TagRequirements(
						new TagContainer(_tagsManager),
						new TagContainer(_tagsManager),
						query),
					new TagRequirements(
						new TagContainer(_tagsManager),
						new TagContainer(_tagsManager),
						new TagQuery()),
					new TagRequirements(
						new TagContainer(_tagsManager),
						new TagContainer(_tagsManager),
						new TagQuery()))
			]);

		var effect = new Effect(effectData, new EffectOwnership(entity, entity));

		entity.EffectsManager.ApplyEffect(effect);

		TestUtils.TestStackData(
			entity.EffectsManager.GetEffectInfo(effectData),
			0,
			[],
			entity,
			entity);
	}

	[Theory]
	[Trait("Ongoing", null)]
	[InlineData(
		new string[] { "color.green" },
		null)]
	[InlineData(
		null,
		new string[] { "color.red" })]
	[InlineData(
		new string[] { "color.green" },
		new string[] { "color.red", "color.blue" })]
	[InlineData(
		new string[] { "color", "enemy.undead" },
		new string[] { "color.dark.green" })]
	public void Effect_with_ongoing_requirement_initializes_normally(
		string[]? requiredOngoingTagKeys,
		string[]? ignoreOngoingTagKeys)
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		EffectData effectData = CreateOngoingRequirementsEffectData(
			[requiredOngoingTagKeys, ignoreOngoingTagKeys]);
		var effect = new Effect(effectData, new EffectOwnership(entity, entity));

		ActiveEffectHandle? activeEffectHandle = entity.EffectsManager.ApplyEffect(effect);
		Debug.Assert(activeEffectHandle is not null, "Effect handle should have a value.");

		TestUtils.TestAttribute(entity, "TestAttributeSet.Attribute1", [11, 1, 10, 0]);
		TestUtils.TestStackData(
			entity.EffectsManager.GetEffectInfo(effectData),
			1,
			[new int[] { 1, 1, 0 }],
			entity,
			entity);

		entity.EffectsManager.UnapplyEffect(activeEffectHandle);

		TestUtils.TestAttribute(entity, "TestAttributeSet.Attribute1", [1, 1, 0, 0]);
		TestUtils.TestStackData(
			entity.EffectsManager.GetEffectInfo(effectData),
			0,
			[],
			entity,
			entity);
	}

	[Theory]
	[Trait("Ongoing", null)]
	[InlineData(
		new string[] { "color.red" },
		null)]
	[InlineData(
		null,
		new string[] { "color.green" })]
	[InlineData(
		new string[] { "color.red", "color.blue" },
		new string[] { "color.green" })]
	[InlineData(
		new string[] { "color.dark.green" },
		new string[] { "color", "enemy.undead" })]
	public void Effect_without_ongoing_requirement_initializes_inhibited(
		string[]? requiredOngoingTagKeys,
		string[]? ignoreOngoingTagKeys)
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		EffectData effectData = CreateOngoingRequirementsEffectData(
			[requiredOngoingTagKeys, ignoreOngoingTagKeys]);
		var effect = new Effect(effectData, new EffectOwnership(entity, entity));

		ActiveEffectHandle? activeEffectHandle = entity.EffectsManager.ApplyEffect(effect);
		Debug.Assert(activeEffectHandle is not null, "Effect handle should have a value.");

		TestUtils.TestAttribute(entity, "TestAttributeSet.Attribute1", [1, 1, 0, 0]);
		TestUtils.TestStackData(
			entity.EffectsManager.GetEffectInfo(effectData),
			1,
			[new int[] { 1, 1, 0 }],
			entity,
			entity);

		entity.EffectsManager.UnapplyEffect(activeEffectHandle);

		TestUtils.TestAttribute(entity, "TestAttributeSet.Attribute1", [1, 1, 0, 0]);
		TestUtils.TestStackData(
			entity.EffectsManager.GetEffectInfo(effectData),
			0,
			[],
			entity,
			entity);
	}

	[Theory]
	[Trait("Ongoing", null)]
	[InlineData(
		new string[] { "color.dark.green" },
		null,
		new string[] { "color.dark.green" })]
	[InlineData(
		new string[] { "item.equipment.weapon.axe" },
		null,
		new string[] { "item" })]
	[InlineData(
		new string[] { "color.red", "color.blue" },
		null,
		new string[] { "color.red", "color.blue" })]
	[InlineData(
		new string[] { "color.dark.green", "item.equipment.weapon.axe" },
		null,
		new string[] { "color.dark", "item.equipment.weapon" })]
	public void Effect_with_ongoing_requirement_gets_inhibited_after_modifier_tag_application(
		string[] modifierTagKeys,
		string[]? requiredOngoingTagKeys,
		string[]? ignoreOngoingTagKeys)
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		EffectData effectData = CreateOngoingRequirementsEffectData(
			[requiredOngoingTagKeys, ignoreOngoingTagKeys]);
		var effect = new Effect(effectData, new EffectOwnership(entity, entity));

		EffectData modifierTagEffectData = CreateModifierTagEffectData(modifierTagKeys);
		var modifierTagEffect = new Effect(modifierTagEffectData, new EffectOwnership(entity, entity));

		entity.EffectsManager.ApplyEffect(effect);
		TestUtils.TestAttribute(entity, "TestAttributeSet.Attribute1", [11, 1, 10, 0]);

		ActiveEffectHandle? activeModifierEffectHandle = entity.EffectsManager.ApplyEffect(modifierTagEffect);
		Debug.Assert(activeModifierEffectHandle is not null, "Effect handle should have a value.");

		TestUtils.TestAttribute(entity, "TestAttributeSet.Attribute1", [1, 1, 0, 0]);

		entity.EffectsManager.UnapplyEffect(activeModifierEffectHandle);
		TestUtils.TestAttribute(entity, "TestAttributeSet.Attribute1", [11, 1, 10, 0]);
	}

	[Theory]
	[Trait("Ongoing", null)]
	[InlineData(
		new string[] { "color.dark.green" },
		new string[] { "color.dark.green" },
		null)]
	[InlineData(
		new string[] { "item.equipment.weapon.axe" },
		new string[] { "item" },
		null)]
	[InlineData(
		new string[] { "color.red", "color.blue" },
		new string[] { "color.red", "color.blue" },
		null)]
	[InlineData(
		new string[] { "color.dark.green", "item.equipment.weapon.axe" },
		new string[] { "color.dark", "item.equipment.weapon" },
		null)]
	public void Effect_with_ongoing_requirement_gets_inhibited_after_modifier_tag_removal(
		string[] modifierTagKeys,
		string[]? requiredOngoingTagKeys,
		string[]? ignoreOngoingTagKeys)
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		EffectData effectData = CreateOngoingRequirementsEffectData(
			[requiredOngoingTagKeys, ignoreOngoingTagKeys]);
		var effect = new Effect(effectData, new EffectOwnership(entity, entity));

		EffectData modifierTagEffectData = CreateModifierTagEffectData(modifierTagKeys);
		var modifierTagEffect = new Effect(modifierTagEffectData, new EffectOwnership(entity, entity));

		ActiveEffectHandle? activeModifierEffectHandle = entity.EffectsManager.ApplyEffect(modifierTagEffect);
		Debug.Assert(activeModifierEffectHandle is not null, "Effect handle should have a value.");

		entity.EffectsManager.ApplyEffect(effect);
		TestUtils.TestAttribute(entity, "TestAttributeSet.Attribute1", [11, 1, 10, 0]);

		entity.EffectsManager.UnapplyEffect(activeModifierEffectHandle);
		TestUtils.TestAttribute(entity, "TestAttributeSet.Attribute1", [1, 1, 0, 0]);
	}

	[Theory]
	[Trait("Ongoing Periodic", null)]
	[InlineData(
		new string[] { "color.green" },
		null)]
	[InlineData(
		null,
		new string[] { "color.red" })]
	[InlineData(
		new string[] { "color.green" },
		new string[] { "color.red", "color.blue" })]
	[InlineData(
		new string[] { "color", "enemy.undead" },
		new string[] { "color.dark.green" })]
	public void Periodic_effect_with_ongoing_requirement_executes_normally(
		string[]? requiredOngoingTagKeys,
		string[]? ignoreOngoingTagKeys)
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		EffectData effectData = CreateOngoingRequirementsPeriodicEffectData(
			[requiredOngoingTagKeys, ignoreOngoingTagKeys],
			PeriodInhibitionRemovedPolicy.NeverReset);
		var effect = new Effect(effectData, new EffectOwnership(entity, entity));

		ActiveEffectHandle? activeEffectHandle = entity.EffectsManager.ApplyEffect(effect);
		Debug.Assert(activeEffectHandle is not null, "Effect handle should have a value.");

		TestUtils.TestAttribute(entity, "TestAttributeSet.Attribute1", [11, 11, 0, 0]);
		TestUtils.TestStackData(
			entity.EffectsManager.GetEffectInfo(effectData),
			1,
			[new int[] { 1, 1, 0 }],
			entity,
			entity);

		entity.EffectsManager.UpdateEffects(3f);

		TestUtils.TestAttribute(entity, "TestAttributeSet.Attribute1", [41, 41, 0, 0]);

		entity.EffectsManager.UnapplyEffect(activeEffectHandle);

		TestUtils.TestAttribute(entity, "TestAttributeSet.Attribute1", [41, 41, 0, 0]);
		TestUtils.TestStackData(
			entity.EffectsManager.GetEffectInfo(effectData),
			0,
			[],
			entity,
			entity);
	}

	[Theory]
	[Trait("Ongoing Periodic", null)]
	[InlineData(
		new string[] { "color.red" },
		null)]
	[InlineData(
		null,
		new string[] { "color.green" })]
	[InlineData(
		new string[] { "color.red", "color.blue" },
		new string[] { "color.green" })]
	[InlineData(
		new string[] { "color.dark.green" },
		new string[] { "color", "enemy.undead" })]
	public void Periodic_effect_without_ongoing_requirement_does_not_execute(
		string[]? requiredOngoingTagKeys,
		string[]? ignoreOngoingTagKeys)
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		EffectData effectData = CreateOngoingRequirementsPeriodicEffectData(
			[requiredOngoingTagKeys, ignoreOngoingTagKeys],
			PeriodInhibitionRemovedPolicy.NeverReset);
		var effect = new Effect(effectData, new EffectOwnership(entity, entity));

		ActiveEffectHandle? activeEffectHandle = entity.EffectsManager.ApplyEffect(effect);
		Debug.Assert(activeEffectHandle is not null, "Effect handle should have a value.");

		entity.EffectsManager.UpdateEffects(100f);

		TestUtils.TestAttribute(entity, "TestAttributeSet.Attribute1", [1, 1, 0, 0]);
		TestUtils.TestStackData(
			entity.EffectsManager.GetEffectInfo(effectData),
			1,
			[new int[] { 1, 1, 0 }],
			entity,
			entity);

		entity.EffectsManager.UnapplyEffect(activeEffectHandle);

		TestUtils.TestAttribute(entity, "TestAttributeSet.Attribute1", [1, 1, 0, 0]);
		TestUtils.TestStackData(
			entity.EffectsManager.GetEffectInfo(effectData),
			0,
			[],
			entity,
			entity);
	}

	[Theory]
	[Trait("Ongoing Periodic", null)]
	[InlineData(
		new string[] { "color.dark.green" },
		null,
		new string[] { "color.dark.green" },
		PeriodInhibitionRemovedPolicy.NeverReset,
		3f,
		3f,
		3f,
		new int[] { 11, 11, 0, 0 },
		new int[] { 41, 41, 0, 0 },
		new int[] { 41, 41, 0, 0 },
		new int[] { 71, 71, 0, 0 })]
	[InlineData(
		new string[] { "item.equipment.weapon.axe" },
		null,
		new string[] { "item" },
		PeriodInhibitionRemovedPolicy.NeverReset,
		3f,
		3f,
		3f,
		new int[] { 11, 11, 0, 0 },
		new int[] { 41, 41, 0, 0 },
		new int[] { 41, 41, 0, 0 },
		new int[] { 71, 71, 0, 0 })]
	[InlineData(
		new string[] { "color.red", "color.blue" },
		null,
		new string[] { "color.red", "color.blue" },
		PeriodInhibitionRemovedPolicy.NeverReset,
		3f,
		3f,
		3f,
		new int[] { 11, 11, 0, 0 },
		new int[] { 41, 41, 0, 0 },
		new int[] { 41, 41, 0, 0 },
		new int[] { 71, 71, 0, 0 })]
	[InlineData(
		new string[] { "color.dark.green", "item.equipment.weapon.axe" },
		null,
		new string[] { "color.dark", "item.equipment.weapon" },
		PeriodInhibitionRemovedPolicy.NeverReset,
		3f,
		3f,
		3f,
		new int[] { 11, 11, 0, 0 },
		new int[] { 41, 41, 0, 0 },
		new int[] { 41, 41, 0, 0 },
		new int[] { 71, 71, 0, 0 })]
	[InlineData(
		new string[] { "color.red" },
		null,
		new string[] { "color.red" },
		PeriodInhibitionRemovedPolicy.ResetPeriod,
		3f,
		3.5f,
		2.5f,
		new int[] { 11, 11, 0, 0 },
		new int[] { 41, 41, 0, 0 },
		new int[] { 41, 41, 0, 0 },
		new int[] { 61, 61, 0, 0 })]
	[InlineData(
		new string[] { "color.red" },
		null,
		new string[] { "color.red" },
		PeriodInhibitionRemovedPolicy.ExecuteAndResetPeriod,
		3f,
		3.5f,
		1f,
		new int[] { 11, 11, 0, 0 },
		new int[] { 41, 41, 0, 0 },
		new int[] { 41, 41, 0, 0 },
		new int[] { 61, 61, 0, 0 })]
	public void Periodic_effect_with_ongoing_requirement_gets_inhibited_after_modifier_tag_application(
		string[] modifierTagKeys,
		string[]? requiredOngoingTagKeys,
		string[]? ignoreOngoingTagKeys,
		PeriodInhibitionRemovedPolicy periodInhibitionRemovedPolicy,
		float firstUpdatePeriod,
		float secondUpdatePeriod,
		float thirdUpdatePeriod,
		int[] firstExpectedResults,
		int[] secondExpectedResults,
		int[] thirdExpectedResults,
		int[] fourthExpectedResults)
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		EffectData effectData = CreateOngoingRequirementsPeriodicEffectData(
			[requiredOngoingTagKeys, ignoreOngoingTagKeys],
			periodInhibitionRemovedPolicy);
		var effect = new Effect(effectData, new EffectOwnership(entity, entity));

		EffectData modifierTagEffectData = CreateModifierTagEffectData(modifierTagKeys);
		var modifierTagEffect = new Effect(modifierTagEffectData, new EffectOwnership(entity, entity));

		entity.EffectsManager.ApplyEffect(effect);
		TestUtils.TestAttribute(entity, "TestAttributeSet.Attribute1", firstExpectedResults);

		entity.EffectsManager.UpdateEffects(firstUpdatePeriod);
		TestUtils.TestAttribute(entity, "TestAttributeSet.Attribute1", secondExpectedResults);

		ActiveEffectHandle? activeModifierEffectHandle = entity.EffectsManager.ApplyEffect(modifierTagEffect);
		Debug.Assert(activeModifierEffectHandle is not null, "Effect handle should have a value.");

		entity.EffectsManager.UpdateEffects(secondUpdatePeriod);
		TestUtils.TestAttribute(entity, "TestAttributeSet.Attribute1", thirdExpectedResults);

		entity.EffectsManager.UnapplyEffect(activeModifierEffectHandle);
		entity.EffectsManager.UpdateEffects(thirdUpdatePeriod);
		TestUtils.TestAttribute(entity, "TestAttributeSet.Attribute1", fourthExpectedResults);
	}

	[Theory]
	[Trait("Ongoing Periodic", null)]
	[InlineData(
		new string[] { "color.dark.green" },
		new string[] { "color.dark.green" },
		null,
		PeriodInhibitionRemovedPolicy.NeverReset,
		3f,
		3f,
		3f,
		new int[] { 11, 11, 0, 0 },
		new int[] { 41, 41, 0, 0 },
		new int[] { 41, 41, 0, 0 },
		new int[] { 71, 71, 0, 0 })]
	[InlineData(
		new string[] { "item.equipment.weapon.axe" },
		new string[] { "item" },
		null,
		PeriodInhibitionRemovedPolicy.NeverReset,
		3f,
		3f,
		3f,
		new int[] { 11, 11, 0, 0 },
		new int[] { 41, 41, 0, 0 },
		new int[] { 41, 41, 0, 0 },
		new int[] { 71, 71, 0, 0 })]
	[InlineData(
		new string[] { "color.red", "color.blue" },
		new string[] { "color.red", "color.blue" },
		null,
		PeriodInhibitionRemovedPolicy.NeverReset,
		3f,
		3f,
		3f,
		new int[] { 11, 11, 0, 0 },
		new int[] { 41, 41, 0, 0 },
		new int[] { 41, 41, 0, 0 },
		new int[] { 71, 71, 0, 0 })]
	[InlineData(
		new string[] { "color.dark.green", "item.equipment.weapon.axe" },
		new string[] { "color.dark", "item.equipment.weapon" },
		null,
		PeriodInhibitionRemovedPolicy.NeverReset,
		3f,
		3f,
		3f,
		new int[] { 11, 11, 0, 0 },
		new int[] { 41, 41, 0, 0 },
		new int[] { 41, 41, 0, 0 },
		new int[] { 71, 71, 0, 0 })]
	[InlineData(
		new string[] { "color.red" },
		new string[] { "color.red" },
		null,
		PeriodInhibitionRemovedPolicy.ResetPeriod,
		3f,
		3.5f,
		2.5f,
		new int[] { 11, 11, 0, 0 },
		new int[] { 41, 41, 0, 0 },
		new int[] { 41, 41, 0, 0 },
		new int[] { 61, 61, 0, 0 })]
	[InlineData(
		new string[] { "color.red" },
		new string[] { "color.red" },
		null,
		PeriodInhibitionRemovedPolicy.ExecuteAndResetPeriod,
		3f,
		3.5f,
		1f,
		new int[] { 11, 11, 0, 0 },
		new int[] { 41, 41, 0, 0 },
		new int[] { 41, 41, 0, 0 },
		new int[] { 61, 61, 0, 0 })]
	public void Periodic_effect_with_ongoing_requirement_gets_inhibited_after_modifier_tag_removal(
		string[] modifierTagKeys,
		string[]? requiredOngoingTagKeys,
		string[]? ignoreOngoingTagKeys,
		PeriodInhibitionRemovedPolicy periodInhibitionRemovedPolicy,
		float firstUpdatePeriod,
		float secondUpdatePeriod,
		float thirdUpdatePeriod,
		int[] firstExpectedResults,
		int[] secondExpectedResults,
		int[] thirdExpectedResults,
		int[] fourthExpectedResults)
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		EffectData effectData = CreateOngoingRequirementsPeriodicEffectData(
			[requiredOngoingTagKeys, ignoreOngoingTagKeys],
			periodInhibitionRemovedPolicy);
		var effect = new Effect(effectData, new EffectOwnership(entity, entity));

		EffectData modifierTagEffectData = CreateModifierTagEffectData(modifierTagKeys);
		var modifierTagEffect = new Effect(modifierTagEffectData, new EffectOwnership(entity, entity));

		ActiveEffectHandle? activeModifierEffectHandle = entity.EffectsManager.ApplyEffect(modifierTagEffect);
		Debug.Assert(activeModifierEffectHandle is not null, "Effect handle should have a value.");

		entity.EffectsManager.ApplyEffect(effect);
		TestUtils.TestAttribute(entity, "TestAttributeSet.Attribute1", firstExpectedResults);

		entity.EffectsManager.UpdateEffects(firstUpdatePeriod);
		TestUtils.TestAttribute(entity, "TestAttributeSet.Attribute1", secondExpectedResults);

		entity.EffectsManager.UnapplyEffect(activeModifierEffectHandle);
		entity.EffectsManager.UpdateEffects(secondUpdatePeriod);
		TestUtils.TestAttribute(entity, "TestAttributeSet.Attribute1", thirdExpectedResults);

		entity.EffectsManager.ApplyEffect(modifierTagEffect);
		entity.EffectsManager.UpdateEffects(thirdUpdatePeriod);
		TestUtils.TestAttribute(entity, "TestAttributeSet.Attribute1", fourthExpectedResults);
	}

	private EffectData CreateTagRequirementsEffectData(
		string[]?[] aplicationTagKeys,
		string[]?[] removalTagKeys)
	{
		HashSet<Tag> requiredApplicationTags = TestUtils.StringToTag(_tagsManager, aplicationTagKeys[0]);
		HashSet<Tag> ignoreApplicationTags = TestUtils.StringToTag(_tagsManager, aplicationTagKeys[1]);

		HashSet<Tag> requiredRemovalTags = TestUtils.StringToTag(_tagsManager, removalTagKeys[0]);
		HashSet<Tag> ignoreRemovalTags = TestUtils.StringToTag(_tagsManager, removalTagKeys[1]);

		return new EffectData(
			"Tag Requirements Effect",
			[],
			new DurationData(DurationType.Infinite),
			new StackingData(
				new ScalableInt(3),
				new ScalableInt(3),
				StackPolicy.AggregateBySource,
				StackLevelPolicy.SegregateLevels,
				StackMagnitudePolicy.Sum,
				StackOverflowPolicy.DenyApplication,
				StackExpirationPolicy.RemoveSingleStackAndRefreshDuration),
			null,
			effectComponents:
			[
				new TargetTagRequirementsEffectComponent(
					new TagRequirements(
						new TagContainer(_tagsManager, requiredApplicationTags),
						new TagContainer(_tagsManager, ignoreApplicationTags),
						new TagQuery()),
					new TagRequirements(
						new TagContainer(_tagsManager, requiredRemovalTags),
						new TagContainer(_tagsManager, ignoreRemovalTags),
						new TagQuery()),
					new TagRequirements(
						new TagContainer(_tagsManager),
						new TagContainer(_tagsManager),
						new TagQuery()))
			]);
	}

	private EffectData CreateOngoingRequirementsEffectData(
		string[]?[] ongoingTagKeys)
	{
		HashSet<Tag> requiredOngoingTags = TestUtils.StringToTag(_tagsManager, ongoingTagKeys[0]);
		HashSet<Tag> ignoreOngoingTags = TestUtils.StringToTag(_tagsManager, ongoingTagKeys[1]);

		return new EffectData(
			"Tag Requirements Effect",
			[
				new Modifier(
					"TestAttributeSet.Attribute1",
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						new ScalableFloat(10)))
			],
			new DurationData(DurationType.Infinite),
			null,
			null,
			effectComponents:
			[
				new TargetTagRequirementsEffectComponent(
					new TagRequirements(
						new TagContainer(_tagsManager),
						new TagContainer(_tagsManager),
						new TagQuery()),
					new TagRequirements(
						new TagContainer(_tagsManager),
						new TagContainer(_tagsManager),
						new TagQuery()),
					new TagRequirements(
						new TagContainer(_tagsManager, requiredOngoingTags),
						new TagContainer(_tagsManager, ignoreOngoingTags),
						new TagQuery()))
			]);
	}

	private EffectData CreateOngoingRequirementsPeriodicEffectData(
		string[]?[] ongoingTagKeys,
		PeriodInhibitionRemovedPolicy periodInhibitionRemovedPolicy)
	{
		HashSet<Tag> requiredOngoingTags = TestUtils.StringToTag(_tagsManager, ongoingTagKeys[0]);
		HashSet<Tag> ignoreOngoingTags = TestUtils.StringToTag(_tagsManager, ongoingTagKeys[1]);

		return new EffectData(
			"Tag Requirements Effect",
			[
				new Modifier(
					"TestAttributeSet.Attribute1",
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						new ScalableFloat(10)))
			],
			new DurationData(DurationType.Infinite),
			null,
			new PeriodicData(new ScalableFloat(1), true, periodInhibitionRemovedPolicy),
			effectComponents:
			[
				new TargetTagRequirementsEffectComponent(
					new TagRequirements(
						new TagContainer(_tagsManager),
						new TagContainer(_tagsManager),
						new TagQuery()),
					new TagRequirements(
						new TagContainer(_tagsManager),
						new TagContainer(_tagsManager),
						new TagQuery()),
					new TagRequirements(
						new TagContainer(_tagsManager, requiredOngoingTags),
						new TagContainer(_tagsManager, ignoreOngoingTags),
						new TagQuery()))
			]);
	}

	private EffectData CreateModifierTagEffectData(string[] tagKeys)
	{
		HashSet<Tag> tags = TestUtils.StringToTag(_tagsManager, tagKeys);

		return new EffectData(
			"Modifier Tags Effect",
			[],
			new DurationData(DurationType.Infinite),
			null,
			null,
			effectComponents:
			[
				new ModifierTagsEffectComponent(new TagContainer(_tagsManager, tags))
			]);
	}
}
