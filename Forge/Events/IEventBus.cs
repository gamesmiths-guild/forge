// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Events;

/// <summary>
/// Interface for an event bus that allows raising and subscribing to events within the Forge framework.
/// </summary>
public interface IEventBus
{
	/// <summary>
	/// Raise a non-generic event.
	/// </summary>
	/// <param name="data">The event data to raise.</param>
	void Raise(in EventData data);

	/// <summary>
	/// Raise a generic event.
	/// </summary>
	/// <typeparam name="TPayload">The type of the payload associated with the event.</typeparam>
	/// <param name="data">The event data to raise.</param>
	void Raise<TPayload>(in EventData<TPayload> data);

	/// <summary>
	/// Subscribe using a tag; returns a token for later un-subscription.
	/// </summary>
	/// <param name="eventTag">The event tag to subscribe to.</param>
	/// <param name="handler">The handler to invoke when the event is raised.</param>
	/// <param name="priority">The priority of the subscription; higher values indicate higher priority.</param>
	/// <returns>The subscription token for later un-subscription.</returns>
	EventSubscriptionToken Subscribe(
		Tag eventTag,
		Action<EventData> handler,
		int priority = 0);

	/// <summary>
	/// Subscribe using a tag; returns a token for later un-subscription.
	/// </summary>
	/// <typeparam name="TPayload">The type of the payload associated with the event.</typeparam>
	/// <param name="eventTag">The event tag to subscribe to.</param>
	/// <param name="handler">The handler to invoke when the event is raised.</param>
	/// <param name="priority">The priority of the subscription; higher values indicate higher priority.</param>
	/// <returns>The subscription token for later un-subscription.</returns>
	EventSubscriptionToken Subscribe<TPayload>(
		Tag eventTag,
		Action<EventData<TPayload>> handler,
		int priority = 0);

	/// <summary>
	/// Unsubscribe using the provided token; returns <see langword="true"/> if successful.
	/// </summary>
	/// <param name="token">The subscription token to unsubscribe.</param>
	/// <returns><see langword="true"/> if un-subscription was successful; otherwise, <see langword="false"/>.</returns>
	bool Unsubscribe(EventSubscriptionToken token);
}
