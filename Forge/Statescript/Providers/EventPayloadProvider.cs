// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Events;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Statescript.Providers;

/// <summary>
/// Base class for typed event-payload providers. Override <see cref="CreatePayload"/> to build the payload from the
/// current graph state. The listener side works out of the box: <see cref="Outputs"/> and <see cref="WriteOutputs"/>
/// default to the payload's own members, so a plain payload record needs neither. The base seals the boxing to and from
/// <see cref="object"/> used by the non-generic event path.
/// </summary>
/// <typeparam name="TPayload">The payload type produced and consumed by this provider.</typeparam>
/// <remarks>
/// Override <see cref="Inputs"/> to expose authored resolvers in the editor and read them from
/// <see cref="EventPayloadInputs"/>. Override <see cref="Outputs"/> and <see cref="WriteOutputs"/> together only when
/// the listener side is not a straight projection of the payload, for example to expose a computed or renamed value.
/// </remarks>
public abstract class EventPayloadProvider<TPayload> : IEventPayloadProvider
{
	private readonly Action<TPayload, EventPayloadOutputs>[] _reflectedWriters;

	private readonly EventPayloadOutput[] _reflectedOutputs;

	/// <summary>
	/// Builds the payload for the current graph execution. Read whatever graph state the payload is derived from
	/// (graph/shared variables, attributes, activation data, and so on) from <paramref name="graphContext"/>, and read
	/// declared <see cref="Inputs"/> from <paramref name="inputs"/>.
	/// </summary>
	/// <param name="graphContext">The graph execution context.</param>
	/// <param name="inputs">The resolved values for the provider's declared <see cref="Inputs"/>.</param>
	/// <returns>The payload to attach to the raised event.</returns>
	public abstract TPayload CreatePayload(GraphContext graphContext, EventPayloadInputs inputs);

	/// <inheritdoc/>
	public virtual IReadOnlyList<EventPayloadInput> Inputs => [];

	/// <summary>
	/// Gets the outputs this provider exposes to the event-listener node. Defaults to every public readable member of
	/// <typeparamref name="TPayload"/> that can be written to a graph variable, in declaration order. Override to
	/// reshape the set, and override <see cref="WriteOutputs"/> to match.
	/// </summary>
	public virtual IReadOnlyList<EventPayloadOutput> Outputs => _reflectedOutputs;

	/// <summary>
	/// Initializes a new instance of the <see cref="EventPayloadProvider{TPayload}"/> class, describing
	/// <typeparamref name="TPayload"/> once so the defaulted listener side costs no reflection per event. Providers are
	/// cached and shared by their registry, so this runs once per provider type.
	/// </summary>
	protected EventPayloadProvider()
	{
		_reflectedWriters = PayloadOutputWriters.Build<TPayload>(out _reflectedOutputs);
	}

	/// <summary>
	/// Writes the values of a received payload to the listener's bound graph variables through
	/// <paramref name="outputs"/>. The default writes each member declared by <see cref="Outputs"/> straight off the
	/// payload, which is what a listener normally wants; override it (together with <see cref="Outputs"/>) to emit
	/// computed or renamed values instead.
	/// </summary>
	/// <param name="payload">The payload carried by the received event.</param>
	/// <param name="outputs">The writer bound to the listener node's output variables.</param>
	public virtual void WriteOutputs(TPayload payload, EventPayloadOutputs outputs)
	{
		for (int i = 0; i < _reflectedWriters.Length; i++)
		{
			_reflectedWriters[i](payload, outputs);
		}
	}

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
