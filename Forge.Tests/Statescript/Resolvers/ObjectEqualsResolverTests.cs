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

public class ObjectEqualsResolverTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Resolver", "ObjectEquals")]
	public void Object_equals_resolver_returns_true_for_same_instance()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var context = new GraphContext();
		context.GraphVariables.DefineObjectVariable<IForgeEntity>("first", entity);
		context.GraphVariables.DefineObjectVariable<IForgeEntity>("second", entity);

		var resolver = new ObjectEqualsResolver(
			new EntityVariableResolver("first"),
			new EntityVariableResolver("second"));

		resolver.Resolve(context).AsBool().Should().BeTrue();
	}

	[Fact]
	[Trait("Resolver", "ObjectEquals")]
	public void Object_equals_resolver_returns_false_for_different_instances()
	{
		var context = new GraphContext();
		context.GraphVariables.DefineObjectVariable<IForgeEntity>(
			"first",
			new TestEntity(_tagsManager, _cuesManager));
		context.GraphVariables.DefineObjectVariable<IForgeEntity>(
			"second",
			new TestEntity(_tagsManager, _cuesManager));

		var resolver = new ObjectEqualsResolver(
			new EntityVariableResolver("first"),
			new EntityVariableResolver("second"));

		resolver.Resolve(context).AsBool().Should().BeFalse();
	}

	[Fact]
	[Trait("Resolver", "ObjectEquals")]
	public void Object_equals_resolver_returns_true_when_both_operands_are_null()
	{
		var resolver = new ObjectEqualsResolver(
			new EntityVariableResolver("missing"),
			new EntityVariableResolver("alsoMissing"));

		resolver.Resolve(new GraphContext()).AsBool().Should().BeTrue();
	}

	[Fact]
	[Trait("Resolver", "ObjectEquals")]
	public void Object_equals_resolver_returns_true_for_same_effect_instance()
	{
		Effect effect = CreateEffect();
		var context = new GraphContext();
		context.GraphVariables.DefineObjectVariable("appliedEffect", effect);
		context.GraphVariables.DefineObjectVariable("lastEffect", effect);

		var resolver = new ObjectEqualsResolver(
			new EffectVariableResolver("appliedEffect"),
			new EffectVariableResolver("lastEffect"));

		resolver.Resolve(context).AsBool().Should().BeTrue();
	}

	[Fact]
	[Trait("Resolver", "ObjectEquals")]
	public void Object_equals_resolver_returns_false_for_different_effect_instances()
	{
		var context = new GraphContext();
		context.GraphVariables.DefineObjectVariable("appliedEffect", CreateEffect());
		context.GraphVariables.DefineObjectVariable("lastEffect", CreateEffect());

		var resolver = new ObjectEqualsResolver(
			new EffectVariableResolver("appliedEffect"),
			new EffectVariableResolver("lastEffect"));

		resolver.Resolve(context).AsBool().Should().BeFalse();
	}

	[Fact]
	[Trait("Resolver", "ObjectEquals")]
	public void Object_equals_resolver_works_with_any_object_type()
	{
		object payload = new();
		var context = new GraphContext();
		context.GraphVariables.DefineObjectVariable("first", payload);
		context.GraphVariables.DefineObjectVariable("second", payload);

		var resolver = new ObjectEqualsResolver(
			new ObjectVariableResolver<object>("first"),
			new ObjectVariableResolver<object>("second"));

		resolver.Resolve(context).AsBool().Should().BeTrue();
	}

	private static Effect CreateEffect()
	{
		return new Effect(
			new EffectData("Burn", new DurationData(DurationType.Instant)),
			new EffectOwnership(null, null));
	}
}
