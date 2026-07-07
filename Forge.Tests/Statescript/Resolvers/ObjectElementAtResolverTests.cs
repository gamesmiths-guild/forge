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

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class ObjectElementAtResolverTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Resolver", "ObjectElementAt")]
	public void Object_element_at_resolver_reads_the_element_at_the_resolved_index()
	{
		var entity1 = new TestEntity(_tagsManager, _cuesManager);
		var entity2 = new TestEntity(_tagsManager, _cuesManager);
		var context = new GraphContext();
		context.GraphVariables.DefineObjectArrayVariable<IForgeEntity>("targets", [entity1, entity2]);
		var source = new EntityArrayVariableResolver("targets");

		var resolver = new ObjectElementAtResolver<IForgeEntity>(
			source,
			new VariantResolver(new Variant128(1), typeof(int)));

		resolver.Resolve(context).Should().BeSameAs(entity2);
	}

	[Fact]
	[Trait("Resolver", "ObjectElementAt")]
	public void Object_element_at_resolver_reads_the_effect_at_the_resolved_index()
	{
		Effect burn = CreateInstantEffect("Burn");
		Effect chill = CreateInstantEffect("Chill");
		var context = new GraphContext();
		context.GraphVariables.DefineObjectArrayVariable("effects", [burn, chill]);

		var resolver = new ObjectElementAtResolver<Effect>(
			new ObjectArrayVariableResolver<Effect>("effects"),
			new VariantResolver(new Variant128(1), typeof(int)));

		resolver.Resolve(context).Should().BeSameAs(chill);
	}

	[Fact]
	[Trait("Resolver", "ObjectElementAt")]
	public void Object_element_at_resolver_returns_null_for_out_of_range_index()
	{
		var context = new GraphContext();
		context.GraphVariables.DefineObjectArrayVariable<IForgeEntity>(
			"targets",
			[new TestEntity(_tagsManager, _cuesManager)]);
		var source = new EntityArrayVariableResolver("targets");

		var resolver = new ObjectElementAtResolver<IForgeEntity>(
			source,
			new VariantResolver(new Variant128(-1), typeof(int)));

		resolver.Resolve(context).Should().BeNull();
	}

	private static Effect CreateInstantEffect(string name)
	{
		return new Effect(
			new EffectData(name, new DurationData(DurationType.Instant)),
			new EffectOwnership(null, null));
	}
}
