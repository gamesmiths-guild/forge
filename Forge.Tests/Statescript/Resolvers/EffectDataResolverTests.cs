// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Resolvers;

public class EffectDataResolverTests
{
	[Fact]
	[Trait("Resolver", "EffectData")]
	public void Effect_data_resolver_supports_struct_typed_resolution()
	{
		var effectData = new EffectData("Burn", new DurationData(DurationType.Instant));
		var node = new ResolveObjectResolverNode<EffectData>(new EffectDataResolver(effectData));
		var graph = new Graph();

		graph.AddNode(node);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			node.InputPorts[ActionNode.InputPort]));

		new GraphProcessor(graph).StartGraph();

		node.LastResolvedValue.Should().Be(effectData);
	}

	[Fact]
	[Trait("Resolver", "EffectData")]
	public void Effect_data_array_resolver_supports_struct_typed_resolution()
	{
		EffectData[] effectData =
		[
			new EffectData("Burn", new DurationData(DurationType.Instant)),
			new EffectData("Slow", new DurationData(DurationType.Infinite)),
		];
		var node = new ResolveObjectArrayResolverNode<EffectData>(
			new EffectDataArrayResolver(effectData));
		var graph = new Graph();

		graph.AddNode(node);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			node.InputPorts[ActionNode.InputPort]));

		new GraphProcessor(graph).StartGraph();

		node.LastResolvedArray.Should().Equal(effectData);
	}
}
