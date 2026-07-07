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

public class ObjectLastResolverTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Resolver", "ObjectLast")]
	public void Object_last_resolver_reads_the_last_element()
	{
		var entity1 = new TestEntity(_tagsManager, _cuesManager);
		var entity2 = new TestEntity(_tagsManager, _cuesManager);
		var context = new GraphContext();
		context.GraphVariables.DefineObjectArrayVariable<IForgeEntity>("targets", [entity1, entity2]);
		var source = new EntityArrayVariableResolver("targets");

		var resolver = new ObjectLastResolver<IForgeEntity>(source);

		resolver.Resolve(context).Should().BeSameAs(entity2);
	}

	[Fact]
	[Trait("Resolver", "ObjectLast")]
	public void Object_last_resolver_reads_the_last_effect()
	{
		Effect burn = CreateInstantEffect("Burn");
		Effect chill = CreateInstantEffect("Chill");
		var context = new GraphContext();
		context.GraphVariables.DefineObjectArrayVariable("effects", [burn, chill]);

		var resolver = new ObjectLastResolver<Effect>(new ObjectArrayVariableResolver<Effect>("effects"));

		resolver.Resolve(context).Should().BeSameAs(chill);
	}

	[Fact]
	[Trait("Resolver", "ObjectLast")]
	public void Object_last_resolver_reads_the_last_active_effect_handle()
	{
		var entity = new VitalTestEntity(_tagsManager, _cuesManager);
		ActiveEffectHandle first = ApplyInfiniteEffect(entity, "Regen");
		ActiveEffectHandle second = ApplyInfiniteEffect(entity, "Haste");
		var context = new GraphContext();
		context.GraphVariables.DefineObjectArrayVariable("handles", [first, second]);

		var resolver = new ObjectLastResolver<ActiveEffectHandle>(
			new ObjectArrayVariableResolver<ActiveEffectHandle>("handles"));

		resolver.Resolve(context).Should().BeSameAs(second);
	}

	[Fact]
	[Trait("Resolver", "ObjectLast")]
	public void Object_last_resolver_returns_null_for_empty_array()
	{
		var resolver = new ObjectLastResolver<IForgeEntity>(new EntityArrayVariableResolver("missing"));

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
