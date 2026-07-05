// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class AnyResolverTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Resolver", "Any")]
	public void Any_resolver_returns_true_for_non_empty_arrays_without_a_predicate()
	{
		var context = new GraphContext();
		context.GraphVariables.DefineArrayVariable("numbers", [new Variant128(3)]);

		var resolver = new AnyResolver(new ArrayVariableResolver("numbers", typeof(int)));

		resolver.Resolve(context).AsBool().Should().BeTrue();
	}

	[Fact]
	[Trait("Resolver", "Any")]
	public void Any_resolver_returns_false_for_empty_arrays()
	{
		var resolver = new AnyResolver(new ArrayVariableResolver("missing", typeof(int)));

		resolver.Resolve(new GraphContext()).AsBool().Should().BeFalse();
	}

	[Fact]
	[Trait("Resolver", "Any")]
	public void Any_resolver_tests_entities_against_the_predicate()
	{
		var entity1 = new VitalTestEntity(_tagsManager, _cuesManager);
		var entity2 = new VitalTestEntity(_tagsManager, _cuesManager);
		entity1.VitalAttributeSet.UpdateBaseHealth(10);
		entity2.VitalAttributeSet.UpdateBaseHealth(70);

		var context = new GraphContext();
		context.GraphVariables.DefineObjectArrayVariable<IForgeEntity>("targets", [entity1, entity2]);

		var resolver = new AnyResolver(
			new EntityArrayVariableResolver("targets"),
			new ComparisonResolver(
				new AttributeResolver("VitalAttributeSet.CurrentHealth", new ElementEntityResolver()),
				ComparisonOperation.GreaterThan,
				new VariantResolver(new Variant128(50), typeof(int))));

		resolver.Resolve(context).AsBool().Should().BeTrue();
	}

	[Fact]
	[Trait("Resolver", "Any")]
	public void Any_resolver_returns_false_when_no_element_matches_the_predicate()
	{
		var context = new GraphContext();
		context.GraphVariables.DefineArrayVariable("numbers", [new Variant128(1), new Variant128(2)]);

		var resolver = new AnyResolver(
			new ArrayVariableResolver("numbers", typeof(int)),
			new ComparisonResolver(
				new ElementValueResolver(typeof(int)),
				ComparisonOperation.GreaterThan,
				new VariantResolver(new Variant128(5), typeof(int))));

		resolver.Resolve(context).AsBool().Should().BeFalse();
	}
}
