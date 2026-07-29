// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Events;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Statescript.Providers;

/// <summary>
/// Base class for typed event-payload providers. Override <see cref="CreatePayload"/> to build the payload from the
/// current graph state, and <see cref="WriteOutputs"/> to extract its values into the listener's bound graph variables.
/// The base seals the boxing to and from <see cref="object"/> used by the non-generic event path.
/// </summary>
/// <typeparam name="TPayload">The payload type produced and consumed by this provider.</typeparam>
/// <remarks>
/// Override <see cref="Members"/> to declare the payload's members once. They become authored resolvers on the raise
/// node, read from <see cref="EventPayloadInputs"/>, and graph-variable bindings on the listener node, written through
/// <see cref="EventPayloadOutputs"/>.
/// </remarks>
public abstract class EventPayloadProvider<TPayload> : IEventPayloadProvider
{
	/// <summary>
	/// Builds the payload for the current graph execution. Read whatever graph state the payload is derived from
	/// (graph/shared variables, attributes, activation data, and so on) from <paramref name="graphContext"/>, and read
	/// declared <see cref="Members"/> from <paramref name="inputs"/>.
	/// </summary>
	/// <param name="graphContext">The graph execution context.</param>
	/// <param name="inputs">The resolved values for the provider's declared <see cref="Members"/>.</param>
	/// <returns>The payload to attach to the raised event.</returns>
	public abstract TPayload CreatePayload(GraphContext graphContext, EventPayloadInputs inputs);

	/// <summary>
	/// Writes the values of a received payload to the listener's bound graph variables through
	/// <paramref name="outputs"/>, keyed by the declared <see cref="Members"/>.
	/// </summary>
	/// <param name="payload">The payload carried by the received event.</param>
	/// <param name="outputs">The writer bound to the listener node's output variables.</param>
	public abstract void WriteOutputs(TPayload payload, EventPayloadOutputs outputs);

	/// <inheritdoc/>
	public virtual IReadOnlyList<EventPayloadMember> Members => [];

	/// <inheritdoc/>
	object IEventPayloadProvider.CreatePayload(GraphContext graphContext, EventPayloadInputs inputs)
	{
		return CreatePayload(graphContext, inputs)!;
	}

	/// <inheritdoc/>
	void IEventPayloadProvider.WriteOutputs(object payload, EventPayloadOutputs outputs)
	{
		WriteOutputs((TPayload)payload, outputs);
	}

	/// <inheritdoc/>
	EventSubscriptionToken IEventPayloadProvider.Subscribe(
		EventManager manager,
		Tag eventTag,
		GraphContext graphContext,
		IReadOnlyDictionary<string, EventOutputBinding> outputBindings,
		Action<IForgeEntity?, IForgeEntity?, float> onReceived)
	{
		var outputs = new EventPayloadOutputs(graphContext, outputBindings);

		// Subscribe through the typed path so the payload is never boxed; decompose it directly into the bound
		// variables.
		return manager.Subscribe<TPayload>(eventTag, data =>
		{
			WriteOutputs(data.Payload, outputs);
			onReceived(data.Source, data.Target, data.EventMagnitude);
		});
	}

	/// <inheritdoc/>
	void IEventPayloadProvider.Raise(
		EventManager manager,
		TagContainer eventTags,
		IForgeEntity? source,
		IForgeEntity? target,
		float magnitude,
		GraphContext graphContext,
		IReadOnlyDictionary<string, IPropertyResolver> inputResolvers)
	{
		TPayload payload = CreatePayload(graphContext, new EventPayloadInputs(graphContext, inputResolvers));

		// Raise through the typed path so the payload is never boxed and typed listeners receive it.
		manager.Raise(new EventData<TPayload>
		{
			EventTags = eventTags,
			Source = source,
			Target = target,
			EventMagnitude = magnitude,
			Payload = payload,
		});
	}
}
