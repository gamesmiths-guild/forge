// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class SelectResolverTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Resolver", "Select")]
	public void Select_resolver_projects_each_value_element()
	{
		var context = new GraphContext();
		context.GraphVariables.DefineArrayVariable(
			"numbers",
			[new Variant128(3), new Variant128(1), new Variant128(2)]);
		var source = new ArrayVariableResolver("numbers", typeof(int));

		var resolver = new SelectResolver(
			source,
			new MultiplyResolver(
				new ElementValueResolver(typeof(int)),
				new VariantResolver(new Variant128(2), typeof(int))));

		Variant128[] result = resolver.ResolveArray(context);

		result.Should().HaveCount(3);
		result[0].AsInt().Should().Be(6);
		result[1].AsInt().Should().Be(2);
		result[2].AsInt().Should().Be(4);
	}

	[Fact]
	[Trait("Resolver", "Select")]
	public void Select_resolver_projects_object_elements_into_values()
	{
		var entity1 = new VitalTestEntity(_tagsManager, _cuesManager);
		var entity2 = new VitalTestEntity(_tagsManager, _cuesManager);
		entity1.VitalAttributeSet.UpdateBaseHealth(40);
		entity2.VitalAttributeSet.UpdateBaseHealth(70);

		var context = new GraphContext();
		context.GraphVariables.DefineObjectArrayVariable<IForgeEntity>("targets", [entity1, entity2]);

		var resolver = new SelectResolver(
			new EntityArrayVariableResolver("targets"),
			new AttributeResolver("VitalAttributeSet.CurrentHealth", new ElementEntityResolver()));

		Variant128[] result = resolver.ResolveArray(context);

		result.Should().HaveCount(2);
		result[0].AsInt().Should().Be(40);
		result[1].AsInt().Should().Be(70);
	}

	[Fact]
	[Trait("Resolver", "Select")]
	public void Select_resolver_reports_the_projection_value_type()
	{
		var resolver = new SelectResolver(
			new ArrayVariableResolver("numbers", typeof(int)),
			new ComparisonResolver(
				new ElementValueResolver(typeof(int)),
				ComparisonOperation.GreaterThan,
				new VariantResolver(new Variant128(1), typeof(int))));

		resolver.ElementType.Should().Be(typeof(bool));
	}
}
