// Copyright © Gamesmiths Guild.

using System.Numerics;
using FluentAssertions;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Abilities;

/// <summary>
/// Covers abilities bound to input actions: they activate in the input stage of the tick whose input shows their edge.
/// </summary>
/// <param name="fixture">The fixture providing the <see cref="TagsManager"/> and <see cref="CuesManager"/>.</param>
public class AbilityInputBindingTests(TagsAndCuesFixture fixture) : IClassFixture<TagsAndCuesFixture>
{
	private const double Step = 1.0 / 64.0;

	private readonly TagsManager _tagsManager = fixture.TagsManager;
	private readonly CuesManager _cuesManager = fixture.CuesManager;

	[Fact]
	[Trait("Abilities", "Input")]
	public void A_bound_ability_activates_in_the_tick_its_action_is_pressed()
	{
		TestEntity entity = CreateEntity(out EntityInput input);
		var behavior = new CountingBehavior();
		Grant(entity, AbilityInputBinding.ForAction("jump"), behavior);

		EntitySimulation.Tick(entity, 1, Step);
		behavior.Starts.Should().Be(0, "nothing was pressed");

		input.Submit(new InputFrame { Held = 0b1, Pressed = 0b1 });
		EntitySimulation.Tick(entity, 2, Step);

		behavior.Starts.Should().Be(1);
	}

	[Fact]
	[Trait("Abilities", "Input")]
	public void Holding_the_action_does_not_activate_the_ability_again()
	{
		TestEntity entity = CreateEntity(out EntityInput input);
		var behavior = new CountingBehavior();
		Grant(entity, AbilityInputBinding.ForAction("jump"), behavior);

		input.Submit(new InputFrame { Held = 0b1, Pressed = 0b1 });
		EntitySimulation.Tick(entity, 1, Step);
		EntitySimulation.Tick(entity, 2, Step);
		EntitySimulation.Tick(entity, 3, Step);

		behavior.Starts.Should().Be(1, "the press happened once, even though the instance ended at once");
	}

	[Fact]
	[Trait("Abilities", "Input")]
	public void A_tap_shorter_than_a_tick_still_activates_the_ability()
	{
		TestEntity entity = CreateEntity(out EntityInput input);
		var behavior = new CountingBehavior();
		Grant(entity, AbilityInputBinding.ForAction("jump"), behavior);

		input.Submit(new InputFrame { Held = 0b1, Pressed = 0b1 });
		input.Submit(new InputFrame { Released = 0b1 });
		EntitySimulation.Tick(entity, 1, Step);

		behavior.Starts.Should().Be(1);
	}

	[Fact]
	[Trait("Abilities", "Input")]
	public void A_binding_on_release_activates_when_the_action_is_released()
	{
		TestEntity entity = CreateEntity(out EntityInput input);
		var behavior = new CountingBehavior();
		Grant(entity, AbilityInputBinding.ForAction("jump", InputEdge.Released), behavior);

		input.Submit(new InputFrame { Held = 0b1, Pressed = 0b1 });
		EntitySimulation.Tick(entity, 1, Step);
		behavior.Starts.Should().Be(0);

		input.Submit(new InputFrame { Released = 0b1 });
		EntitySimulation.Tick(entity, 2, Step);

		behavior.Starts.Should().Be(1);
	}

	[Fact]
	[Trait("Abilities", "Input")]
	public void Activation_data_is_built_from_the_same_ticks_input()
	{
		TestEntity entity = CreateEntity(out EntityInput input);
		var behavior = new AimBehavior();
		Grant(
			entity,
			AbilityInputBinding.ForAction("jump", static (_, entityInput) => entityInput.Current.AimDirection),
			behavior);

		input.Submit(new InputFrame { Held = 0b1, Pressed = 0b1 }.WithAim(Vector3.Zero, new Vector3(0, 0, -1)));
		EntitySimulation.Tick(entity, 1, Step);

		behavior.Aim.Should().Be(new Vector3(0, 0, -1));
	}

	[Fact]
	[Trait("Abilities", "Input")]
	public void A_bound_ability_activates_before_its_graph_takes_the_ticks_fixed_step()
	{
		TestEntity entity = CreateEntity(out EntityInput input);
		var node = new FixedCountingNode();
		var graph = new Graph();
		graph.AddNode(node);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			node.InputPorts[StateNode<StateNodeContext>.InputPort]));

		Grant(entity, AbilityInputBinding.ForAction("jump"), new GraphAbilityBehavior(graph));

		input.Submit(new InputFrame { Held = 0b1, Pressed = 0b1 });
		EntitySimulation.Tick(entity, 1, Step);

		node.FixedUpdates.Should().Be(1, "the input stage runs before the fixed rail of the same tick");
	}

	[Fact]
	[Trait("Abilities", "Input")]
	public void A_binding_never_fires_for_an_entity_without_input_or_an_action_its_input_does_not_carry()
	{
		var withoutInput = new TestEntity(_tagsManager, _cuesManager);
		var first = new CountingBehavior();
		Grant(withoutInput, AbilityInputBinding.ForAction("jump"), first);
		EntitySimulation.Tick(withoutInput, 1, Step);

		TestEntity entity = CreateEntity(out EntityInput input);
		var second = new CountingBehavior();
		Grant(entity, AbilityInputBinding.ForAction("fly"), second);
		input.Submit(new InputFrame { Held = ulong.MaxValue, Pressed = ulong.MaxValue });
		EntitySimulation.Tick(entity, 1, Step);

		first.Starts.Should().Be(0);
		second.Starts.Should().Be(0);
	}

	private static void Grant(TestEntity entity, AbilityInputBinding binding, IAbilityBehavior behavior)
	{
		entity.Abilities.GrantAbilityPermanently(
			new AbilityData("Bound", behaviorFactory: () => behavior, inputBinding: binding),
			1,
			LevelComparison.Higher,
			null);
	}

	private TestEntity CreateEntity(out EntityInput input)
	{
		input = new EntityInput(new InputActionSet(["jump"]));
		return new TestEntity(_tagsManager, _cuesManager) { Input = input };
	}

	private sealed class CountingBehavior : IAbilityBehavior
	{
		public int Starts { get; private set; }

		public void OnStarted(AbilityBehaviorContext context)
		{
			Starts++;
			context.InstanceHandle.End();
		}

		public void OnEnded(AbilityBehaviorContext context)
		{
		}
	}

	private sealed class AimBehavior : IAbilityBehavior<Vector3>
	{
		public Vector3 Aim { get; private set; }

		public void OnStarted(AbilityBehaviorContext context, Vector3 data)
		{
			Aim = data;
		}

		public void OnEnded(AbilityBehaviorContext context)
		{
		}
	}

	private sealed class FixedCountingNode : StateNode<StateNodeContext>
	{
		public int FixedUpdates { get; private set; }

		protected override void OnActivate(GraphContext graphContext)
		{
		}

		protected override void OnDeactivate(GraphContext graphContext)
		{
		}

		protected override void OnFixedUpdate(double deltaTime, GraphContext graphContext)
		{
			FixedUpdates++;
		}
	}
}
