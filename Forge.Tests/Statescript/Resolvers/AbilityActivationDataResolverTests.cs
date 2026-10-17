// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

using static Gamesmiths.Forge.Tests.Helpers.ResolverTestContextFactory;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class AbilityActivationDataResolverTests(TagsAndCuesFixture tagsAndCuesFixture)
	: IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Resolver", "ActivationData")]
	public void Activation_data_resolver_value_type_matches_property_type()
	{
		var resolver = new AbilityActivationDataResolver(typeof(DamageData), nameof(DamageData.Multiplier));

		resolver.ValueType.Should().Be(typeof(double));
	}

	[Fact]
	[Trait("Resolver", "ActivationData")]
	public void Activation_data_resolver_reads_public_property()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var resolver = new AbilityActivationDataResolver(typeof(DamageData), nameof(DamageData.Amount));
		GraphContext context = CreateAbilityGraphContext(entity, new DamageData(42, 1.5));

		Variant128 result = resolver.Resolve(context);

		result.AsInt().Should().Be(42);
	}

	[Fact]
	[Trait("Resolver", "ActivationData")]
	public void Activation_data_resolver_reads_public_field()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var resolver = new AbilityActivationDataResolver(typeof(ChargeData), nameof(ChargeData.IsCharged));
		GraphContext context = CreateAbilityGraphContext(entity, new ChargeData { IsCharged = true });

		Variant128 result = resolver.Resolve(context);

		result.AsBool().Should().BeTrue();
	}

	[Fact]
	[Trait("Resolver", "ActivationData")]
	public void Activation_data_resolver_returns_default_when_no_activation_context()
	{
		var resolver = new AbilityActivationDataResolver(typeof(DamageData), nameof(DamageData.Amount));

		Variant128 result = resolver.Resolve(new GraphContext());

		result.AsInt().Should().Be(0);
	}

	[Fact]
	[Trait("Resolver", "ActivationData")]
	public void Activation_data_resolver_returns_default_for_mismatched_activation_data_type()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var resolver = new AbilityActivationDataResolver(typeof(DamageData), nameof(DamageData.Amount));
		GraphContext context = CreateAbilityGraphContext(entity, new ChargeData { IsCharged = true });

		Variant128 result = resolver.Resolve(context);

		result.AsInt().Should().Be(0);
	}

	[Fact]
	[Trait("Resolver", "ActivationData")]
	public void Activation_data_resolver_throws_for_missing_member()
	{
#pragma warning disable CA1806 // Constructor invocation is the assertion target
		Action act = () => new AbilityActivationDataResolver(typeof(DamageData), "MissingValue");
#pragma warning restore CA1806 // Constructor invocation is the assertion target

		act.Should().Throw<ArgumentException>()
			.WithMessage("*MissingValue*");
	}

	[Fact]
	[Trait("Resolver", "ActivationData")]
	public void Activation_data_resolver_throws_for_unsupported_member_type()
	{
#pragma warning disable CA1806 // Constructor invocation is the assertion target
		Action act = () => new AbilityActivationDataResolver(typeof(UnsupportedData), nameof(UnsupportedData.Name));
#pragma warning restore CA1806 // Constructor invocation is the assertion target

		act.Should().Throw<ArgumentException>()
			.WithMessage("*does not support member type*of 'Name'*")
			.And.ParamName.Should().Be("memberName");
	}

	[Theory]
	[Trait("Resolver", "ActivationData")]
	[InlineData(nameof(UnreadableData.WriteOnly))]
	[InlineData(nameof(UnreadableData.PrivateGetter))]
	public void Activation_data_resolver_throws_for_member_without_public_getter(string memberName)
	{
#pragma warning disable CA1806 // Constructor invocation is the assertion target
		Action act = () => new AbilityActivationDataResolver(typeof(UnreadableData), memberName);
#pragma warning restore CA1806 // Constructor invocation is the assertion target

		act.Should().Throw<ArgumentException>()
			.WithMessage("*to be readable*")
			.And.ParamName.Should().Be("memberName");
	}

	[Fact]
	[Trait("Resolver", "ActivationData")]
	public void Activation_data_resolver_throws_for_indexer()
	{
#pragma warning disable CA1806 // Constructor invocation is the assertion target
		Action act = () => new AbilityActivationDataResolver(typeof(IndexedData), "Item");
#pragma warning restore CA1806 // Constructor invocation is the assertion target

		act.Should().Throw<ArgumentException>()
			.WithMessage("*could not find*'Item'*");
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

	private sealed class UnreadableData
	{
#pragma warning disable S2376 // Write-only property is required to test unreadable members
		public int WriteOnly
		{
			set => PrivateGetter = value;
		}
#pragma warning restore S2376 // Write-only property is required to test unreadable members

		public int PrivateGetter { private get; set; }
	}

	private sealed class IndexedData
	{
#pragma warning disable S1144 // Indexer is only reached through reflection
		public int this[int index] => index;
#pragma warning restore S1144 // Indexer is only reached through reflection
	}
}
