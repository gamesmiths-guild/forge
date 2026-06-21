// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Tests.Helpers;

/// <summary>
/// A test payload carried by an event in the event-node tests.
/// </summary>
/// <param name="Amount">An arbitrary value the provider builds from an input and writes back to an output.</param>
internal sealed record TestEventPayload(int Amount);
