// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript;

/// <summary>
/// Declares an authored input that an <see cref="IEventPayloadProvider"/> exposes to the graph editor when building an
/// event payload. Each input is rendered as a nested resolver on the provider's <c>Payload</c> section of the
/// raise-event node, and the resolved value is handed to the provider through <see cref="EventPayloadInputs"/>.
/// </summary>
/// <param name="Name">The input name, used both as the editor label and as the key to read the resolved value with
/// <see cref="EventPayloadInputs.Get{T}(string)"/>.</param>
/// <param name="ValueType">The expected value type. The editor lists resolvers compatible with this type; values must
/// be supported by <see cref="Variant128"/> (numbers, vectors, planes, quaternions, and so on).</param>
public sealed record EventPayloadInput(string Name, Type ValueType);
