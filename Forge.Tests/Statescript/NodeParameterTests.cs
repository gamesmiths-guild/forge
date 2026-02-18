// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Nodes.Action;
using Gamesmiths.Forge.Statescript.Nodes.Condition;
using Gamesmiths.Forge.Statescript.Nodes.State;
using Gamesmiths.Forge.Tests.Helpers;

using static Gamesmiths.Forge.Tests.Helpers.NodeBindings;

namespace Gamesmiths.Forge.Tests.Statescript;

public class NodeParameterTests
{
	[Fact]
	[Trait("Parameters", "Schema")]
	public void TimerNode_declares_one_input_property_of_type_double()
	{
		var node = new TimerNode();

		node.InputProperties.Should().ContainSingle();
		node.InputProperties[0].Label.Should().Be("Duration");
		node.InputProperties[0].ExpectedType.Should().Be(typeof(double));
		node.OutputVariables.Should().BeEmpty();
	}

	[Fact]
	[Trait("Parameters", "Schema")]
	public void ExpressionNode_declares_one_input_property_of_type_bool()
	{
		var node = new ExpressionNode();

		node.InputProperties.Should().ContainSingle();
		node.InputProperties[0].Label.Should().Be("Condition");
		node.InputProperties[0].ExpectedType.Should().Be(typeof(bool));
		node.OutputVariables.Should().BeEmpty();
	}

	[Fact]
	[Trait("Parameters", "Schema")]
	public void SetVariableNode_declares_one_input_and_one_output()
	{
		var node = new SetVariableNode();

		node.InputProperties.Should().ContainSingle();
		node.InputProperties[0].Label.Should().Be("Source");
		node.InputProperties[0].ExpectedType.Should().Be(typeof(Variant128));

		node.OutputVariables.Should().ContainSingle();
		node.OutputVariables[0].Label.Should().Be("Target");
		node.OutputVariables[0].ValueType.Should().Be(typeof(Variant128));
		node.OutputVariables[0].Scope.Should().Be(VariableScope.Graph);
	}

	[Fact]
	[Trait("Parameters", "Schema")]
	public void TrackingActionNode_declares_no_parameters()
	{
		var node = new TrackingActionNode();

		node.InputProperties.Should().BeEmpty();
		node.OutputVariables.Should().BeEmpty();
	}

	[Fact]
	[Trait("Parameters", "Schema")]
	public void EntryNode_declares_no_parameters()
	{
		var node = new EntryNode();

		node.InputProperties.Should().BeEmpty();
		node.OutputVariables.Should().BeEmpty();
	}

	[Fact]
	[Trait("Parameters", "Schema")]
	public void ExitNode_declares_no_parameters()
	{
		var node = new ExitNode();

		node.InputProperties.Should().BeEmpty();
		node.OutputVariables.Should().BeEmpty();
	}

	[Fact]
	[Trait("Parameters", "Binding")]
	public void BindInput_sets_bound_name()
	{
		var node = new TimerNode();

		node.BindInput(TimerNode.DurationInput, "myDuration");

		node.InputProperties[TimerNode.DurationInput].BoundName.Should().Be("myDuration");
	}

	[Fact]
	[Trait("Parameters", "Binding")]
	public void BindOutput_sets_bound_name()
	{
		var node = new SetVariableNode();

		node.BindOutput(SetVariableNode.TargetOutput, "myTarget");

		node.OutputVariables[SetVariableNode.TargetOutput].BoundName.Should().Be("myTarget");
	}

	[Fact]
	[Trait("Parameters", "Binding")]
	public void BindOutput_with_scope_overrides_default_scope()
	{
		var node = new SetVariableNode();

		node.BindOutput(SetVariableNode.TargetOutput, "sharedVar", VariableScope.Shared);

		node.OutputVariables[SetVariableNode.TargetOutput].BoundName.Should().Be("sharedVar");
		node.OutputVariables[SetVariableNode.TargetOutput].Scope.Should().Be(VariableScope.Shared);
	}

	[Fact]
	[Trait("Parameters", "Binding")]
	public void BindInput_throws_for_out_of_range_index()
	{
		var node = new TimerNode();

		Action act = () => node.BindInput(5, "invalid");

		act.Should().Throw<ArgumentOutOfRangeException>();
	}

	[Fact]
	[Trait("Parameters", "Binding")]
	public void BindOutput_throws_for_out_of_range_index()
	{
		var node = new SetVariableNode();

		Action act = () => node.BindOutput(5, "invalid");

		act.Should().Throw<ArgumentOutOfRangeException>();
	}

	[Fact]
	[Trait("Parameters", "Binding")]
	public void BindInput_throws_for_node_with_no_inputs()
	{
		var node = new TrackingActionNode();

		Action act = () => node.BindInput(0, "anything");

		act.Should().Throw<ArgumentOutOfRangeException>();
	}

	[Fact]
	[Trait("Parameters", "Runtime")]
	public void SetVariableNode_writes_to_shared_variables_when_scope_is_shared()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("source", 42);

		SetVariableNode setNode = CreateSetVariableNode("source", "sharedTarget", VariableScope.Shared);

		graph.AddNode(setNode);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			setNode.InputPorts[ActionNode.InputPort]));

		var sharedVars = new Variables();
		sharedVars.DefineVariable("sharedTarget", 0);

		var processor = new GraphProcessor(graph, sharedVars);
		processor.StartGraph();

		sharedVars.TryGetVar("sharedTarget", out int result).Should().BeTrue();
		result.Should().Be(42);
	}

	[Fact]
	[Trait("Parameters", "Runtime")]
	public void Bound_timer_node_works_end_to_end()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("dur", 3.0);

		TimerNode timer = CreateTimerNode("dur");
		graph.AddNode(timer);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			timer.InputPorts[StateNode<TimerNodeContext>.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		processor.GraphContext.IsActive.Should().BeTrue();

		processor.UpdateGraph(3.0);

		processor.GraphContext.IsActive.Should().BeFalse();
	}
}
