// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Events;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Statescript.Providers;

/// <summary>
/// Builds and decomposes the custom <c>Payload</c> of an event for the event nodes. The same provider serves both
/// directions: <see cref="CreatePayload"/> builds the payload object the raise-event node attaches to an event, and
/// <see cref="WriteOutputs"/> extracts values from a received payload so the event-listener node can write them to
/// graph variables.
/// </summary>
/// <remarks>
/// <para>The recommended way is to derive from <see cref="EventPayloadProvider{TPayload}"/>, which supplies the default
/// (empty) input/output lists and the boxing so implementations work with the typed payload directly.</para>
/// <para>Declared <see cref="Inputs"/> render as nested resolvers on the raise-event node; declared
/// <see cref="Outputs"/> render as graph-variable bindings on the event-listener node.</para>
/// </remarks>
public interface IEventPayloadProvider
{
	/// <summary>
	/// Gets the authored inputs this provider exposes to the raise-event node. Defaults to an empty list for providers
	/// that build their payload entirely from the graph state.
	/// </summary>
	IReadOnlyList<EventPayloadInput> Inputs { get; }

	/// <summary>
	/// Gets the outputs this provider exposes to the event-listener node. Defaults to an empty list for providers whose
	/// payload is not decomposed into graph variables.
	/// </summary>
	IReadOnlyList<EventPayloadOutput> Outputs { get; }

	/// <summary>
	/// Builds the payload for an event raised by the graph.
	/// </summary>
	/// <param name="graphContext">The graph execution context.</param>
	/// <param name="inputs">The resolved values for the provider's declared <see cref="Inputs"/>.</param>
	/// <returns>The payload object attached to the raised event.</returns>
	object CreatePayload(GraphContext graphContext, EventPayloadInputs inputs);

	/// <summary>
	/// Writes the values of a received payload to the graph variables bound to the declared <see cref="Outputs"/>.
	/// </summary>
	/// <param name="payload">The payload carried by the received event.</param>
	/// <param name="outputs">The writer bound to the listener node's output variables.</param>
	void WriteOutputs(object payload, EventPayloadOutputs outputs);

	/// <summary>
	/// Subscribes to <paramref name="eventTag"/> on <paramref name="manager"/> through the typed (non-boxing) event
	/// path.
	/// Each received event's payload is decomposed into the graph variables described by
	/// <paramref name="outputBindings"/>, and <paramref name="onReceived"/> is invoked with the event's source, target,
	/// and magnitude. Use this instead of a non-generic subscription to avoid boxing value-type payloads.
	/// </summary>
	/// <param name="manager">The event bus to subscribe to.</param>
	/// <param name="eventTag">The tag to subscribe to.</param>
	/// <param name="graphContext">The graph execution context whose variables receive the decomposed payload.</param>
	/// <param name="outputBindings">The output-name to graph-variable bindings authored on the listener node.</param>
	/// <param name="onReceived">Invoked for each received event with its source, target, and magnitude.</param>
	/// <returns>The subscription token to release on deactivation.</returns>
	EventSubscriptionToken Subscribe(
		EventManager manager,
		Tag eventTag,
		GraphContext graphContext,
		IReadOnlyDictionary<string, EventOutputBinding> outputBindings,
		Action<IForgeEntity?, IForgeEntity?, float> onReceived);

	/// <summary>
	/// Builds the payload from the current graph state and raises a typed (non-boxing) event on
	/// <paramref name="manager"/>. Use this instead of attaching a boxed payload to a non-generic raise so that typed
	/// (<c>Subscribe&lt;TPayload&gt;</c>) listeners receive the event.
	/// </summary>
	/// <param name="manager">The event bus to raise on.</param>
	/// <param name="eventTags">The event's tag container.</param>
	/// <param name="source">The optional event source.</param>
	/// <param name="target">The event target.</param>
	/// <param name="magnitude">The event magnitude.</param>
	/// <param name="graphContext">The graph execution context the payload is built from.</param>
	/// <param name="inputResolvers">The resolvers for the provider's declared inputs, keyed by input name.</param>
	void Raise(
		EventManager manager,
		TagContainer eventTags,
		IForgeEntity? source,
		IForgeEntity? target,
		float magnitude,
		GraphContext graphContext,
		IReadOnlyDictionary<string, IPropertyResolver> inputResolvers);
}
