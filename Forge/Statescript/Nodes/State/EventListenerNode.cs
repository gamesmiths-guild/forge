// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Events;
using Gamesmiths.Forge.Statescript.Ports;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Statescript.Nodes.State;

/// <summary>
/// Subscribes to one or more event tags on a chosen entity's <see cref="EventManager"/> while active and emits the
/// <c>OnEvent</c> port every time a matching event is raised.
/// </summary>
/// <remarks>
/// <para>The event-tag input accepts a single <see cref="Tag"/> or an array of tags; the node subscribes to each. The
/// listen-on input selects the entity whose <see cref="IForgeEntity.Events"/> bus is observed.</para>
/// <para>On each matching event the node writes the optional built-in outputs (Source, Target, Event Magnitude) and,
/// when a payload provider is bound, decomposes <see cref="EventData.Payload"/> into the provider's output variables,
/// then emits <c>OnEvent</c>. The node has no timer: it stays active until deactivated externally, unsubscribing on
/// deactivation.</para>
/// <para>When a payload provider is bound, the node subscribes through the provider's typed payload type
/// (<see cref="EventManager.Subscribe{TPayload}(Tag, Action{EventData{TPayload}}, int)"/>), so generic raises are
/// received with no boxing and the typed payload is decomposed directly. When no provider is bound, the node subscribes
/// non-generically and is a catch-all: it also receives generic raises with the payload boxed into
/// <see cref="EventData.Payload"/>. Either way the handler emits synchronously from the raise call.</para>
/// </remarks>
public class EventListenerNode : StateNode<EventListenerNodeContext>
{
	/// <summary>
	/// Input property index for the event tag(s) to listen for.
	/// </summary>
	public const byte EventTagInput = 0;

	/// <summary>
	/// Input property index for the entity whose event bus is observed.
	/// </summary>
	public const byte ListenOnInput = 1;

	/// <summary>
	/// Input property index for the optional payload writer (decomposes received payloads into output variables).
	/// </summary>
	public const byte PayloadOutputInput = 2;

	/// <summary>
	/// Output variable index for the event source entity.
	/// </summary>
	public const byte SourceOutput = 0;

	/// <summary>
	/// Output variable index for the event target entity.
	/// </summary>
	public const byte TargetOutput = 1;

	/// <summary>
	/// Output variable index for the event magnitude.
	/// </summary>
	public const byte MagnitudeOutput = 2;

	/// <summary>
	/// Output port index for the per-event signal.
	/// </summary>
	public const byte OnEventPort = 4;

	/// <inheritdoc/>
	public override string Description =>
		"Listens for events while active and emits OnEvent each time a matching event fires.";

	/// <inheritdoc/>
	protected override void DefinePorts(List<InputPort> inputPorts, List<OutputPort> outputPorts)
	{
		base.DefinePorts(inputPorts, outputPorts);
		outputPorts.Add(CreatePort<EventPort>(OnEventPort, "OnEvent"));
	}

