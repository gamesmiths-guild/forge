// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Components;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Effects.Modifiers;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Nodes.Action;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class EffectQueryResolverTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private const string TargetAttribute = "TestAttributeSet.Attribute90";

	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Resolver", "ActiveEffectTagQuery")]
	public void Active_effect_tag_query_resolver_reads_each_tag_source()
	{
		TestEntity target = CreateEntity();
		ActiveEffectHandle handle = Apply(
			target,
			CreateEffectData("Hex", effectTagKeys: ["color.red"], grantedTagKeys: ["color.blue"]))!;

		var context = new GraphContext();
		context.GraphVariables.DefineObjectVariable("handle", handle);
		var handleResolver = new ObjectVariableResolver<ActiveEffectHandle>("handle");

		var redTag = Tag.RequestTag(_tagsManager, "color.red");
		var blueTag = Tag.RequestTag(_tagsManager, "color.blue");

		new ActiveEffectTagQueryResolver(handleResolver, redTag, EffectTagSource.EffectTags)
			.Resolve(context).AsBool().Should().BeTrue();
		new ActiveEffectTagQueryResolver(handleResolver, blueTag, EffectTagSource.EffectTags)
			.Resolve(context).AsBool().Should().BeFalse();

		new ActiveEffectTagQueryResolver(handleResolver, blueTag, EffectTagSource.GrantedTags)
			.Resolve(context).AsBool().Should().BeTrue();
		new ActiveEffectTagQueryResolver(handleResolver, redTag, EffectTagSource.GrantedTags)
			.Resolve(context).AsBool().Should().BeFalse();

		// OwningTags is the default and sees both sides.
		new ActiveEffectTagQueryResolver(handleResolver, redTag)
			.Resolve(context).AsBool().Should().BeTrue();
		new ActiveEffectTagQueryResolver(handleResolver, blueTag)
			.Resolve(context).AsBool().Should().BeTrue();
	}

	[Fact]
	[Trait("Resolver", "ActiveEffectTagQuery")]
	public void Active_effect_tag_query_resolver_accepts_query_expressions()
	{
		TestEntity target = CreateEntity();
		ActiveEffectHandle handle = Apply(target, CreateEffectData("Poison", effectTagKeys: ["color.dark.red"]))!;

		var context = new GraphContext();
		context.GraphVariables.DefineObjectVariable("handle", handle);
		var handleResolver = new ObjectVariableResolver<ActiveEffectHandle>("handle");

		TagQueryExpression expression = new TagQueryExpression(_tagsManager)
			.AnyTagsMatch()
			.AddTag("color.dark");

		new ActiveEffectTagQueryResolver(handleResolver, expression, EffectTagSource.EffectTags)
			.Resolve(context).AsBool().Should().BeTrue();
	}

	[Fact]
	[Trait("Resolver", "ActiveEffectTagQuery")]
	public void Active_effect_tag_query_resolver_returns_false_for_missing_handles()
	{
		var context = new GraphContext();
		context.GraphVariables.DefineObjectVariable<ActiveEffectHandle>("handle");
		var handleResolver = new ObjectVariableResolver<ActiveEffectHandle>("handle");

		new ActiveEffectTagQueryResolver(handleResolver, Tag.RequestTag(_tagsManager, "color.red"))
			.Resolve(context).AsBool().Should().BeFalse();
	}

	[Fact]
	[Trait("Resolver", "EffectQueryMatch")]
	public void Effect_query_match_resolver_evaluates_the_whole_query()
	{
		TestEntity target = CreateEntity();
		EffectData poisonData = CreateEffectData("Poison", effectTagKeys: ["color.red"]);
		ActiveEffectHandle handle = Apply(target, poisonData)!;

		var context = new GraphContext();
		context.GraphVariables.DefineObjectVariable("handle", handle);
		var handleResolver = new ObjectVariableResolver<ActiveEffectHandle>("handle");

		new EffectQueryMatchResolver(
			handleResolver,
			new EffectQuery(EffectDefinition: poisonData, ModifyingAttribute: TargetAttribute))
			.Resolve(context).AsBool().Should().BeTrue();

		new EffectQueryMatchResolver(handleResolver, new EffectQuery(EffectSource: CreateEntity()))
			.Resolve(context).AsBool().Should().BeFalse();
	}

	[Fact]
	[Trait("Resolver", "EffectQueryMatch")]
	public void Effect_query_match_resolver_returns_false_for_missing_handles()
	{
		var context = new GraphContext();
		context.GraphVariables.DefineObjectVariable<ActiveEffectHandle>("handle");

		new EffectQueryMatchResolver(
			new ObjectVariableResolver<ActiveEffectHandle>("handle"),
			default)
			.Resolve(context).AsBool().Should().BeFalse();
	}

	[Fact]
	[Trait("Resolver", "ActiveEffectSource")]
	public void Active_effect_source_and_owner_resolvers_read_the_handle()
	{
		TestEntity owner = CreateEntity();
		TestEntity source = CreateEntity();
		TestEntity target = CreateEntity();

		ActiveEffectHandle handle = target.EffectsManager.ApplyEffect(
			new Effect(CreateEffectData("Poison"), new EffectOwnership(owner, source)))!;

		var context = new GraphContext();
		context.GraphVariables.DefineObjectVariable("handle", handle);
		var handleResolver = new ObjectVariableResolver<ActiveEffectHandle>("handle");

		new ActiveEffectSourceResolver(handleResolver).Resolve(context).Should().Be(source);
		new ActiveEffectOwnerResolver(handleResolver).Resolve(context).Should().Be(owner);

		target.EffectsManager.RemoveEffect(handle, forceRemoval: true);

		new ActiveEffectSourceResolver(handleResolver).Resolve(context).Should().BeNull();
		new ActiveEffectOwnerResolver(handleResolver).Resolve(context).Should().BeNull();
	}

	[Fact]
	[Trait("Resolver", "QueryActiveEffects")]
	public void Query_active_effects_resolver_filters_by_effect_query()
	{
		TestEntity target = CreateEntity();
		ActiveEffectHandle poisonHandle = Apply(target, CreateEffectData("Poison", effectTagKeys: ["color.red"]))!;
		Apply(target, CreateEffectData("Curse", effectTagKeys: ["color.blue"]));

		var context = new GraphContext();
		context.GraphVariables.DefineObjectVariable<IForgeEntity>("entity", target);
		var entityResolver = new EntityVariableResolver("entity");

		var query = new EffectQuery(
			EffectTagQuery: TagQuery.MakeQueryMatchAnyTags(MakeContainer("color.red")));

		new QueryActiveEffectsResolver(query, entityResolver)
			.ResolveArray(context).Should().Equal(poisonHandle);

		new QueryActiveEffectsResolver(default, entityResolver)
			.ResolveArray(context).Should().HaveCount(2);
	}

	[Fact]
	[Trait("Graph", "Dispel")]
	public void Query_filter_and_remove_dispels_exactly_the_matching_effects()
	{
		TestEntity target = CreateEntity();

		ActiveEffectHandle curseHandle = Apply(target, CreateEffectData("Curse", effectTagKeys: ["color.dark"]))!;
		ActiveEffectHandle hexHandle = Apply(target, CreateEffectData("Hex", effectTagKeys: ["color.dark.red"]))!;
		ActiveEffectHandle blessingHandle = Apply(target, CreateEffectData("Blessing", effectTagKeys: ["color.blue"]))!;
		ActiveEffectHandle plainHandle = Apply(target, CreateEffectData("Plain"))!;

		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("entity", target);

		// QueryActiveEffects -> ObjectWhere(ActiveEffectTagQuery) -> RemoveEffect, with no new nodes involved.
		var dispelResolver = new ObjectWhereResolver<ActiveEffectHandle>(
			new QueryActiveEffectsResolver(default, new EntityVariableResolver("entity")),
			new ActiveEffectTagQueryResolver(
				new ElementResolver<ActiveEffectHandle>(),
				Tag.RequestTag(_tagsManager, "color.dark"),
				EffectTagSource.EffectTags));

		graph.VariableDefinitions.DefineObjectArrayProperty("cursed", dispelResolver);

		var removeNode = new RemoveEffectNode(forceRemoval: true);
		removeNode.BindInput(RemoveEffectNode.HandleInput, "cursed");

		graph.AddNode(removeNode);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			removeNode.InputPorts[ActionNode.InputPort]));

		new GraphProcessor(graph).StartGraph();

		// "color.dark.red" is a child of "color.dark", so the hex goes with the curse.
		curseHandle.IsValid.Should().BeFalse();
		hexHandle.IsValid.Should().BeFalse();
		blessingHandle.IsValid.Should().BeTrue();
		plainHandle.IsValid.Should().BeTrue();

		target.EffectsManager.GetActiveEffects().Should().BeEquivalentTo([blessingHandle, plainHandle]);
	}

	private static ActiveEffectHandle? Apply(TestEntity target, EffectData effectData)
	{
		return target.EffectsManager.ApplyEffect(
			new Effect(effectData, new EffectOwnership(target, target)));
	}

	private TagContainer MakeContainer(params string[] tagKeys)
	{
		return new TagContainer(_tagsManager, TestUtils.StringToTag(_tagsManager, tagKeys));
	}

	private EffectData CreateEffectData(
		string name,
		string[]? effectTagKeys = null,
		string[]? grantedTagKeys = null)
	{
		IEffectComponent[]? components = grantedTagKeys is null
			? null
			: [new ModifierTagsEffectComponent(MakeContainer(grantedTagKeys))];

		return new EffectData(
			name,
			new DurationData(DurationType.Infinite),
			[
				new Modifier(
					TargetAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(5)))
			],
			effectComponents: components,
			effectTags: effectTagKeys is null ? null : MakeContainer(effectTagKeys));
	}

	private TestEntity CreateEntity()
	{
		return new TestEntity(_tagsManager, _cuesManager);
	}
}
