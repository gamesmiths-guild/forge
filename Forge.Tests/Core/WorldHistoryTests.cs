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
using Gamesmiths.Forge.Effects.Periodic;
using Gamesmiths.Forge.Effects.Stacking;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Nodes.State;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Core;

/// <summary>
/// A world history puts a whole world of entities back on one tick: every entity as the tick left it, entities that
/// left since back from limbo and entities that joined since set aside, and recording after a rollback leaves the
/// ticks after it behind.
/// </summary>
/// <param name="fixture">The fixture providing the <see cref="TagsManager"/> and <see cref="CuesManager"/>.</param>
public class WorldHistoryTests(TagsAndCuesFixture fixture) : IClassFixture<TagsAndCuesFixture>
{
	private const double Step = 1.0 / 64.0;

	private readonly TagsManager _tagsManager = fixture.TagsManager;
	private readonly CuesManager _cuesManager = fixture.CuesManager;

	[Fact]
	[Trait("Simulation", "World")]
	public void Every_entity_goes_back_to_the_tick_together()
	{
		TestEntity a = Entity();
		TestEntity b = Entity();
		TestEntity c = Entity();
		WorldHistory world = World(32, a, b, c);
		var states = new Dictionary<long, string>();

		for (long tick = 1; tick <= 24; tick++)
		{
			if (tick % 5 == 0)
			{
				Burn(a, b);
			}

			if (tick % 7 == 0)
			{
				Burn(b, c);
			}

			EntitySimulation.Tick(world.Entities, tick, Step);
			world.Record(tick);
			states[tick] = Describe(a, b, c);
		}

		foreach (long tick in (long[])[3, 17, 9, 24, 1, 12])
		{
			world.Rollback(tick);

			Describe(a, b, c).Should().Be(states[tick], $"tick {tick} was rolled back to");
			world.CurrentTick.Should().Be(tick);
			world.NewestTick.Should().Be(24, "the ticks after one rolled back to are kept");
		}
	}

	[Fact]
	[Trait("Simulation", "World")]
	public void Handlers_sharing_an_event_keep_their_order_whichever_entity_they_belong_to()
	{
		var log = new List<string>();
		TestEntity a = Entity();
		TestEntity b = Entity();
		WorldHistory world = World(8, a, b);
		EntityReplay.Watch(a, log);

		// Each takes hold of a's tags in turn: a ward of a's, a graph of b's listening to a, then another ward of a's.
		Apply(a, Ward("First"));
		Grant(b, ListenerAbility(a, log)).TryActivate(out _).Should().BeTrue();
		Apply(a, Ward("Last"));
		Run(world, 1, 2);

		world.Rollback(1);
		log.Clear();
		Apply(a, Tagged("Red", "color.red"));

		log.Where(x => !x.StartsWith("tags", StringComparison.Ordinal)).Should().Equal(
			"changed First",
			"heard",
			"changed Last",
			"added Red");
	}

	[Fact]
	[Trait("Simulation", "World")]
	public void An_entity_that_leaves_waits_in_limbo_for_a_rollback_to_bring_it_back()
	{
		TestEntity a = Entity();
		TestEntity b = Entity();
		WorldHistory world = World(16, a, b);
		List<string> changes = Watch(world, a, b);
		Burn(a, b);
		Run(world, 1, 4);
		string atFour = Describe(b);

		world.Remove(b);

		world.Entities.Should().Equal(a);
		changes.Should().BeEmpty("it goes to limbo when the next tick is recorded");

		Run(world, 5, 8);

		changes.Should().Equal("entered b");
		Describe(b).Should().Be(atFour, "nothing advances an entity in limbo");

		world.Rollback(4);

		world.Entities.Should().Equal(a, b);
		Describe(b).Should().Be(atFour);
		changes.Should().Equal("entered b", "left b");

		world.Rollback(6);

		world.Entities.Should().Equal(a);
		changes.Should().Equal("entered b", "left b", "entered b");
	}

