// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Nodes.Action;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

using static Gamesmiths.Forge.Tests.Helpers.NodeBindings;

namespace Gamesmiths.Forge.Tests.Statescript.Nodes.Action;

public class SetVariableNodeTests
{
	[Fact]
	[Trait("Graph", "SetVariable")]
	public void Set_variable_node_copies_value_from_source_to_target()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("source", 42);
		graph.VariableDefinitions.DefineVariable("target", 0);

		SetVariableNode setNode = CreateSetVariableNode("source", "target");
		var readNode = new ReadVariableNode<int>("target");

		graph.AddNode(setNode);
		graph.AddNode(readNode);

		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			setNode.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			setNode.OutputPorts[ActionNode.OutputPort],
			readNode.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
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
		SetVariableNode setNode = CreateSetVariableNode("counter", "result");
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

		var processor = new GraphProcessor(graph);
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

		SetVariableNode setNode = CreateSetVariableNode("source", "target");
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

		var processor = new GraphProcessor(graph);
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

		SetVariableNode setNode = CreateSetVariableNode("nonexistent", "target");
		var readNode = new ReadVariableNode<int>("target");

		graph.AddNode(setNode);
		graph.AddNode(readNode);

		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			setNode.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			setNode.OutputPorts[ActionNode.OutputPort],
			readNode.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
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

		SetVariableNode setNode = CreateSetVariableNode("duration", "cachedDuration");
		var readNode = new ReadVariableNode<double>("cachedDuration");

		graph.AddNode(setNode);
		graph.AddNode(readNode);

		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			setNode.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			setNode.OutputPorts[ActionNode.OutputPort],
			readNode.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
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

		SetVariableNode setNode = CreateSetVariableNode("flag", "copy");
		var readNode = new ReadVariableNode<bool>("copy");

		graph.AddNode(setNode);
		graph.AddNode(readNode);

		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			setNode.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			setNode.OutputPorts[ActionNode.OutputPort],
			readNode.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		readNode.LastReadValue.Should().BeTrue();
	}

	[Fact]
	[Trait("Graph", "SetVariable")]
	public void Set_variable_node_copies_reference_values_from_source_to_target()
	{
		TestEntity entity = CreateTestEntity();
		var graph = new Graph();
		graph.VariableDefinitions.DefineReferenceVariable<IForgeEntity>("source", entity);
		graph.VariableDefinitions.DefineReferenceVariable<IForgeEntity>("target");

		SetVariableNode setNode = CreateSetVariableNode("source", "target");
		var readNode = new ReadReferencePropertyNode<IForgeEntity>();
		readNode.BindInput(0, "target");

		graph.AddNode(setNode);
		graph.AddNode(readNode);

		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			setNode.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			setNode.OutputPorts[ActionNode.OutputPort],
			readNode.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		readNode.LastReadValue.Should().BeSameAs(entity);
	}

	[Fact]
	[Trait("Graph", "SetVariable")]
	public void Set_variable_node_copies_reference_arrays_from_source_to_target()
	{
		TestEntity entity1 = CreateTestEntity();
		TestEntity entity2 = CreateTestEntity();
		var graph = new Graph();
		graph.VariableDefinitions.DefineReferenceArrayVariable<IForgeEntity>("source", entity1, entity2);
		graph.VariableDefinitions.DefineReferenceArrayVariable<IForgeEntity>("target");

		SetVariableNode setNode = CreateSetVariableNode("source", "target");
		var readNode = new ReadReferenceArrayPropertyNode<IForgeEntity>();
		readNode.BindInput(0, "target");

		graph.AddNode(setNode);
		graph.AddNode(readNode);

		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			setNode.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			setNode.OutputPorts[ActionNode.OutputPort],
			readNode.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		readNode.LastReadArray.Should().Equal(entity1, entity2);
	}

	[Fact]
	[Trait("Graph", "SetVariable")]
	public void Set_variable_node_writes_reference_values_to_shared_variables()
	{
		TestEntity entity = CreateTestEntity();
		var graph = new Graph();
		graph.VariableDefinitions.DefineReferenceVariable<IForgeEntity>("source", entity);

		SetVariableNode setNode = CreateSetVariableNode("source", "sharedTarget", VariableScope.Shared);

		graph.AddNode(setNode);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			setNode.InputPorts[ActionNode.InputPort]));

		var sharedVariables = new Variables();
		sharedVariables.DefineReferenceVariable<IForgeEntity>("sharedTarget");

		var processor = new GraphProcessor(graph, sharedVariables);
		processor.StartGraph();

		sharedVariables.TryGetReference("sharedTarget", out IForgeEntity? result).Should().BeTrue();
		result.Should().BeSameAs(entity);
	}

	[Fact]
	[Trait("Graph", "SetVariable")]
	public void Set_variable_node_rebinds_shared_target_when_processor_uses_new_shared_variables()
	{
		TestEntity entity1 = CreateTestEntity();
		TestEntity entity2 = CreateTestEntity();
		var graph = new Graph();
		graph.VariableDefinitions.DefineReferenceVariable<IForgeEntity>("source");

		SetVariableNode setNode = CreateSetVariableNode("source", "sharedTarget", VariableScope.Shared);
		graph.AddNode(setNode);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			setNode.InputPorts[ActionNode.InputPort]));

		var sharedVariables1 = new Variables();
		sharedVariables1.DefineReferenceVariable<IForgeEntity>("sharedTarget");

		var processor = new GraphProcessor(graph, sharedVariables1);
		processor.StartGraph(variables => variables.SetReference("source", entity1));

		sharedVariables1.TryGetReference("sharedTarget", out IForgeEntity? firstResult).Should().BeTrue();
		firstResult.Should().BeSameAs(entity1);

		var sharedVariables2 = new Variables();
		sharedVariables2.DefineReferenceVariable<IForgeEntity>("sharedTarget");
		processor.GraphContext.SharedVariables = sharedVariables2;

		processor.StartGraph(variables => variables.SetReference("source", entity2));

		sharedVariables2.TryGetReference("sharedTarget", out IForgeEntity? secondResult).Should().BeTrue();
		secondResult.Should().BeSameAs(entity2);
		sharedVariables1.TryGetReference("sharedTarget", out IForgeEntity? firstRunValue).Should().BeTrue();
		firstRunValue.Should().BeSameAs(entity1);
	}

	[Fact]
	[Trait("Graph", "SetVariable")]
	public void Two_processors_using_set_variable_have_independent_state()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("source", 10);
		graph.VariableDefinitions.DefineVariable("target", 0);

		var incrementNode = new IncrementCounterNode("source");
		SetVariableNode setNode = CreateSetVariableNode("source", "target");

		graph.AddNode(incrementNode);
		graph.AddNode(setNode);

		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			incrementNode.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			incrementNode.OutputPorts[ActionNode.OutputPort],
			setNode.InputPorts[ActionNode.InputPort]));

		var processor1 = new GraphProcessor(graph);
		var processor2 = new GraphProcessor(graph);

		processor1.StartGraph();
		processor2.StartGraph();

		processor1.GraphContext.GraphVariables.TryGetVar("target", out int value1);
		processor2.GraphContext.GraphVariables.TryGetVar("target", out int value2);

		value1.Should().Be(11);
		value2.Should().Be(11);
	}

	private static TestEntity CreateTestEntity()
	{
		return new TestEntity(
			new TagsManager(["enemy.undead.zombie", "color.green"]),
			new CuesManager());
	}
}
