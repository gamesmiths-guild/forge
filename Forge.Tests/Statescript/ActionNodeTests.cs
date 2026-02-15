// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Nodes.Action;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript;

public class ActionNodeTests
{
	[Fact]
	[Trait("Graph", "SetVariable")]
	public void Set_variable_node_copies_value_from_source_to_target()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("source", 42);
		graph.VariableDefinitions.DefineVariable("target", 0);

		var setNode = new SetVariableNode("source", "target");
		var readNode = new ReadVariableNode<int>("target");

		graph.AddNode(setNode);
		graph.AddNode(readNode);

		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			setNode.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			setNode.OutputPorts[ActionNode.OutputPort],
			readNode.InputPorts[ActionNode.InputPort]));

		var context = new TestGraphContext();
		var processor = new GraphProcessor(graph, context);
		processor.StartGraph();

		readNode.LastReadValue.Should().Be(42);
	}

	[Fact]
	[Trait("Graph", "SetVariable")]
	public void Set_variable_node_copies_value_between_different_variables()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("counter", 0);
		graph.VariableDefinitions.DefineVariable("result", 0);

		var incrementNode = new IncrementCounterNode("counter");
		var setNode = new SetVariableNode("counter", "result");
		var readNode = new ReadVariableNode<int>("result");

		graph.AddNode(incrementNode);
		graph.AddNode(setNode);
		graph.AddNode(readNode);

		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			incrementNode.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			incrementNode.OutputPorts[ActionNode.OutputPort],
			setNode.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			setNode.OutputPorts[ActionNode.OutputPort],
			readNode.InputPorts[ActionNode.InputPort]));

		var context = new TestGraphContext();
		var processor = new GraphProcessor(graph, context);
		processor.StartGraph();

		readNode.LastReadValue.Should().Be(1);
	}

	[Fact]
	[Trait("Graph", "SetVariable")]
	public void Set_variable_node_does_not_modify_source()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("source", 99);
		graph.VariableDefinitions.DefineVariable("target", 0);

		var setNode = new SetVariableNode("source", "target");
		var readSource = new ReadVariableNode<int>("source");
		var readTarget = new ReadVariableNode<int>("target");

		graph.AddNode(setNode);
		graph.AddNode(readTarget);
		graph.AddNode(readSource);

		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			setNode.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			setNode.OutputPorts[ActionNode.OutputPort],
			readTarget.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			readTarget.OutputPorts[ActionNode.OutputPort],
			readSource.InputPorts[ActionNode.InputPort]));

		var context = new TestGraphContext();
		var processor = new GraphProcessor(graph, context);
		processor.StartGraph();

		readTarget.LastReadValue.Should().Be(99);
		readSource.LastReadValue.Should().Be(99);
	}

	[Fact]
	[Trait("Graph", "SetVariable")]
	public void Set_variable_node_with_nonexistent_source_does_not_modify_target()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("target", 77);

		var setNode = new SetVariableNode("nonexistent", "target");
		var readNode = new ReadVariableNode<int>("target");

		graph.AddNode(setNode);
		graph.AddNode(readNode);

		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			setNode.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			setNode.OutputPorts[ActionNode.OutputPort],
			readNode.InputPorts[ActionNode.InputPort]));

		var context = new TestGraphContext();
		var processor = new GraphProcessor(graph, context);
		processor.StartGraph();

		readNode.LastReadValue.Should().Be(77);
	}

	[Fact]
	[Trait("Graph", "SetVariable")]
	public void Set_variable_node_works_with_double_values()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("duration", 3.5);
		graph.VariableDefinitions.DefineVariable("cachedDuration", 0.0);

		var setNode = new SetVariableNode("duration", "cachedDuration");
		var readNode = new ReadVariableNode<double>("cachedDuration");

		graph.AddNode(setNode);
		graph.AddNode(readNode);

		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			setNode.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			setNode.OutputPorts[ActionNode.OutputPort],
			readNode.InputPorts[ActionNode.InputPort]));

		var context = new TestGraphContext();
		var processor = new GraphProcessor(graph, context);
		processor.StartGraph();

		readNode.LastReadValue.Should().Be(3.5);
	}

	[Fact]
	[Trait("Graph", "SetVariable")]
	public void Set_variable_node_works_with_bool_values()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("flag", true);
		graph.VariableDefinitions.DefineVariable("copy", false);

		var setNode = new SetVariableNode("flag", "copy");
		var readNode = new ReadVariableNode<bool>("copy");

		graph.AddNode(setNode);
		graph.AddNode(readNode);

		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			setNode.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			setNode.OutputPorts[ActionNode.OutputPort],
			readNode.InputPorts[ActionNode.InputPort]));

		var context = new TestGraphContext();
		var processor = new GraphProcessor(graph, context);
		processor.StartGraph();

		readNode.LastReadValue.Should().BeTrue();
	}

	[Fact]
	[Trait("Graph", "SetVariable")]
	public void Two_processors_using_set_variable_have_independent_state()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("source", 10);
		graph.VariableDefinitions.DefineVariable("target", 0);

		var incrementNode = new IncrementCounterNode("source");
		var setNode = new SetVariableNode("source", "target");

		graph.AddNode(incrementNode);
		graph.AddNode(setNode);

		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			incrementNode.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			incrementNode.OutputPorts[ActionNode.OutputPort],
			setNode.InputPorts[ActionNode.InputPort]));

		var context1 = new TestGraphContext();
		var processor1 = new GraphProcessor(graph, context1);

		var context2 = new TestGraphContext();
		var processor2 = new GraphProcessor(graph, context2);

		processor1.StartGraph();
		processor2.StartGraph();

		context1.GraphVariables.TryGetVar("target", out int value1);
		context2.GraphVariables.TryGetVar("target", out int value2);

		value1.Should().Be(11);
		value2.Should().Be(11);
	}
}