	[Fact]
	[Trait("Simulation", "World")]
	public void An_entity_coming_back_from_limbo_is_announced_before_it_is_restored()
	{
		TestEntity a = Entity();
		TestEntity b = Entity();
		WorldHistory world = World(16, a, b);
		Burn(a, b);
		Run(world, 1, 2);
		string atTwo = Describe(b);
		Run(world, 3, 6);
		string whenLeaving = Describe(b);
		world.Remove(b);
		Run(world, 7, 7);
		string? whenAnnounced = null;
		world.LeftLimbo += x => whenAnnounced = Describe(x);

		world.Rollback(2);

		whenAnnounced.Should().Be(whenLeaving, "the host makes the entity's object live before its state comes back");
		Describe(b).Should().Be(atTwo);
	}

	[Fact]
	[Trait("Simulation", "World")]
	public void An_entity_in_limbo_lets_go_of_what_it_held()
	{
		var log = new List<string>();
		TestEntity a = Entity();
		TestEntity b = Entity();
		WorldHistory world = World(16, a, b);
		Grant(b, ListenerAbility(a, log)).TryActivate(out _).Should().BeTrue();
		Run(world, 1, 2);
		world.Remove(b);
		Run(world, 3, 3);

		Apply(a, Tagged("Red", "color.red"));

		log.Should().BeEmpty("b let go of a's tags when it went to limbo");

		world.Rollback(2);
		Apply(a, Tagged("Red", "color.red"));

		log.Should().Equal("heard");
	}

	[Fact]
	[Trait("Simulation", "World")]
	public void An_entity_that_joined_after_the_tick_waits_in_limbo_until_the_world_is_past_it_again()
	{
		TestEntity a = Entity();
		TestEntity c = Entity();
		WorldHistory world = World(16, a);
		List<string> changes = Watch(world, a, null, c);
		Run(world, 1, 3);

		world.Add(c);
		Burn(a, c);
		Run(world, 4, 4);
		string atFour = Describe(c);
		Run(world, 5, 6);

		world.Rollback(3);

		world.Entities.Should().Equal(a);
		changes.Should().Equal("entered c");

		world.Rollback(4);

		world.Entities.Should().Equal(a, c);
		Describe(c).Should().Be(atFour);
		changes.Should().Equal("entered c", "left c");
	}

	[Fact]
	[Trait("Simulation", "World")]
	public void Recording_after_a_rollback_leaves_the_ticks_after_it_behind()
	{
		TestEntity a = Entity();
		TestEntity b = Entity();
		TestEntity c = Entity();
		WorldHistory world = World(16, a, b);
		List<string> changes = Watch(world, a, b, c);
		Run(world, 1, 3);
		world.Remove(b);
		world.Add(c);
		Run(world, 4, 6);

		world.Rollback(2);
		changes.Clear();
		Run(world, 3, 3);

		changes.Should().Equal("released c");
		world.Entities.Should().Equal(a, b);
		world.NewestTick.Should().Be(3);
		world.Invoking(x => x.Rollback(4)).Should().Throw<ArgumentOutOfRangeException>();

		Run(world, 4, 6);
		world.Rollback(5);

		world.Entities.Should().Equal(
			[a, b],
			"b stayed in the world after the ticks it left in were left behind");
		changes.Should().Equal("released c");
	}

