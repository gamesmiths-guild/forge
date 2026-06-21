// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using FluentAssertions;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Statescript.Providers;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class EffectContextDataResolverTests
{
	[Fact]
	[Trait("Resolver", "EffectContextData")]
	public void Resolver_wraps_provider_data_in_a_typed_application_context()
	{
		var context = new GraphContext();
		context.GraphVariables.DefineVariable("damage", 42);
		var resolver = new EffectContextDataResolver(new DamageContextProvider());

		EffectApplicationContext result = resolver.Resolve(context);

		result.Should().NotBeNull();
		result.TryGetData(out DamageContext? data).Should().BeTrue();
		data!.Damage.Should().Be(42);
	}

	[Fact]
	[Trait("Resolver", "EffectContextData")]
	public void Resolver_builds_data_from_the_current_graph_state_on_each_resolve()
	{
		var context = new GraphContext();
		context.GraphVariables.DefineVariable("damage", 10);
		var resolver = new EffectContextDataResolver(new DamageContextProvider());

		resolver.Resolve(context).TryGetData(out DamageContext? first).Should().BeTrue();
		first!.Damage.Should().Be(10);

		context.GraphVariables.SetVar("damage", 99);

		resolver.Resolve(context).TryGetData(out DamageContext? second).Should().BeTrue();
		second!.Damage.Should().Be(99);
	}

	[Fact]
	[Trait("Resolver", "EffectContextData")]
	public void Resolver_supplies_declared_input_values_to_the_provider()
	{
		var context = new GraphContext();
		var resolver = new EffectContextDataResolver(
			new DirectionContextProvider(),
			new Dictionary<string, IPropertyResolver>
			{
				["Strength"] = new VariantResolver(new Variant128(7), typeof(int)),
			});

		resolver.Resolve(context).TryGetData(out DirectionContext? data).Should().BeTrue();
		data!.Strength.Should().Be(7);
	}

	[Fact]
	[Trait("Resolver", "EffectContextData")]
	public void Resolver_uses_default_input_value_when_no_resolver_is_bound()
	{
		var context = new GraphContext();
		var resolver = new EffectContextDataResolver(new DirectionContextProvider());

		resolver.Resolve(context).TryGetData(out DirectionContext? data).Should().BeTrue();
		data!.Strength.Should().Be(0);
	}

	[Fact]
	[Trait("Resolver", "EffectContextData")]
	public void Resolver_value_type_is_the_application_context_base()
	{
		var resolver = new EffectContextDataResolver(new DamageContextProvider());

		resolver.ValueType.Should().Be(typeof(EffectApplicationContext));
	}

	private sealed record DamageContext(int Damage);

	private sealed record DirectionContext(int Strength);

	private sealed class DamageContextProvider : EffectContextDataProvider<DamageContext>
	{
		public override DamageContext CreateData(GraphContext graphContext, EffectContextDataInputs inputs)
		{
			graphContext.TryResolve("damage", out int damage);
			return new DamageContext(damage);
		}
	}

	private sealed class DirectionContextProvider : EffectContextDataProvider<DirectionContext>
	{
		public override IReadOnlyList<EffectContextDataInput> Inputs =>
			[new EffectContextDataInput("Strength", typeof(int))];

		public override DirectionContext CreateData(GraphContext graphContext, EffectContextDataInputs inputs)
		{
			return new DirectionContext(inputs.Get<int>("Strength"));
		}
	}
}
