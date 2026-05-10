// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Effects.Modifiers;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

using static Gamesmiths.Forge.Tests.Helpers.ResolverTestContextFactory;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class AttributeResolverTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Resolver", "Attribute")]
	public void Attribute_resolver_returns_current_value_of_existing_attribute_by_default()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var resolver = new AttributeResolver("TestAttributeSet.Attribute5");

		GraphContext context = CreateAbilityGraphContext(entity);

		Variant128 result = resolver.Resolve(context);

		result.AsInt().Should().Be(5);
	}

	[Theory]
	[Trait("Resolver", "Attribute")]
	[InlineData(AttributeCalculationType.CurrentValue, 99)]
	[InlineData(AttributeCalculationType.BaseValue, 5)]
	[InlineData(AttributeCalculationType.Modifier, 200)]
	[InlineData(AttributeCalculationType.Overflow, 106)]
	[InlineData(AttributeCalculationType.ValidModifier, 94)]
	[InlineData(AttributeCalculationType.Min, 0)]
	[InlineData(AttributeCalculationType.Max, 99)]
	public void Attribute_resolver_reads_selected_attribute_values(
		AttributeCalculationType attributeCalculationType,
		int expectedValue)
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		ApplyFlatModifier(entity, "TestAttributeSet.Attribute5", 200);
		var resolver = new AttributeResolver("TestAttributeSet.Attribute5", attributeCalculationType);

		GraphContext context = CreateAbilityGraphContext(entity);

		Variant128 result = resolver.Resolve(context);

		result.AsInt().Should().Be(expectedValue);
	}

	[Fact]
	[Trait("Resolver", "Attribute")]
	public void Attribute_resolver_can_read_from_entity_variable_without_activation_context()
	{
		var selectedEntity = new TestEntity(_tagsManager, _cuesManager);
		var context = new GraphContext();
		var resolver = new AttributeResolver(
			"TestAttributeSet.Attribute5",
			new EntityVariableResolver("selectedEntity"));

		ApplyFlatModifier(selectedEntity, "TestAttributeSet.Attribute5", 10);
		context.GraphVariables.DefineReferenceVariable<IForgeEntity>("selectedEntity", selectedEntity);

		Variant128 result = resolver.Resolve(context);

		result.AsInt().Should().Be(15);
	}

	[Fact]
	[Trait("Resolver", "Attribute")]
	public void Attribute_resolver_can_read_from_target_entity()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);
		var node = new ResolvePropertyNode(
			new AttributeResolver(
				"TestAttributeSet.Attribute5",
				new TargetEntityResolver()));

		ApplyFlatModifier(target, "TestAttributeSet.Attribute5", 10);

		ExecuteAbilityGraph(owner, node, target, source: null);

		node.LastResolvedValue.AsInt().Should().Be(15);
	}

	[Fact]
	[Trait("Resolver", "Attribute")]
	public void Attribute_resolver_reads_magnitude_evaluated_up_to_channel()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		ApplyFlatModifier(entity, "TestAttributeSet.Attribute5", 10, channel: 0);
		ApplyFlatModifier(entity, "TestAttributeSet.Attribute5", 20, channel: 1);
		var resolver = new AttributeResolver(
			"TestAttributeSet.Attribute5",
			AttributeCalculationType.MagnitudeEvaluatedUpToChannel,
			finalChannel: 1);

		GraphContext context = CreateAbilityGraphContext(entity);

		Variant128 result = resolver.Resolve(context);

		result.AsInt().Should().Be(15);
	}

	[Fact]
	[Trait("Resolver", "Attribute")]
	public void Attribute_resolver_returns_default_for_missing_attribute()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var resolver = new AttributeResolver("TestAttributeSet.NonExistent");

		GraphContext context = CreateAbilityGraphContext(entity);

		Variant128 result = resolver.Resolve(context);

		result.AsInt().Should().Be(0);
	}

	[Fact]
	[Trait("Resolver", "Attribute")]
	public void Attribute_resolver_returns_default_when_no_activation_context()
	{
		var resolver = new AttributeResolver("TestAttributeSet.Attribute5");

		var context = new GraphContext();

		Variant128 result = resolver.Resolve(context);

		result.AsInt().Should().Be(0);
	}

	[Fact]
	[Trait("Resolver", "Attribute")]
	public void Attribute_resolver_value_type_is_int()
	{
		var resolver = new AttributeResolver("TestAttributeSet.Attribute5");

		resolver.ValueType.Should().Be(typeof(int));
	}

	[Fact]
	[Trait("Resolver", "Attribute")]
	public void Attribute_resolver_throws_for_negative_final_channel()
	{
		Func<AttributeResolver> createResolver = () => new AttributeResolver(
			"TestAttributeSet.Attribute5",
			AttributeCalculationType.MagnitudeEvaluatedUpToChannel,
			finalChannel: -1);

		createResolver.Should().Throw<ArgumentOutOfRangeException>();
	}

	private static void ApplyFlatModifier(TestEntity entity, StringKey attributeKey, int value, int channel = 0)
	{
		var effectData = new EffectData(
			"AttributeResolverTestModifier",
			new DurationData(DurationType.Infinite),
			[
				new Modifier(
					attributeKey,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						scalableFloatMagnitude: new ScalableFloat(value)),
					channel)
			]);

		var effect = new Effect(effectData, new EffectOwnership(null, null));

		_ = entity.EffectsManager.ApplyEffect(effect);
	}
}
