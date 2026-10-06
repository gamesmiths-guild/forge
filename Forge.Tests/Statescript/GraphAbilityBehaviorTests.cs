// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Components;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Effects.Modifiers;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Nodes.State;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

using static Gamesmiths.Forge.Tests.Helpers.NodeBindings;

namespace Gamesmiths.Forge.Tests.Statescript;

public class GraphAbilityBehaviorTests(TagsAndCuesFixture fixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = fixture.TagsManager;
	private readonly CuesManager _cuesManager = fixture.CuesManager;

	[Fact]
	[Trait("GraphBehavior", "Lifecycle")]
	public void Action_only_graph_ends_ability_instance_on_start()
	{
		var graph = new Graph();
		var actionNode = new TrackingActionNode();

		graph.AddNode(actionNode);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			actionNode.InputPorts[ActionNode.InputPort]));

		var entity = new TestEntity(_tagsManager, _cuesManager);
		var behavior = new GraphAbilityBehavior(graph);

		AbilityData abilityData = CreateAbilityData("ActionGraph", behaviorFactory: () => behavior);
		AbilityHandle? handle = Grant(entity, abilityData);
		handle.Should().NotBeNull();

		handle!.TryActivate(out AbilityActivationFailures failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);

		actionNode.ExecutionCount.Should().Be(1);
		handle.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("GraphBehavior", "Lifecycle")]
	public void Timer_graph_keeps_ability_active_until_timer_completes()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("duration", 2.0);

		TimerNode timer = CreateTimerNode("duration");
		graph.AddNode(timer);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			timer.InputPorts[StateNode<TimerNodeContext>.InputPort]));

		var entity = new TestEntity(_tagsManager, _cuesManager);
		var behavior = new GraphAbilityBehavior(graph);

		AbilityData abilityData = CreateAbilityData("TimerGraph", behaviorFactory: () => behavior);
		AbilityHandle? handle = Grant(entity, abilityData);
		handle.Should().NotBeNull();

		handle!.TryActivate(out AbilityActivationFailures failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);
		handle.IsActive.Should().BeTrue();

		behavior.Processor.UpdateGraph(1.0);
		handle.IsActive.Should().BeTrue();

		behavior.Processor.UpdateGraph(1.0);
		handle.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("GraphBehavior", "Lifecycle")]
	public void Canceling_ability_stops_graph()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("duration", 10.0);

		TimerNode timer = CreateTimerNode("duration");
		graph.AddNode(timer);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			timer.InputPorts[StateNode<TimerNodeContext>.InputPort]));

		var entity = new TestEntity(_tagsManager, _cuesManager);
		var behavior = new GraphAbilityBehavior(graph);

		AbilityData abilityData = CreateAbilityData("CancelGraph", behaviorFactory: () => behavior);
		AbilityHandle? handle = Grant(entity, abilityData);
		handle.Should().NotBeNull();

		handle!.TryActivate(out _).Should().BeTrue();
		handle.IsActive.Should().BeTrue();
		behavior.Processor.GraphContext.IsActive.Should().BeTrue();

		handle.Cancel();
		handle.IsActive.Should().BeFalse();
		behavior.Processor.GraphContext.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("GraphBehavior", "Variables")]
	public void Graph_variables_are_initialized_from_definitions()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("counter", 0);

		var incrementNode = new IncrementCounterNode("counter");
		graph.AddNode(incrementNode);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			incrementNode.InputPorts[ActionNode.InputPort]));

		var entity = new TestEntity(_tagsManager, _cuesManager);
		var behavior = new GraphAbilityBehavior(graph);

		AbilityData abilityData = CreateAbilityData("VarGraph", behaviorFactory: () => behavior);
		AbilityHandle? handle = Grant(entity, abilityData);
		handle.Should().NotBeNull();

		handle!.TryActivate(out _).Should().BeTrue();

		behavior.Processor.GraphContext.GraphVariables.TryGetVar("counter", out int value).Should().BeTrue();
		value.Should().Be(1);
	}

	[Fact]
	[Trait("GraphBehavior", "SharedVariables")]
	public void Shared_variables_are_set_from_ability_context_owner()
	{
		var graph = new Graph();
		var actionNode = new TrackingActionNode();

		graph.AddNode(actionNode);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			actionNode.InputPorts[ActionNode.InputPort]));

		var entity = new TestEntity(_tagsManager, _cuesManager);
		var behavior = new GraphAbilityBehavior(graph);

		AbilityData abilityData = CreateAbilityData("SharedVarsGraph", behaviorFactory: () => behavior);
		AbilityHandle? handle = Grant(entity, abilityData);
		handle.Should().NotBeNull();

		handle!.TryActivate(out _).Should().BeTrue();

		behavior.Processor.GraphContext.SharedVariables.Should().BeSameAs(entity.SharedVariables);
	}

	[Fact]
	[Trait("GraphBehavior", "TypedData")]
	public void Typed_data_binder_writes_activation_data_into_graph_variables()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("damage", 0);
		graph.VariableDefinitions.DefineVariable("multiplier", 1.0);

		var readDamage = new ReadVariableNode<int>("damage");
		var readMultiplier = new ReadVariableNode<double>("multiplier");

		graph.AddNode(readDamage);
		graph.AddNode(readMultiplier);

		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			readDamage.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			readDamage.OutputPorts[ActionNode.OutputPort],
			readMultiplier.InputPorts[ActionNode.InputPort]));

		var entity = new TestEntity(_tagsManager, _cuesManager);
		var behavior = new GraphAbilityBehavior<DamageData>(
			graph,
			dataBinder: (data, vars) =>
			{
				vars.SetVar("damage", data.Amount);
				vars.SetVar("multiplier", data.Multiplier);
			});

		AbilityData abilityData = CreateAbilityData("TypedGraph", behaviorFactory: () => behavior);
		AbilityHandle? handle = Grant(entity, abilityData);
		handle.Should().NotBeNull();

		var activationData = new DamageData(50, 2.5);
		handle!.TryActivate(activationData, out AbilityActivationFailures failureFlags).Should().BeTrue();
		failureFlags.Should().Be(AbilityActivationFailures.None);

		readDamage.LastReadValue.Should().Be(50);
		readMultiplier.LastReadValue.Should().Be(2.5);
	}

	[Fact]
	[Trait("GraphBehavior", "ExitNode")]
	public void Exit_node_ends_ability_instance()
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineVariable("duration", 1.0);

		TimerNode timer = CreateTimerNode("duration");
		var exitNode = new ExitNode();

		graph.AddNode(timer);
		graph.AddNode(exitNode);

		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			timer.InputPorts[StateNode<TimerNodeContext>.InputPort]));
		graph.AddConnection(new Connection(
			timer.OutputPorts[StateNode<TimerNodeContext>.OnActivatePort],
			exitNode.InputPorts[ExitNode.InputPort]));

		var entity = new TestEntity(_tagsManager, _cuesManager);
		var behavior = new GraphAbilityBehavior(graph);

		AbilityData abilityData = CreateAbilityData("ExitGraph", behaviorFactory: () => behavior);
		AbilityHandle? handle = Grant(entity, abilityData);
		handle.Should().NotBeNull();

		handle!.TryActivate(out _).Should().BeTrue();
		handle.IsActive.Should().BeFalse();
	}

	[Fact]
	[Trait("GraphBehavior", "ActivationContext")]
	public void Activation_context_contains_ability_behavior_context()
	{
		var graph = new Graph();
		var captureNode = new CaptureActivationContextNode();

		graph.AddNode(captureNode);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			captureNode.InputPorts[ActionNode.InputPort]));

		var entity = new TestEntity(_tagsManager, _cuesManager);
		var behavior = new GraphAbilityBehavior(graph);

		AbilityData abilityData = CreateAbilityData("ActivationContextGraph", behaviorFactory: () => behavior);
		AbilityHandle? handle = Grant(entity, abilityData);
		handle.Should().NotBeNull();

		handle!.TryActivate(out _).Should().BeTrue();

		captureNode.CapturedActivationContext.Should().NotBeNull();
		captureNode.CapturedActivationContext.Should().BeOfType<AbilityBehaviorContext>();

		var capturedContext = (AbilityBehaviorContext)captureNode.CapturedActivationContext!;
		capturedContext.Owner.Should().Be(entity);
		capturedContext.AbilityHandle.Should().Be(handle);
	}

	[Fact]
	[Trait("GraphBehavior", "ActivationContext")]
	public void TryGetActivationContext_returns_typed_ability_behavior_context()
	{
		var graph = new Graph();
		var typedCaptureNode = new TryGetActivationContextNode<AbilityBehaviorContext>();

		graph.AddNode(typedCaptureNode);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			typedCaptureNode.InputPorts[ActionNode.InputPort]));

		var entity = new TestEntity(_tagsManager, _cuesManager);
		var behavior = new GraphAbilityBehavior(graph);

		AbilityData abilityData = CreateAbilityData("TypedContextGraph", behaviorFactory: () => behavior);
		AbilityHandle? handle = Grant(entity, abilityData);
		handle.Should().NotBeNull();

		handle!.TryActivate(out _).Should().BeTrue();

		typedCaptureNode.Found.Should().BeTrue();
		typedCaptureNode.CapturedContext.Should().NotBeNull();
		typedCaptureNode.CapturedContext!.AbilityHandle.Should().Be(handle);
	}

	[Fact]
	[Trait("GraphBehavior", "Lifecycle")]
	public void Ability_activated_again_as_its_graph_stops_takes_the_graph_once_the_stop_completes()
	{
		(GraphAbilityBehavior behavior, AbilityHandle handle, DeactivationCallbackNode first,
			DeactivationCallbackNode second) = GrantTeardownAbility();

		handle.TryActivate(out _).Should().BeTrue();
		object? firstContext = behavior.Processor.GraphContext.ActivationContext;

		// The first activation's teardown activates the ability again before the rest of that teardown runs.
		bool activatedAgain = false;
		first.Deactivated = _ => activatedAgain = activatedAgain || handle.TryActivate(out AbilityActivationFailures _);
		object? laterInTeardown = null;
		second.Deactivated = graphContext => laterInTeardown ??= graphContext.ActivationContext;

		handle.Cancel();

		activatedAgain.Should().BeTrue();
		laterInTeardown.Should().BeSameAs(firstContext, "the rest of the teardown belongs to the first activation");
		handle.IsActive.Should().BeTrue("the second activation is not ended by the first one's completion");
		behavior.Processor.GraphContext.ActivationContext.Should().NotBeSameAs(firstContext);
		behavior.Processor.GraphContext.IsActive.Should().BeTrue();
	}

	[Fact]
	[Trait("GraphBehavior", "Lifecycle")]
	public void Ability_activated_again_and_ended_as_its_graph_stops_starts_no_graph()
	{
		(GraphAbilityBehavior behavior, AbilityHandle handle, DeactivationCallbackNode first,
			DeactivationCallbackNode second) = GrantTeardownAbility();

		handle.TryActivate(out _).Should().BeTrue();

		// The first activation's teardown activates the ability again, and the rest of it ends that second activation.
		bool activatedAgain = false;
		first.Deactivated = _ => activatedAgain = activatedAgain || handle.TryActivate(out AbilityActivationFailures _);
		bool canceledAgain = false;
		second.Deactivated = _ =>
		{
			if (activatedAgain && !canceledAgain)
			{
				canceledAgain = true;
				handle.Cancel();
			}
		};

		handle.Cancel();

		canceledAgain.Should().BeTrue();
		handle.IsActive.Should().BeFalse();
		behavior.Processor.GraphContext.IsActive.Should().BeFalse("the second activation ended before its graph ran");
	}

	[Fact]
	[Trait("GraphBehavior", "Lifecycle")]
	public void Ability_activated_again_as_its_own_code_ends_it_takes_the_graph_once_that_code_returns()
	{
		var graph = new Graph();
		var node = new ActivationCallbackNode();
		graph.AddNode(node);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			node.InputPorts[ActivationCallbackNode.InputPort]));

		var entity = new TestEntity(_tagsManager, _cuesManager);
		var behavior = new GraphAbilityBehavior(graph);
		AbilityHandle? handle = Grant(entity, new AbilityData("Recast", behaviorFactory: () => behavior));
		handle.Should().NotBeNull();

		// The first activation's own code ends it, once, as an effect it applies might, and the ability is activated
		// again as soon as it has ended.
		bool activatedAgain = false;
		entity.Abilities.OnAbilityEnded += _ =>
		{
			if (!activatedAgain)
			{
				activatedAgain = true;
				handle!.TryActivate(out AbilityActivationFailures _);
			}
		};

		bool ended = false;
		object? firstContext = null;
		object? afterEnding = null;
		node.Activated = graphContext =>
		{
			if (!ended)
			{
				ended = true;
				firstContext = graphContext.ActivationContext;
				handle!.Cancel();
				afterEnding = graphContext.ActivationContext;
			}
		};

		handle!.TryActivate(out _).Should().BeTrue();

		activatedAgain.Should().BeTrue();
		afterEnding.Should().BeSameAs(firstContext, "the rest of that code belongs to the first activation");
		handle.IsActive.Should().BeTrue();
		behavior.Processor.GraphContext.ActivationContext.Should().NotBeSameAs(firstContext);
		behavior.Processor.GraphContext.IsActive.Should().BeTrue();
	}

	[Fact]
	[Trait("GraphBehavior", "ActivationContext")]
	public void Standalone_graph_has_null_activation_context()
	{
		var graph = new Graph();
		var typedCaptureNode = new TryGetActivationContextNode<AbilityBehaviorContext>();

		graph.AddNode(typedCaptureNode);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			typedCaptureNode.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		typedCaptureNode.Found.Should().BeFalse();
		typedCaptureNode.CapturedContext.Should().BeNull();
	}

	private static AbilityHandle? Grant(
		TestEntity target,
		AbilityData data,
		IForgeEntity? sourceEntity = null)
	{
		var grantConfig = new GrantAbilityConfig(
			data,
			new ScalableInt(1),
			AbilityDeactivationPolicy.CancelImmediately,
			AbilityDeactivationPolicy.CancelImmediately,
			false,
			false,
			LevelComparison.Higher);

		var effectData = new EffectData(
			"Grant",
			new DurationData(DurationType.Infinite),
			effectComponents: [new GrantAbilityEffectComponent([grantConfig])]);

		var grantEffect = new Effect(effectData, new EffectOwnership(null, sourceEntity));
		_ = target.EffectsManager.ApplyEffect(grantEffect);
		target.Abilities.TryGetAbility(data, out AbilityHandle? handle, sourceEntity);
		return handle;
	}

	private AbilityData CreateAbilityData(
		string name,
		Func<IAbilityBehavior> behaviorFactory)
	{
		EffectData[] cooldownEffectData = [new EffectData(
			$"{name} Cooldown",
			new DurationData(
				DurationType.HasDuration,
				new ModifierMagnitude(
					MagnitudeCalculationType.ScalableFloat,
					scalableFloatMagnitude: new ScalableFloat(3f))),
			effectComponents:
			[
				new ModifierTagsEffectComponent(
					new TagContainer(_tagsManager, TestUtils.StringToTag(_tagsManager, ["simple.tag"])))
			])];

		var costEffectData = new EffectData(
			$"{name} Cost",
			new DurationData(DurationType.Instant),
			[
				new Modifier(
					"TestAttributeSet.Attribute90",
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						scalableFloatMagnitude: new ScalableFloat(-1f)))
			]);

		return new AbilityData(
			name,
			costEffectData,
			cooldownEffectData,
			behaviorFactory: behaviorFactory);
	}

	// An ability with no cost or cooldown that shares one behavior between its activations, as the documented setup
	// does, over two state nodes that end one after the other when the graph stops.
	private (GraphAbilityBehavior Behavior, AbilityHandle Handle, DeactivationCallbackNode First,
		DeactivationCallbackNode Second) GrantTeardownAbility()
	{
		var graph = new Graph();
		var first = new DeactivationCallbackNode();
		var second = new DeactivationCallbackNode();
		graph.AddNode(first);
		graph.AddNode(second);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			first.InputPorts[DeactivationCallbackNode.InputPort]));
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			second.InputPorts[DeactivationCallbackNode.InputPort]));

		var entity = new TestEntity(_tagsManager, _cuesManager);
		var behavior = new GraphAbilityBehavior(graph);
		AbilityHandle? handle = Grant(entity, new AbilityData("Recast", behaviorFactory: () => behavior));
		handle.Should().NotBeNull();

		return (behavior, handle!, first, second);
	}

	private sealed record DamageData(int Amount, double Multiplier);

	// Runs a callback as it is activated, as a node whose start sets something off would.
	private sealed class ActivationCallbackNode : StateNode<StateNodeContext>
	{
		public Action<GraphContext>? Activated { get; set; }

		protected override void OnActivate(GraphContext graphContext)
		{
			Activated?.Invoke(graphContext);
		}

		protected override void OnDeactivate(GraphContext graphContext)
		{
		}
	}

	// Runs a callback as it is deactivated, as a node whose cleanup sets something off would.
	private sealed class DeactivationCallbackNode : StateNode<StateNodeContext>
	{
		public Action<GraphContext>? Deactivated { get; set; }

		protected override void OnActivate(GraphContext graphContext)
		{
		}

		protected override void OnDeactivate(GraphContext graphContext)
		{
			Deactivated?.Invoke(graphContext);
		}
	}
}
