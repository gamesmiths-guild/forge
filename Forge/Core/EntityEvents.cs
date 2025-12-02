// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Events;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Core;

/// <summary>
/// Per-entity event bus that supports both non-generic and generic (typed) event subscriptions.
/// Subscriptions are ordered by priority (higher priority invoked first).
/// Generic handlers are invoked without boxing. Generic raises do NOT forward to non-generic handlers.
/// </summary>
public sealed class EntityEvents : IEventBus
{
	private readonly List<NonGenericSubscription> _nonGeneric = [];
	private readonly Dictionary<Type, List<GenericSubscription>> _genericByType = [];

	/// <inheritdoc/>
	public void Raise(in EventData data)
	{
		for (var i = 0; i < _nonGeneric.Count; i++)
		{
			NonGenericSubscription sub = _nonGeneric[i];
			if (!data.EventTags.HasTag(sub.EventTag))
			{
				continue;
			}

			sub.Handler.Invoke(data);
		}
	}

	/// <inheritdoc/>
	public void Raise<TPayload>(in EventData<TPayload> data)
	{
		Type key = typeof(TPayload);
		if (_genericByType.TryGetValue(key, out List<GenericSubscription>? typedList))
		{
			for (var i = 0; i < typedList.Count; i++)
			{
				GenericSubscription sub = typedList[i];
				if (!data.EventTags.HasTag(sub.EventTag))
				{
					continue;
				}

				((Action<EventData<TPayload>>)sub.Handler).Invoke(data);
			}
		}
	}

	/// <inheritdoc/>
	public EventSubscriptionToken Subscribe(Tag eventTag, Action<EventData> handler, int priority = 0)
	{
		var token = new EventSubscriptionToken(Guid.NewGuid());
		_nonGeneric.Add(new NonGenericSubscription(token, eventTag, priority, handler));

		_nonGeneric.Sort((a, b) => b.Priority.CompareTo(a.Priority));
		return token;
	}

	/// <inheritdoc/>
	public EventSubscriptionToken Subscribe<TPayload>(Tag eventTag, Action<EventData<TPayload>> handler, int priority = 0)
	{
		var token = new EventSubscriptionToken(Guid.NewGuid());
		Type key = typeof(TPayload);

		if (!_genericByType.TryGetValue(key, out List<GenericSubscription>? list))
		{
			list = [];
			_genericByType[key] = list;
		}

		list.Add(new GenericSubscription(token, eventTag, priority, handler));

		list.Sort((a, b) => b.Priority.CompareTo(a.Priority));
		return token;
	}

	/// <inheritdoc/>
	public bool Unsubscribe(EventSubscriptionToken token)
	{
		var removed = _nonGeneric.RemoveAll(x => x.Token == token) > 0;

		List<Type>? keysToRemove = null;
		foreach (KeyValuePair<Type, List<GenericSubscription>> keyValuePair in _genericByType)
		{
			List<GenericSubscription> list = keyValuePair.Value;
			if (list.RemoveAll(x => x.Token == token) > 0)
			{
				removed = true;
			}

			if (list.Count == 0)
			{
				keysToRemove ??= [];
				keysToRemove.Add(keyValuePair.Key);
			}
		}

		if (keysToRemove is null)
		{
			return removed;
		}

		for (var i = 0; i < keysToRemove.Count; i++)
		{
			_genericByType.Remove(keysToRemove[i]);
		}

		return removed;
	}

	private readonly record struct NonGenericSubscription(
		EventSubscriptionToken Token,
		Tag EventTag,
		int Priority,
		Action<EventData> Handler);

	private readonly record struct GenericSubscription(
		EventSubscriptionToken Token,
		Tag EventTag,
		int Priority,
		Delegate Handler);
}
