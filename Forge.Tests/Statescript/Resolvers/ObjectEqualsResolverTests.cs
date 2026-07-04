// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class ObjectEqualsResolverTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Resolver", "ObjectEquals")]
	public void Object_equals_resolver_returns_true_for_same_instance()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var context = new GraphContext();
		context.GraphVariables.DefineObjectVariable<IForgeEntity>("first", entity);
		context.GraphVariables.DefineObjectVariable<IForgeEntity>("second", entity);

		var resolver = new ObjectEqualsResolver(
			new EntityVariableResolver("first"),
			new EntityVariableResolver("second"));

		resolver.Resolve(context).AsBool().Should().BeTrue();
	}

	[Fact]
	[Trait("Resolver", "ObjectEquals")]
	public void Object_equals_resolver_returns_false_for_different_instances()
	{
		var context = new GraphContext();
		context.GraphVariables.DefineObjectVariable<IForgeEntity>(
			"first",
			new TestEntity(_tagsManager, _cuesManager));
		context.GraphVariables.DefineObjectVariable<IForgeEntity>(
			"second",
			new TestEntity(_tagsManager, _cuesManager));

		var resolver = new ObjectEqualsResolver(
			new EntityVariableResolver("first"),
			new EntityVariableResolver("second"));

		resolver.Resolve(context).AsBool().Should().BeFalse();
	}

	[Fact]
	[Trait("Resolver", "ObjectEquals")]
	public void Object_equals_resolver_returns_true_when_both_operands_are_null()
	{
		var resolver = new ObjectEqualsResolver(
			new EntityVariableResolver("missing"),
			new EntityVariableResolver("alsoMissing"));

		resolver.Resolve(new GraphContext()).AsBool().Should().BeTrue();
	}
}
