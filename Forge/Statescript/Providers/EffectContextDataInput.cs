// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Statescript.Providers;

/// <summary>
/// Declares an authored input that an <see cref="IEffectContextDataProvider"/> exposes to the graph editor. Each input
/// is rendered as a nested resolver on the provider's <c>Context Data</c> section, and the resolved value is handed to
/// the provider through <see cref="EffectContextDataInputs"/>.
/// </summary>
/// <param name="Name">The input name, used both as the editor label and as the key to read the resolved value with
/// <see cref="EffectContextDataInputs.Get{T}(string)"/>.</param>
/// <param name="ValueType">The expected value type. The editor lists resolvers compatible with this type; values must
/// be supported by <see cref="Variant128"/> (numbers, vectors, planes, quaternions, and so on).</param>
public sealed record EffectContextDataInput(string Name, Type ValueType);
