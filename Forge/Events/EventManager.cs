// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Events;

/// <summary>
/// Per-entity event bus that supports both non-generic and generic (typed) event subscriptions.
/// Subscriptions are ordered by priority (higher priority invoked first).
/// Generic handlers are invoked without boxing. Non-generic subscriptions are catch-all: a generic raise also reaches
/// them with the payload boxed into <see cref="EventData.Payload"/>. A non-generic raise does NOT reach generic
/// (typed) handlers.
/// </summary>
/// <remarks>
/// Handlers may subscribe and unsubscribe while an event is being raised. A handler unsubscribed mid-raise is not
/// invoked for the rest of it, and one subscribed mid-raise is first invoked by the next raise.
/// </remarks>
public sealed class EventManager
{
	private readonly Dictionary<Type, Subscription[]> _genericByType = [];

	// Replaced rather than mutated on every (un)subscription, so a raise walks the arrays it started with and a handler
	// changing the subscriptions can't shift the walk.
	private Subscription[] _nonGeneric = [];

	/// <summary>
	/// Raise a non-generic event.
	/// </summary>
	/// <param name="data">The event data to raise.</param>
	public void Raise(in EventData data)
	{
		InvokeNonGenericHandlers(_nonGeneric, data);
	}

	/// <summary>
	/// Raise a generic event.
	/// </summary>
	/// <typeparam name="TPayload">The type of the payload associated with the event.</typeparam>
	/// <param name="data">The event data to raise.</param>
	public void Raise<TPayload>(in EventData<TPayload> data)
	{
		// Taken before the typed handlers run, so a catch-all one of them subscribes waits for the next raise.
		Subscription[] nonGeneric = _nonGeneric;

		if (_genericByType.TryGetValue(typeof(TPayload), out Subscription[]? typedSubscriptions))
		{
			for (int i = 0; i < typedSubscriptions.Length; i++)
			{
				Subscription subscription = typedSubscriptions[i];
				if (!subscription.IsActive || !data.EventTags.HasTag(subscription.EventTag))
				{
					continue;
				}

				((Action<EventData<TPayload>>)subscription.Handler).Invoke(data);
			}
		}

		// Non-generic subscriptions are catch-all: a generic raise also reaches them with the payload boxed. Only build
		// the boxed event when such subscribers exist, so typed-only buses pay no boxing cost.
		if (nonGeneric.Length > 0)
		{
			InvokeNonGenericHandlers(nonGeneric, new EventData
			{
				EventTags = data.EventTags,
				Source = data.Source,
				Target = data.Target,
				EventMagnitude = data.EventMagnitude,
				Payload = data.Payload,
			});
		}
	}

	/// <summary>
	/// Subscribe using a tag; returns a token for later un-subscription.
	/// </summary>
	/// <param name="eventTag">The event tag to subscribe to.</param>
	/// <param name="handler">The handler to invoke when the event is raised.</param>
	/// <param name="priority">The priority of the subscription; higher values indicate higher priority.</param>
	/// <returns>The subscription token for later un-subscription.</returns>
	public EventSubscriptionToken Subscribe(Tag eventTag, Action<EventData> handler, int priority = 0)
	{
		var token = new EventSubscriptionToken(Guid.NewGuid());
		_nonGeneric = Insert(_nonGeneric, new Subscription(token, eventTag, priority, handler));
		return token;
	}

	/// <summary>
	/// Subscribe using a tag; returns a token for later un-subscription.
	/// </summary>
	/// <typeparam name="TPayload">The type of the payload associated with the event.</typeparam>
	/// <param name="eventTag">The event tag to subscribe to.</param>
	/// <param name="handler">The handler to invoke when the event is raised.</param>
	/// <param name="priority">The priority of the subscription; higher values indicate higher priority.</param>
	/// <returns>The subscription token for later un-subscription.</returns>
	public EventSubscriptionToken Subscribe<TPayload>(
		Tag eventTag,
		Action<EventData<TPayload>> handler,
		int priority = 0)
	{
		var token = new EventSubscriptionToken(Guid.NewGuid());
		Type key = typeof(TPayload);

		_genericByType[key] = Insert(
			_genericByType.TryGetValue(key, out Subscription[]? subscriptions) ? subscriptions : [],
			new Subscription(token, eventTag, priority, handler));
		return token;
	}

	/// <summary>
	/// Unsubscribe using the provided token; returns <see langword="true"/> if successful.
	/// </summary>
	/// <param name="token">The subscription token to unsubscribe.</param>
	/// <returns><see langword="true"/> if un-subscription was successful; otherwise, <see langword="false"/>.</returns>
	public bool Unsubscribe(EventSubscriptionToken token)
	{
		if (TryRemove(_nonGeneric, token, out Subscription[] remaining))
		{
			_nonGeneric = remaining;
			return true;
		}

		foreach (KeyValuePair<Type, Subscription[]> keyValuePair in _genericByType)
		{
			if (!TryRemove(keyValuePair.Value, token, out remaining))
			{
				continue;
			}

			if (remaining.Length == 0)
			{
				_genericByType.Remove(keyValuePair.Key);
			}
			else
			{
				_genericByType[keyValuePair.Key] = remaining;
			}

			return true;
		}

		return false;
	}

	// Inserts after every subscription of equal or higher priority, so equal priorities keep subscription order.
	private static Subscription[] Insert(Subscription[] subscriptions, Subscription subscription)
	{
		int index = 0;
		while (index < subscriptions.Length && subscriptions[index].Priority >= subscription.Priority)
		{
			index++;
		}

		var result = new Subscription[subscriptions.Length + 1];
		Array.Copy(subscriptions, result, index);
		result[index] = subscription;
		Array.Copy(subscriptions, index, result, index + 1, subscriptions.Length - index);
		return result;
	}

	private static bool TryRemove(
		Subscription[] subscriptions,
		EventSubscriptionToken token,
		out Subscription[] remaining)
	{
		int index = Array.FindIndex(subscriptions, x => x.Token == token);
		if (index < 0)
		{
			remaining = subscriptions;
			return false;
		}

		// A raise in progress may still hold the old array; this keeps it from invoking the removed handler.
		subscriptions[index].IsActive = false;

		remaining = new Subscription[subscriptions.Length - 1];
		Array.Copy(subscriptions, remaining, index);
		Array.Copy(subscriptions, index + 1, remaining, index, subscriptions.Length - index - 1);
		return true;
	}

	private static void InvokeNonGenericHandlers(Subscription[] subscriptions, in EventData data)
	{
		for (int i = 0; i < subscriptions.Length; i++)
		{
			Subscription sub = subscriptions[i];
			if (!sub.IsActive || !data.EventTags.HasTag(sub.EventTag))
			{
				continue;
			}

			((Action<EventData>)sub.Handler).Invoke(data);
		}
	}

	private sealed class Subscription(EventSubscriptionToken token, Tag eventTag, int priority, Delegate handler)
	{
		public EventSubscriptionToken Token { get; } = token;

		public Tag EventTag { get; } = eventTag;

		public int Priority { get; } = priority;

		public Delegate Handler { get; } = handler;

		public bool IsActive { get; set; } = true;
	}
}
