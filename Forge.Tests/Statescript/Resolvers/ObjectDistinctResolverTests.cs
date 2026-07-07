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

public class ObjectDistinctResolverTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Resolver", "ObjectDistinct")]
	public void Object_distinct_resolver_keeps_the_first_occurrence_of_each_reference()
	{
		var entity1 = new TestEntity(_tagsManager, _cuesManager);
		var entity2 = new TestEntity(_tagsManager, _cuesManager);
		var context = new GraphContext();
		context.GraphVariables.DefineObjectArrayVariable<IForgeEntity>(
			"targets",
			[entity1, entity2, entity1, entity2]);

		var resolver = new ObjectDistinctResolver<IForgeEntity>(new EntityArrayVariableResolver("targets"));

		resolver.ResolveArray(context).Should().Equal(entity1, entity2);
	}

	[Fact]
	[Trait("Resolver", "ObjectDistinct")]
	public void Object_distinct_resolver_dedupes_effects_by_reference_identity()
	{
		Effect burn = CreateInstantEffect("Burn");
		Effect chill = CreateInstantEffect("Chill");
		var context = new GraphContext();
		context.GraphVariables.DefineObjectArrayVariable("effects", [burn, chill, burn, chill]);

		var resolver = new ObjectDistinctResolver<Effect>(new ObjectArrayVariableResolver<Effect>("effects"));

		resolver.ResolveArray(context).Should().Equal(burn, chill);
	}

	[Fact]
	[Trait("Resolver", "ObjectDistinct")]
	public void Object_distinct_resolver_returns_empty_array_for_missing_variable()
	{
		var resolver = new ObjectDistinctResolver<IForgeEntity>(new EntityArrayVariableResolver("missing"));

		resolver.ResolveArray(new GraphContext()).Should().BeEmpty();
	}

	private static Effect CreateInstantEffect(string name)
	{
		return new Effect(
			new EffectData(name, new DurationData(DurationType.Instant)),
			new EffectOwnership(null, null));
	}
}
