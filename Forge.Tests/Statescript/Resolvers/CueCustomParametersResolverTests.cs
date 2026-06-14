// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class CueCustomParametersResolverTests
{
	private static readonly StringKey _damageKey = new("damage");
	private static readonly StringKey _strengthKey = new("strength");

	[Fact]
	[Trait("Resolver", "CueCustomParameters")]
	public void Resolver_builds_the_parameter_bag_from_the_provider()
	{
		var context = new GraphContext();
		context.GraphVariables.DefineVariable("damage", 42);
		var resolver = new CueCustomParametersResolver(new DamageParamsProvider());

		Dictionary<StringKey, object> result = resolver.Resolve(context);

		result.Should().ContainKey(_damageKey);
		result[_damageKey].Should().Be(42);
	}

	[Fact]
	[Trait("Resolver", "CueCustomParameters")]
	public void Resolver_builds_the_bag_from_the_current_graph_state_on_each_resolve()
	{
		var context = new GraphContext();
		context.GraphVariables.DefineVariable("damage", 10);
		var resolver = new CueCustomParametersResolver(new DamageParamsProvider());

		resolver.Resolve(context)[_damageKey].Should().Be(10);

		context.GraphVariables.SetVar("damage", 99);

		resolver.Resolve(context)[_damageKey].Should().Be(99);
	}

	[Fact]
	[Trait("Resolver", "CueCustomParameters")]
	public void Resolver_supplies_declared_input_values_to_the_provider()
	{
		var context = new GraphContext();
		var resolver = new CueCustomParametersResolver(
			new StrengthParamsProvider(),
			new Dictionary<string, IPropertyResolver>
			{
				["Strength"] = new VariantResolver(new Variant128(7), typeof(int)),
			});

		resolver.Resolve(context)[_strengthKey].Should().Be(7);
	}

	[Fact]
	[Trait("Resolver", "CueCustomParameters")]
	public void Resolver_uses_default_input_value_when_no_resolver_is_bound()
	{
		var context = new GraphContext();
		var resolver = new CueCustomParametersResolver(new StrengthParamsProvider());

		resolver.Resolve(context)[_strengthKey].Should().Be(0);
	}

	[Fact]
	[Trait("Resolver", "CueCustomParameters")]
	public void Resolver_value_type_is_the_custom_parameters_dictionary()
	{
		var resolver = new CueCustomParametersResolver(new DamageParamsProvider());

		resolver.ValueType.Should().Be(typeof(Dictionary<StringKey, object>));
	}

	private sealed class DamageParamsProvider : CueCustomParametersProvider
	{
		public override Dictionary<StringKey, object> CreateCustomParameters(
			GraphContext graphContext,
			CueCustomParameterInputs inputs)
		{
			graphContext.TryResolve("damage", out int damage);
			return new Dictionary<StringKey, object> { { _damageKey, damage } };
		}
	}

	private sealed class StrengthParamsProvider : CueCustomParametersProvider
	{
		public override IReadOnlyList<CueCustomParameterInput> Inputs =>
			[new CueCustomParameterInput("Strength", typeof(int))];

		public override Dictionary<StringKey, object> CreateCustomParameters(
			GraphContext graphContext,
			CueCustomParameterInputs inputs)
		{
			return new Dictionary<StringKey, object> { { _strengthKey, inputs.Get<int>("Strength") } };
		}
	}
}