	[Fact]
	[Trait("Simulation", "World")]
	public void Limbo_lets_an_entity_go_once_no_tick_the_history_holds_has_it()
	{
		TestEntity a = Entity();
		TestEntity b = Entity();
		WorldHistory world = World(4, a, b);
		List<string> changes = Watch(world, a, b);
		Run(world, 1, 2);
		world.Remove(b);
		Run(world, 3, 5);

		changes.Should().Equal("entered b");
		world.SnapshotCount.Should().Be(6, "a holds ticks 2 to 5 and b ticks 1 and 2");

		Run(world, 6, 6);

		changes.Should().Equal("entered b", "released b");
		world.SnapshotCount.Should().Be(4);
		world.Invoking(x => x.Remove(b)).Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Simulation", "World")]
	public void An_entity_that_leaves_before_it_was_recorded_is_released()
	{
		TestEntity a = Entity();
		TestEntity c = Entity();
		WorldHistory world = World(8, a);
		List<string> changes = Watch(world, a, null, c);
		Run(world, 1, 2);

		world.Add(c);
		world.Remove(c);
		Run(world, 3, 3);

		changes.Should().Equal("released c");
	}

	[Fact]
	[Trait("Simulation", "World")]
	public void An_entity_that_leaves_running_a_behavior_that_is_not_a_graph_is_refused()
	{
		TestEntity a = Entity();
		TestEntity c = Entity();
		WorldHistory world = World(8, a);
		Run(world, 1, 2);

		world.Add(c);
		AbilityHandle coded = Grant(c, new AbilityData("Coded", behaviorFactory: () => new CodedBehavior()));
		coded.TryActivate(out _).Should().BeTrue();
		world.Remove(c);
		EntitySimulation.Tick(world.Entities, 3, Step);

		world.Invoking(x => x.Record(3)).Should().Throw<NotSupportedException>();
	}

	[Fact]
	[Trait("Simulation", "World")]
	public void A_forgotten_entity_never_comes_back()
	{
		TestEntity a = Entity();
		TestEntity b = Entity();
		TestEntity c = Entity();
		WorldHistory world = World(16, a, b, c);
		List<string> changes = Watch(world, a, b, c);
		Run(world, 1, 3);
		world.Remove(c);
		Run(world, 4, 4);
		changes.Clear();

		world.Forget(b);
		world.Forget(c);

		world.Entities.Should().Equal(a);

		Run(world, 5, 5);
		world.Rollback(2);

		world.Entities.Should().Equal(a);
		changes.Should().BeEmpty();
		world.Invoking(x => x.Forget(b)).Should().Throw<ArgumentException>();
	}

	[Fact]
	[Trait("Simulation", "World")]
	public void A_tick_that_does_not_follow_the_current_one_starts_the_history_over()
	{
		TestEntity a = Entity();
		TestEntity b = Entity();
		WorldHistory world = World(16, a, b);
		List<string> changes = Watch(world, a, b);
		Run(world, 1, 3);
		world.Remove(b);
		Run(world, 4, 4);

		Run(world, 10, 10);

		changes.Should().Equal("entered b", "released b");
		world.OldestTick.Should().Be(10);
		world.NewestTick.Should().Be(10);
		world.SnapshotCount.Should().Be(1);
	}

	[Fact]
	[Trait("Simulation", "World")]
	public void The_entity_list_a_tick_advances_stays_as_it_was()
	{
		TestEntity a = Entity();
		TestEntity b = Entity();
		TestEntity c = Entity();
		WorldHistory world = World(8, a, b);
		IReadOnlyList<IForgeEntity> advancing = world.Entities;

		world.Remove(b);
		world.Add(c);

		advancing.Should().Equal(a, b);
		world.Entities.Should().Equal(a, c);
	}

	[Fact]
	[Trait("Simulation", "World")]
	public void A_world_refuses_what_it_cannot_do()
	{
		TestEntity a = Entity();
		WorldHistory world = World(4, a);

		FluentActions.Invoking(() => new WorldHistory(0, Step)).Should().Throw<ArgumentOutOfRangeException>();
		world.Invoking(x => x.Rollback(0)).Should().Throw<ArgumentOutOfRangeException>("nothing was recorded yet");
		world.Invoking(x => x.Add(a)).Should().Throw<ArgumentException>();
		world.Invoking(x => x.Remove(Entity())).Should().Throw<ArgumentException>();

		Run(world, 1, 6);

		world.Invoking(x => x.Rollback(2)).Should().Throw<ArgumentOutOfRangeException>();
		world.Invoking(x => x.Rollback(7)).Should().Throw<ArgumentOutOfRangeException>();
	}

	private static WorldHistory World(int capacity, params TestEntity[] entities)
	{
		var world = new WorldHistory(capacity, Step);

		foreach (TestEntity entity in entities)
		{
			world.Add(entity);
		}

		return world;
	}

	private static void Run(WorldHistory world, long from, long to)
	{
		for (long tick = from; tick <= to; tick++)
		{
			EntitySimulation.Tick(world.Entities, tick, Step);
			world.Record(tick);
		}
	}

	// Names the entities a, b and c in order, a null standing for one the test has none for, and writes what the world
	// does with them.
	private static List<string> Watch(WorldHistory world, params IForgeEntity?[] entities)
	{
		var changes = new List<string>();
		string Name(IForgeEntity entity)
		{
			return ((char)('a' + Array.IndexOf(entities, entity))).ToString();
		}

		world.EnteredLimbo += x => changes.Add($"entered {Name(x)}");
		world.LeftLimbo += x => changes.Add($"left {Name(x)}");
		world.Released += x => changes.Add($"released {Name(x)}");
		return changes;
	}

	private static string Describe(params IForgeEntity[] entities)
	{
		return string.Join(
			$"{Environment.NewLine}--{Environment.NewLine}",
			entities.Select(x => EntityState.Describe(x)));
	}

	private static void Burn(IForgeEntity source, IForgeEntity target)
	{
		target.EffectsManager.ApplyEffect(new Effect(Burning(), new EffectOwnership(source, source)));
	}

	private static void Apply(IForgeEntity entity, EffectData effectData)
	{
		entity.EffectsManager.ApplyEffect(new Effect(effectData, new EffectOwnership(entity, entity)));
	}

	private static AbilityHandle Grant(IForgeEntity entity, AbilityData abilityData)
	{
		return entity.Abilities.GrantAbilityPermanently(abilityData, 1, LevelComparison.None, entity);
	}

	// Stacks up to three, a quarter of a second each, taking health every eighth of a second.
	private static EffectData Burning()
	{
		return new EffectData(
			"Burning",
			new DurationData(
				DurationType.HasDuration,
				new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(0.25f))),
			[
				new Modifier(
					"TestAttributeSet.Attribute90",
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(-1))),
			],
			new StackingData(
				new ScalableInt(3),
				new ScalableInt(1),
				StackPolicy.AggregateByTarget,
				StackLevelPolicy.AggregateLevels,
				StackMagnitudePolicy.Sum,
				StackOverflowPolicy.AllowApplication,
				StackExpirationPolicy.ClearEntireStack,
				ApplicationRefreshPolicy: StackApplicationRefreshPolicy.RefreshOnSuccessfulApplication,
				ApplicationResetPeriodPolicy: StackApplicationResetPeriodPolicy.NeverReset),
			new PeriodicData(new ScalableFloat(0.125f), false, PeriodInhibitionRemovedPolicy.NeverReset));
	}

	private TestEntity Entity()
	{
		return new TestEntity(_tagsManager, _cuesManager);
	}

	private EffectData Tagged(string name, string tag)
	{
		return new EffectData(
			name,
			new DurationData(DurationType.Infinite),
			effectComponents: [new ModifierTagsEffectComponent(Tags(tag))]);
	}

	// Held back while the entity has the red tag.
	private EffectData Ward(string name)
	{
		return new EffectData(
			name,
			new DurationData(DurationType.Infinite),
			effectComponents:
			[
				new TargetTagRequirementsEffectComponent(
					ongoingTagRequirements: new TagRequirements(IgnoreTags: Tags("color.red"))),
			]);
	}

	// Listens for the red tag on another entity and says so.
	private AbilityData ListenerAbility(IForgeEntity watched, List<string> log)
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectVariable("watched", watched);
		graph.VariableDefinitions.DefineObjectVariable("red", Tag.RequestTag(_tagsManager, "color.red"));

		var listener = new TagListenerNode();
		listener.BindInput(TagListenerNode.EntityInput, "watched");
		listener.BindInput(TagListenerNode.TagInput, "red");
		graph.AddNode(listener);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			listener.InputPorts[ActionNode.InputPort]));

		var heard = new TrackingActionNode("heard", log);
		graph.AddNode(heard);
		graph.AddConnection(new Connection(
			listener.OutputPorts[TagListenerNode.OnTagAddedPort],
			heard.InputPorts[ActionNode.InputPort]));

		return new AbilityData("Listener", behaviorFactory: () => new GraphAbilityBehavior(graph));
	}

	private TagContainer Tags(string tag)
	{
		return new TagContainer(_tagsManager, [Tag.RequestTag(_tagsManager, tag)]);
	}

	private sealed class CodedBehavior : IAbilityBehavior
	{
		public void OnStarted(AbilityBehaviorContext context)
		{
		}

		public void OnEnded(AbilityBehaviorContext context)
		{
		}
	}
}
