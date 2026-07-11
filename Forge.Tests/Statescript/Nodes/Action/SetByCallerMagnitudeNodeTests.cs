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
using Gamesmiths.Forge.Tests.Helpers;

using static Gamesmiths.Forge.Tests.Helpers.NodeBindings;

namespace Gamesmiths.Forge.Tests.Statescript.Nodes.Action;

public class SetByCallerMagnitudeNodeTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private const string TargetAttribute = "TestAttributeSet.Attribute90";

	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;
	private readonly CuesManager _cuesManager = tagsAndCuesFixture.CuesManager;

	[Fact]
	[Trait("Graph", "SetByCaller")]
	public void Set_by_caller_node_configures_magnitude_before_application()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);
		var identifierTag = Tag.RequestTag(_tagsManager, "color.red");
		var effect = new Effect(
			CreateSetByCallerEffectData(identifierTag, snapshot: true),
			new EffectOwnership(target, target));

		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectVariable("effect", effect);
		graph.VariableDefinitions.DefineObjectVariable("identifierTag", identifierTag);
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("target", target);
		graph.VariableDefinitions.DefineVariable("magnitude", 7.0);

		var setNode = new SetByCallerMagnitudeNode();
		setNode.BindInput(SetByCallerMagnitudeNode.EffectInput, "effect");
		setNode.BindInput(SetByCallerMagnitudeNode.TagInput, "identifierTag");
		setNode.BindInput(SetByCallerMagnitudeNode.MagnitudeInput, "magnitude");

		ApplyEffectNode applyNode = CreateApplyEffectNode("effect", "target");

		graph.AddNode(setNode);
		graph.AddNode(applyNode);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			setNode.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			setNode.OutputPorts[ActionNode.OutputPort],
			applyNode.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		target.Attributes[TargetAttribute].CurrentValue.Should().Be(97);
	}

	[Fact]
	[Trait("Graph", "SetByCaller")]
	public void Set_by_caller_node_live_updates_non_snapshot_magnitudes()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);
		var identifierTag = Tag.RequestTag(_tagsManager, "color.red");
		var effect = new Effect(
			CreateSetByCallerEffectData(identifierTag, snapshot: false, DurationType.Infinite),
			new EffectOwnership(target, target));

		effect.SetSetByCallerMagnitude(identifierTag, 5f);
		target.EffectsManager.ApplyEffect(effect);
		target.Attributes[TargetAttribute].CurrentValue.Should().Be(95);

		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectVariable("effect", effect);
		graph.VariableDefinitions.DefineObjectVariable("identifierTag", identifierTag);
		graph.VariableDefinitions.DefineVariable("magnitude", 9.0);

		var setNode = new SetByCallerMagnitudeNode();
		setNode.BindInput(SetByCallerMagnitudeNode.EffectInput, "effect");
		setNode.BindInput(SetByCallerMagnitudeNode.TagInput, "identifierTag");
		setNode.BindInput(SetByCallerMagnitudeNode.MagnitudeInput, "magnitude");

		graph.AddNode(setNode);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			setNode.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		target.Attributes[TargetAttribute].CurrentValue.Should().Be(99);
	}

	[Fact]
	[Trait("Graph", "SetByCaller")]
	public void Effect_from_data_resolver_applies_set_by_caller_magnitudes()
	{
		var target = new TestEntity(_tagsManager, _cuesManager);
		var identifierTag = Tag.RequestTag(_tagsManager, "color.red");
		EffectData effectData = CreateSetByCallerEffectData(identifierTag, snapshot: true);

		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectProperty(
			"effect",
			new EffectFromDataResolver(
				effectData,
				setByCallerMagnitudes: [new(identifierTag, new ConstantFloatResolver(6f))]));
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("target", target);

		ApplyEffectNode applyNode = CreateApplyEffectNode("effect", "target");

		graph.AddNode(applyNode);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			applyNode.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		target.Attributes[TargetAttribute].CurrentValue.Should().Be(96);
	}

	private static EffectData CreateSetByCallerEffectData(
		Tag identifierTag,
		bool snapshot,
		DurationType durationType = DurationType.Instant)
	{
		DurationData durationData = durationType == DurationType.Instant
			? new DurationData(DurationType.Instant)
			: new DurationData(durationType);

		return new EffectData(
			"SetByCaller Effect",
			durationData,
			[
				new Modifier(
					TargetAttribute,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(
						MagnitudeCalculationType.SetByCaller,
						setByCallerFloat: new SetByCallerFloat(identifierTag, snapshot)))
			]);
	}

	private sealed class ConstantFloatResolver(float value) : IPropertyResolver
	{
		private readonly float _value = value;

		public Type ValueType => typeof(float);

		public Variant128 Resolve(GraphContext graphContext)
		{
			return new Variant128(_value);
		}
	}
}