	/// <inheritdoc/>
	protected override void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Event Tags", typeof(Tag)));
		inputProperties.Add(new InputProperty("Listen On", typeof(IForgeEntity)));
		inputProperties.Add(new InputProperty("Payload", typeof(EventPayloadWriter)));
		outputVariables.Add(new OutputVariable("Source", typeof(IForgeEntity)));
		outputVariables.Add(new OutputVariable("Target", typeof(IForgeEntity)));
		outputVariables.Add(new OutputVariable("Magnitude", typeof(float)));
	}

	/// <inheritdoc/>
	protected override void OnActivate(GraphContext graphContext)
	{
		EventListenerNodeContext nodeContext = graphContext.GetNodeContext<EventListenerNodeContext>(NodeID);
		nodeContext.Tokens.Clear();
		nodeContext.SubscribedManager = null;
		nodeContext.PayloadWriter = null;

		EventManager? manager = ResolveListenManager(graphContext);
		if (manager is null)
		{
			return;
		}

		nodeContext.SubscribedManager = manager;
		EventPayloadWriter? payloadWriter = ResolvePayloadWriter(graphContext);
		nodeContext.PayloadWriter = payloadWriter;

		List<Tag> tags = ResolveEventTags(graphContext);
		for (int i = 0; i < tags.Count; i++)
		{
			Tag tag = tags[i];

			// With a payload provider, subscribe through its typed (non-boxing) path; otherwise subscribe
			// non-generically (a catch-all that also receives generic raises with the payload boxed).
			EventSubscriptionToken token;
			if (payloadWriter is not null)
			{
				token = payloadWriter.Subscribe(
					manager,
					tag,
					graphContext,
					(source, target, magnitude) => OnTypedEventReceived(graphContext, source, target, magnitude));
			}
			else
			{
				token = manager.Subscribe(tag, data => OnEventReceived(data, graphContext));
			}

			nodeContext.Tokens.Add(token);
		}
	}

	/// <inheritdoc/>
	protected override void OnDeactivate(GraphContext graphContext)
	{
		EventListenerNodeContext nodeContext = graphContext.GetNodeContext<EventListenerNodeContext>(NodeID);

		if (nodeContext.SubscribedManager is not null)
		{
			for (int i = 0; i < nodeContext.Tokens.Count; i++)
			{
				nodeContext.SubscribedManager.Unsubscribe(nodeContext.Tokens[i]);
			}
		}

		nodeContext.Tokens.Clear();
		nodeContext.SubscribedManager = null;
		nodeContext.PayloadWriter = null;
	}

	private static void WriteEntityOutput(GraphContext graphContext, OutputVariable output, IForgeEntity? value)
	{
		if (output.BoundName == StringKey.Empty)
		{
			return;
		}

		Variables? variables = output.Scope == VariableScope.Shared
			? graphContext.SharedVariables
			: graphContext.GraphVariables;
		variables?.SetObject(output.BoundName, value);
	}

	private static void WriteMagnitudeOutput(GraphContext graphContext, OutputVariable output, float value)
	{
		if (output.BoundName == StringKey.Empty)
		{
			return;
		}

		Variables? variables = output.Scope == VariableScope.Shared
			? graphContext.SharedVariables
			: graphContext.GraphVariables;

		// Floating-point graph variables are double-backed, so widen the float magnitude before storing it.
		variables?.SetVar(output.BoundName, (double)value);
	}

	private void OnTypedEventReceived(
		GraphContext graphContext,
		IForgeEntity? source,
		IForgeEntity? target,
		float magnitude)
	{
		// The provider already decomposed the typed payload into its bound variables before this callback.
		WriteBuiltInOutputsAndEmit(graphContext, source, target, magnitude);
	}

	private void OnEventReceived(EventData data, GraphContext graphContext)
	{
		// Non-generic (provider-less) path: there is no provider to decompose the payload, so only the built-in fields
		// are written.
		WriteBuiltInOutputsAndEmit(graphContext, data.Source, data.Target, data.EventMagnitude);
	}

	private void WriteBuiltInOutputsAndEmit(
		GraphContext graphContext,
		IForgeEntity? source,
		IForgeEntity? target,
		float magnitude)
	{
		if (!graphContext.HasNodeContext(NodeID))
		{
			return;
		}

		if (!graphContext.GetNodeContext<EventListenerNodeContext>(NodeID).Active)
		{
			return;
		}

		WriteEntityOutput(graphContext, OutputVariables[SourceOutput], source);
		WriteEntityOutput(graphContext, OutputVariables[TargetOutput], target);
		WriteMagnitudeOutput(graphContext, OutputVariables[MagnitudeOutput], magnitude);

		OutputPorts[OnEventPort].EmitMessage(graphContext);
	}

	private EventManager? ResolveListenManager(GraphContext graphContext)
	{
		StringKey inputName = InputProperties[ListenOnInput].BoundName;

		return inputName != StringKey.Empty
			&& graphContext.TryResolveObject(inputName, typeof(IForgeEntity), out object? resolved)
			&& resolved is IForgeEntity entity
			? entity.Events
			: null;
	}

	private List<Tag> ResolveEventTags(GraphContext graphContext)
	{
		StringKey inputName = InputProperties[EventTagInput].BoundName;
		var tags = new List<Tag>();

		if (graphContext.TryResolveObjectArray(inputName, typeof(Tag), out object?[]? resolvedArray))
		{
			for (int i = 0; i < resolvedArray.Length; i++)
			{
				if (resolvedArray[i] is Tag tag)
				{
					tags.Add(tag);
				}
			}
		}
		else if (graphContext.TryResolveObject(inputName, typeof(Tag), out object? resolved)
			&& resolved is Tag singleTag)
		{
			tags.Add(singleTag);
		}

		return tags;
	}

	private EventPayloadWriter? ResolvePayloadWriter(GraphContext graphContext)
	{
		StringKey inputName = InputProperties[PayloadOutputInput].BoundName;

		return inputName != StringKey.Empty
			&& graphContext.TryResolveObject(inputName, typeof(EventPayloadWriter), out object? resolved)
			? resolved as EventPayloadWriter
			: null;
	}
}
