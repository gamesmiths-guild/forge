// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript.Providers;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Object resolver that produces an <see cref="EventPayloadWriter"/> for the event-listener node's optional payload
/// output. The listener resolves it once on activation and uses it to decompose each received payload into the bound
/// graph variables.
/// </summary>
/// <param name="provider">The provider that decomposes the payload.</param>
/// <param name="bindings">The output-name to graph-variable bindings authored on the listener node.</param>
public class EventPayloadOutputResolver(
	IEventPayloadProvider provider,
	IReadOnlyDictionary<string, EventOutputBinding> bindings) : ObjectResolver<EventPayloadWriter>
{
	private readonly EventPayloadWriter _writer = new(provider, bindings);

	/// <inheritdoc/>
	public override EventPayloadWriter Resolve(GraphContext graphContext)
	{
		return _writer;
	}
}
