// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript.Providers;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Object resolver that produces an <see cref="EventPayloadRaiser"/> for the optional payload input of
/// <c>RaiseEventNode</c>. The node uses the raiser to build and raise a typed (non-boxing) event through the bound
/// <see cref="IEventPayloadProvider"/>.
/// </summary>
/// <remarks>
/// When the provider declares authored inputs, the matching <paramref name="inputResolvers"/> resolve them on demand as
/// the payload is built.
/// </remarks>
/// <param name="provider">The provider that builds the payload from the graph state.</param>
/// <param name="inputResolvers">The resolvers for the provider's declared inputs, keyed by input name. May be
/// <see langword="null"/> when the provider declares no inputs.</param>
public class EventPayloadResolver(
	IEventPayloadProvider provider,
	IReadOnlyDictionary<string, IPropertyResolver>? inputResolvers = null) : ObjectResolver<EventPayloadRaiser>
{
	private readonly EventPayloadRaiser _raiser = new(provider, inputResolvers);

	/// <inheritdoc/>
	public override EventPayloadRaiser Resolve(GraphContext graphContext)
	{
		return _raiser;
	}
}
