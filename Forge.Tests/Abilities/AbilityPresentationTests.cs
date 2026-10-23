// Copyright © Gamesmiths Guild.

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
/// Covers the presentation pass for an entity's abilities, including what an ended instance's graph still shows.
/// </summary>
/// <param name="fixture">The fixture providing the <see cref="TagsManager"/> and <see cref="CuesManager"/>.</param>
public class AbilityPresentationTests(TagsAndCuesFixture fixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = fixture.TagsManager;
	private readonly CuesManager _cuesManager = fixture.CuesManager;

	[Fact]
	[Trait("Abilities", "Presentation")]
	public void An_ability_graph_is_presented_through_its_entity()
	{
		var log = new List<string>();
		var entity = new TestEntity(_tagsManager, _cuesManager);
		AbilityHandle handle = GrantGraphAbility(entity, new PresentingNode("roll", log));

		handle.TryActivate(out _).Should().BeTrue();
		log.Should().BeEmpty();

		entity.Abilities.PresentAbilities();

		log.Should().Equal("start roll#1");
	}

	[Fact]
	[Trait("Abilities", "Presentation")]
	public void An_instance_that_ends_while_presenting_is_presented_once_more()
	{
		var log = new List<string>();
		var entity = new TestEntity(_tagsManager, _cuesManager);
		AbilityHandle handle = GrantGraphAbility(entity, new PresentingNode("roll", log));
		handle.TryActivate(out _);
		entity.Abilities.PresentAbilities();

		handle.Cancel();
		entity.Abilities.PresentAbilities();

		log.Should().Equal("start roll#1", "end roll#1");
	}

	[Fact]
	[Trait("Abilities", "Presentation")]
	public void What_an_ended_instance_showed_ends_before_a_new_instance_starts_showing()
	{
		var log = new List<string>();
		var entity = new TestEntity(_tagsManager, _cuesManager);
		AbilityHandle handle = GrantGraphAbility(entity, new PresentingNode("roll", log));
		handle.TryActivate(out _);
		entity.Abilities.PresentAbilities();

		handle.Cancel();
		handle.TryActivate(out _).Should().BeTrue();
		entity.Abilities.PresentAbilities();

		log.Should().Equal("start roll#1", "end roll#1", "start roll#2");
	}

	[Fact]
	[Trait("Abilities", "Presentation")]
	public void Only_a_behavior_that_ends_while_presenting_is_kept_for_the_pass()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var presenting = new CountingBehavior { IsPresenting = true };
		var idle = new CountingBehavior();
		AbilityHandle presentingHandle = GrantAbility(entity, "Presenting", presenting);
		AbilityHandle idleHandle = GrantAbility(entity, "Idle", idle);
		presentingHandle.TryActivate(out _);
		idleHandle.TryActivate(out _);

		presentingHandle.Cancel();
		idleHandle.Cancel();
		entity.Abilities.PresentAbilities();

		presenting.PresentCount.Should().Be(1);
		idle.PresentCount.Should().Be(0);
	}

	[Fact]
	[Trait("Abilities", "Presentation")]
	public void An_instance_that_throws_does_not_keep_the_others_of_its_ability_from_being_presented()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var presented = new CountingBehavior();
		var behaviors = new Queue<IAbilityBehavior>([new ThrowingBehavior(), presented]);
		AbilityHandle handle = entity.Abilities.GrantAbilityPermanently(
			new AbilityData(
				"Twice",
				instancingPolicy: AbilityInstancingPolicy.PerExecution,
				behaviorFactory: behaviors.Dequeue),
			1,
			LevelComparison.Higher,
			null);
		handle.TryActivate(out _).Should().BeTrue();
		handle.TryActivate(out _).Should().BeTrue();

		entity.Abilities.Invoking(x => x.PresentAbilities()).Should().Throw<InvalidOperationException>();

		presented.PresentCount.Should().Be(1);
	}

	[Fact]
	[Trait("Abilities", "Presentation")]
	public void An_ability_that_throws_does_not_keep_later_abilities_from_being_presented()
	{
		var entity = new TestEntity(_tagsManager, _cuesManager);
		var presented = new CountingBehavior();
		GrantAbility(entity, "Throwing", new ThrowingBehavior()).TryActivate(out _);
		GrantAbility(entity, "Counting", presented).TryActivate(out _);

		entity.Abilities.Invoking(x => x.PresentAbilities()).Should().Throw<InvalidOperationException>();

		presented.PresentCount.Should().Be(1);
	}

	private static AbilityHandle GrantGraphAbility(TestEntity entity, Node node)
	{
		var graph = new Graph();
		graph.AddNode(node);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			node.InputPorts[StateNode<StateNodeContext>.InputPort]));

		return entity.Abilities.GrantAbilityPermanently(
			new AbilityData("Graph", behaviorFactory: () => new GraphAbilityBehavior(graph)),
			1,
			LevelComparison.Higher,
			null);
	}

	private static AbilityHandle GrantAbility(TestEntity entity, string name, IAbilityBehavior behavior)
	{
		return entity.Abilities.GrantAbilityPermanently(
			new AbilityData(name, behaviorFactory: () => behavior),
			1,
			LevelComparison.Higher,
			null);
	}

	private sealed class PresentingNode(string name, List<string> log) : StateNode<StateNodeContext>
	{
		private readonly string _name = name;

		private readonly List<string> _log = log;

		private int _starts;

		public override NodeNetworkKind NetworkKind => NodeNetworkKind.Presentation;

		protected override void OnActivate(GraphContext graphContext)
		{
		}

		protected override void OnDeactivate(GraphContext graphContext)
		{
		}

		protected override object? OnPresentationActivated(GraphContext graphContext)
		{
			string presentation = $"{_name}#{++_starts}";
			_log.Add($"start {presentation}");
			return presentation;
		}

		protected override void OnPresentationDeactivated(object? presentation)
		{
			_log.Add($"end {presentation}");
		}
	}

	private sealed class CountingBehavior : IAbilityBehavior
	{
		public bool IsPresenting { get; init; }

		public int PresentCount { get; private set; }

		public void OnStarted(AbilityBehaviorContext context)
		{
		}

		public void OnEnded(AbilityBehaviorContext context)
		{
		}

		public void OnPresent()
		{
			PresentCount++;
		}
	}

	private sealed class ThrowingBehavior : IAbilityBehavior
	{
		public void OnStarted(AbilityBehaviorContext context)
		{
		}

		public void OnEnded(AbilityBehaviorContext context)
		{
		}

		public void OnPresent()
		{
			throw new InvalidOperationException("Presentation failed.");
		}
	}
}
