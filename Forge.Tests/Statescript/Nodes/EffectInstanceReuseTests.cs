// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Effects.Modifiers;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Nodes.Action;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Core;
using Gamesmiths.Forge.Tests.Helpers;

using static Gamesmiths.Forge.Tests.Helpers.NodeBindings;

namespace Gamesmiths.Forge.Tests.Statescript.Nodes;

public class EffectInstanceReuseTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Graph", "EffectInstanceReuse")]
	public void Stored_effect_instance_is_reused_and_updates_target_live_on_level_up()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);

		var effectData = new EffectData(
			"Level Scaling",
			new DurationData(DurationType.Infinite),
			[
				new Modifier(
					"TestAttributeSet.Attribute1",
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.ScalableFloat,
						new ScalableFloat(10, new Curve(
						[
							new CurveKey(1, 1),
							new CurveKey(2, 2),
						]))))
			],
			snapshotLevel: false);

		var graph = new Graph();

		// The EffectFromDataResolver builds a single Effect instance; SetVariableNode stores it in the "effect"
		// variable so it can be re-read and reused, and the ApplyEffectNode reads that same instance back through the
		// variable.
		graph.VariableDefinitions.DefineObjectProperty(
			"effectSource",
			new EffectFromDataResolver(effectData));
		graph.VariableDefinitions.DefineObjectVariable<Effect>("effect");
		graph.VariableDefinitions.DefineObjectProperty("effectVar", new EffectVariableResolver("effect"));
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("target", target);

		SetVariableNode storeEffect = CreateSetVariableNode("effectSource", "effect");
		ApplyEffectNode applyEffect = CreateApplyEffectNode("effectVar", "target");

		graph.AddNode(storeEffect);
		graph.AddNode(applyEffect);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			storeEffect.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			storeEffect.OutputPorts[ActionNode.OutputPort],
			applyEffect.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		// Applied at level 1 -> flat +10.
		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [11, 1, 10, 0]);

		processor.GraphContext.GraphVariables.TryGetObject("effect", out Effect? storedEffect).Should().BeTrue();
		storedEffect.Should().NotBeNull();

		// Mutating the stored instance updates the already-applied, non-snapshot effect live.
		storedEffect!.LevelUp();

		TestUtils.TestAttribute(target, "TestAttributeSet.Attribute1", [21, 1, 20, 0]);
	}
}
