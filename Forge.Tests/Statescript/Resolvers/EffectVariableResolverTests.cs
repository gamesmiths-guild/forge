// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class EffectVariableResolverTests
{
	[Fact]
	[Trait("Resolver", "EffectVariable")]
	public void Effect_variable_resolver_reads_graph_reference()
	{
		Effect effect = CreateEffect();
		var context = new GraphContext();
		var resolver = new EffectVariableResolver("effect");

		context.GraphVariables.DefineObjectVariable("effect", effect);

		resolver.Resolve(context).Should().BeSameAs(effect);
	}

	[Fact]
	[Trait("Resolver", "EffectVariable")]
	public void Effect_variable_resolver_reads_shared_reference()
	{
		Effect effect = CreateEffect();
		var sharedVariables = new Variables();
		var context = new GraphContext { SharedVariables = sharedVariables };
		var resolver = new EffectVariableResolver("effect", VariableScope.Shared);

		sharedVariables.DefineObjectVariable("effect", effect);

		resolver.Resolve(context).Should().BeSameAs(effect);
	}

	[Fact]
	[Trait("Resolver", "EffectVariable")]
	public void Effect_variable_resolver_returns_null_for_missing_variable()
	{
		var resolver = new EffectVariableResolver("missing");

		resolver.Resolve(new GraphContext()).Should().BeNull();
	}

	private static Effect CreateEffect()
	{
		return new Effect(
			new EffectData("Burn", new DurationData(DurationType.Instant)),
			new EffectOwnership(null, null));
	}
}
