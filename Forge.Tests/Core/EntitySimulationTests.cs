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
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Core;

public class EntitySimulationTests(TagsAndCuesFixture fixture) : IClassFixture<TagsAndCuesFixture>
{
	// A power of two, so a duration authored as a float is exactly a whole number of steps. With 1/60 the float
	// duration is a hair longer than the double steps it is counted down by, and ends one tick later.
	private const double Step = 1.0 / 64.0;

	private const string Attribute = "TestAttributeSet.Attribute1";

	private readonly TagsManager _tagsManager = fixture.TagsManager;
	private readonly CuesManager _cuesManager = fixture.CuesManager;

	[Fact]
	[Trait("Simulation", "Tick")]
	public void Ticks_advance_effects_by_the_fixed_step()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		entity.EffectsManager.ApplyEffect(BuffFor(seconds: 1.0, entity));

		for (long tick = 1; tick < 64; tick++)
		{
			EntitySimulation.Tick(entity, tick, Step);
		}

		entity.Attributes[Attribute].CurrentValue.Should().Be(
			11,
			"63 steps of a sixty-fourth of a second are short of one");

		EntitySimulation.Tick(entity, 64, Step);

		entity.Attributes[Attribute].CurrentValue.Should().Be(1, "the sixty-fourth step completes the second");
	}

	[Fact]
	[Trait("Simulation", "Tick")]
	public void Effects_advance_before_fixed_graphs_in_the_same_tick()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var probe = new AttributeProbeNode(Attribute);
		ActivateGraph(entity, probe);

		entity.EffectsManager.ApplyEffect(BuffFor(seconds: Step, entity));
		entity.Attributes[Attribute].CurrentValue.Should().Be(11);

		EntitySimulation.Tick(entity, 1, Step);

		probe.SeenOnFixedUpdate.Should().Equal(
			[1],
			"the buff expires in this tick's effect pass, before the graph's fixed pass looks at the attribute");
	}

	[Fact]
	[Trait("Simulation", "Tick")]
	public void Ticks_leave_the_frame_rail_alone()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var node = new TrackingStateNode();
		ActivateGraph(entity, node);

		EntitySimulation.Tick(entity, 1, Step);

		node.FixedUpdateCount.Should().Be(1);
		node.UpdateCount.Should().Be(0);
	}

	[Fact]
	[Trait("Simulation", "Tick")]
	public void Each_tick_restarts_the_entity_random_for_that_tick()
	{
		var random = new DeterministicRandom(77UL);
		var entity = new TestEntity(_tagsManager, _cuesManager) { Random = random };
		var reference = new DeterministicRandom(77UL);

		EntitySimulation.Tick(entity, 5, Step);
		long drawnOnTickFive = random.NextInt64();

		reference.BeginTick(5);
		drawnOnTickFive.Should().Be(reference.NextInt64());

		EntitySimulation.Tick(entity, 5, Step);
		random.NextInt64().Should().Be(drawnOnTickFive, "replaying a tick repeats its rolls");
	}

	[Fact]
	[Trait("Simulation", "Tick")]
	public void An_entity_without_a_random_still_ticks()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		entity.EffectsManager.ApplyEffect(BuffFor(seconds: Step, entity));

		FluentActions.Invoking(() => EntitySimulation.Tick(entity, 1, Step)).Should().NotThrow();
		entity.Attributes[Attribute].CurrentValue.Should().Be(1);
	}

	private static Effect BuffFor(double seconds, TestEntity entity)
	{
		var effectData = new EffectData(
			"Buff",
			new DurationData(
				DurationType.HasDuration,
				new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat((float)seconds))),
			[
				new Modifier(
					Attribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(10)))
			]);

		return new Effect(effectData, new EffectOwnership(entity, entity));
	}

	private static void ActivateGraph(TestEntity entity, Node stateNode)
	{
		var graph = new Graph();
		graph.AddNode(stateNode);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			stateNode.InputPorts[StateNode<StateNodeContext>.InputPort]));

		var abilityData = new AbilityData("Probe", behaviorFactory: () => new GraphAbilityBehavior(graph));
		AbilityHandle handle = entity.Abilities.GrantAbilityPermanently(abilityData, 1, LevelComparison.None, entity);
		handle.TryActivate(out _).Should().BeTrue();
	}

	private sealed class AttributeProbeNode(string attribute) : StateNode<StateNodeContext>
	{
		private readonly string _attribute = attribute;

		public List<int> SeenOnFixedUpdate { get; } = [];

		protected override void OnActivate(GraphContext graphContext)
		{
		}

		protected override void OnDeactivate(GraphContext graphContext)
		{
		}

		protected override void OnFixedUpdate(double deltaTime, GraphContext graphContext)
		{
			graphContext.TryGetActivationContext(out AbilityBehaviorContext? context).Should().BeTrue();
			SeenOnFixedUpdate.Add(context!.Owner.Attributes[_attribute].CurrentValue);
		}
	}
}
