// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Nodes.Action;
using Gamesmiths.Forge.Statescript.Nodes.Condition;
using Gamesmiths.Forge.Statescript.Nodes.State;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Statescript.Providers;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Nodes;

public class GrantAbilityNodesTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Graph", "GrantAbility")]
	public void Grant_ability_node_grants_while_active_and_revokes_on_deactivation()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var abilityData = new AbilityData("Granted");

		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectVariable("abilityData", abilityData);
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("target", owner);
		graph.VariableDefinitions.DefineObjectVariable<AbilityHandle>("grantedAbility");

		var grantNode = new GrantAbilityNode();
		grantNode.BindInput(GrantAbilityNode.AbilityDataInput, "abilityData");
		grantNode.BindInput(GrantAbilityNode.EntityInput, "target");
		grantNode.BindOutput(GrantAbilityNode.AbilityOutput, "grantedAbility");

		graph.AddNode(grantNode);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			grantNode.InputPorts[StateNode<GrantAbilityNodeContext>.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		owner.Abilities.TryGetAbility(abilityData, out AbilityHandle? handle);
		handle.Should().NotBeNull();
		handle!.IsValid.Should().BeTrue();

		processor.GraphContext.GraphVariables.TryGetObject("grantedAbility", out object? outputHandle)
			.Should().BeTrue();
		outputHandle.Should().Be(handle);

		processor.StopGraph();

		owner.Abilities.TryGetAbility(abilityData, out AbilityHandle? removedHandle);
		removedHandle.Should().BeNull();
	}

	[Fact]
	[Trait("Graph", "GrantAbility")]
	public void Grant_ability_node_output_feeds_try_activate_ability_node()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);

		var grantedGraph = new Graph();
		var captureNode = new CaptureGraphContextNode();
		grantedGraph.AddNode(captureNode);
		grantedGraph.AddConnection(new Connection(
			grantedGraph.EntryNode.OutputPorts[EntryNode.OutputPort],
			captureNode.InputPorts[ActionNode.InputPort]));

		var abilityData = new AbilityData(
			"Granted Proc",
			behaviorFactory: () => new GraphAbilityBehavior(grantedGraph));

		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectVariable("abilityData", abilityData);
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("target", owner);
		graph.VariableDefinitions.DefineObjectVariable<AbilityHandle>("grantedAbility");

		var grantNode = new GrantAbilityNode();
		grantNode.BindInput(GrantAbilityNode.AbilityDataInput, "abilityData");
		grantNode.BindInput(GrantAbilityNode.EntityInput, "target");
		grantNode.BindOutput(GrantAbilityNode.AbilityOutput, "grantedAbility");

		var activateNode = new TryActivateAbilityNode();
		activateNode.BindInput(TryActivateAbilityNode.AbilityInput, "grantedAbility");

		graph.AddNode(grantNode);
		graph.AddNode(activateNode);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			grantNode.InputPorts[StateNode<GrantAbilityNodeContext>.InputPort]));
		graph.AddConnection(new Connection(
			grantNode.OutputPorts[StateNode<GrantAbilityNodeContext>.OnActivatePort],
			activateNode.InputPorts[ConditionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		captureNode.CapturedGraphContext.Should().NotBeNull();
	}

	[Fact]
	[Trait("Graph", "GrantAbility")]
	public void Grant_ability_permanently_node_grants_beyond_the_graph_lifetime()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var abilityData = new AbilityData("Unlocked");

		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectVariable("abilityData", abilityData);
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("target", owner);

		var grantNode = new GrantAbilityPermanentlyNode();
		grantNode.BindInput(GrantAbilityPermanentlyNode.AbilityDataInput, "abilityData");
		grantNode.BindInput(GrantAbilityPermanentlyNode.EntityInput, "target");

		graph.AddNode(grantNode);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			grantNode.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		processor.GraphContext.IsActive.Should().BeFalse();

		owner.Abilities.TryGetAbility(abilityData, out AbilityHandle? handle);
		handle.Should().NotBeNull();
		handle!.IsValid.Should().BeTrue();
	}

	[Fact]
	[Trait("Graph", "GrantAbility")]
	public void Try_activate_abilities_by_tag_node_routes_by_activation_result()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var abilityTag = Tag.RequestTag(_tagsManager, "simple.tag");

		var behaviorGraph = new Graph();
		var captureNode = new CaptureGraphContextNode();
		behaviorGraph.AddNode(captureNode);
		behaviorGraph.AddConnection(new Connection(
			behaviorGraph.EntryNode.OutputPorts[EntryNode.OutputPort],
			captureNode.InputPorts[ActionNode.InputPort]));

		var abilityData = new AbilityData(
			"Tagged",
			abilityTags: abilityTag.GetSingleTagContainer(),
			behaviorFactory: () => new GraphAbilityBehavior(behaviorGraph));

		owner.Abilities.GrantAbilityPermanently(abilityData, 1, LevelComparison.None, sourceEntity: null);

		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectVariable("tag", abilityTag);
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("entity", owner);

		var activateNode = new TryActivateAbilitiesByTagNode();
		activateNode.BindInput(TryActivateAbilitiesByTagNode.TagInput, "tag");
		activateNode.BindInput(TryActivateAbilitiesByTagNode.EntityInput, "entity");

		var onTrue = new TrackingActionNode();
		var onFalse = new TrackingActionNode();

		graph.AddNode(activateNode);
		graph.AddNode(onTrue);
		graph.AddNode(onFalse);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			activateNode.InputPorts[ConditionNode.InputPort]));
		graph.AddConnection(new Connection(
			activateNode.OutputPorts[ConditionNode.TruePort],
			onTrue.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			activateNode.OutputPorts[ConditionNode.FalsePort],
			onFalse.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		onTrue.ExecutionCount.Should().Be(1);
		onFalse.ExecutionCount.Should().Be(0);
		captureNode.CapturedGraphContext.Should().NotBeNull();
	}

	[Fact]
	[Trait("Graph", "GrantAbility")]
	public void Cancel_abilities_by_tag_node_cancels_matching_active_abilities()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var abilityTag = Tag.RequestTag(_tagsManager, "simple.tag");
		var endedAbilities = new List<AbilityEndedData>();
		owner.Abilities.OnAbilityEnded += endedAbilities.Add;

		var behaviorGraph = new Graph();
		behaviorGraph.VariableDefinitions.DefineVariable("duration", 100.0);
		TimerNode timer = NodeBindings.CreateTimerNode("duration");
		behaviorGraph.AddNode(timer);
		behaviorGraph.AddConnection(new Connection(
			behaviorGraph.EntryNode.OutputPorts[EntryNode.OutputPort],
			timer.InputPorts[StateNode<TimerNodeContext>.InputPort]));

		var abilityData = new AbilityData(
			"Channel",
			abilityTags: abilityTag.GetSingleTagContainer(),
			behaviorFactory: () => new GraphAbilityBehavior(behaviorGraph));

		AbilityHandle handle = owner.Abilities.GrantAbilityPermanently(
			abilityData,
			1,
			LevelComparison.None,
			sourceEntity: null);
		handle.Activate(out _).Should().BeTrue();
		handle.IsActive.Should().BeTrue();

		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectVariable("tag", abilityTag);
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("target", owner);

		var cancelNode = new CancelAbilitiesByTagNode();
		cancelNode.BindInput(CancelAbilitiesByTagNode.TagInput, "tag");
		cancelNode.BindInput(CancelAbilitiesByTagNode.TargetInput, "target");

		graph.AddNode(cancelNode);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			cancelNode.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		handle.IsActive.Should().BeFalse();
		endedAbilities.Should().ContainSingle().Which.WasCanceled.Should().BeTrue();
	}

	[Fact]
	[Trait("Graph", "GrantAbility")]
	public void Try_activate_ability_node_activates_through_a_handle()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);

		var behaviorGraph = new Graph();
		var captureNode = new CaptureGraphContextNode();
		behaviorGraph.AddNode(captureNode);
		behaviorGraph.AddConnection(new Connection(
			behaviorGraph.EntryNode.OutputPorts[EntryNode.OutputPort],
			captureNode.InputPorts[ActionNode.InputPort]));

		var abilityData = new AbilityData(
			"Handle Activated",
			behaviorFactory: () => new GraphAbilityBehavior(behaviorGraph));

		AbilityHandle handle = owner.Abilities.GrantAbilityPermanently(
			abilityData,
			1,
			LevelComparison.None,
			sourceEntity: null);

		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectVariable("ability", handle);

		var activateNode = new TryActivateAbilityNode();
		activateNode.BindInput(TryActivateAbilityNode.AbilityInput, "ability");

		var onTrue = new TrackingActionNode();

		graph.AddNode(activateNode);
		graph.AddNode(onTrue);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			activateNode.InputPorts[ConditionNode.InputPort]));
		graph.AddConnection(new Connection(
			activateNode.OutputPorts[ConditionNode.TruePort],
			onTrue.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		onTrue.ExecutionCount.Should().Be(1);
		captureNode.CapturedGraphContext.Should().NotBeNull();
	}

	[Fact]
	[Trait("Graph", "GrantAbility")]
	public void Cancel_ability_node_cancels_the_running_ability()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var endedAbilities = new List<AbilityEndedData>();
		owner.Abilities.OnAbilityEnded += endedAbilities.Add;

		var abilityGraph = new Graph();
		abilityGraph.VariableDefinitions.DefineVariable("delay", 1.0);
		TimerNode timer = NodeBindings.CreateTimerNode("delay");
		var cancelNode = new CancelAbilityNode();

		abilityGraph.AddNode(timer);
		abilityGraph.AddNode(cancelNode);
		abilityGraph.AddConnection(new Connection(
			abilityGraph.EntryNode.OutputPorts[EntryNode.OutputPort],
			timer.InputPorts[StateNode<TimerNodeContext>.InputPort]));
		abilityGraph.AddConnection(new Connection(
			timer.OutputPorts[TimerNode.OnTimerEndPort],
			cancelNode.InputPorts[ActionNode.InputPort]));

		var abilityData = new AbilityData(
			"Self Canceling",
			behaviorFactory: () => new GraphAbilityBehavior(abilityGraph));

		AbilityHandle handle = owner.Abilities.GrantAbilityPermanently(
			abilityData,
			1,
			LevelComparison.None,
			sourceEntity: null);
		handle.Activate(out _).Should().BeTrue();
		handle.IsActive.Should().BeTrue();

		owner.Abilities.UpdateAbilities(1.0);

		handle.IsActive.Should().BeFalse();
		endedAbilities.Should().ContainSingle().Which.WasCanceled.Should().BeTrue();
	}

	[Fact]
	[Trait("Graph", "GrantAbility")]
	public void Grant_ability_and_activate_once_node_procs_the_ability()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);

		var behaviorGraph = new Graph();
		var captureNode = new CaptureGraphContextNode();
		behaviorGraph.AddNode(captureNode);
		behaviorGraph.AddConnection(new Connection(
			behaviorGraph.EntryNode.OutputPorts[EntryNode.OutputPort],
			captureNode.InputPorts[ActionNode.InputPort]));

		var abilityData = new AbilityData(
			"Proc",
			behaviorFactory: () => new GraphAbilityBehavior(behaviorGraph));

		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectVariable("abilityData", abilityData);
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("entity", owner);

		var procNode = new GrantAbilityAndActivateOnceNode();
		procNode.BindInput(GrantAbilityAndActivateOnceNode.AbilityDataInput, "abilityData");
		procNode.BindInput(GrantAbilityAndActivateOnceNode.EntityInput, "entity");

		var onTrue = new TrackingActionNode();

		graph.AddNode(procNode);
		graph.AddNode(onTrue);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			procNode.InputPorts[ConditionNode.InputPort]));
		graph.AddConnection(new Connection(
			procNode.OutputPorts[ConditionNode.TruePort],
			onTrue.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		onTrue.ExecutionCount.Should().Be(1);
		captureNode.CapturedGraphContext.Should().NotBeNull();

		// Transient grant: the ability is removed after it ends.
		owner.Abilities.TryGetAbility(abilityData, out AbilityHandle? handle);
		handle.Should().BeNull();
	}

	[Fact]
	[Trait("Graph", "GrantAbility")]
	public void Try_activate_ability_node_passes_activation_data_to_the_ability()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var captured = new List<Shout>();

		AbilityHandle handle = owner.Abilities.GrantAbilityPermanently(
			new AbilityData("Shouter", behaviorFactory: () => new ShoutBehavior(captured)),
			1,
			LevelComparison.None,
			sourceEntity: null);

		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("volume", 8);
		graph.VariableDefinitions.DefineObjectVariable("ability", handle);
		graph.VariableDefinitions.DefineObjectProperty(
			"activationData",
			new AbilityActivatorResolver(new ShoutProvider()));

		var activateNode = new TryActivateAbilityNode();
		activateNode.BindInput(TryActivateAbilityNode.AbilityInput, "ability");
		activateNode.BindInput(TryActivateAbilityNode.ActivationDataInput, "activationData");

		var onTrue = new TrackingActionNode();

		graph.AddNode(activateNode);
		graph.AddNode(onTrue);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			activateNode.InputPorts[ConditionNode.InputPort]));
		graph.AddConnection(new Connection(
			activateNode.OutputPorts[ConditionNode.TruePort],
			onTrue.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		onTrue.ExecutionCount.Should().Be(1);
		captured.Should().ContainSingle().Which.Volume.Should().Be(8);
	}

	[Fact]
	[Trait("Graph", "GrantAbility")]
	public void Try_activate_abilities_by_tag_node_passes_activation_data_to_matching_abilities()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var abilityTag = Tag.RequestTag(_tagsManager, "simple.tag");
		TagContainer abilityTags = new(_tagsManager, [abilityTag]);

		var captured = new List<Shout>();
		int untypedStarts = 0;

		owner.Abilities.GrantAbilityPermanently(
			new AbilityData(
				"Typed",
				abilityTags: abilityTags,
				behaviorFactory: () => new ShoutBehavior(captured)),
			1,
			LevelComparison.None,
			sourceEntity: null);

		owner.Abilities.GrantAbilityPermanently(
			new AbilityData(
				"Untyped",
				abilityTags: abilityTags,
				behaviorFactory: () => new CountingBehavior(() => untypedStarts++)),
			1,
			LevelComparison.None,
			sourceEntity: null);

		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("volume", 4);
		graph.VariableDefinitions.DefineObjectVariable("tag", abilityTag);
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("entity", owner);
		graph.VariableDefinitions.DefineObjectProperty(
			"activationData",
			new AbilityActivatorResolver(new ShoutProvider()));

		var activateNode = new TryActivateAbilitiesByTagNode();
		activateNode.BindInput(TryActivateAbilitiesByTagNode.TagInput, "tag");
		activateNode.BindInput(TryActivateAbilitiesByTagNode.EntityInput, "entity");
		activateNode.BindInput(TryActivateAbilitiesByTagNode.ActivationDataInput, "activationData");

		var onTrue = new TrackingActionNode();

		graph.AddNode(activateNode);
		graph.AddNode(onTrue);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			activateNode.InputPorts[ConditionNode.InputPort]));
		graph.AddConnection(new Connection(
			activateNode.OutputPorts[ConditionNode.TruePort],
			onTrue.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		onTrue.ExecutionCount.Should().Be(1);

		// The matching ability got the data; the one that does not accept it still activated without it.
		captured.Should().ContainSingle().Which.Volume.Should().Be(4);
		untypedStarts.Should().Be(1);
	}

	[Fact]
	[Trait("Graph", "GrantAbility")]
	public void Grant_ability_and_activate_once_node_passes_activation_data_to_the_ability()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var captured = new List<Shout>();

		var abilityData = new AbilityData("Proc", behaviorFactory: () => new ShoutBehavior(captured));

		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("volume", 12);
		graph.VariableDefinitions.DefineObjectVariable("abilityData", abilityData);
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("entity", owner);
		graph.VariableDefinitions.DefineObjectProperty(
			"activationData",
			new AbilityActivatorResolver(new ShoutProvider()));

		var procNode = new GrantAbilityAndActivateOnceNode();
		procNode.BindInput(GrantAbilityAndActivateOnceNode.AbilityDataInput, "abilityData");
		procNode.BindInput(GrantAbilityAndActivateOnceNode.EntityInput, "entity");
		procNode.BindInput(GrantAbilityAndActivateOnceNode.ActivationDataInput, "activationData");

		var onTrue = new TrackingActionNode();

		graph.AddNode(procNode);
		graph.AddNode(onTrue);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			procNode.InputPorts[ConditionNode.InputPort]));
		graph.AddConnection(new Connection(
			procNode.OutputPorts[ConditionNode.TruePort],
			onTrue.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		onTrue.ExecutionCount.Should().Be(1);
		captured.Should().ContainSingle().Which.Volume.Should().Be(12);

		// Transient grant: the ability is removed after it ends.
		owner.Abilities.TryGetAbility(abilityData, out AbilityHandle? handle);
		handle.Should().BeNull();
	}

	private sealed record Shout(int Volume);

	private sealed class ShoutProvider : AbilityActivationDataProvider<Shout>
	{
		public override Shout CreateData(GraphContext graphContext, AbilityActivationDataInputs inputs)
		{
			graphContext.TryResolve("volume", out int volume);
			return new Shout(volume);
		}
	}

	private sealed class ShoutBehavior(List<Shout> captured) : IAbilityBehavior<Shout>
	{
		public void OnStarted(AbilityBehaviorContext context, Shout data)
		{
			captured.Add(data);
			context.InstanceHandle.End();
		}

		public void OnEnded(AbilityBehaviorContext context)
		{
			// No-op.
		}
	}

	private sealed class CountingBehavior(System.Action onStarted) : IAbilityBehavior
	{
		public void OnStarted(AbilityBehaviorContext context)
		{
			onStarted();
			context.InstanceHandle.End();
		}

		public void OnEnded(AbilityBehaviorContext context)
		{
			// No-op.
		}
	}
}
