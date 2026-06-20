// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Events;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Statescript.Properties;

/// <summary>
/// Builds and raises a typed event through an <see cref="IEventPayloadProvider"/> with no boxing. Created by
/// <see cref="EventPayloadResolver"/> and used by <c>RaiseEventNode</c> to raise the provider's typed payload through
/// <see cref="EventManager.Raise{TPayload}(in EventData{TPayload})"/>.
/// </summary>
/// <param name="provider">The provider that builds the payload.</param>
/// <param name="inputResolvers">The resolvers for the provider's declared inputs, keyed by input name. May be
/// <see langword="null"/> when the provider declares no inputs.</param>
public sealed class EventPayloadRaiser(
	IEventPayloadProvider provider,
	IReadOnlyDictionary<string, IPropertyResolver>? inputResolvers)
{
	private static readonly Dictionary<string, IPropertyResolver> _noInputs = [];

	private readonly IEventPayloadProvider _provider = provider;
	private readonly IReadOnlyDictionary<string, IPropertyResolver> _inputResolvers = inputResolvers ?? _noInputs;

	/// <summary>
	/// Builds the payload from the current graph state and raises a typed event on <paramref name="manager"/>.
	/// </summary>
	/// <param name="manager">The event bus to raise on.</param>
	/// <param name="eventTags">The event's tag container.</param>
	/// <param name="source">The optional event source.</param>
	/// <param name="target">The event target.</param>
	/// <param name="magnitude">The event magnitude.</param>
	/// <param name="graphContext">The graph execution context the payload is built from.</param>
	public void Raise(
		EventManager manager,
		TagContainer eventTags,
		IForgeEntity? source,
		IForgeEntity? target,
		float magnitude,
		GraphContext graphContext)
	{
		_provider.Raise(manager, eventTags, source, target, magnitude, graphContext, _inputResolvers);
	}
}
