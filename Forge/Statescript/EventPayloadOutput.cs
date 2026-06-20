// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript;

/// <summary>
/// Declares an output that an <see cref="IEventPayloadProvider"/> exposes to the graph editor when decomposing a
/// received event payload. Each output is rendered as a graph-variable binding on the event-listener node, and the
/// provider writes its value through <see cref="EventPayloadOutputs"/> when an event fires.
/// </summary>
/// <param name="Name">The output name, used both as the editor label and as the key the provider writes with
/// <see cref="EventPayloadOutputs.Set{T}(string, T)"/>.</param>
/// <param name="ValueType">The value type written to the bound graph variable. Unmanaged types are written through the
/// scalar lane; reference types through the object lane.</param>
public sealed record EventPayloadOutput(string Name, Type ValueType);
