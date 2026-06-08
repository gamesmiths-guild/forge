// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class EffectArrayVariableResolverTests
{
	[Fact]
	[Trait("Resolver", "EffectArrayVariable")]
	public void Effect_array_variable_resolver_reads_graph_reference_array()
	{
		Effect effect1 = CreateEffect("Burn");
		Effect effect2 = CreateEffect("Slow");
		var context = new GraphContext();
		var resolver = new EffectArrayVariableResolver("effects");

		context.GraphVariables.DefineObjectArrayVariable("effects", [effect1, effect2]);

		resolver.ResolveArray(context).Should().Equal(effect1, effect2);
	}

	[Fact]
	[Trait("Resolver", "EffectArrayVariable")]
	public void Effect_array_variable_resolver_reads_shared_reference_array()
	{
		Effect effect1 = CreateEffect("Burn");
		Effect effect2 = CreateEffect("Slow");
		var sharedVariables = new Variables();
		var context = new GraphContext { SharedVariables = sharedVariables };
		var resolver = new EffectArrayVariableResolver("effects", VariableScope.Shared);

		sharedVariables.DefineObjectArrayVariable("effects", [effect1, effect2]);

		resolver.ResolveArray(context).Should().Equal(effect1, effect2);
	}

	[Fact]
	[Trait("Resolver", "EffectArrayVariable")]
	public void Effect_array_variable_resolver_returns_empty_array_for_missing_variable()
	{
		var resolver = new EffectArrayVariableResolver("missing");

		resolver.ResolveArray(new GraphContext()).Should().BeEmpty();
	}

	private static Effect CreateEffect(string name)
	{
		return new Effect(
			new EffectData(name, new DurationData(DurationType.Instant)),
			new EffectOwnership(null, null));
	}
}
