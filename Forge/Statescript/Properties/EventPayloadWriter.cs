// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Events;
using Gamesmiths.Forge.Statescript.Providers;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Wraps an <see cref="IEventPayloadProvider"/> and its output-to-variable bindings for the event-listener node.
/// Created by <see cref="EventPayloadOutputResolver"/>: <c>Subscribe</c> sets up the provider's typed (non-boxing)
/// subscription that <c>EventListenerNode</c> uses while active; <c>Write</c> decomposes an already-boxed payload
/// directly into the bound graph variables.
/// </summary>
/// <param name="provider">The provider that decomposes the payload.</param>
/// <param name="bindings">The output-name to graph-variable bindings authored on the listener node.</param>
public sealed class EventPayloadWriter(
	IEventPayloadProvider provider,
	IReadOnlyDictionary<string, EventOutputBinding> bindings)
{
	private readonly IEventPayloadProvider _provider = provider;
	private readonly IReadOnlyDictionary<string, EventOutputBinding> _bindings = bindings;

	/// <summary>
	/// Subscribes to <paramref name="eventTag"/> through the provider's typed (non-boxing) event path, decomposing each
	/// received payload into the bound graph variables and invoking <paramref name="onReceived"/> with the event's
	/// source, target, and magnitude.
	/// </summary>
	/// <param name="manager">The event bus to subscribe to.</param>
	/// <param name="eventTag">The tag to subscribe to.</param>
	/// <param name="graphContext">The graph execution context whose variables receive the decomposed payload.</param>
	/// <param name="onReceived">Invoked for each received event with its source, target, and magnitude.</param>
	/// <returns>The subscription token to release on deactivation.</returns>
	public EventSubscriptionToken Subscribe(
		EventManager manager,
		Tag eventTag,
		GraphContext graphContext,
		Action<IForgeEntity?, IForgeEntity?, float> onReceived)
	{
		return _provider.Subscribe(manager, eventTag, graphContext, _bindings, onReceived);
	}

	/// <summary>
	/// Writes the values of <paramref name="payload"/> to the bound graph variables.
	/// </summary>
	/// <param name="payload">The payload carried by the received event.</param>
	/// <param name="graphContext">The graph execution context whose variables receive the values.</param>
	public void Write(object payload, GraphContext graphContext)
	{
		_provider.WriteOutputs(payload, new EventPayloadOutputs(graphContext, _bindings));
	}
}
