// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

using static Gamesmiths.Forge.Tests.Helpers.ResolverTestContextFactory;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class MagnitudeResolverTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Resolver", "Magnitude")]
	public void Magnitude_resolver_value_type_is_float()
	{
		var resolver = new MagnitudeResolver();

		resolver.ValueType.Should().Be(typeof(float));
	}

	[Fact]
	[Trait("Resolver", "Magnitude")]
	public void Magnitude_resolver_returns_default_when_no_activation_context()
	{
		var resolver = new MagnitudeResolver();

		var context = new GraphContext();

		Variant128 result = resolver.Resolve(context);

		result.AsFloat().Should().Be(0f);
	}

	[Fact]
	[Trait("Resolver", "Magnitude")]
	public void Magnitude_resolver_returns_zero_magnitude()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var resolver = new MagnitudeResolver();

		GraphContext context = CreateAbilityGraphContext(entity);

		Variant128 result = resolver.Resolve(context);

		result.AsFloat().Should().Be(0f);
	}

	[Fact]
	[Trait("Resolver", "Magnitude")]
	public void Magnitude_resolver_returns_magnitude_from_activation_context()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var resolver = new MagnitudeResolver();

		GraphContext context = CreateAbilityGraphContext(entity, magnitude: 42.5f);

		Variant128 result = resolver.Resolve(context);

		result.AsFloat().Should().Be(42.5f);
	}

	[Fact]
	[Trait("Resolver", "Magnitude")]
	public void Magnitude_resolver_returns_different_magnitudes_for_different_activations()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var resolver = new MagnitudeResolver();

		GraphContext context1 = CreateAbilityGraphContext(entity, magnitude: 10f);
		GraphContext context2 = CreateAbilityGraphContext(entity, magnitude: 99.9f);

		resolver.Resolve(context1).AsFloat().Should().Be(10f);
		resolver.Resolve(context2).AsFloat().Should().Be(99.9f);
	}
}
