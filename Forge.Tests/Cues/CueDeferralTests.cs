// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Nodes.State;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

namespace Gamesmiths.Forge.Tests.Cues;

/// <summary>
/// Covers deferred cue dispatch: handlers wait for the presentation pass and see only net change.
/// </summary>
/// <param name="fixture">The fixture providing the <see cref="TagsManager"/>.</param>
public class CueDeferralTests(TagsAndCuesFixture fixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = fixture.TagsManager;

	[Fact]
	[Trait("Cues", "Deferral")]
	public void Handlers_are_called_at_once_unless_dispatch_is_deferred()
	{
		var log = new List<string>();
		CuesManager cues = CreateCues(log, out Tag cue);

		cues.ApplyCue(cue, null, null);

		log.Should().Equal("apply");
	}

	[Fact]
	[Trait("Cues", "Deferral")]
	public void Deferred_cues_wait_for_the_pass_and_arrive_in_the_order_they_fired()
	{
		var log = new List<string>();
		CuesManager cues = CreateCues(log, out Tag cue);
		cues.DeferDispatch = true;

		cues.ExecuteCue(cue, null, null);
		cues.ApplyCue(cue, null, null);
		cues.UpdateCue(cue, null, null);
		log.Should().BeEmpty();

		cues.PresentCues();

		log.Should().Equal("execute", "apply", "update");
	}

	[Fact]
	[Trait("Cues", "Deferral")]
	public void A_cue_applied_and_removed_before_the_pass_is_never_shown()
	{
		var log = new List<string>();
		CuesManager cues = CreateCues(log, out Tag cue);
		cues.DeferDispatch = true;

		cues.ApplyCue(cue, null, null);
		cues.UpdateCue(cue, null, null);
		cues.RemoveCue(cue, null, false);
		cues.PresentCues();

		log.Should().BeEmpty();
	}

	[Fact]
	[Trait("Cues", "Deferral")]
	public void Removing_a_cue_shown_in_an_earlier_pass_reaches_the_handlers()
	{
		var log = new List<string>();
		CuesManager cues = CreateCues(log, out Tag cue);
		cues.DeferDispatch = true;
		cues.ApplyCue(cue, null, null);
		cues.PresentCues();

		cues.RemoveCue(cue, null, true);
		cues.PresentCues();

		log.Should().Equal("apply", "remove interrupted");
	}

	[Fact]
	[Trait("Cues", "Deferral")]
	public void A_shown_cue_removed_applied_and_removed_again_between_passes_is_removed_once()
	{
		var log = new List<string>();
		CuesManager cues = CreateCues(log, out Tag cue);
		cues.DeferDispatch = true;
		cues.ApplyCue(cue, null, null);
		cues.PresentCues();

		cues.RemoveCue(cue, null, false);
		cues.ApplyCue(cue, null, null);
		cues.RemoveCue(cue, null, false);
		cues.PresentCues();

		log.Should().Equal("apply", "remove");
	}

	[Fact]
	[Trait("Cues", "Deferral")]
	public void Executed_cues_are_dispatched_even_between_an_apply_and_its_removal()
	{
		var log = new List<string>();
		CuesManager cues = CreateCues(log, out Tag cue);
		cues.DeferDispatch = true;

		cues.ApplyCue(cue, null, null);
		cues.ExecuteCue(cue, null, null);
		cues.RemoveCue(cue, null, false);
		cues.PresentCues();

		log.Should().Equal("execute");
	}

	[Fact]
	[Trait("Cues", "Deferral")]
	public void A_removal_cancels_only_the_apply_on_the_same_target()
	{
		var log = new List<string>();
		CuesManager cues = CreateCues(log, out Tag cue);
		cues.DeferDispatch = true;
		var first = new TestEntity(_tagsManager, cues);
		var second = new TestEntity(_tagsManager, cues);

		cues.ApplyCue(cue, first, null);
		cues.RemoveCue(cue, second, false);
		cues.PresentCues();

		log.Should().Equal("apply", "remove");
	}

	[Fact]
	[Trait("Cues", "Deferral")]
	public void Cues_fired_by_handlers_during_the_pass_wait_for_the_next_one()
	{
		var log = new List<string>();
		var cues = new CuesManager { DeferDispatch = true };
		var cue = Tag.RequestTag(_tagsManager, "test.cue1");
		cues.RegisterCue(cue, new LoggingCueHandler(log) { OnApplied = () => cues.ExecuteCue(cue, null, null) });

		cues.ApplyCue(cue, null, null);
		cues.PresentCues();
		log.Should().Equal("apply");

		cues.PresentCues();

		log.Should().Equal("apply", "execute");
	}

	[Fact]
	[Trait("Cues", "Deferral")]
	public void An_effect_applied_and_removed_within_a_frame_shows_no_cue()
	{
		var log = new List<string>();
		CuesManager cues = CreateCues(log, out Tag cue);
		cues.DeferDispatch = true;
		var target = new TestEntity(_tagsManager, cues);

		var effectData = new EffectData(
			"Flicker",
			new DurationData(
				DurationType.HasDuration,
				new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(10))),
			cues: [new CueData(new TagContainer(_tagsManager, [cue]), 0, 10, CueMagnitudeType.EffectLevel)]);

		ActiveEffectHandle? handle =
			target.EffectsManager.ApplyEffect(new Effect(effectData, new EffectOwnership(target, target)));
		target.EffectsManager.RemoveEffect(handle!);
		cues.PresentCues();

		log.Should().BeEmpty();
	}

	[Fact]
	[Trait("Cues", "Deferral")]
	public void Removing_one_of_two_overlapping_effects_cancels_only_its_own_apply()
	{
		var log = new List<string>();
		CuesManager cues = CreateCues(log, out Tag cue);
		cues.DeferDispatch = true;
		var target = new TestEntity(_tagsManager, cues);
		var cueData = new CueData(new TagContainer(_tagsManager, [cue]), 0, 10, CueMagnitudeType.EffectLevel);
		var duration = new DurationData(
			DurationType.HasDuration,
			new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(10)));
		var ownership = new EffectOwnership(target, target);

		ActiveEffectHandle? first = target.EffectsManager.ApplyEffect(
			new Effect(new EffectData("First", duration, cues: [cueData]), ownership, level: 1));
		target.EffectsManager.ApplyEffect(
			new Effect(new EffectData("Second", duration, cues: [cueData]), ownership, level: 2));
		target.EffectsManager.RemoveEffect(first!);
		cues.PresentCues();

		log.Should().Equal(["apply 2"], "only the second effect is still active, so only its apply is shown");
	}

	[Fact]
	[Trait("Cues", "Deferral")]
	public void Removing_one_of_two_cue_nodes_on_a_target_cancels_only_its_own_apply()
	{
		var log = new List<string>();
		CuesManager cues = CreateCues(log, out Tag cue);
		cues.DeferDispatch = true;
		var target = new TestEntity(_tagsManager, cues);

		GraphProcessor first = StartCueGraph(cue, target, magnitude: 1);
		StartCueGraph(cue, target, magnitude: 2);
		first.StopGraph();
		cues.PresentCues();

		log.Should().Equal(["apply 2"], "only the second node still holds the cue");
	}

	[Fact]
	[Trait("Cues", "Deferral")]
	public void A_handler_that_unregisters_during_the_pass_does_not_end_it()
	{
		var log = new List<string>();
		var cues = new CuesManager { DeferDispatch = true };
		var cue = Tag.RequestTag(_tagsManager, "test.cue1");
		LoggingCueHandler? handler = null;
		handler = new LoggingCueHandler(log) { OnApplied = () => cues.UnregisterCue(cue, handler!) };
		cues.RegisterCue(cue, handler);
		cues.RegisterCue(cue, new LoggingCueHandler(log));

		cues.ApplyCue(cue, null, null);
		cues.ExecuteCue(cue, null, null);
		cues.PresentCues();

		log.Should().Equal("apply", "apply", "execute");
	}

	private static GraphProcessor StartCueGraph(Tag cue, IForgeEntity target, int magnitude)
	{
		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectVariable("cue", cue);
		graph.VariableDefinitions.DefineObjectVariable("target", target);
		graph.VariableDefinitions.DefineVariable("magnitude", magnitude);

		var node = new CueNode();
		node.BindInput(CueNode.CueTagInput, "cue");
		node.BindInput(CueNode.TargetInput, "target");
		node.BindInput(CueNode.MagnitudeInput, "magnitude");
		graph.AddNode(node);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			node.InputPorts[StateNode<CueNodeContext>.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();
		return processor;
	}

	private CuesManager CreateCues(List<string> log, out Tag cue)
	{
		var cues = new CuesManager();
		cue = Tag.RequestTag(_tagsManager, "test.cue1");
		cues.RegisterCue(cue, new LoggingCueHandler(log));
		return cues;
	}

	private sealed class LoggingCueHandler(List<string> log) : ICueHandler
	{
		private readonly List<string> _log = log;

		public Action? OnApplied { get; init; }

		public void OnExecute(IForgeEntity? target, CueParameters? parameters)
		{
			_log.Add("execute");
		}

		public void OnApply(IForgeEntity? target, CueParameters? parameters)
		{
			_log.Add(parameters is CueParameters applied ? $"apply {applied.Magnitude}" : "apply");
			OnApplied?.Invoke();
		}

		public void OnRemove(IForgeEntity? target, bool interrupted)
		{
			_log.Add(interrupted ? "remove interrupted" : "remove");
		}

		public void OnUpdate(IForgeEntity? target, CueParameters? parameters)
		{
			_log.Add("update");
		}
	}
}
