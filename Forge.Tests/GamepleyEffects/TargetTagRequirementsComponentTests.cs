// Copyright © 2024 Gamesmiths Guild.

using Gamesmiths.Forge.GameplayEffects;
using Gamesmiths.Forge.GameplayEffects.Components;
using Gamesmiths.Forge.GameplayEffects.Duration;
using Gamesmiths.Forge.GameplayEffects.Magnitudes;
using Gamesmiths.Forge.GameplayEffects.Modifiers;
using Gamesmiths.Forge.GameplayEffects.Periodic;
using Gamesmiths.Forge.GameplayEffects.Stacking;
using Gamesmiths.Forge.GameplayTags;
using Gamesmiths.Forge.Tests.GameplayTags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.GamepleyEffects;

public class TargetTagRequirementsComponentTests(GameplayTagsManagerFixture fixture)
	: IClassFixture<GameplayTagsManagerFixture>
{
	private readonly GameplayTagsManager _gameplayTagsManager = fixture.GameplayTagsManager;

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
		var entity = new TestEntity(_gameplayTagsManager);
		GameplayEffectData effectData = CreateTagRequirementsEffectData(
			[requiredAplicationTagKeys, ignoreApplicationTagKeys],
			[requiredRemovalTagKeys, ignoreRemovalTagKeys]);
		var effect = new GameplayEffect(effectData, new GameplayEffectOwnership(entity, entity));

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
		var entity = new TestEntity(_gameplayTagsManager);
		GameplayEffectData effectData = CreateTagRequirementsEffectData(
			[requiredAplicationTagKeys, ignoreApplicationTagKeys],
			[requiredRemovalTagKeys, ignoreRemovalTagKeys]);
		var effect = new GameplayEffect(effectData, new GameplayEffectOwnership(entity, entity));

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
		var entity = new TestEntity(_gameplayTagsManager);
		GameplayEffectData effectData = CreateTagRequirementsEffectData(
			[requiredAplicationTagKeys, ignoreApplicationTagKeys],
			[requiredRemovalTagKeys, ignoreRemovalTagKeys]);
		var effect = new GameplayEffect(effectData, new GameplayEffectOwnership(entity, entity));

		GameplayEffectData modifierTagEffectData = CreateModifierTagEffectData(modifierTagKeys);
		var modifierTagEffect = new GameplayEffect(modifierTagEffectData, new GameplayEffectOwnership(entity, entity));

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
		var entity = new TestEntity(_gameplayTagsManager);
		GameplayEffectData effectData = CreateTagRequirementsEffectData(
			[requiredAplicationTagKeys, ignoreApplicationTagKeys],
			[requiredRemovalTagKeys, ignoreRemovalTagKeys]);
		var effect = new GameplayEffect(effectData, new GameplayEffectOwnership(entity, entity));

		GameplayEffectData modifierTagEffectData = CreateModifierTagEffectData(modifierTagKeys);
		var modifierTagEffect = new GameplayEffect(modifierTagEffectData, new GameplayEffectOwnership(entity, entity));

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
		var entity = new TestEntity(_gameplayTagsManager);
		GameplayEffectData effectData = CreateTagRequirementsEffectData(
			[requiredAplicationTagKeys, ignoreApplicationTagKeys],
			[requiredRemovalTagKeys, ignoreRemovalTagKeys]);
		var effect = new GameplayEffect(effectData, new GameplayEffectOwnership(entity, entity));

		GameplayEffectData modifierTagEffectData = CreateModifierTagEffectData(modifierTagKeys);
		var modifierTagEffect = new GameplayEffect(modifierTagEffectData, new GameplayEffectOwnership(entity, entity));

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
		var entity = new TestEntity(_gameplayTagsManager);
		GameplayEffectData effectData = CreateTagRequirementsEffectData(
			[requiredAplicationTagKeys, ignoreApplicationTagKeys],
			[requiredRemovalTagKeys, ignoreRemovalTagKeys]);
		var effect = new GameplayEffect(effectData, new GameplayEffectOwnership(entity, entity));

		GameplayEffectData modifierTagEffectData = CreateModifierTagEffectData(modifierTagKeys);
		var modifierTagEffect = new GameplayEffect(modifierTagEffectData, new GameplayEffectOwnership(entity, entity));

		entity.EffectsManager.ApplyEffect(modifierTagEffect);
		entity.EffectsManager.ApplyEffect(effect);

		TestUtils.TestStackData(
			entity.EffectsManager.GetEffectInfo(effectData),
			1,
			[new int[] { 3, 1, 0 }],
			entity,
			entity);

		entity.EffectsManager.UnapplyEffect(modifierTagEffect);

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
		var entity = new TestEntity(_gameplayTagsManager);

		var query = new GameplayTagQuery();
		query.Build(new GameplayTagQueryExpression(_gameplayTagsManager)
			.AllExpressionsMatch()
				.AddExpression(new GameplayTagQueryExpression(_gameplayTagsManager)
					.AnyTagsMatch()
						.AddTag("color.green")
						.AddTag("color.blue"))
				.AddExpression(new GameplayTagQueryExpression(_gameplayTagsManager)
					.NoExpressionsMatch()
						.AddExpression(new GameplayTagQueryExpression(_gameplayTagsManager)
							.AllTagsMatch()
								.AddTag("color.green")
								.AddTag("color.blue"))
						.AddExpression(new GameplayTagQueryExpression(_gameplayTagsManager)
							.AnyTagsMatch()
								.AddTag("color.red"))));

		var effectData = new GameplayEffectData(
			"Tag Requirements Effect with Query",
			[],
			new DurationData(DurationType.Infinite),
			null,
			null,
			gameplayEffectComponents:
			[
				new TargetTagRequirementsEffectComponent(
					new GameplayTagRequirements(
						new GameplayTagContainer(_gameplayTagsManager),
						new GameplayTagContainer(_gameplayTagsManager),
						query),
					new GameplayTagRequirements(
						new GameplayTagContainer(_gameplayTagsManager),
						new GameplayTagContainer(_gameplayTagsManager),
						new GameplayTagQuery()),
					new GameplayTagRequirements(
						new GameplayTagContainer(_gameplayTagsManager),
						new GameplayTagContainer(_gameplayTagsManager),
						new GameplayTagQuery()))
			]);

		var effect = new GameplayEffect(effectData, new GameplayEffectOwnership(entity, entity));

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
		var entity = new TestEntity(_gameplayTagsManager);

		var query = new GameplayTagQuery();
		query.Build(new GameplayTagQueryExpression(_gameplayTagsManager)
			.AllExpressionsMatch()
				.AddExpression(new GameplayTagQueryExpression(_gameplayTagsManager)
					.AnyTagsMatch()
						.AddTag("color.green")
						.AddTag("color.blue"))
				.AddExpression(new GameplayTagQueryExpression(_gameplayTagsManager)
					.NoExpressionsMatch()
						.AddExpression(new GameplayTagQueryExpression(_gameplayTagsManager)
							.AllTagsMatch()
								.AddTag("color.green")
								.AddTag("color.blue"))
						.AddExpression(new GameplayTagQueryExpression(_gameplayTagsManager)
							.AnyTagsMatch()
								.AddTag("enemy.undead"))));

		var effectData = new GameplayEffectData(
			"Tag Requirements Effect with Query",
			[],
			new DurationData(DurationType.Infinite),
			null,
			null,
			gameplayEffectComponents:
			[
				new TargetTagRequirementsEffectComponent(
					new GameplayTagRequirements(
						new GameplayTagContainer(_gameplayTagsManager),
						new GameplayTagContainer(_gameplayTagsManager),
						query),
					new GameplayTagRequirements(
						new GameplayTagContainer(_gameplayTagsManager),
						new GameplayTagContainer(_gameplayTagsManager),
						new GameplayTagQuery()),
					new GameplayTagRequirements(
						new GameplayTagContainer(_gameplayTagsManager),
						new GameplayTagContainer(_gameplayTagsManager),
						new GameplayTagQuery()))
			]);

		var effect = new GameplayEffect(effectData, new GameplayEffectOwnership(entity, entity));

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
		var entity = new TestEntity(_gameplayTagsManager);
		GameplayEffectData effectData = CreateOngoingRequirementsEffectData(
			[requiredOngoingTagKeys, ignoreOngoingTagKeys]);
		var effect = new GameplayEffect(effectData, new GameplayEffectOwnership(entity, entity));

		entity.EffectsManager.ApplyEffect(effect);

		TestUtils.TestAttribute(entity, "TestAttributeSet.Attribute1", [11, 1, 10, 0]);
		TestUtils.TestStackData(
			entity.EffectsManager.GetEffectInfo(effectData),
			1,
			[new int[] { 1, 1, 0 }],
			entity,
			entity);

		entity.EffectsManager.UnapplyEffect(effect);

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
		var entity = new TestEntity(_gameplayTagsManager);
		GameplayEffectData effectData = CreateOngoingRequirementsEffectData(
			[requiredOngoingTagKeys, ignoreOngoingTagKeys]);
		var effect = new GameplayEffect(effectData, new GameplayEffectOwnership(entity, entity));

		entity.EffectsManager.ApplyEffect(effect);

		TestUtils.TestAttribute(entity, "TestAttributeSet.Attribute1", [1, 1, 0, 0]);
		TestUtils.TestStackData(
			entity.EffectsManager.GetEffectInfo(effectData),
			1,
			[new int[] { 1, 1, 0 }],
			entity,
			entity);

		entity.EffectsManager.UnapplyEffect(effect);

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
		var entity = new TestEntity(_gameplayTagsManager);
		GameplayEffectData effectData = CreateOngoingRequirementsEffectData(
			[requiredOngoingTagKeys, ignoreOngoingTagKeys]);
		var effect = new GameplayEffect(effectData, new GameplayEffectOwnership(entity, entity));

		GameplayEffectData modifierTagEffectData = CreateModifierTagEffectData(modifierTagKeys);
		var modifierTagEffect = new GameplayEffect(modifierTagEffectData, new GameplayEffectOwnership(entity, entity));

		entity.EffectsManager.ApplyEffect(effect);
		TestUtils.TestAttribute(entity, "TestAttributeSet.Attribute1", [11, 1, 10, 0]);

		entity.EffectsManager.ApplyEffect(modifierTagEffect);
		TestUtils.TestAttribute(entity, "TestAttributeSet.Attribute1", [1, 1, 0, 0]);

		entity.EffectsManager.UnapplyEffect(modifierTagEffect);
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
		var entity = new TestEntity(_gameplayTagsManager);
		GameplayEffectData effectData = CreateOngoingRequirementsEffectData(
			[requiredOngoingTagKeys, ignoreOngoingTagKeys]);
		var effect = new GameplayEffect(effectData, new GameplayEffectOwnership(entity, entity));

		GameplayEffectData modifierTagEffectData = CreateModifierTagEffectData(modifierTagKeys);
		var modifierTagEffect = new GameplayEffect(modifierTagEffectData, new GameplayEffectOwnership(entity, entity));

		entity.EffectsManager.ApplyEffect(modifierTagEffect);
		entity.EffectsManager.ApplyEffect(effect);
		TestUtils.TestAttribute(entity, "TestAttributeSet.Attribute1", [11, 1, 10, 0]);

		entity.EffectsManager.UnapplyEffect(modifierTagEffect);
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
		var entity = new TestEntity(_gameplayTagsManager);
		GameplayEffectData effectData = CreateOngoingRequirementsPeriodicEffectData(
			[requiredOngoingTagKeys, ignoreOngoingTagKeys]);
		var effect = new GameplayEffect(effectData, new GameplayEffectOwnership(entity, entity));

		entity.EffectsManager.ApplyEffect(effect);

		TestUtils.TestAttribute(entity, "TestAttributeSet.Attribute1", [11, 11, 0, 0]);
		TestUtils.TestStackData(
			entity.EffectsManager.GetEffectInfo(effectData),
			1,
			[new int[] { 1, 1, 0 }],
			entity,
			entity);

		entity.EffectsManager.UpdateEffects(3f);

		TestUtils.TestAttribute(entity, "TestAttributeSet.Attribute1", [41, 41, 0, 0]);

		entity.EffectsManager.UnapplyEffect(effect);

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
		var entity = new TestEntity(_gameplayTagsManager);
		GameplayEffectData effectData = CreateOngoingRequirementsPeriodicEffectData(
			[requiredOngoingTagKeys, ignoreOngoingTagKeys]);
		var effect = new GameplayEffect(effectData, new GameplayEffectOwnership(entity, entity));

		entity.EffectsManager.ApplyEffect(effect);
		entity.EffectsManager.UpdateEffects(100f);

		TestUtils.TestAttribute(entity, "TestAttributeSet.Attribute1", [1, 1, 0, 0]);
		TestUtils.TestStackData(
			entity.EffectsManager.GetEffectInfo(effectData),
			1,
			[new int[] { 1, 1, 0 }],
			entity,
			entity);

		entity.EffectsManager.UnapplyEffect(effect);

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
	public void Periodic_effect_with_ongoing_requirement_gets_inhibited_after_modifier_tag_application(
		string[] modifierTagKeys,
		string[]? requiredOngoingTagKeys,
		string[]? ignoreOngoingTagKeys)
	{
		var entity = new TestEntity(_gameplayTagsManager);
		GameplayEffectData effectData = CreateOngoingRequirementsPeriodicEffectData(
			[requiredOngoingTagKeys, ignoreOngoingTagKeys]);
		var effect = new GameplayEffect(effectData, new GameplayEffectOwnership(entity, entity));

		GameplayEffectData modifierTagEffectData = CreateModifierTagEffectData(modifierTagKeys);
		var modifierTagEffect = new GameplayEffect(modifierTagEffectData, new GameplayEffectOwnership(entity, entity));

		entity.EffectsManager.ApplyEffect(effect);
		TestUtils.TestAttribute(entity, "TestAttributeSet.Attribute1", [11, 11, 0, 0]);

		entity.EffectsManager.UpdateEffects(3f);
		TestUtils.TestAttribute(entity, "TestAttributeSet.Attribute1", [41, 41, 0, 0]);

		entity.EffectsManager.ApplyEffect(modifierTagEffect);
		entity.EffectsManager.UpdateEffects(3f);
		TestUtils.TestAttribute(entity, "TestAttributeSet.Attribute1", [41, 41, 0, 0]);

		entity.EffectsManager.UnapplyEffect(modifierTagEffect);
		entity.EffectsManager.UpdateEffects(3f);
		TestUtils.TestAttribute(entity, "TestAttributeSet.Attribute1", [71, 71, 0, 0]);
	}

	[Theory]
	[Trait("Ongoing Periodic", null)]
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
	public void Periodic_effect_with_ongoing_requirement_gets_inhibited_after_modifier_tag_removal(
		string[] modifierTagKeys,
		string[]? requiredOngoingTagKeys,
		string[]? ignoreOngoingTagKeys)
	{
		var entity = new TestEntity(_gameplayTagsManager);
		GameplayEffectData effectData = CreateOngoingRequirementsPeriodicEffectData(
			[requiredOngoingTagKeys, ignoreOngoingTagKeys]);
		var effect = new GameplayEffect(effectData, new GameplayEffectOwnership(entity, entity));

		GameplayEffectData modifierTagEffectData = CreateModifierTagEffectData(modifierTagKeys);
		var modifierTagEffect = new GameplayEffect(modifierTagEffectData, new GameplayEffectOwnership(entity, entity));

		entity.EffectsManager.ApplyEffect(modifierTagEffect);
		entity.EffectsManager.ApplyEffect(effect);
		TestUtils.TestAttribute(entity, "TestAttributeSet.Attribute1", [11, 11, 0, 0]);

		entity.EffectsManager.UpdateEffects(3f);
		TestUtils.TestAttribute(entity, "TestAttributeSet.Attribute1", [41, 41, 0, 0]);

		entity.EffectsManager.UnapplyEffect(modifierTagEffect);
		entity.EffectsManager.UpdateEffects(3f);
		TestUtils.TestAttribute(entity, "TestAttributeSet.Attribute1", [41, 41, 0, 0]);

		entity.EffectsManager.ApplyEffect(modifierTagEffect);
		entity.EffectsManager.UpdateEffects(3f);
		TestUtils.TestAttribute(entity, "TestAttributeSet.Attribute1", [71, 71, 0, 0]);
	}

	private GameplayEffectData CreateTagRequirementsEffectData(
		string[]?[] aplicationTagKeys,
		string[]?[] removalTagKeys)
	{
		HashSet<GameplayTag> requiredApplicationTags = TestUtils.StringToGameplayTag(_gameplayTagsManager, aplicationTagKeys[0]);
		HashSet<GameplayTag> ignoreApplicationTags = TestUtils.StringToGameplayTag(_gameplayTagsManager, aplicationTagKeys[1]);

		HashSet<GameplayTag> requiredRemovalTags = TestUtils.StringToGameplayTag(_gameplayTagsManager, removalTagKeys[0]);
		HashSet<GameplayTag> ignoreRemovalTags = TestUtils.StringToGameplayTag(_gameplayTagsManager, removalTagKeys[1]);

		return new GameplayEffectData(
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
			gameplayEffectComponents:
			[
				new TargetTagRequirementsEffectComponent(
					new GameplayTagRequirements(
						new GameplayTagContainer(_gameplayTagsManager, requiredApplicationTags),
						new GameplayTagContainer(_gameplayTagsManager, ignoreApplicationTags),
						new GameplayTagQuery()),
					new GameplayTagRequirements(
						new GameplayTagContainer(_gameplayTagsManager, requiredRemovalTags),
						new GameplayTagContainer(_gameplayTagsManager, ignoreRemovalTags),
						new GameplayTagQuery()),
					new GameplayTagRequirements(
						new GameplayTagContainer(_gameplayTagsManager),
						new GameplayTagContainer(_gameplayTagsManager),
						new GameplayTagQuery()))
			]);
	}

	private GameplayEffectData CreateOngoingRequirementsEffectData(
		string[]?[] ongoingTagKeys)
	{
		HashSet<GameplayTag> requiredOngoingTags = TestUtils.StringToGameplayTag(_gameplayTagsManager, ongoingTagKeys[0]);
		HashSet<GameplayTag> ignoreOngoingTags = TestUtils.StringToGameplayTag(_gameplayTagsManager, ongoingTagKeys[1]);

		return new GameplayEffectData(
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
			gameplayEffectComponents:
			[
				new TargetTagRequirementsEffectComponent(
					new GameplayTagRequirements(
						new GameplayTagContainer(_gameplayTagsManager),
						new GameplayTagContainer(_gameplayTagsManager),
						new GameplayTagQuery()),
					new GameplayTagRequirements(
						new GameplayTagContainer(_gameplayTagsManager),
						new GameplayTagContainer(_gameplayTagsManager),
						new GameplayTagQuery()),
					new GameplayTagRequirements(
						new GameplayTagContainer(_gameplayTagsManager, requiredOngoingTags),
						new GameplayTagContainer(_gameplayTagsManager, ignoreOngoingTags),
						new GameplayTagQuery()))
			]);
	}

	private GameplayEffectData CreateOngoingRequirementsPeriodicEffectData(
		string[]?[] ongoingTagKeys)
	{
		HashSet<GameplayTag> requiredOngoingTags = TestUtils.StringToGameplayTag(_gameplayTagsManager, ongoingTagKeys[0]);
		HashSet<GameplayTag> ignoreOngoingTags = TestUtils.StringToGameplayTag(_gameplayTagsManager, ongoingTagKeys[1]);

		return new GameplayEffectData(
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
			new PeriodicData(new ScalableFloat(1), true),
			gameplayEffectComponents:
			[
				new TargetTagRequirementsEffectComponent(
					new GameplayTagRequirements(
						new GameplayTagContainer(_gameplayTagsManager),
						new GameplayTagContainer(_gameplayTagsManager),
						new GameplayTagQuery()),
					new GameplayTagRequirements(
						new GameplayTagContainer(_gameplayTagsManager),
						new GameplayTagContainer(_gameplayTagsManager),
						new GameplayTagQuery()),
					new GameplayTagRequirements(
						new GameplayTagContainer(_gameplayTagsManager, requiredOngoingTags),
						new GameplayTagContainer(_gameplayTagsManager, ignoreOngoingTags),
						new GameplayTagQuery()))
			]);
	}

	private GameplayEffectData CreateModifierTagEffectData(string[] tagKeys)
	{
		HashSet<GameplayTag> tags = TestUtils.StringToGameplayTag(_gameplayTagsManager, tagKeys);

		return new GameplayEffectData(
			"Modifier Tags Effect",
			[],
			new DurationData(DurationType.Infinite),
			null,
			null,
			gameplayEffectComponents:
			[
				new ModifierTagsEffectComponent(new GameplayTagContainer(_gameplayTagsManager, tags))
			]);
	}
}
