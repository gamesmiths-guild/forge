// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class ObjectOrderByResolverTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Resolver", "ObjectOrderBy")]
	public void Object_order_by_resolver_sorts_entities_by_attribute_key()
	{
		var entity1 = new VitalTestEntity(_tagsManager, _cuesManager);
		var entity2 = new VitalTestEntity(_tagsManager, _cuesManager);
		var entity3 = new VitalTestEntity(_tagsManager, _cuesManager);
		entity1.VitalAttributeSet.UpdateBaseHealth(40);
		entity2.VitalAttributeSet.UpdateBaseHealth(10);
		entity3.VitalAttributeSet.UpdateBaseHealth(30);

		var context = new GraphContext();
		context.GraphVariables.DefineObjectArrayVariable<IForgeEntity>("targets", [entity1, entity2, entity3]);

		var resolver = new ObjectOrderByResolver<IForgeEntity>(
			new EntityArrayVariableResolver("targets"),
			new AttributeResolver("VitalAttributeSet.CurrentHealth", new ElementEntityResolver()));

		IForgeEntity[] result = resolver.ResolveArray(context);

		result.Should().Equal(entity2, entity3, entity1);
	}

	[Fact]
	[Trait("Resolver", "ObjectOrderBy")]
	public void Object_order_by_resolver_sorts_entities_descending_when_configured()
	{
		var entity1 = new VitalTestEntity(_tagsManager, _cuesManager);
		var entity2 = new VitalTestEntity(_tagsManager, _cuesManager);
		entity1.VitalAttributeSet.UpdateBaseHealth(10);
		entity2.VitalAttributeSet.UpdateBaseHealth(40);

		var context = new GraphContext();
		context.GraphVariables.DefineObjectArrayVariable<IForgeEntity>("targets", [entity1, entity2]);

		var resolver = new ObjectOrderByResolver<IForgeEntity>(
			new EntityArrayVariableResolver("targets"),
			new AttributeResolver("VitalAttributeSet.CurrentHealth", new ElementEntityResolver()),
			SortDirection.Descending);

		IForgeEntity[] result = resolver.ResolveArray(context);

		result.Should().Equal(entity2, entity1);
	}

	[Fact]
	[Trait("Resolver", "ObjectOrderBy")]
	public void Object_order_by_resolver_keeps_original_order_for_equal_keys()
	{
		var entity1 = new VitalTestEntity(_tagsManager, _cuesManager);
		var entity2 = new VitalTestEntity(_tagsManager, _cuesManager);
		var entity3 = new VitalTestEntity(_tagsManager, _cuesManager);
		entity1.VitalAttributeSet.UpdateBaseHealth(50);
		entity2.VitalAttributeSet.UpdateBaseHealth(50);
		entity3.VitalAttributeSet.UpdateBaseHealth(10);

		var context = new GraphContext();
		context.GraphVariables.DefineObjectArrayVariable<IForgeEntity>("targets", [entity1, entity2, entity3]);

		var resolver = new ObjectOrderByResolver<IForgeEntity>(
			new EntityArrayVariableResolver("targets"),
			new AttributeResolver("VitalAttributeSet.CurrentHealth", new ElementEntityResolver()));

		IForgeEntity[] result = resolver.ResolveArray(context);

		result.Should().Equal(entity3, entity1, entity2);
	}
}
