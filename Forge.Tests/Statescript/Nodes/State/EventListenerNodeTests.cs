// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Events;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Nodes.State;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;
using Gamesmiths.Forge.Tests.Helpers;

using static Gamesmiths.Forge.Tests.Helpers.NodeBindings;

namespace Gamesmiths.Forge.Tests.Statescript.Nodes.State;

public class EventListenerNodeTests(TagsAndCuesFixture tagsAndCuesFixture) : IClassFixture<TagsAndCuesFixture>
{
	private readonly TagsManager _tagsManager = tagsAndCuesFixture.TagsManager;

	[Fact]
	[Trait("Graph", "EventListener")]
	public void Listener_emits_on_event_and_writes_built_in_outputs_when_a_matching_event_fires()
	{
		var cuesManager = new CuesManager();
		var entity = new TestEntity(_tagsManager, cuesManager);
		var source = new TestEntity(_tagsManager, cuesManager);
		var eventTag = Tag.RequestTag(_tagsManager, "test.cue1");
		var tracking = new TrackingActionNode();

		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectVariable("eventTag", eventTag);
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("listenOn", entity);
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("sourceOut", null!);
		graph.VariableDefinitions.DefineVariable("magOut", 0.0);

		EventListenerNode listener = CreateEventListenerNode("eventTag", "listenOn");
		listener.BindOutput(EventListenerNode.SourceOutput, "sourceOut", VariableScope.Graph);
		listener.BindOutput(EventListenerNode.MagnitudeOutput, "magOut", VariableScope.Graph);
		graph.AddNode(listener);
		graph.AddNode(tracking);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			listener.InputPorts[StateNode<EventListenerNodeContext>.InputPort]));
		graph.AddConnection(new Connection(
			listener.OutputPorts[EventListenerNode.OnEventPort],
			tracking.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();
		tracking.ExecutionCount.Should().Be(0);

		entity.Events.Raise(new EventData
		{
			EventTags = eventTag.GetSingleTagContainer()!,
			Source = source,
			Target = entity,
			EventMagnitude = 5f,
		});

		tracking.ExecutionCount.Should().Be(1);
		processor.GraphContext.GraphVariables.TryGetObject("sourceOut", out IForgeEntity? capturedSource)
			.Should().BeTrue();
		capturedSource.Should().BeSameAs(source);
		processor.GraphContext.GraphVariables.TryGetVar("magOut", out double magnitude).Should().BeTrue();
		magnitude.Should().Be(5.0);
	}

	[Fact]
	[Trait("Graph", "EventListener")]
	public void Listener_writes_payload_outputs_through_the_provider()
	{
		var cuesManager = new CuesManager();
		var entity = new TestEntity(_tagsManager, cuesManager);
		var eventTag = Tag.RequestTag(_tagsManager, "test.cue1");

		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectVariable("eventTag", eventTag);
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("listenOn", entity);
		graph.VariableDefinitions.DefineVariable("amountOut", 0);
		graph.VariableDefinitions.DefineObjectProperty(
			"payloadOut",
			new EventPayloadOutputResolver(
				new TestEventPayloadProvider(),
				new Dictionary<string, EventOutputBinding>
				{
					[TestEventPayloadProvider.AmountKey] = new EventOutputBinding("amountOut", VariableScope.Graph),
				}));

		EventListenerNode listener = CreateEventListenerNode("eventTag", "listenOn");
		listener.BindInput(EventListenerNode.PayloadOutputInput, "payloadOut");
		graph.AddNode(listener);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			listener.InputPorts[StateNode<EventListenerNodeContext>.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();

		// The provider is typed, so the listener subscribes through the typed path; raise a typed event so the payload
		// is delivered without boxing.
		entity.Events.Raise(new EventData<TestEventPayload>
		{
			EventTags = eventTag.GetSingleTagContainer()!,
			Target = entity,
			Payload = new TestEventPayload(7),
		});

		processor.GraphContext.GraphVariables.TryGetVar("amountOut", out int amount).Should().BeTrue();
		amount.Should().Be(7);
	}

	[Fact]
	[Trait("Graph", "EventListener")]
	public void Listener_stops_receiving_events_after_deactivation()
	{
		ListenerGraph listenerGraph = BuildListenerGraph();

		listenerGraph.Processor.StartGraph();
		listenerGraph.Entity.Events.Raise(new EventData
		{
			EventTags = listenerGraph.EventTag.GetSingleTagContainer()!,
			Target = listenerGraph.Entity,
		});
		listenerGraph.Tracking.ExecutionCount.Should().Be(1);

		listenerGraph.Processor.StopGraph();
		listenerGraph.Entity.Events.Raise(new EventData
		{
			EventTags = listenerGraph.EventTag.GetSingleTagContainer()!,
			Target = listenerGraph.Entity,
		});

		listenerGraph.Tracking.ExecutionCount.Should().Be(1, "the listener unsubscribes on deactivation");
	}

	[Fact]
	[Trait("Graph", "EventListener")]
	public void Listener_ignores_events_with_non_matching_tags()
	{
		ListenerGraph listenerGraph = BuildListenerGraph();
		var otherTag = Tag.RequestTag(_tagsManager, "test.cue2");

		listenerGraph.Processor.StartGraph();
		listenerGraph.Entity.Events.Raise(new EventData
		{
			EventTags = otherTag.GetSingleTagContainer()!,
			Target = listenerGraph.Entity,
		});

		listenerGraph.Tracking.ExecutionCount.Should().Be(0);
	}

	[Fact]
	[Trait("Graph", "EventListener")]
	public void Listener_receives_generic_events_as_a_catch_all_subscriber()
	{
		ListenerGraph listenerGraph = BuildListenerGraph();

		listenerGraph.Processor.StartGraph();

		// Generic (typed) raise, like DamageExecution's Raise<DamageType>; the non-generic listener still catches it.
		listenerGraph.Entity.Events.Raise(new EventData<int>
		{
			EventTags = listenerGraph.EventTag.GetSingleTagContainer()!,
			Target = listenerGraph.Entity,
			Payload = 7,
		});

		listenerGraph.Tracking.ExecutionCount.Should().Be(1);
	}

	private ListenerGraph BuildListenerGraph()
	{
		var cuesManager = new CuesManager();
		var entity = new TestEntity(_tagsManager, cuesManager);
		var eventTag = Tag.RequestTag(_tagsManager, "test.cue1");
		var tracking = new TrackingActionNode();

		var graph = new Graph();
		graph.VariableDefinitions.DefineObjectVariable("eventTag", eventTag);
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("listenOn", entity);

		EventListenerNode listener = CreateEventListenerNode("eventTag", "listenOn");
		graph.AddNode(listener);
		graph.AddNode(tracking);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			listener.InputPorts[StateNode<EventListenerNodeContext>.InputPort]));
		graph.AddConnection(new Connection(
			listener.OutputPorts[EventListenerNode.OnEventPort],
			tracking.InputPorts[ActionNode.InputPort]));

		return new ListenerGraph(new GraphProcessor(graph), entity, eventTag, tracking);
	}

	private readonly record struct ListenerGraph(
		GraphProcessor Processor,
		TestEntity Entity,
		Tag EventTag,
		TrackingActionNode Tracking);
}
