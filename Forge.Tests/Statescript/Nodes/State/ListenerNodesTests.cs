// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Effects.Modifiers;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Nodes.State;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript.Nodes.State;

public class ListenerNodesTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private const string TargetAttribute = "TestAttributeSet.Attribute90";

	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Graph", "AttributeListener")]
	public void Attribute_listener_node_emits_on_attribute_changes_with_outputs()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);

		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("entity", entity);
		graph.VariableDefinitions.DefineVariable("newValue", 0);
		graph.VariableDefinitions.DefineVariable("delta", 0);

		var listener = new AttributeListenerNode(TargetAttribute);
		listener.BindInput(AttributeListenerNode.EntityInput, "entity");
		listener.BindOutput(AttributeListenerNode.NewValueOutput, "newValue");
		listener.BindOutput(AttributeListenerNode.DeltaOutput, "delta");

		var onChanged = new TrackingActionNode();

		graph.AddNode(listener);
		graph.AddNode(onChanged);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			listener.InputPorts[StateNode<AttributeListenerNodeContext>.InputPort]));
		graph.AddConnection(new Connection(
			listener.OutputPorts[AttributeListenerNode.OnChangedPort],
			onChanged.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		onChanged.ExecutionCount.Should().Be(0);

		ApplyInstantDamage(entity, -5);

		onChanged.ExecutionCount.Should().Be(1);
		processor.GraphContext.GraphVariables.TryGetVar("newValue", out int newValue).Should().BeTrue();
		processor.GraphContext.GraphVariables.TryGetVar("delta", out int delta).Should().BeTrue();
		newValue.Should().Be(85);
		delta.Should().Be(-5);

		processor.StopGraph();
		ApplyInstantDamage(entity, -5);

		onChanged.ExecutionCount.Should().Be(1);
	}

	[Fact]
	[Trait("Graph", "EffectLevelListener")]
	public void Effect_level_listener_node_emits_on_level_changes()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var effect = new Effect(CreateInfiniteEffectData(), new EffectOwnership(owner, owner));

		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectVariable("effect", effect);
		graph.VariableDefinitions.DefineVariable("newLevel", 0);

		var listener = new EffectLevelListenerNode();
		listener.BindInput(EffectLevelListenerNode.EffectInput, "effect");
		listener.BindOutput(EffectLevelListenerNode.NewLevelOutput, "newLevel");

		var onLevelChanged = new TrackingActionNode();

		graph.AddNode(listener);
		graph.AddNode(onLevelChanged);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			listener.InputPorts[StateNode<EffectLevelListenerNodeContext>.InputPort]));
		graph.AddConnection(new Connection(
			listener.OutputPorts[EffectLevelListenerNode.OnLevelChangedPort],
			onLevelChanged.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		effect.LevelUp();

		onLevelChanged.ExecutionCount.Should().Be(1);
		processor.GraphContext.GraphVariables.TryGetVar("newLevel", out int newLevel).Should().BeTrue();
		newLevel.Should().Be(2);

		processor.StopGraph();
		effect.LevelUp();

		onLevelChanged.ExecutionCount.Should().Be(1);
	}

	[Fact]
	[Trait("Graph", "AbilityEndListener")]
	public void Ability_end_listener_node_emits_when_abilities_end()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		AbilityHandle handle = GrantInstantAbility(owner, "Watched");

		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("entity", owner);
		graph.VariableDefinitions.DefineVariable("wasCanceled", true);

		var listener = new AbilityEndListenerNode();
		listener.BindInput(AbilityEndListenerNode.EntityInput, "entity");
		listener.BindOutput(AbilityEndListenerNode.WasCanceledOutput, "wasCanceled");

		var onEnded = new TrackingActionNode();

		graph.AddNode(listener);
		graph.AddNode(onEnded);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			listener.InputPorts[StateNode<AbilityEndListenerNodeContext>.InputPort]));
		graph.AddConnection(new Connection(
			listener.OutputPorts[AbilityEndListenerNode.OnAbilityEndedPort],
			onEnded.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		handle.Activate(out _).Should().BeTrue();

		onEnded.ExecutionCount.Should().Be(1);
		processor.GraphContext.GraphVariables.TryGetVar("wasCanceled", out bool wasCanceled).Should().BeTrue();
		wasCanceled.Should().BeFalse();
	}

	[Fact]
	[Trait("Graph", "AbilityEndListener")]
	public void Ability_end_listener_node_filters_by_ability_data()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);

		var behaviorGraph = new Graph();
		var captureNode = new CaptureGraphContextNode();
		behaviorGraph.AddNode(captureNode);
		behaviorGraph.AddConnection(new Connection(
			behaviorGraph.EntryNode.OutputPorts[EntryNode.OutputPort],
			captureNode.InputPorts[ActionNode.InputPort]));

		var watchedData = new AbilityData(
			"Watched Filter",
			behaviorFactory: () => new GraphAbilityBehavior(behaviorGraph));

		AbilityHandle watchedHandle = owner.Abilities.GrantAbilityPermanently(
			watchedData,
			1,
			LevelComparison.None,
			sourceEntity: null);
		AbilityHandle otherHandle = GrantInstantAbility(owner, "Other");

		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("entity", owner);
		graph.VariableDefinitions.DefineObjectVariable("filterData", watchedData);

		var listener = new AbilityEndListenerNode();
		listener.BindInput(AbilityEndListenerNode.EntityInput, "entity");
		listener.BindInput(AbilityEndListenerNode.AbilityDataInput, "filterData");

		var onEnded = new TrackingActionNode();

		graph.AddNode(listener);
		graph.AddNode(onEnded);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			listener.InputPorts[StateNode<AbilityEndListenerNodeContext>.InputPort]));
		graph.AddConnection(new Connection(
			listener.OutputPorts[AbilityEndListenerNode.OnAbilityEndedPort],
			onEnded.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		otherHandle.Activate(out _).Should().BeTrue();
		onEnded.ExecutionCount.Should().Be(0);

		watchedHandle.Activate(out _).Should().BeTrue();
		onEnded.ExecutionCount.Should().Be(1);
	}

	[Fact]
	[Trait("Graph", "AbilityEndListener")]
	public void Ability_end_listener_node_filters_by_ability_data_when_granted_with_a_source()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);
		var source = new TestEntity(_tagsManager, _cuesManager);

		var behaviorGraph = new Graph();
		var captureNode = new CaptureGraphContextNode();
		behaviorGraph.AddNode(captureNode);
		behaviorGraph.AddConnection(new Connection(
			behaviorGraph.EntryNode.OutputPorts[EntryNode.OutputPort],
			captureNode.InputPorts[ActionNode.InputPort]));

		var watchedData = new AbilityData(
			"Watched Filter",
			behaviorFactory: () => new GraphAbilityBehavior(behaviorGraph));

		AbilityHandle watchedHandle = owner.Abilities.GrantAbilityPermanently(
			watchedData,
			1,
			LevelComparison.None,
			sourceEntity: source);
		AbilityHandle otherHandle = GrantInstantAbility(owner, "Other");

		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("entity", owner);
		graph.VariableDefinitions.DefineObjectVariable("filterData", watchedData);

		var listener = new AbilityEndListenerNode();
		listener.BindInput(AbilityEndListenerNode.EntityInput, "entity");
		listener.BindInput(AbilityEndListenerNode.AbilityDataInput, "filterData");

		var onEnded = new TrackingActionNode();

		graph.AddNode(listener);
		graph.AddNode(onEnded);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			listener.InputPorts[StateNode<AbilityEndListenerNodeContext>.InputPort]));
		graph.AddConnection(new Connection(
			listener.OutputPorts[AbilityEndListenerNode.OnAbilityEndedPort],
			onEnded.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		otherHandle.Activate(out _).Should().BeTrue();
		onEnded.ExecutionCount.Should().Be(0);

		watchedHandle.Activate(out _).Should().BeTrue();
		onEnded.ExecutionCount.Should().Be(1);
	}

	[Fact]
	[Trait("Graph", "AbilityEndListener")]
	public void Ability_end_listener_node_filters_ability_granted_after_activation()
	{
		var owner = new TestEntity(_tagsManager, _cuesManager);

		var behaviorGraph = new Graph();
		var captureNode = new CaptureGraphContextNode();
		behaviorGraph.AddNode(captureNode);
		behaviorGraph.AddConnection(new Connection(
			behaviorGraph.EntryNode.OutputPorts[EntryNode.OutputPort],
			captureNode.InputPorts[ActionNode.InputPort]));

		var watchedData = new AbilityData(
			"Watched Filter",
			behaviorFactory: () => new GraphAbilityBehavior(behaviorGraph));

		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("entity", owner);
		graph.VariableDefinitions.DefineObjectVariable("filterData", watchedData);

		var listener = new AbilityEndListenerNode();
		listener.BindInput(AbilityEndListenerNode.EntityInput, "entity");
		listener.BindInput(AbilityEndListenerNode.AbilityDataInput, "filterData");

		var onEnded = new TrackingActionNode();

		graph.AddNode(listener);
		graph.AddNode(onEnded);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			listener.InputPorts[StateNode<AbilityEndListenerNodeContext>.InputPort]));
		graph.AddConnection(new Connection(
			listener.OutputPorts[AbilityEndListenerNode.OnAbilityEndedPort],
			onEnded.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		// The watched ability is granted only after the listener is already active, so a filter resolved to a handle at
		// activation time would never match it.
		AbilityHandle watchedHandle = owner.Abilities.GrantAbilityPermanently(
			watchedData,
			1,
			LevelComparison.None,
			sourceEntity: null);
		AbilityHandle otherHandle = GrantInstantAbility(owner, "Other");

		otherHandle.Activate(out _).Should().BeTrue();
		onEnded.ExecutionCount.Should().Be(0);

		watchedHandle.Activate(out _).Should().BeTrue();
		onEnded.ExecutionCount.Should().Be(1);
	}

	private static EffectData CreateInfiniteEffectData()
	{
		return new EffectData(
			"Buff",
			new DurationData(DurationType.Infinite),
			[
				new Modifier(
					TargetAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(5)))
			]);
	}

	private static AbilityHandle GrantInstantAbility(TestEntity owner, string name)
	{
		var behaviorGraph = new Graph();
		var captureNode = new CaptureGraphContextNode();
		behaviorGraph.AddNode(captureNode);
		behaviorGraph.AddConnection(new Connection(
			behaviorGraph.EntryNode.OutputPorts[EntryNode.OutputPort],
			captureNode.InputPorts[ActionNode.InputPort]));

		var abilityData = new AbilityData(
			name,
			behaviorFactory: () => new GraphAbilityBehavior(behaviorGraph));

		return owner.Abilities.GrantAbilityPermanently(abilityData, 1, LevelComparison.None, sourceEntity: null);
	}

	private void ApplyInstantDamage(TestEntity entity, int magnitude)
	{
		var effectData = new EffectData(
			"Damage",
			new DurationData(DurationType.Instant),
			[
				new Modifier(
					TargetAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(magnitude)))
			]);

		var source = new TestEntity(_tagsManager, _cuesManager);
		entity.EffectsManager.ApplyEffect(new Effect(effectData, new EffectOwnership(source, source)));
	}
}
