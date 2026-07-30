// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Providers;

/// <summary>
/// Declares one member of an event payload, serving both directions at once. On the raise side it renders as a nested
/// resolver on the raise-event node, and its resolved value reaches the provider through
/// <see cref="EventPayloadInputs"/>. On the listener side it renders as a graph-variable binding, which the provider
/// writes through <see cref="EventPayloadOutputs"/>.
/// </summary>
/// <param name="Name">The member name. It is the editor label on both nodes, the key to read the authored value with
/// <see cref="EventPayloadInputs.Get{T}(string)"/>, and the key to write the received value with
/// <see cref="EventPayloadOutputs.Set{T}(string, T)"/>.</param>
/// <param name="ValueType">The value type. Unmanaged types supported by <see cref="Variant128"/> travel the scalar
/// lane; reference types travel the object lane.</param>
public sealed record EventPayloadMember(string Name, Type ValueType);
