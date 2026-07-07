// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Effects.Modifiers;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class ObjectFirstResolverTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Resolver", "ObjectFirst")]
	public void Object_first_resolver_reads_the_first_element()
	{
		var entity1 = new TestEntity(_tagsManager, _cuesManager);
		var entity2 = new TestEntity(_tagsManager, _cuesManager);
		var context = new GraphContext();
		context.GraphVariables.DefineObjectArrayVariable<IForgeEntity>("targets", [entity1, entity2]);
		var source = new EntityArrayVariableResolver("targets");

		var resolver = new ObjectFirstResolver<IForgeEntity>(source);

		resolver.Resolve(context).Should().BeSameAs(entity1);
	}

	[Fact]
	[Trait("Resolver", "ObjectFirst")]
	public void Object_first_resolver_reads_the_first_effect()
	{
		Effect burn = CreateInstantEffect("Burn");
		Effect chill = CreateInstantEffect("Chill");
		var context = new GraphContext();
		context.GraphVariables.DefineObjectArrayVariable("effects", [burn, chill]);

		var resolver = new ObjectFirstResolver<Effect>(new ObjectArrayVariableResolver<Effect>("effects"));

		resolver.Resolve(context).Should().BeSameAs(burn);
	}

	[Fact]
	[Trait("Resolver", "ObjectFirst")]
	public void Object_first_resolver_reads_the_first_active_effect_handle()
	{
		var entity = new VitalTestEntity(_tagsManager, _cuesManager);
		ActiveEffectHandle first = ApplyInfiniteEffect(entity, "Regen");
		ActiveEffectHandle second = ApplyInfiniteEffect(entity, "Haste");
		var context = new GraphContext();
		context.GraphVariables.DefineObjectArrayVariable("handles", [first, second]);

		var resolver = new ObjectFirstResolver<ActiveEffectHandle>(
			new ObjectArrayVariableResolver<ActiveEffectHandle>("handles"));

		resolver.Resolve(context).Should().BeSameAs(first);
	}

	[Fact]
	[Trait("Resolver", "ObjectFirst")]
	public void Object_first_resolver_returns_null_for_empty_array()
	{
		var resolver = new ObjectFirstResolver<IForgeEntity>(new EntityArrayVariableResolver("missing"));

		resolver.Resolve(new GraphContext()).Should().BeNull();
	}

	private static Effect CreateInstantEffect(string name)
	{
		return new Effect(
			new EffectData(name, new DurationData(DurationType.Instant)),
			new EffectOwnership(null, null));
	}

	private static ActiveEffectHandle ApplyInfiniteEffect(VitalTestEntity entity, string name)
	{
		var effectData = new EffectData(
			name,
			new DurationData(DurationType.Infinite),
			[
				new Modifier(
					"VitalAttributeSet.CurrentHealth",
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(1))),
			]);

		ActiveEffectHandle? handle = entity.EffectsManager.ApplyEffect(
			new Effect(effectData, new EffectOwnership(entity, entity)));
		handle.Should().NotBeNull();
		return handle!;
	}
}
