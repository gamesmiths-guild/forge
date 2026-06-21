// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Events;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Nodes.Action;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

using static Gamesmiths.Forge.Tests.Helpers.NodeBindings;

namespace Gamesmiths.Forge.Tests.Statescript.Nodes.Action;

public class RaiseEventNodeTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;

	[Fact]
	[Trait("Graph", "RaiseEvent")]
	public void Raise_event_node_raises_event_with_resolved_fields()
	{
		var cuesManager = new CuesManager();
		var target = new TestEntity(_tagsManager, cuesManager);
		var source = new TestEntity(_tagsManager, cuesManager);
		var eventTag = Tag.RequestTag(_tagsManager, "test.cue1");

		EventData? captured = null;
		target.Events.Subscribe(eventTag, data => captured = data);

		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectVariable("eventTag", eventTag);
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("target", target);
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("source", source);
		graph.VariableDefinitions.DefineVariable("magnitude", 25f);

		RaiseEventNode node = CreateRaiseEventNode("eventTag", "target");
		node.BindInput(RaiseEventNode.SourceInput, "source");
		node.BindInput(RaiseEventNode.MagnitudeInput, "magnitude");
		graph.AddNode(node);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			node.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		captured.Should().NotBeNull();
		captured!.Value.EventTags.HasTag(eventTag).Should().BeTrue();
		captured.Value.Source.Should().BeSameAs(source);
		captured.Value.Target.Should().BeSameAs(target);
		captured.Value.EventMagnitude.Should().Be(25f);
		captured.Value.Payload.Should().BeNull();
	}

	[Fact]
	[Trait("Graph", "RaiseEvent")]
	public void Raise_event_node_combines_multiple_tags_into_one_event()
	{
		var cuesManager = new CuesManager();
		var target = new TestEntity(_tagsManager, cuesManager);
		var firstTag = Tag.RequestTag(_tagsManager, "test.cue1");
		var secondTag = Tag.RequestTag(_tagsManager, "test.cue2");

		EventData? captured = null;
		target.Events.Subscribe(firstTag, data => captured = data);

		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectArrayVariable("eventTag", firstTag, secondTag);
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("target", target);

		RaiseEventNode node = CreateRaiseEventNode("eventTag", "target");
		graph.AddNode(node);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			node.InputPorts[ActionNode.InputPort]));

		new GraphProcessor(graph).StartGraph();

		captured.Should().NotBeNull();
		captured!.Value.EventTags.HasTag(firstTag).Should().BeTrue();
		captured.Value.EventTags.HasTag(secondTag).Should().BeTrue();
	}

	[Fact]
	[Trait("Graph", "RaiseEvent")]
	public void Raise_event_node_raises_on_every_target()
	{
		var cuesManager = new CuesManager();
		var firstTarget = new TestEntity(_tagsManager, cuesManager);
		var secondTarget = new TestEntity(_tagsManager, cuesManager);
		var eventTag = Tag.RequestTag(_tagsManager, "test.cue1");

		bool firstFired = false;
		bool secondFired = false;
		firstTarget.Events.Subscribe(eventTag, _ => firstFired = true);
		secondTarget.Events.Subscribe(eventTag, _ => secondFired = true);

		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectVariable("eventTag", eventTag);
		graph.VariableDefinitions.DefineObjectArrayVariable<IForgeEntity>("target", firstTarget, secondTarget);

		RaiseEventNode node = CreateRaiseEventNode("eventTag", "target");
		graph.AddNode(node);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			node.InputPorts[ActionNode.InputPort]));

		new GraphProcessor(graph).StartGraph();

		firstFired.Should().BeTrue();
		secondFired.Should().BeTrue();
	}

	[Fact]
	[Trait("Graph", "RaiseEvent")]
	public void Raise_event_node_raises_a_typed_event_with_the_provider_payload()
	{
		var cuesManager = new CuesManager();
		var target = new TestEntity(_tagsManager, cuesManager);
		var eventTag = Tag.RequestTag(_tagsManager, "test.cue1");

		// A typed subscriber receives the event only if the node raises through the typed path (with no boxing).
		TestEventPayload? captured = null;
		target.Events.Subscribe<TestEventPayload>(eventTag, data => captured = data.Payload);

		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectVariable("eventTag", eventTag);
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("target", target);
		graph.VariableDefinitions.DefineObjectProperty(
			"payload",
			new EventPayloadResolver(
				new TestEventPayloadProvider(),
				new Dictionary<string, IPropertyResolver>
				{
					[TestEventPayloadProvider.AmountKey] = new VariantResolver(new Variant128(42), typeof(int)),
				}));

		RaiseEventNode node = CreateRaiseEventNode("eventTag", "target");
		node.BindInput(RaiseEventNode.PayloadInput, "payload");
		graph.AddNode(node);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			node.InputPorts[ActionNode.InputPort]));

		new GraphProcessor(graph).StartGraph();

		captured.Should().NotBeNull();
		captured!.Amount.Should().Be(42);
	}
}
