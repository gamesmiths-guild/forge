// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

using static Gamesmiths.Forge.Tests.Helpers.ResolverTestContextFactory;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class EffectFromDataResolverTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Resolver", "EffectFromData")]
	public void Effect_from_data_resolver_builds_effect_with_default_level_and_empty_ownership_without_context()
	{
		var effectData = new EffectData("Burn", new DurationData(DurationType.Instant));
		var resolver = new EffectFromDataResolver(effectData);

		Effect effect = resolver.Resolve(new GraphContext());

		effect.EffectData.Should().Be(effectData);
		effect.Level.Should().Be(1);
		effect.Ownership.Should().Be(new EffectOwnership(null, null));
	}

	[Fact]
	[Trait("Resolver", "EffectFromData")]
	public void Effect_from_data_resolver_uses_explicit_level_and_ownership_resolvers()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var source = new TestEntity(_tagsManager, _cuesManager);
		var effectData = new EffectData("Burn", new DurationData(DurationType.Instant));
		var context = new GraphContext();
		context.GraphVariables.DefineObjectVariable<IForgeEntity>("owner", owner);
		context.GraphVariables.DefineObjectVariable<IForgeEntity>("source", source);

		var resolver = new EffectFromDataResolver(
			effectData,
			new VariantResolver(new Variant128(9), typeof(int)),
			new OwnershipResolver(
				new EntityVariableResolver("owner"),
				new EntityVariableResolver("source")));

		Effect effect = resolver.Resolve(context);

		effect.Level.Should().Be(9);
		effect.Ownership.Owner.Should().BeSameAs(owner);
		effect.Ownership.Source.Should().BeSameAs(source);
	}

	[Fact]
	[Trait("Resolver", "EffectFromData")]
	public void Effect_from_data_resolver_falls_back_to_ability_context_level_and_ownership()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var target = new TestEntity(_tagsManager, _cuesManager);
		var source = new TestEntity(_tagsManager, _cuesManager);
		var effectData = new EffectData("Burn", new DurationData(DurationType.Instant));
		GraphContext context = CreateAbilityGraphContext(owner, target, source, level: 4);

		Effect effect = new EffectFromDataResolver(effectData).Resolve(context);

		effect.Level.Should().Be(4);
		effect.Ownership.Should().Be(new AbilityOwnershipResolver().Resolve(context));
	}

	[Fact]
	[Trait("Resolver", "EffectFromData")]
	public void Effect_array_from_data_resolver_builds_one_effect_per_data_with_shared_level_and_ownership()
	{
		EffectData[] effectData =
		[
			new EffectData("Burn", new DurationData(DurationType.Instant)),
			new EffectData("Slow", new DurationData(DurationType.Infinite)),
		];

		var resolver = new EffectArrayFromDataResolver(
			effectData,
			new VariantResolver(new Variant128(3), typeof(int)));

		Effect[] effects = resolver.ResolveArray(new GraphContext());

		effects.Should().HaveCount(2);
		effects.Select(effect => effect.EffectData).Should().Equal(effectData[0], effectData[1]);
		effects.Should().OnlyContain(effect => effect.Level == 3);
	}
}
