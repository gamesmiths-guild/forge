// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

using static Gamesmiths.Forge.Tests.Helpers.ResolverTestContextFactory;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class ActivationDataResolverTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Resolver", "ActivationData")]
	public void Activation_data_resolver_value_type_matches_property_type()
	{
		var resolver = new ActivationDataResolver(typeof(DamageData), nameof(DamageData.Multiplier));

		resolver.ValueType.Should().Be(typeof(double));
	}

	[Fact]
	[Trait("Resolver", "ActivationData")]
	public void Activation_data_resolver_reads_public_property()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var resolver = new ActivationDataResolver(typeof(DamageData), nameof(DamageData.Amount));
		GraphContext context = CreateAbilityGraphContext(entity, new DamageData(42, 1.5));

		Variant128 result = resolver.Resolve(context);

		result.AsInt().Should().Be(42);
	}

	[Fact]
	[Trait("Resolver", "ActivationData")]
	public void Activation_data_resolver_reads_public_field()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var resolver = new ActivationDataResolver(typeof(ChargeData), nameof(ChargeData.IsCharged));
		GraphContext context = CreateAbilityGraphContext(entity, new ChargeData { IsCharged = true });

		Variant128 result = resolver.Resolve(context);

		result.AsBool().Should().BeTrue();
	}

	[Fact]
	[Trait("Resolver", "ActivationData")]
	public void Activation_data_resolver_returns_default_when_no_activation_context()
	{
		var resolver = new ActivationDataResolver(typeof(DamageData), nameof(DamageData.Amount));

		Variant128 result = resolver.Resolve(new GraphContext());

		result.AsInt().Should().Be(0);
	}

	[Fact]
	[Trait("Resolver", "ActivationData")]
	public void Activation_data_resolver_returns_default_for_mismatched_activation_data_type()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var resolver = new ActivationDataResolver(typeof(DamageData), nameof(DamageData.Amount));
		GraphContext context = CreateAbilityGraphContext(entity, new ChargeData { IsCharged = true });

		Variant128 result = resolver.Resolve(context);

		result.AsInt().Should().Be(0);
	}

	[Fact]
	[Trait("Resolver", "ActivationData")]
	public void Activation_data_resolver_throws_for_missing_member()
	{
#pragma warning disable CA1806 // Constructor invocation is the assertion target
		Action act = () => new ActivationDataResolver(typeof(DamageData), "MissingValue");
#pragma warning restore CA1806 // Constructor invocation is the assertion target

		act.Should().Throw<ArgumentException>()
			.WithMessage("*MissingValue*");
	}

	[Fact]
	[Trait("Resolver", "ActivationData")]
	public void Activation_data_resolver_throws_for_unsupported_member_type()
	{
#pragma warning disable CA1806 // Constructor invocation is the assertion target
		Action act = () => new ActivationDataResolver(typeof(UnsupportedData), nameof(UnsupportedData.Name));
#pragma warning restore CA1806 // Constructor invocation is the assertion target

		act.Should().Throw<ArgumentException>()
			.WithMessage("*does not support member type*");
	}

	private readonly record struct DamageData(int Amount, double Multiplier);

	private sealed class ChargeData
	{
#pragma warning disable SA1401 // Public field is required to test public-field binding
		public bool IsCharged;
#pragma warning restore SA1401 // Public field is required to test public-field binding
	}

	private sealed class UnsupportedData
	{
		public string Name { get; init; } = string.Empty;
	}
}
