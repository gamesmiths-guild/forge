// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Effects.Modifiers;
using Gamesmiths.Forge.Effects.Periodic;
using Gamesmiths.Forge.Effects.Stacking;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class ActiveEffectResolverTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private const string TargetAttribute = "TestAttributeSet.Attribute90";

	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Resolver", "ActiveEffectData")]
	public void Active_effect_data_resolver_reads_durations()
	{
		TestEntity target = CreateEntity();
		ActiveEffectHandle handle = Apply(target, CreateDurationEffectData(10f))!;

		var context = new GraphContext();
		context.GraphVariables.DefineObjectVariable("handle", handle);
		var handleResolver = new ObjectVariableResolver<ActiveEffectHandle>("handle");

		target.EffectsManager.UpdateEffects(4);

		new ActiveEffectDataResolver(handleResolver, ActiveEffectDataType.RemainingDuration)
			.Resolve(context).Get<double>().Should().BeApproximately(6, 0.001);
		new ActiveEffectDataResolver(handleResolver, ActiveEffectDataType.TotalDuration)
			.Resolve(context).Get<double>().Should().BeApproximately(10, 0.001);
		new ActiveEffectDataResolver(handleResolver, ActiveEffectDataType.RemainingFraction)
			.Resolve(context).Get<double>().Should().BeApproximately(0.6, 0.001);
	}

	[Fact]
	[Trait("Resolver", "ActiveEffectData")]
	public void Active_effect_data_resolver_reads_stack_count_and_level()
	{
		TestEntity owner = CreateEntity();
		TestEntity target = CreateEntity();
		EffectData effectData = CreateStackableEffectData();

		target.EffectsManager.ApplyEffect(new Effect(effectData, new EffectOwnership(owner, owner), 3));
		ActiveEffectHandle handle =
			target.EffectsManager.ApplyEffect(new Effect(effectData, new EffectOwnership(owner, owner), 3))!;

		var context = new GraphContext();
		context.GraphVariables.DefineObjectVariable("handle", handle);
		var handleResolver = new ObjectVariableResolver<ActiveEffectHandle>("handle");

		new ActiveEffectDataResolver(handleResolver, ActiveEffectDataType.StackCount)
			.Resolve(context).Get<int>().Should().Be(2);
		new ActiveEffectDataResolver(handleResolver, ActiveEffectDataType.Level)
			.Resolve(context).Get<int>().Should().Be(3);
	}

	[Fact]
	[Trait("Resolver", "ActiveEffectData")]
	public void Active_effect_data_resolver_reads_periodic_data_and_states()
	{
		TestEntity target = CreateEntity();
		ActiveEffectHandle handle = Apply(target, CreatePeriodicEffectData(10f, 1f))!;

		var context = new GraphContext();
		context.GraphVariables.DefineObjectVariable("handle", handle);
		var handleResolver = new ObjectVariableResolver<ActiveEffectHandle>("handle");

		new ActiveEffectDataResolver(handleResolver, ActiveEffectDataType.Period)
			.Resolve(context).Get<double>().Should().BeApproximately(1, 0.001);
		new ActiveEffectDataResolver(handleResolver, ActiveEffectDataType.ExecutionCount)
			.Resolve(context).Get<int>().Should().Be(1);
		new ActiveEffectDataResolver(handleResolver, ActiveEffectDataType.IsValid)
			.Resolve(context).AsBool().Should().BeTrue();
		new ActiveEffectDataResolver(handleResolver, ActiveEffectDataType.IsInhibited)
			.Resolve(context).AsBool().Should().BeFalse();

		handle.SetInhibit(true);

		new ActiveEffectDataResolver(handleResolver, ActiveEffectDataType.IsInhibited)
			.Resolve(context).AsBool().Should().BeTrue();
	}

	[Fact]
	[Trait("Resolver", "ActiveEffectData")]
	public void Active_effect_data_resolver_returns_defaults_for_missing_handles()
	{
		var context = new GraphContext();
		context.GraphVariables.DefineObjectVariable<ActiveEffectHandle>("handle");
		var handleResolver = new ObjectVariableResolver<ActiveEffectHandle>("handle");

		new ActiveEffectDataResolver(handleResolver, ActiveEffectDataType.RemainingDuration)
			.Resolve(context).Get<double>().Should().Be(0);
		new ActiveEffectDataResolver(handleResolver, ActiveEffectDataType.StackCount)
			.Resolve(context).Get<int>().Should().Be(0);
		new ActiveEffectDataResolver(handleResolver, ActiveEffectDataType.IsValid)
			.Resolve(context).AsBool().Should().BeFalse();
	}

	[Fact]
	[Trait("Resolver", "ActiveEffectTarget")]
	public void Active_effect_target_and_effect_resolvers_read_the_handle()
	{
		TestEntity owner = CreateEntity();
		TestEntity target = CreateEntity();
		var effect = new Effect(CreateDurationEffectData(10f), new EffectOwnership(owner, owner));
		ActiveEffectHandle handle = target.EffectsManager.ApplyEffect(effect)!;

		var context = new GraphContext();
		context.GraphVariables.DefineObjectVariable("handle", handle);
		var handleResolver = new ObjectVariableResolver<ActiveEffectHandle>("handle");

		new ActiveEffectTargetResolver(handleResolver).Resolve(context).Should().Be(target);
		new ActiveEffectEffectResolver(handleResolver).Resolve(context).Should().BeSameAs(effect);

		target.EffectsManager.RemoveEffect(handle, forceRemoval: true);

		new ActiveEffectTargetResolver(handleResolver).Resolve(context).Should().BeNull();
		new ActiveEffectEffectResolver(handleResolver).Resolve(context).Should().BeNull();
	}

	[Fact]
	[Trait("Resolver", "QueryActiveEffects")]
	public void Query_active_effects_resolver_returns_matching_handles()
	{
		TestEntity target = CreateEntity();
		EffectData buffData = CreateDurationEffectData(10f);
		EffectData otherData = CreatePeriodicEffectData(10f, 1f);

		ActiveEffectHandle firstHandle = Apply(target, buffData)!;
		ActiveEffectHandle secondHandle = Apply(target, buffData)!;
		Apply(target, otherData);

		var context = new GraphContext();
		context.GraphVariables.DefineObjectVariable<IForgeEntity>("entity", target);
		var entityResolver = new EntityVariableResolver("entity");

		new QueryActiveEffectsResolver(new EffectQuery(EffectDefinition: buffData), entityResolver)
			.ResolveArray(context).Should().BeEquivalentTo([firstHandle, secondHandle]);
		new QueryActiveEffectsResolver(default, entityResolver)
			.ResolveArray(context).Should().HaveCount(3);
	}

	[Fact]
	[Trait("Resolver", "QueryActiveEffects")]
	public void Query_active_effects_resolver_returns_empty_without_an_entity()
	{
		var context = new GraphContext();
		context.GraphVariables.DefineObjectVariable<IForgeEntity>("entity");
		var entityResolver = new EntityVariableResolver("entity");

		new QueryActiveEffectsResolver(default, entityResolver)
			.ResolveArray(context).Should().BeEmpty();
	}

	[Fact]
	[Trait("Resolver", "EffectStackData")]
	public void Effect_stack_data_resolver_aggregates_stack_data()
	{
		TestEntity owner = CreateEntity();
		TestEntity target = CreateEntity();
		EffectData effectData = CreateStackableEffectData();

		target.EffectsManager.ApplyEffect(new Effect(effectData, new EffectOwnership(owner, owner), 2));
		target.EffectsManager.ApplyEffect(new Effect(effectData, new EffectOwnership(owner, owner), 2));

		var context = new GraphContext();
		context.GraphVariables.DefineObjectVariable<IForgeEntity>("entity", target);
		var entityResolver = new EntityVariableResolver("entity");

		new EffectStackDataResolver(effectData, EffectStackDataType.TotalStackCount, entityResolver)
			.Resolve(context).Get<int>().Should().Be(2);
		new EffectStackDataResolver(effectData, EffectStackDataType.InstanceCount, entityResolver)
			.Resolve(context).Get<int>().Should().Be(1);
		new EffectStackDataResolver(effectData, EffectStackDataType.MaxLevel, entityResolver)
			.Resolve(context).Get<int>().Should().Be(2);
	}

	[Fact]
	[Trait("Resolver", "SetByCallerMagnitude")]
	public void Set_by_caller_magnitude_resolver_reads_stored_values()
	{
		TestEntity owner = CreateEntity();
		var identifierTag = Tag.RequestTag(_tagsManager, "color.red");
		var effect = new Effect(CreateDurationEffectData(10f), new EffectOwnership(owner, owner));

		var context = new GraphContext();
		context.GraphVariables.DefineObjectVariable("effect", effect);
		var effectResolver = new ObjectVariableResolver<Effect>("effect");

		var resolver = new SetByCallerMagnitudeResolver(effectResolver, identifierTag);

		resolver.Resolve(context).Get<float>().Should().Be(0f);

		effect.SetSetByCallerMagnitude(identifierTag, 7.5f);

		resolver.Resolve(context).Get<float>().Should().Be(7.5f);
	}

	private static EffectData CreateDurationEffectData(float duration)
	{
		return new EffectData(
			"Buff",
			new DurationData(
				DurationType.HasDuration,
				new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(duration))),
			[
				new Modifier(
					TargetAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(5)))
			]);
	}

	private static EffectData CreatePeriodicEffectData(float duration, float period)
	{
		return new EffectData(
			"Periodic Buff",
			new DurationData(
				DurationType.HasDuration,
				new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(duration))),
			[
				new Modifier(
					TargetAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(5)))
			],
			periodicData: new PeriodicData(new ScalableFloat(period), true, PeriodInhibitionRemovedPolicy.NeverReset));
	}

	private static EffectData CreateStackableEffectData()
	{
		return new EffectData(
			"Stackable Buff",
			new DurationData(DurationType.Infinite),
			[
				new Modifier(
					TargetAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(5)))
			],
			new StackingData(
				new ScalableInt(3),
				new ScalableInt(1),
				StackPolicy.AggregateByTarget,
				StackLevelPolicy.SegregateLevels,
				StackMagnitudePolicy.Sum,
				StackOverflowPolicy.AllowApplication,
				StackExpirationPolicy.ClearEntireStack,
				StackOwnerDenialPolicy.AlwaysAllow,
				StackOwnerOverridePolicy.Override,
				StackOwnerOverrideStackCountPolicy.IncreaseStacks));
	}

	private TestEntity CreateEntity()
	{
		return new TestEntity(_tagsManager, _cuesManager);
	}

	private ActiveEffectHandle? Apply(TestEntity target, EffectData effectData)
	{
		TestEntity owner = CreateEntity();
		return target.EffectsManager.ApplyEffect(new Effect(effectData, new EffectOwnership(owner, owner)));
	}
}
