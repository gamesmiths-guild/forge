// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class IsValidResolverTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Resolver", "IsValid")]
	public void Is_valid_resolver_returns_true_for_set_object_variable()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var context = new GraphContext();
		context.GraphVariables.DefineObjectVariable<IForgeEntity>("target", entity);

		var resolver = new IsValidResolver(new EntityVariableResolver("target"));

		resolver.Resolve(context).AsBool().Should().BeTrue();
	}

	[Fact]
	[Trait("Resolver", "IsValid")]
	public void Is_valid_resolver_returns_false_for_null_object_variable()
	{
		var context = new GraphContext();
		context.GraphVariables.DefineObjectVariable<IForgeEntity>("target");

		var resolver = new IsValidResolver(new EntityVariableResolver("target"));

		resolver.Resolve(context).AsBool().Should().BeFalse();
	}

	[Fact]
	[Trait("Resolver", "IsValid")]
	public void Is_valid_resolver_returns_false_for_missing_object_variable()
	{
		var resolver = new IsValidResolver(new EntityVariableResolver("missing"));

		resolver.Resolve(new GraphContext()).AsBool().Should().BeFalse();
	}

	[Fact]
	[Trait("Resolver", "IsValid")]
	public void Is_valid_resolver_composes_with_not_for_null_checks()
	{
		var resolver = new NotResolver(new IsValidResolver(new EntityVariableResolver("missing")));

		resolver.Resolve(new GraphContext()).AsBool().Should().BeTrue();
	}

	[Fact]
	[Trait("Resolver", "IsValid")]
	public void Is_valid_resolver_returns_true_for_set_effect_variable()
	{
		var context = new GraphContext();
		context.GraphVariables.DefineObjectVariable("storedEffect", CreateEffect());

		var resolver = new IsValidResolver(new EffectVariableResolver("storedEffect"));

		resolver.Resolve(context).AsBool().Should().BeTrue();
	}

	[Fact]
	[Trait("Resolver", "IsValid")]
	public void Is_valid_resolver_returns_false_for_null_effect_variable()
	{
		var context = new GraphContext();
		context.GraphVariables.DefineObjectVariable<Effect>("storedEffect");

		var resolver = new IsValidResolver(new EffectVariableResolver("storedEffect"));

		resolver.Resolve(context).AsBool().Should().BeFalse();
	}

	[Fact]
	[Trait("Resolver", "IsValid")]
	public void Is_valid_resolver_works_with_any_object_type()
	{
		var context = new GraphContext();
		context.GraphVariables.DefineObjectVariable("payload", new object());

		var resolver = new IsValidResolver(new ObjectVariableResolver<object>("payload"));

		resolver.Resolve(context).AsBool().Should().BeTrue();
	}

	private static Effect CreateEffect()
	{
		return new Effect(
			new EffectData("Burn", new DurationData(DurationType.Instant)),
			new EffectOwnership(null, null));
	}
}
