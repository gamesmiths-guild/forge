// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class ObjectWhereResolverTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Resolver", "ObjectWhere")]
	public void Object_where_resolver_keeps_only_entities_matching_the_predicate()
	{
		var entity1 = new VitalTestEntity(_tagsManager, _cuesManager);
		var entity2 = new VitalTestEntity(_tagsManager, _cuesManager);
		var entity3 = new VitalTestEntity(_tagsManager, _cuesManager);
		entity1.VitalAttributeSet.UpdateBaseHealth(40);
		entity2.VitalAttributeSet.UpdateBaseHealth(70);
		entity3.VitalAttributeSet.UpdateBaseHealth(10);

		var context = new GraphContext();
		context.GraphVariables.DefineObjectArrayVariable<IForgeEntity>("targets", [entity1, entity2, entity3]);

		var resolver = new ObjectWhereResolver<IForgeEntity>(
			new EntityArrayVariableResolver("targets"),
			new ComparisonResolver(
				new AttributeResolver("VitalAttributeSet.CurrentHealth", new ElementEntityResolver()),
				ComparisonOperation.GreaterThan,
				new VariantResolver(new Variant128(30), typeof(int))));

		IForgeEntity[] result = resolver.ResolveArray(context);

		result.Should().Equal(entity1, entity2);
	}

	[Fact]
	[Trait("Resolver", "ObjectWhere")]
	public void Object_where_resolver_returns_empty_array_for_missing_variable()
	{
		var resolver = new ObjectWhereResolver<IForgeEntity>(
			new EntityArrayVariableResolver("missing"),
			new IsValidResolver(new ElementResolver<IForgeEntity>()));

		resolver.ResolveArray(new GraphContext()).Should().BeEmpty();
	}

	[Fact]
	[Trait("Resolver", "ObjectWhere")]
	public void Object_where_resolver_rejects_non_boolean_predicates()
	{
		var source = new EntityArrayVariableResolver("targets");

		Action act = () => _ = new ObjectWhereResolver<IForgeEntity>(
			source,
			new VariantResolver(new Variant128(1), typeof(int)));

		act.Should().Throw<ArgumentException>();
	}
}
