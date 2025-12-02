// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Events;

/// <summary>
/// Represents a unique identifier for an event subscription.
/// </summary>
/// <param name="Id">The unique identifier for the subscription.</param>
public readonly record struct EventSubscriptionToken(Guid Id);
