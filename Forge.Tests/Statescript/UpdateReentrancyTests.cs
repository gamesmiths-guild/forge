// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Components;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Statescript;

/// <summary>
/// Covers the re-entrancy guard on the update walks. Each walk runs off one reused buffer, so a nested pass would clear
/// and refill the list the outer walk is still indexing - advancing some entries twice and skipping others, silently.
/// The nested pass is refused instead: loudly when validation is on, quietly when it is off.
/// </summary>
/// <remarks>
/// Validation is a global switch, which is why the assembly disables test parallelization. Every test here restores it.
/// </remarks>
/// <param name="fixture">The fixture providing tags and cues managers.</param>
public sealed class UpdateReentrancyTests(TagsAndCuesFixture fixture) : IClassFixture<TagsAndCuesFixture>, IDisposable
{
	private readonly TagsManager _tagsManager = fixture.TagsManager;
	private readonly CuesManager _cuesManager = fixture.CuesManager;

	public void Dispose()
	{
		Validation.Enabled = false;
		GC.SuppressFinalize(this);
	}

	[Fact]
	[Trait("Graph", "Reentrancy")]
	public void Re_entering_a_graph_update_is_refused_and_reported()
	{
		Validation.Enabled = true;

		GraphProcessor? processor = null;
		var node = new CallbackStateNode(() => processor!.UpdateGraph(0.016));

		processor = StartGraphWith(node);

		processor.Invoking(x => x.UpdateGraph(0.016))
			.Should().Throw<ValidationException>()
			.WithMessage("*re-entered*");
	}

	[Fact]
	[Trait("Graph", "Reentrancy")]
	public void A_refused_graph_update_leaves_the_outer_walk_intact()
	{
		Validation.Enabled = false;

		GraphProcessor? processor = null;
		var node = new CallbackStateNode(() => processor!.UpdateGraph(0.016));

		processor = StartGraphWith(node);

		processor.Invoking(x => x.UpdateGraph(0.016)).Should().NotThrow();

		// One update, not two: the nested pass was dropped rather than allowed to walk the same node again.
		node.UpdateCount.Should().Be(1);
	}

	[Fact]
	[Trait("Graph", "Reentrancy")]
	public void A_refused_graph_update_does_not_advance_the_stamp()
	{
		Validation.Enabled = false;

		GraphProcessor? processor = null;
		var node = new CallbackStateNode(() => processor!.UpdateGraph(0.016));

		processor = StartGraphWith(node);

		processor.UpdateGraph(0.016);

		// A refused pass is not a pass, so a resolver keyed on the stamp must not see it as a new one.
		processor.GraphContext.UpdateStamp.Should().Be(1);
	}

	[Fact]
	[Trait("Graph", "Reentrancy")]
	public void A_graph_update_can_run_again_after_one_was_refused()
	{
		Validation.Enabled = false;

		GraphProcessor? processor = null;
		var node = new CallbackStateNode(() => processor!.UpdateGraph(0.016));

		processor = StartGraphWith(node);

		processor.UpdateGraph(0.016);
		processor.UpdateGraph(0.016);

		// The flag is cleared in a finally, so a refusal - or a node that throws - cannot wedge the graph shut.
		node.UpdateCount.Should().Be(2);
	}

	[Fact]
	[Trait("GraphBehavior", "Reentrancy")]
	public void Re_entering_an_ability_update_is_refused_and_reported()
	{
		Validation.Enabled = true;

		var entity = new TestEntity(_tagsManager, _cuesManager);
		var behavior = new ReenteringBehavior(entity);

		GrantAndActivate(entity, behavior);

		entity.Abilities.Invoking(x => x.UpdateAbilities(0.016))
			.Should().Throw<ValidationException>()
			.WithMessage("*re-entered*");
	}

	[Fact]
	[Trait("GraphBehavior", "Reentrancy")]
	public void A_refused_ability_update_leaves_the_outer_walk_intact()
	{
		Validation.Enabled = false;

		var entity = new TestEntity(_tagsManager, _cuesManager);
		var behavior = new ReenteringBehavior(entity);

		GrantAndActivate(entity, behavior);

		entity.Abilities.Invoking(x => x.UpdateAbilities(0.016)).Should().NotThrow();

		behavior.UpdateCount.Should().Be(1);
	}

	private static GraphProcessor StartGraphWith(CallbackStateNode node)
	{
		var graph = new Graph();
		graph.AddNode(node);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			node.InputPorts[StateNode<StateNodeContext>.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		return processor;
	}

	private static void GrantAndActivate(TestEntity entity, IAbilityBehavior behavior)
	{
		var abilityData = new AbilityData("ReentrantGraph", behaviorFactory: () => behavior);

		var grantConfig = new GrantAbilityConfig(
			abilityData,
			new ScalableInt(1),
			AbilityDeactivationPolicy.CancelImmediately,
			AbilityDeactivationPolicy.CancelImmediately,
			false,
			false,
			LevelComparison.Higher);

		var effectData = new EffectData(
			"GrantReentrantGraph",
			new DurationData(DurationType.Infinite),
			effectComponents: [new GrantAbilityEffectComponent([grantConfig])]);

		entity.EffectsManager.ApplyEffect(new Effect(effectData, new EffectOwnership(entity, entity)));

		AbilityHandle handle = entity.Abilities.GrantedAbilities.First();
		handle.TryActivate(out _).Should().BeTrue();
	}
}
